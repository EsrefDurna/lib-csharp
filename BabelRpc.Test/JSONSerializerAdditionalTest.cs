using System;
using System.IO;
using System.Text;
using System.Collections.Generic;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BabelRpc.Test
{
	public class TestRequest : IBabelModel
	{
		/// <summary>
		/// Default constructor
		/// </summary>
		public TestRequest()
		{
			ListOfStrings = new List<string>();
		}

		public string Target { get; set; }

		public int? Count { get; set; }

		public List<string> ListOfStrings { get; set; }

		public override string ToString()
		{
			var ser = new BabelJsonSerializer();
			using (var strm = (System.IO.MemoryStream)ser.Serialize(this))
			{
				return (new UTF8Encoding(false)).GetString(strm.ToArray());
			}
		}
		#region IBabelModel
		public virtual void RunOnChildren<T>(BabelModelAction<T> method, T auxData, bool runOnAll = true)
		{
			if (runOnAll) Target = (string)method("Target", typeof(string), Target, auxData);
			if (runOnAll) Count = (int?)method("Count", typeof(int?), Count, auxData);
			ListOfStrings = (List<string>)method("ListOfStrings", typeof(List<string>), ListOfStrings, auxData);

		}

		public virtual bool RunOnChild<T>(string name, BabelModelAction<T> method, T auxData)
		{
			switch (name)
			{
				case "Target": Target = (string)method("Target", typeof(string), Target, auxData); return true;
				case "Count": Count = (int?)method("Count", typeof(int?), Count, auxData); return true;
				case "ListOfStrings": ListOfStrings = (List<string>)method("ListOfStrings", typeof(List<string>), ListOfStrings, auxData); return true;
				default: return false;
			}
		}
		#endregion
	}

	[TestClass]
	public class JSONSerializerAdditionalTest
	{
		static List<TestRequest> GetData()
		{
			return new List<TestRequest>() { new TestRequest{
					Count = 5,
					ListOfStrings = new List<string> { "a", "bbb", null },
					Target = "some string"
				},	new TestRequest{
					Count = 6,
					ListOfStrings = new List<string> { "b", "ccb", null },
					Target = "some string 2"
				}
			};
		}



		[TestMethod]
		public void DictionaryOfComplexObjectsSerilaization()
		{
			var ser = new BabelJsonSerializer();
			var data = new Dictionary<string, object> { { "a", GetData() }, { "b", null }, { "Sdfd", 61 } };
			byte[] babelData;
			string babelJson;
			using (var ms = (MemoryStream)ser.Serialize(data))
			{
				babelData = ms.ToArray();
				babelJson = Encoding.UTF8.GetString(babelData);
			}
			Assert.AreEqual("{\"a\":[{\"Target\":\"some string\",\"Count\":5,\"ListOfStrings\":[\"a\",\"bbb\",null]},{\"Target\":\"some string 2\",\"Count\":6,\"ListOfStrings\":[\"b\",\"ccb\",null]}],\"b\":null,\"Sdfd\":61}", babelJson);
		}

		
		

		[TestMethod]
		public void BoolDeserializationTest()
		{
			var ser = new BabelJsonSerializer();
			using (var ms = new MemoryStream(new byte[] { 116, 114, 117, 101 }))
			{
				var result = ser.Deserialize<bool?>(ms);
				Assert.AreEqual(true, result);
			}

			using (var ms = new MemoryStream(new byte[] { 9, 32, 116, 114, 117, 101, 32, 10, 13 }))
			{
				var result = ser.Deserialize<bool?>(ms);
				Assert.AreEqual(true, result);
			}
		}

		[TestMethod]
		public void WhitestpaceDeserializationTest()
		{
			var ser = new BabelJsonSerializer();
			using (var ms = new MemoryStream(new byte[] { 32, 32, 32, 32, 9 }))
			{
				var result = ser.Deserialize<bool?>(ms);
				Assert.IsNull(result);
			}
		}

		[TestMethod]
		public void DecimalFormatsDeserializationTest()
		{
			var ser = new BabelJsonSerializer();
			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes("0.000000000000000")))
			{
				var result = ser.Deserialize<decimal?>(ms);
				Assert.AreEqual(0.0m, result, "Decimal zero");
			}

			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes("0e-8")))
			{
				var result = ser.Deserialize<decimal?>(ms);
				Assert.AreEqual(0.0m, result, "Zero EN negative exponent");
			}

			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes("0E8")))
			{
				var result = ser.Deserialize<decimal?>(ms);
				Assert.AreEqual(0.0m, result, "Zero EN positeve exponent");
			}

			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes("0.00045")))
			{
				var result = ser.Deserialize<decimal?>(ms);
				Assert.AreEqual(0.00045m, result, "Leading zero");
			}

			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(".00045")))
			{
				var result = ser.Deserialize<decimal?>(ms);
				Assert.AreEqual(0.00045m, result, "No leading zero");
			}

			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(".0045e-2")))
			{
				var result = ser.Deserialize<decimal?>(ms);
				Assert.AreEqual(0.000045m, result, "No leading zero, exponent");
			}

			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes("0.0045e-2")))
			{
				var result = ser.Deserialize<decimal?>(ms);
				Assert.AreEqual(0.000045m, result, "Leading zero, exponent");
			}

			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes("bad daTA")))
			{
				try
				{
					var result = ser.Deserialize<decimal?>(ms);
					Assert.Fail("Bad data deserialization suppjsed to throw an exception");
				}
				catch (JsonSerializationError err)
				{
					Assert.AreEqual(typeof(InvalidCastException), err.InnerException.GetType(), "Unexpected message on error");
				}
			}
		}




		[TestMethod]
		public void DateTimeSerilaizationTest()
		{
			var ser = new BabelJsonSerializer();
			DateTime? dt = new DateTime(2012, 12, 31, 0, 0, 0, DateTimeKind.Utc);

			using (var str = (MemoryStream)ser.Serialize(dt))
			{
				string babelJson = Encoding.UTF8.GetString(str.ToArray());
				Assert.AreEqual("\"2012-12-31T00:00:00.000Z\"", babelJson);
			}

			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes("\"2012-12-31T00:00:00.000Z\"")))
			{
				var result = ser.Deserialize<DateTime?>(ms);
				Assert.AreEqual(dt, result, "Deserialization with milliseconds");
			}

			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes("\"2012-12-31T00:00:00Z\"")))
			{
				var result = ser.Deserialize<DateTime?>(ms);
				Assert.AreEqual(dt, result, "Deserialization with no milliseconds");
			}
		}
	}


}

