using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Concur.Babel
{
	/// <summary>
	/// XML serializer impementing IBabelSerializer. Thread safe.
	/// </summary>
	public class BabelXmlSerializer : IBabelSerializer
	{
		public BabelXmlSerializer()
		{
		}

		const string ARRAY_ELEMENT = "Item";
		const string DICTIONARY_ELEMENT = "Value";
		const string DICTIONARY_ATTR = "key";

		const string NIL_ATTR = "nil";
		const string NIL_ATTR_NS = "http://www.w3.org/2001/XMLSchema-instance";

		#region Deserialization
		public T Deserialize<T>(System.IO.Stream str)
		{
			T res;
			using(var xr = new XmlTextReader(str))
			{
				xr.MoveToContent();
				res = (T) ReadValue(xr, typeof(T));
			}

			return res;
		}

	
		static object ReadValue(XmlTextReader xr, Type expectedType)
		{
			//We should be positioned on the value element already 
			if(expectedType.IsGenericType)
			{
				Type gType = expectedType.GetGenericTypeDefinition();
				var gArgs = expectedType.GetGenericArguments();
				if(gType == typeof(List<>))
				{
					if(gArgs.Length != 1) throw new ApplicationException("List supposed to have 1 generic argument");
					Type elType = gArgs[0];
					var list = (IList)Activator.CreateInstance(expectedType);
					if(xr.IsEmptyElement) return list;
					while(xr.Read())
					{
						switch(xr.NodeType)
						{
							case XmlNodeType.Element:
								if (xr.Name != ARRAY_ELEMENT) throw new XmlSerializationError(string.Format("Invalid array element {0}", xr.Name), xr);
								list.Add(ReadValue(xr, elType));
								break;
							case XmlNodeType.Comment:
								break;
							case XmlNodeType.EndElement:
								return list;
							default: throw new XmlSerializationError(string.Format("Unexpected array node type: {0}" , xr.NodeType), xr); 
						}
					}
					throw new XmlSerializationError("Unexpected document end", xr);
				}
				else if(gType == typeof(Dictionary<,>))
				{
					if(gArgs.Length != 2) throw new ApplicationException("Dictionary supposed to have 1 generic argument");
					Type keyType = gArgs[0];
					Type valType = gArgs[1];
					var dict = (IDictionary)Activator.CreateInstance(expectedType);
					if(xr.IsEmptyElement) return dict;
					while(xr.Read())
					{
						switch(xr.NodeType)
						{
							case XmlNodeType.Element:
								bool forceNull = false;
								if (xr.Name != DICTIONARY_ELEMENT) throw new XmlSerializationError(string.Format("Invalid dictionary element {0}", xr.Name), xr);
								if(xr.MoveToAttribute(NIL_ATTR, NIL_ATTR_NS)) 
								{
									forceNull = (xr.ReadContentAsString().ToLowerInvariant() == "true");
								}

								if(!xr.MoveToAttribute(DICTIONARY_ATTR)) throw new XmlSerializationError("Invalid dictionary format - no attribute", xr);
								string keyS = xr.ReadContentAsString();
								object key = ObjectExtensions.ConvertString(keyType, keyS);

								xr.MoveToElement();
								object val = (forceNull) ? null : ReadValue(xr, valType);
								dict.Add(key, val);
								break;
							case XmlNodeType.Comment:
								break;
							case XmlNodeType.EndElement:
								return dict;
							default: throw new XmlSerializationError(string.Format("Unexpected array node type: {0}" , xr.NodeType), xr); 
						}
					}
					throw new XmlSerializationError("Unexpected document end", xr);
				}
				else if(gType == typeof(Nullable<>))
				{
					if(gArgs.Length != 1) throw new ApplicationException("Nullable supposed to have 1 generic argument");
					Type elType = gArgs[0];
					string s = xr.ReadString();
					return ObjectExtensions.ConvertString(elType, s);
				}
				else
				{
					throw new ApplicationException("Unsupported generic type: " + gType.Name);
				}
			}
			else 
			{
				//Non-generic type
					if (typeof(IBabelModel).IsAssignableFrom(expectedType))
				{
					var model = (IBabelModel) Activator.CreateInstance(expectedType);
					if(xr.IsEmptyElement) return model;
					xr.Read();
					while(!xr.EOF)
					{
						switch(xr.NodeType)
						{
							case XmlNodeType.Element:
								if(!model.RunOnChild<XmlTextReader>(xr.Name, SetProperty, xr))
								{
									xr.ReadInnerXml(); //Skip all the content
								}
								else
								{
									xr.Read();
								}
								break;
							case XmlNodeType.EndElement:
								return model;
							case XmlNodeType.XmlDeclaration:
							case XmlNodeType.Comment:
							case XmlNodeType.Whitespace:
								xr.Read();
								break; //Do nothing
							default: throw new XmlSerializationError(string.Format("Unexpected array node type: {0}" , xr.NodeType), xr); 
						}
					}
					throw new XmlSerializationError("Unexpected document end", xr);
				}
				else 
				{
					if(xr.MoveToAttribute(NIL_ATTR, NIL_ATTR_NS))
					{
						if(xr.ReadContentAsString().ToLowerInvariant() == "true") return null;
					}
					string s = xr.ReadString();
					return ObjectExtensions.ConvertString(expectedType, s);
				}
			}
		}

		static object SetProperty(string name, Type itemType, object itemData, XmlTextReader xr)
		{
			return ReadValue(xr, itemType);
		}
		#endregion

		#region Serialization
		public System.IO.Stream Serialize(object data)
		{
			var ms = new MemoryStream();
			
			var encoding = new System.Text.UTF8Encoding(false);
			var settings = new XmlWriterSettings {
				Encoding = encoding
			};

			if (data != null)
			{
				using(var writer = XmlWriter.Create(ms, settings))
				{
					Type tp = data.GetType();
					writer.WriteStartElement(tp.Name);

					//It is better to write it once then recursively search everything for the situations (dictionary and arrays with null elements) when we need xsi:nil
					writer.WriteAttributeString("xmlns", "xsi", null, NIL_ATTR_NS); 

					WriteValue(writer, tp, data);
					writer.WriteEndElement();
				}
			}
			return ms;
		}
		
		
		static string ConvertToXmlString(Type dataType, object data)
		{
			if(dataType == typeof(object))
			{
				dataType = data.GetType();
			}

			if (dataType == typeof(DateTime))
			{
				return ((DateTime) data).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFK"); 
			}

			if (dataType == typeof(bool))
			{
				return ((bool) data) ? "true" : "false"; 
			}

			if(dataType == typeof(byte[]))
			{
				return Convert.ToBase64String((byte[])data);
			}

			return data.ToString();
		}

	

		static void WriteValue(XmlWriter wr, Type dataType, object data)
		{
			// Shouldn't be called if null if(data == null) return;
			if(dataType.IsGenericType)
			{
				Type gType = dataType.GetGenericTypeDefinition();
				var gArgs = dataType.GetGenericArguments();
				if(gType == typeof(List<>))
				{
					if(gArgs.Length != 1) throw new ApplicationException("List supposed to have 1 generic argument");
					Type elType = gArgs[0];
					var list = (IList)data;
					foreach(object item in list)
					{
						wr.WriteStartElement(ARRAY_ELEMENT);
						if(item == null)
						{
							wr.WriteAttributeString(NIL_ATTR, NIL_ATTR_NS, "true");
						}
						else
						{
							WriteValue(wr, elType, item);
						}
						wr.WriteEndElement();
					}
					
				}
				else if(gType == typeof(Dictionary<,>))
				{
					if(gArgs.Length != 2) throw new ApplicationException("Dictionary supposed to have 1 generic argument");
					Type keyType = gArgs[0];
					Type valType = gArgs[1];
					var dict = (IDictionary)data;
					
					foreach(DictionaryEntry kv in dict)
					{
						wr.WriteStartElement(DICTIONARY_ELEMENT);
						wr.WriteAttributeString(DICTIONARY_ATTR, ConvertToXmlString(keyType, kv.Key));
						if(kv.Value == null)
						{
							wr.WriteAttributeString(NIL_ATTR, NIL_ATTR_NS, "true");
						}
						else
						{
							WriteValue(wr, valType, kv.Value);
						}
						wr.WriteEndElement();
					}
				}
				else if(gType == typeof(Nullable<>))
				{
					if(gArgs.Length != 1) throw new ApplicationException("Nullable supposed to have 1 generic argument");
					Type elType = gArgs[0];
					wr.WriteString(ConvertToXmlString(elType, data));
				}
				else
				{
					throw new ApplicationException("Unsupported type: " + gType.Name);
				}
			}
			else
			{
				var model = data as IBabelModel;
				if(model != null)
				{
					model.RunOnChildren<XmlWriter>(WriteProperty, wr, true);
				}
				else
				{
					wr.WriteString(ConvertToXmlString(dataType, data));
				}
			}
		}

		static object WriteProperty(string name, Type itemType, object itemData, XmlWriter wr)
		{
			if (itemData != null)
			{
				wr.WriteStartElement(name);
				WriteValue(wr, itemType, itemData);
				wr.WriteEndElement();
			}
			return itemData;
		}

		#endregion
	}

	

	

	public class XmlSerializationError : BabelApplicationException
	{
		public XmlSerializationError(string what, XmlTextReader xr, Exception innerError = null)
			: base(string.Format("{0} at line {1} position {2}", what, xr == null ? "?" : xr.LineNumber.ToString(), xr == null ? "?" : xr.LinePosition.ToString()), innerError)
		{ 
			//this.Data
			//this.Context
			var err = new Error() { Code = "INVALID_XML", Message = this.Message};
			err.Params.Add(what);
			err.Params.Add(xr == null ? "?" : xr.LineNumber.ToString());
			err.Params.Add(xr == null ? "?" : xr.LinePosition.ToString());
			this.Errors.Add(err); 
		}
		
	}
}
