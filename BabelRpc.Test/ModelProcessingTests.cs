using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BabelRpc.Demo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace BabelRpc.Test
{
	[TestClass]
	public class JsonSerializerTest
	{
		public enum SomeEnum { Ok, Error, SomethingElse };
		
		[TestMethod]
		public void TestJokes()
		{
			var ser = new BabelJsonSerializer();
			List<Dictionary<string,List<Joke>>> res;

			string src = @"[
	{
	  ""Andrew"": [
		{
		  ""Question"": ""Q1"",
		  ""Answer"": ""A1"",
		  ""DateAdded"": ""2013-11-01T20:23:17.617Z""
		},
		{
		  ""Question"": ""Q2"",
		  ""Answer"": ""A2"",
		  ""DateAdded"": ""2013-11-01T16:23:17.617-04:00""
		}
	  ],
	  ""Craig"": [
		{
		  ""Question"": ""Q3"",
		  ""Answer"": ""A3"",
		  ""DateAdded"": ""2013-11-01T20:23:17.617Z""
		},
		{
		  ""Question"": ""Q4"",
		  ""Answer"": ""A4"",
		  ""DateAdded"": ""2013-11-01T16:23:17.617-04:00""
		}
	  ]
	},
	{
	  ""Viktor"": [
		{
		  ""Question"": ""Q5"",
		  ""Answer"": ""A5"",
		  ""DateAdded"": ""2013-11-01T20:23:17.617Z""
		},
		{
		  ""Question"": ""Q6"",
		  ""Answer"": ""A6"",
		  ""DateAdded"": ""2013-11-01T16:23:17.617-04:00""
		}
	  ],
	  ""John"": [
		{
		  ""Question"": ""Q7"",
		  ""Answer"": ""A7"",
		  ""DateAdded"": ""2013-11-01T20:23:17.617Z""
		},
		{
		  ""Question"": ""Q8"",
		  ""Answer"": ""A8"",
		  ""DateAdded"": ""2013-11-01T16:23:17.617-04:00""
		}
	  ]
	}
  ]";

			using(var strm2 = new MemoryStream(Encoding.UTF8.GetBytes(src)))
			{
				res = ser.Deserialize<List<Dictionary<string,List<Joke>>>>(strm2);
			}

			using(var ms = ser.Serialize(res))
			{
				string s2 = Encoding.UTF8.GetString(((MemoryStream)ms).ToArray());
				Assert.AreEqual(s2.Replace("\n\r", "").Replace(" ", ""), s2);
			}
		}
		
		private static Wholesome GetWholesome()
		{
			var wholesome = new Wholesome
			{
				MyBOOL = true,
				MyBYTE = 8,
				MyINT8 = -8,
				MyINT16 = 16,
				//MyINT32 = TestEnum.THREE,
				MyINT64 = 64,
				MyFLOAT32 = -30.32f,
				MyFLOAT64 = 64.64,
				MySTRING = Strings.STRING,
				MyCHAR = Strings.CHAR,
				MyDATETIME = DateTime.Now,	
				MyDECIMAL = 128.128M,
				MyBINARY = new byte[] { 65, 66, 67, 68, 69 },
				MyJOKE = new Joke() { Question = "", Answer = "Because.", DateAdded = DateTime.Now }
			};

			wholesome.MySTRING += "\\\\\r\n\t У попа была собака - он ее любил";
			wholesome.Names.Add("Homer");
			wholesome.Names.Add(null);
			wholesome.Names.Add("Bart");
			wholesome.Names.Add("Lisa");
			wholesome.Names.Add("Maggie");
			wholesome.Names.Add("Marge");
			wholesome.Jokes.Add(new Joke());
			wholesome.Pokes.Add("Jon", null);
			wholesome.Pokes.Add("Mike", new Joke() { Question = "Why not?", Answer = "Because I said so.", DateAdded = DateTime.Now });
			wholesome.Pokes.Add("Brian", new Joke() { Question = "Ok, but why?", Answer = "I have no idea.", DateAdded = DateTime.Now });

			wholesome.Argh.Add(
				new Dictionary<string, List<Joke>>()
				{
					{
						"Andrew", new List<Joke>()
						{
							new Joke() { Question = "Q1", Answer = "A1", DateAdded = DateTime.UtcNow },
							new Joke() { Question = "Q2", Answer = null, DateAdded = DateTime.Now },
						}
					},
					{
						"Craig", new List<Joke>()
						{
							new Joke() { Question = "Q3", Answer = "A3", DateAdded = DateTime.UtcNow },
							new Joke() { Question = "Q4", Answer = "A4", DateAdded = DateTime.Now },
						}
					}
				}
			);

			wholesome.Argh.Add(
				new Dictionary<string, List<Joke>>()
				{
					{
						"Viktor", new List<Joke>()
						{
							new Joke() { Question = "Q5", Answer = "A5", DateAdded = DateTime.UtcNow },
							new Joke() { Question = "Q6", Answer = "A6", DateAdded = DateTime.Now },
						}
					},
					{
						"John", new List<Joke>()
						{
							new Joke() { Question = "Q7", Answer = "A7", DateAdded = DateTime.UtcNow },
							new Joke() { Question = "Q8", Answer = "A8", DateAdded = DateTime.Now },
						}
					}
				}
			);

			return wholesome;
		}

		static int StringCompare(string s1, string s2)
		{
			if(s1 == null)
			{
				return s2 == null ? 0 : -1;
			}
			if(s2 == null) return -2;
			if (s1.Length != s2.Length) return -3;

			for(int i = 0; i < s1.Length; i++)
			{
				if(s1[i] != s2[i]) return i;
			}
			return 0;
		}

		[TestMethod]
		public void TestDeepCopy()
		{
			var data = GetWholesome();
			var newtonsoftSerializer = GetNewtonsoftSerializer();
			string nsJson1; 
			using(var ms = new MemoryStream())
			{
				using(var writer = new StreamWriter(ms, new UTF8Encoding(false), 1024, true))
				{
					newtonsoftSerializer.Serialize(writer, data);
					writer.Flush();
					byte[] arr = ms.ToArray();
					nsJson1 = Encoding.UTF8.GetString(arr);
				}
			}
			var clone = data.DeepCopy();
			data.MyBINARY[2] = 128;
			data.Names[1] = "Peter";
			data.Names.Add("John");
			data.Jokes[0].Question = "What was it agsain?";
			data.Pokes.Add("test", new Joke { Answer = "A", DateAdded = DateTime.Now, Question = "QQQ" });
			data.Pokes["Mike"].Answer = "aaaaa";
			string nsJson2;
			using(var ms = new MemoryStream())
			{
				using(var writer = new StreamWriter(ms, new UTF8Encoding(false), 1024, true))
				{
					newtonsoftSerializer.Serialize(writer, clone);
					writer.Flush();
					byte[] arr = ms.ToArray();
					nsJson2 = Encoding.UTF8.GetString(arr);
				}
			}
			//int i = StringCompare(nsJson1, nsJson2);
			Assert.AreEqual(nsJson1, nsJson2, "Clone should be the same");
		}

		[TestMethod]
		public void TestCompare()
		{
			var data = GetWholesome();
			var clone = data.DeepCopy();

			Assert.IsTrue(data.Compare(clone), "DeepCopy should produce identical data");
			byte b1 = data.MyBINARY[2];
			data.MyBINARY[2] = 128;
			string path;
			Assert.IsFalse(data.Compare(clone, out path), "byte[] compare");
			Assert.AreEqual("myBINARY[2]", path, "byte[] compare path");
			data.MyBINARY[2] = b1;
			data.Pokes.Add("test", new Joke { Answer = "A", DateAdded = DateTime.Now, Question = "QQQ" });
			Assert.IsFalse(data.Compare(clone, out path), "A dictionary with an extra element");
			Assert.AreEqual("Pokes", path, "A dictionary with an extra element: Path");
			data.Pokes.Remove("test");
			string answ = data.Pokes["Mike"].Answer;
			data.Pokes["Mike"].Answer = "aaaaa";
			Assert.IsFalse(data.Compare(clone, out path), "Property of an object in a dictionary");
			Assert.AreEqual("Pokes[Mike]/Answer", path, "Pokes should be different: property of the obhect in diectionary: Path ");
			data.Pokes["Mike"].Answer = answ;

			string q = data.Jokes[0].Question;
			data.Jokes[0].Question = "What was it agsain?";
			Assert.IsFalse(data.Compare(clone, out path), "Property of an object in a list");
			Assert.AreEqual("Jokes[0]/Question", path, "Pokes should be different: property of the obhect in list: Path");
			data.Jokes[0].Question = q;

			data.Jokes.Add(new Joke{Question = "q", Answer = "a"});

			Assert.IsFalse(data.Compare(clone, out path), "Length of a list");
			Assert.AreEqual("Jokes", path, "Length of a list: Path");
		}

		[TestMethod]
		public void TestJsonSerialization()
		{
			var data = GetWholesome();

			var ser = new BabelJsonSerializer();
			using (var ms = (MemoryStream)ser.Serialize(null))
			{
				byte[] arr = ms.ToArray();
				string s = Encoding.UTF8.GetString(arr);
				Assert.AreEqual("null", s, "Null test");
			}
			string babelJson;
			byte [] babelData;
			using(var ms = (MemoryStream)ser.Serialize(data))
			{
				babelData = ms.ToArray();
				babelJson = Encoding.UTF8.GetString(babelData);
			}

			/*Newtonsoft serializer doesn't produce the same result
			 * var newtonsoftSerializer = GetNewtonsoftSerializer();
			//serializer.Converters.Add(new ItinServices.Models.XmlSerializedBlobConverter());
			string nsJson;
			using (var ms = new MemoryStream())
			{
				using(var writer = new StreamWriter(ms, new UTF8Encoding(false), 1024, true))
				{
					newtonsoftSerializer.Serialize(writer, data);
					writer.Flush();
					byte[] arr = ms.ToArray();
					nsJson = Encoding.UTF8.GetString(arr);
					Assert.IsTrue(string.Compare(nsJson, babelJson, true) == 0, "Babel:     {0}\r\n NewtonSoft: <{1}>", nsJson, babelJson);
				}
			}*/

			using (var ms = new MemoryStream(babelData))
			{
				var result = ser.Deserialize<Wholesome>(ms);
				string compRes;
				Assert.IsTrue(result.Compare(data, out compRes), "Data before serialization don't match data after deserialization: " + compRes ?? "");  
			}
		}

		
		public class GetByIdRequest : BabelRpc.Mvc.IBabelRequest
		{
			/// <summary>
			/// The COMPANY_ID in the OUTTASK_COMPANY table in the OUTTASK database
			/// </summary>
			public int? CompanyId;

			#region IBabelRequest
			public void RunOnChildren<T>(BabelModelAction<T> method, T auxData, bool runOnAll = true)
			{
				if(runOnAll) CompanyId = (int?)method("companyId", typeof(int?), CompanyId, auxData);
			}

			public bool RunOnChild<T>(string name, BabelModelAction<T> method, T auxData)
			{
				switch(name)
				{
					case "companyId": CompanyId = (int?)method("companyId", typeof(int?), CompanyId, auxData); return true;
					default: return false;
				}
			}

			public void SetDefaults()
			{
			}
			#endregion
		}

		[TestMethod]
		public void TestJsonDeserializationFormat1()
		{
			const string JSON = @"{
  ""companyId"": 123
}";
			var ser = new BabelJsonSerializer();


			using(var ms = new MemoryStream(Encoding.UTF8.GetBytes(JSON)))
			{
				var result = ser.Deserialize<GetByIdRequest>(ms);
				Assert.AreEqual(123, result.CompanyId);
			}
		}

		[TestMethod]
		public void TestJsonNullCollectionsSerialization()
		{
			var data = GetWholesome();
			data.Jokes = null;
			data.Pokes = null; 
			var ser = new BabelJsonSerializer();
			
			string babelJson;
			byte[] babelData;
			using(var ms = (MemoryStream)ser.Serialize(data))
			{
				babelData = ms.ToArray();
				babelJson = Encoding.UTF8.GetString(babelData);
			}

			
			using(var ms = new MemoryStream(babelData))
			{
				var result = ser.Deserialize<Wholesome>(ms);
				Assert.IsTrue(ms.CanSeek, "Deserialization shouldn't close stream");
				Assert.IsNotNull(result.Jokes, "List should not be null");
				Assert.AreEqual(0, result.Jokes.Count, "List should be empty");

				Assert.IsNotNull(result.Pokes, "Dictionary should not be null");
				Assert.AreEqual(0, result.Pokes.Count, "Dictionary should be empty");
			}
		}

		[TestMethod]
		public void TestXmlSerialization()
		{
			var data = GetWholesome();
			var ser = new BabelXmlSerializer();
			using(var ms = (MemoryStream)ser.Serialize(null))
			{
				byte[] arr = ms.ToArray();
				Assert.AreEqual(0, arr.Length, "Null test");
			}

			string babelXml;
			byte[] babelData;
			using(var ms = (MemoryStream)ser.Serialize(data))
			{
				babelData = ms.ToArray();
				babelXml = Encoding.UTF8.GetString(babelData);
			}

			using(var ms = new MemoryStream(Encoding.UTF8.GetBytes(babelXml)))
			{
				var result = ser.Deserialize<Wholesome>(ms);
				string compRes;
				Assert.IsTrue(result.Compare(data, out compRes), "Data before serialization don't match data after deserialization: " + compRes ?? "");
			}
		}

	
		/*class BabelDecimalAndLongConverter : JsonConverter
		{
			public BabelDecimalAndLongConverter()
			{
			}

			public override bool CanConvert(System.Type objectType)
			{
				return objectType == typeof(long) || objectType == typeof(decimal) || objectType == typeof(ulong) ||
					   objectType == typeof(long?) || objectType == typeof(decimal?) || objectType == typeof(ulong?);
			}

			static Exception GetException(JsonReader reader, string message, Exception inner = null)
			{
				string extra = "";
				var lineInfo = reader as IJsonLineInfo;
				if((lineInfo != null) && lineInfo.HasLineInfo())
				{
					extra = string.Format(", line {0}, position {1}", lineInfo.LineNumber, lineInfo.LinePosition);
				}

				return new JsonSerializationException(string.Format("{0} Path '{1}'{2}.", message, reader.Path, extra), inner);
			}

			public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue, JsonSerializer serializer)
			{
				if(reader.TokenType == JsonToken.Null)
				{
					bool isNullable = IsNullable(objectType);
					if(!isNullable)
					{
						throw GetException(reader, string.Format("Cannot convert null value to {0}.", objectType.Name));
					}
					return null;
				}
				switch(reader.TokenType)
				{
					case JsonToken.String:
						string str = reader.Value.ToString();
						if(objectType == typeof(decimal) || objectType == typeof(decimal?))
						{
							decimal res;
							if(!decimal.TryParse(str, out res))
							{
								throw GetException(reader, string.Format("Cannot convert {0} value to decimal.", str));
							}
							return res;
						}
						if(objectType == typeof(long) || objectType == typeof(long?))
						{
							long res;
							if(!long.TryParse(str, out res))
							{
								throw GetException(reader, string.Format("Cannot convert {0} value to long.", str));
							}
							return res;
						}
						if(objectType == typeof(ulong) || objectType == typeof(ulong?))
						{
							ulong res;
							if(!ulong.TryParse(str, out res))
							{
								throw GetException(reader, string.Format("Cannot convert {0} value to ulong.", str));
							}
							return res;
						}
						throw GetException(reader, string.Format("Unsupported object type {0}.", objectType.Name));
					case JsonToken.Float:
					case JsonToken.Integer:
						try
						{
							bool isNullable = IsNullable(objectType);
							if(isNullable) objectType = Nullable.GetUnderlyingType(objectType);
							return Convert.ChangeType(reader.Value, objectType);
						}
						catch(Exception err)
						{
							throw GetException(reader, string.Format("Cannot convert {0} value to {1}.", reader.Value, objectType.Name), err);
						}
					default: throw GetException(reader, string.Format("Unexpected token type {0}.", reader.TokenType));
				}
			}

			private static bool IsNullable(System.Type objectType)
			{
				return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>);
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				if(value == null)
				{
					writer.WriteNull();
				}
				else
				{
					writer.WriteValue(value.ToString());
				}
			}
		}*/
		private static Newtonsoft.Json.JsonSerializer GetNewtonsoftSerializer()
		{
			var newtonsoftSerializer = new Newtonsoft.Json.JsonSerializer() {
				TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None,
				NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
				TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
				//Binder = new TypeNameSerializationBinder(defaultNamespace) 
			};
			/*newtonsoftSerializer.Converters.Add(new Newtonsoft.Json.Converters.IsoDateTimeConverter() { DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFK" });
			newtonsoftSerializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter { CamelCaseText = false });
			newtonsoftSerializer.Converters.Add(new BabelDecimalAndLongConverter());*/
			return newtonsoftSerializer;
		}

		private void SpeedTest(object data, Action<object, BabelJsonSerializer> babelSerialization, Action<object, Newtonsoft.Json.JsonSerializer> nkSerialization)
		{
			var ser = new BabelJsonSerializer();

			babelSerialization(data, ser); 

			var newtonsoftSerializer = GetNewtonsoftSerializer();

			nkSerialization(data, newtonsoftSerializer);

			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			const int NUM_RUN = 100000;
			for(int i = 0; i < NUM_RUN; i++)
			{
				babelSerialization(data, ser); 
			}
			sw.Stop();
			long t1 = sw.ElapsedMilliseconds;
			sw.Reset();
			sw.Start();
			for(int i = 0; i < NUM_RUN; i++)
			{
				nkSerialization(data, newtonsoftSerializer);
			}
			sw.Stop();
			long tNK1 = sw.ElapsedMilliseconds;
			sw.Reset();
			sw.Start();
			for(int i = 0; i < NUM_RUN; i++)
			{
				babelSerialization(data, ser); 
			}
			sw.Stop();
			long t2 = sw.ElapsedMilliseconds;
			sw.Reset();
			sw.Start();
			for(int i = 0; i < NUM_RUN; i++)
			{
				nkSerialization(data, newtonsoftSerializer);
			}
			sw.Stop();
			long tNK2 = sw.ElapsedMilliseconds;

			Assert.Fail(string.Format("BabelJSONSerializer: {0}\t\t {1}\r\nNewtonsoft: {2}\t\t {3}", ((double)t1) / NUM_RUN, ((double)t2) / NUM_RUN, ((double)tNK1) / NUM_RUN, ((double)tNK2) / NUM_RUN));

		}

		//[TestMethod]
		public void TestSerializationSpeed()
		{
			var data = GetWholesome();
			SpeedTest(data, (d, ser) => { ser.Serialize(d).Dispose(); }, (d, newtonsoftSerializer) => {
					using(var ms = new MemoryStream())
					{
						using(var writer = new StreamWriter(ms, new UTF8Encoding(false), 1024, true))
						{
							newtonsoftSerializer.Serialize(writer, d);
						}
					}
				});
		}

		//[TestMethod]
		public void TestDeserializationSpeed()
		{
			var data = GetWholesome();
			byte[] bin;
			var ser = new BabelJsonSerializer();
			using(var ms = (MemoryStream)ser.Serialize(data))
			{
				bin = ms.ToArray();
			}

			SpeedTest(bin, (b, s) => 
				{
					using(var ms = new MemoryStream((byte[]) bin))
					{
						var result = s.Deserialize<Wholesome>(ms);
					}
				}, (b, newtonsoftSerializer) => 
				{
					using(var ms = new MemoryStream((byte[])b))
					{
						using(var stringReader = new System.IO.StreamReader(ms))
						{
							using(var jsonReader = new Newtonsoft.Json.JsonTextReader(stringReader))
							{
								var res = newtonsoftSerializer.Deserialize<Wholesome>(jsonReader);
							}
						}
					}
				});
		}

		[TestMethod]
		public void TestValidation()
		{
			var data = GetWholesome();
			var res = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

			Assert.IsFalse(data.Validate(out res));
			Assert.AreEqual(3, res.Count);

			try
			{
				data.Validate();
				Assert.Fail("ValidateThrow should throw BabelValidionException"); 
			}
			catch(BabelValidionException ve)
			{
				Assert.AreEqual(3, ve.Errors.Count, "BabelValidionException should contain 3 errors");
			}

			data.MyNULL2 = 5;
			Assert.IsFalse(data.Validate(out res));
			Assert.AreEqual(2, res.Count);

			data.Jokes[0].Answer = "Some Answer";
			Assert.IsFalse(data.Validate(out res));
			Assert.AreEqual(1, res.Count);

			data.Argh[0]["Andrew"][1].Answer = "Another one";
			Assert.IsTrue(data.Validate(out res));
			Assert.AreEqual(0, res.Count);
		}

		[TestMethod]
		public void TestNonCharacterData()
		{
			var ser = new BabelJsonSerializer();

			//successful deserialization
			var bytes = Encoding.UTF8.GetBytes(GetWholesome().ToString());
			Wholesome result;
			using(var stream = new MemoryStream(bytes))
			{
				result = ser.Deserialize<Wholesome>(stream);
			}
			Assert.IsNotNull(result);

			//unsuccessful deserialization (client used wrong encoding)
			var whStr = new Wholesome { MySTRING = "\u0402\u0404" }.ToString();
			bytes = Encoding.Unicode.GetBytes(whStr);
			try
			{
				using(var stream = new MemoryStream(bytes))
				{
					result = ser.Deserialize<Wholesome>(stream);
				}
				Assert.Fail("Did not throw expected JsonSerializationError");
			}
			catch(JsonSerializationError)
			{
				//expected
			}

			//unsuccessful deserialization using garbage bytes
			bytes = new byte[] { 14, 0, 0, 0, 16, 118, 97, 108, 0, 17, 0, 0, 0, 0 };

			try
			{
				using(var stream = new MemoryStream(bytes))
				{
					result = ser.Deserialize<Wholesome>(stream);
				}
				Assert.Fail("Did not throw expected JsonSerializationError");
			}
			catch(JsonSerializationError)
			{
				//expected
			}
		}
	}
}

