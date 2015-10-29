using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BabelRpc
{
	public sealed class JsonSerializationError : BabelApplicationException
	{
		internal JsonSerializationError(string what, StreamReader sr, char c, Exception innerError = null)
			: base(string.Format("{0} '{1}'({2}) at position {3}", what, c, (int)c, sr == null ? "?" : sr.BaseStream.Position.ToString()), innerError)
		{ 
			//this.Data
			//this.Context
			var err = new Error { Code = "INVALID_JSON", Message = this.Message};
			err.Params.Add(what);
			err.Params.Add(sr == null ? "?" : sr.BaseStream.Position.ToString());
			err.Params.Add(c.ToString());
			this.Errors.Add(err); 
		}
		
	}

	/// <summary>
	/// [De]serializes Babel model from/to Babel JSON format. Thread safe 
	/// </summary>
	public sealed class BabelJsonSerializer : IBabelSerializer
	{
		#region Write support
		private static void WriteString(TextWriter sw, string val)
		{
			sw.Write("\""); 
			foreach(char c in val)
			{
				switch(c)
				{
					case '\\': sw.Write("\\\\"); break;
					case '"': sw.Write("\\\""); break;
					case '/': sw.Write("\\/"); break;
					case '\b': sw.Write("\\b"); break;
					case '\f': sw.Write("\\f"); break;
					case '\n': sw.Write("\\n"); break;
					case '\r': sw.Write("\\r"); break;
					case '\t': sw.Write("\\t"); break;
					default: sw.Write(c); break;
				}
			}
			sw.Write("\""); 
		}

		class WritePropertyData
		{
			public WritePropertyData(StreamWriter wr) { Writer = wr; }
			public readonly StreamWriter Writer;
			public bool WriteDelimiter;
		}
		object WriteAction(string name, Type itemType, object itemData, WritePropertyData wr)
		{
			if(itemData == null) return null;

			if(wr.WriteDelimiter) wr.Writer.Write(',');
			wr.WriteDelimiter = true;

			WriteString(wr.Writer, name);
			wr.Writer.Write(':');
			WriteData(itemType, itemData, wr.Writer);
			return itemData;
		}

		private void WriteData(Type itemType, object itemData, StreamWriter sw)
		{
			if(itemData == null) return;
			if(itemType == typeof(object))
			{
				itemType = itemData.GetType();
			}
			if(itemType.IsGenericType)
			{
				Type gType = itemType.GetGenericTypeDefinition();
				var gArgs = itemType.GetGenericArguments();
				if(gType == typeof(List<>))
				{
					if(gArgs.Length != 1) throw new ApplicationException("List supposed to have 1 generic argument");
					Type elType = gArgs[0];
					var list = (IList)itemData;
					string sep = "";
					sw.Write('[');
					foreach(object item in list)
					{
						sw.Write(sep);
						sep = ",";
						if(item == null)
						{
							sw.Write("null");
						}
						else
						{
							WriteData(elType, item, sw);
						}
					}
					sw.Write(']');
				}
				else if(gType == typeof(Dictionary<,>))
				{
					if(gArgs.Length != 2) throw new ApplicationException("Dictionary supposed to have 1 generic argument");
					//Type keyType = gArgs[0];
					Type valType = gArgs[1];
					var dict = (IDictionary)itemData;
					string sep = "";
					sw.Write('{');
					foreach(DictionaryEntry kv in dict)
					{
						sw.Write(sep);
						sep = ",";
						if(kv.Key == null)
						{
							sw.Write("\"\"");
						}
						else
						{
							WriteString(sw, kv.Key.ToString());
						}
						sw.Write(':');
						if(kv.Value == null)
						{
							sw.Write("null");
						}
						else
						{
							WriteData(valType, kv.Value, sw);
						}
					}
					sw.Write('}');
				}
				else if(gType == typeof(Nullable<>))
				{
					if(gArgs.Length != 1) throw new ApplicationException("Nullable supposed to have 1 generic argument");
					Type elType = gArgs[0];
					WriteSimple(elType, itemData, sw);
				}
				else
				{
					throw new ApplicationException("Unsupported type: " + gType.Name);
				}
			}
			else
			{
				var bm = itemData as IBabelModel;
				if(bm != null)
				{
					sw.Write('{');
					var wr = new WritePropertyData(sw);
					bm.RunOnChildren<WritePropertyData>(WriteAction, wr);
					sw.Write('}');
				}
				else
				{
					WriteSimple(itemType, itemData, sw);
				}
			}
		}

		private static void WriteSimple(Type itemType, object itemData, StreamWriter sw)
		{
			if(itemType == typeof(object))
			{
				itemType = itemData.GetType();
			}

			if (itemType == typeof(string))
			{
				WriteString(sw, (string) itemData);
			}
			else if(itemType == typeof(DateTime))
			{
				sw.Write('"');
				sw.Write(((DateTime)itemData).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffK"));
				sw.Write('"');
			}
			else if(itemType == typeof(long) || itemType == typeof(ulong) || itemType == typeof(Decimal) || itemType.IsEnum || itemType == typeof(char))
			{
				sw.Write('"');
				sw.Write(itemData.ToString());
				sw.Write('"');
			}
			else if(itemType == typeof(bool))
			{
				sw.Write(((bool)itemData) ? "true" : "false");
			}
			else if(itemType.IsValueType)
			{
				sw.Write(itemData.ToString());
			}
			else if(itemType == typeof(byte[]))
			{
				byte[] bin = (byte[])itemData;
				sw.Write('"');
				sw.Write(Convert.ToBase64String(bin));
				sw.Write('"');
			}
			else
			{
				throw new NotSupportedException(string.Format("Type {0} is not supported", itemType.Name));
			}
		}
		#endregion

		#region Read support
		private object ReadArray(StreamReader sr, Type expectedType)
		{
			if (!expectedType.IsGenericType || expectedType.GetGenericTypeDefinition() != typeof(List<>))
			{
				throw new JsonSerializationError(string.Format("Invalid array type {0}", expectedType.Name), sr, ' ');
			}
			
			var gArgs = expectedType.GetGenericArguments();
			if(gArgs.Length != 1) throw new ApplicationException("List supposed to have 1 generic argument");
			Type itemType = gArgs[0];
			
			var listType = typeof(List<>).MakeGenericType(itemType);
			var res = (IList)Activator.CreateInstance(listType);
									
			object val;
			int ic;
			if(ReadValue(sr, itemType, out ic, out val))
			{
				res.Add(val);
			}
			else 
			{
				//If there is no value only valid last char is ']'
				if (((char) ic) != ']') throw new JsonSerializationError("Empty item in array", sr, ' ');
			}

			while(ic != -1)
			{
				char c = (char)ic;
				
				switch(c)
				{
					case ',':
						if(ReadValue(sr, itemType, out ic, out val))
						{
							res.Add(val);
							break;
						}
						else
						{
							throw new JsonSerializationError("Empty item in array", sr, ' ');
						}
					case ']':
						return res;
					case ' ':
					case '\t':
					case '\n':
					case '\r':
						//Just skip those
						ic = sr.Read();
						break;
					default:
						throw new JsonSerializationError("Invalid array definition", sr, ' ');
				}
			}
			throw new JsonSerializationError("Unexpected end of array", null, ' ');
		}

		
		class SetPropertyData
		{
			public SetPropertyData(StreamReader rd) 
			{ 
				Reader = rd; 
				NextCharCode = -1; 
			}

			public StreamReader Reader;
			public int NextCharCode;
		}

		private object SetProperty(string name, Type itemType, object itemData, SetPropertyData propData)
		{
			object val;
			if (!ReadValue(propData.Reader, itemType, out propData.NextCharCode, out val))
			{
				throw new JsonSerializationError(string.Format("Can't read {0} property value", name), propData.Reader, ' '); 
			}
			return val;
		}

		private object ReadObject(StreamReader sr, Type expectedType)
		{
			Type keyType; 
			Type valType;
			IDictionary dictRes = null;
			IBabelModel modelRes = null;
			if (expectedType.IsGenericType && expectedType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
			{
				var gArgs = expectedType.GetGenericArguments();
				if (gArgs.Length != 2) throw new ApplicationException("Dictionary supposed to have 2 generic arguments");
				keyType = gArgs[0];
				valType = gArgs[1];
				var dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valType);
				dictRes = (IDictionary) Activator.CreateInstance(dictType);
			}
			else 
			{
				keyType = typeof(string);
				valType = null;
				modelRes = Activator.CreateInstance(expectedType) as IBabelModel;
				if (modelRes == null) throw new JsonSerializationError("Object is not IBabelModel", sr, ' ');
			}

			int ic;
			object key;
			if (!ReadValue(sr, keyType, out ic, out key))
			{
				if (((char) ic) != '}') throw new JsonSerializationError("Invalid object key definition", sr, ' ');
			}

			while(ic != -1)
			{
				char c = (char)ic;
				switch(c)
				{
					case ':':
						if (key == null) throw new JsonSerializationError("Object value has no key", sr, ' ');
						if (modelRes != null)
						{
							var pd = new SetPropertyData(sr);
							string keys = key.ToString();
							if(!modelRes.RunOnChild<SetPropertyData>(keys, SetProperty, pd))
							{
								System.Diagnostics.Debug.WriteLine("BabelJsonSerializer: property {0} was not found", key);

								SetProperty(keys, typeof(object), null, pd); 
							}
							ic = pd.NextCharCode;
						}
						else 
						{
							object val;
							if (!ReadValue(sr, valType, out ic, out val))
							{
								throw new JsonSerializationError(string.Format("Can't read {0} key value", key), sr, ' '); 
							}
							dictRes[key]= val;
						}
						break;
					case ',':
						if (!ReadValue(sr, keyType, out ic, out key))
						{
							throw new JsonSerializationError("Invalid object definition", sr, ' ');
						}
						break;
					case '}':
						return (object)modelRes ?? dictRes;
					case ' ':
					case '\t':
					case '\n':
					case '\r':
						//Just skip those
						ic = sr.Read();
						break;
					default:
						throw new JsonSerializationError("Invalid object format", sr, c);
				}
			}
			throw new JsonSerializationError("Unexpected end of the object", sr, ' ');
		}

		


		/// <summary>
		/// Reads string, number, object, array, bool and null. Eats leading and trailing whitespace
		/// </summary>
		/// <param name="sr"></param>
		/// <param name="expectedType"></param>
		/// <param name="ic"></param>
		/// <param name="val"></param>
		/// <returns></returns>
		private bool ReadValue(StreamReader sr, Type expectedType, out int ic, out object val)
		{
			ic = sr.Read();
			while(ic != -1)
			{
				char c = (char)ic;
				switch(c)
				{
					case '"':
						string s = ReadString(sr);

						//if the expectedType is object we can't find this object it the result model, so we just need to read the JSON throw result away 
						if(expectedType == typeof(object) || expectedType == typeof(string))
						{
							val = s;
						}
						else
						{
							try
							{
								val = ObjectExtensions.ConvertString(expectedType, s);
							}
							catch(Exception err)
							{
								throw new JsonSerializationError("Conversion error", sr, ' ', err);
							}
						}
						ic = SkipWhitespace(sr);
						return true;
					case '[':
						if(expectedType == typeof(object))
						{
							expectedType = typeof(List<object>);
						}

						val = ReadArray(sr, expectedType);
						ic = SkipWhitespace(sr);
						return true; 
					case '{':

						if(expectedType == typeof(object))
						{
							expectedType = typeof(Dictionary<string, object>);
						}

						val = ReadObject(sr, expectedType);
						ic = SkipWhitespace(sr);
						return true; 
					case ' ':
					case '\t':
					case '\n':
					case '\r':
						//Just skip those
						ic = sr.Read();
						break;
					default:
						//This should handle 
						//	null
						//	bool
						//	numerics
						string lit = ReadLiteral(sr, ref ic);
						if(lit.Length == 0)
						{
							val = null; //Nothing except whitespace
							return false;
						}
						if(lit == "null")
						{
							val = null; //Null literal
							return true;
						}
						try
						{
							val = ObjectExtensions.ConvertString(expectedType, lit);
							return true;
						}
						catch(Exception err)
						{
							throw new JsonSerializationError("Conversion error", sr, ' ', err);
						}
				}
			}
			val = null;
			return false;
		}

		private static string ReadString(StreamReader sr)
		{
			int ic = sr.Read();
			var currValue = new List<char>(32);
			bool isEscape = false;
			while (ic != -1)
			{
				char c = (char) ic;
				if (isEscape)
				{
					switch(c)
					{ 
						case '\\':
							currValue.Add('\\');
							break;
						case '"':
							currValue.Add('"');
							break;
						case '/':
							currValue.Add('/');
							break;
						case 'b':
							currValue.Add('\b');
							break;
						case 'f':
							currValue.Add('\f');
							break;
						case 'n':
							currValue.Add('\n');
							break;
						case 'r':
							currValue.Add('\r');
							break;
						case 't':
							currValue.Add('\t');
							break;
						case '\'':
							currValue.Add('\'');
							break;
						case 'u':
							int unicodeChar = 0;
							for (int shift = 12; shift >=0; shift -= 4)
							{
								ic = sr.Read();
								if (ic == -1) throw new JsonSerializationError("Incomplete Unicode character sequence", sr, ' ');
								unicodeChar += HexToInt((char) ic, sr) << shift;
							}
							currValue.Add((char)unicodeChar);
							break;
						default:
							throw new JsonSerializationError("Invalid escape sequence", sr, c);
					}
					isEscape = false;
				}
				else 
				{
					switch(c)
					{
						case '\\':
							isEscape = true;
							break;
						case '"':
							return new string(currValue.ToArray());
						default:
							currValue.Add(c);
							break;
					}
				}

				ic = sr.Read();
			}
			throw new JsonSerializationError("Unterminated string", sr, ' ');
		}

		private static string ReadLiteral(StreamReader sr, ref int ic)
		{ 
			var currValue = new List<char>(32);
			char c = (char)ic;
			while(ic >= 0 && c != ' ' && c != '\t' && c != '\n' && c != '\r' && c != ']' && c != '}' && c != ','&& c != ':') //Read all till the first whitespace all special character or stream end
			{
				currValue.Add(c);
				ic = sr.Read();
				c = (char)ic;
			}
			return new string(currValue.ToArray());
		}

		private static int SkipWhitespace(StreamReader sr)
		{
			int ic = sr.Read();
			char c = (char)ic;
			while(c == ' ' || c == '\t' || c == '\n' || c == '\r')
			{
				ic = sr.Read();
				c = (char)ic;
			}
			return ic;
		}

		private static int HexToInt(char c, StreamReader sr)
		{
			if(c >= '0' && c <= '9') return (int)c - (int)'0';
			if(c >= 'A' && c <= 'F') return (int)c - 55;
			if(c >= 'a' && c <= 'f') return (int)c - 87;
			throw new JsonSerializationError("Invalid hex symbol", sr, c);
		}
		#endregion		

		public T Deserialize<T>(Stream input) 
		{
			using(var sr = new StreamReader(input, new UTF8Encoding(false), false, 1024, true))
			{
				object val;
				int ic;
				bool hasVal = ReadValue(sr, typeof(T), out ic, out val);

				ic = SkipWhitespace(sr);
				if(ic != -1)
				{
					throw new JsonSerializationError("Extra characters after end of document", sr, (char)ic);
				}
				return hasVal ? (T)val : default(T);
			}
		}

		static readonly byte[] s_null = Encoding.ASCII.GetBytes("null");

		public System.IO.Stream Serialize(object data)
		{
			var ms = new MemoryStream();
			if (data == null) 
			{
				ms.Write(s_null, 0, s_null.Length);
				return ms;
			}

			using(var writer = new StreamWriter(ms, new UTF8Encoding(false), 1024, true))
			{
				WriteData(data.GetType(), data, writer);
				writer.Flush();
				//var bytes = ms.ToArray();
				ms.Position = 0;
				return ms;
			}
		}
	}
}
