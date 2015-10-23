using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Concur.Government.Database;
using Concur.SharedModels;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Concur.Babel.Test
{
	public class IpmMsgRequest : IBabelModel
	{
		/// <summary>
		/// Default constructor
		/// </summary>
		public IpmMsgRequest()
		{
			ForcedKeys = new List<string>();
		}

		public string Target { get; set; }

		public int? Count { get; set; }

		public List<string> ForcedKeys { get; set; }

		public override string ToString()
		{
			var ser = new BabelJsonSerializer();
			using(var strm = (System.IO.MemoryStream)ser.Serialize(this))
			{
				return (new UTF8Encoding(false)).GetString(strm.ToArray());
			}
		}
		#region IBabelModel
		public virtual void RunOnChildren<T>(BabelModelAction<T> method, T auxData, bool runOnAll = true)
		{
			if(runOnAll) Target = (string)method("Target", typeof(string), Target, auxData);
			if(runOnAll) Count = (int?)method("Count", typeof(int?), Count, auxData);
			ForcedKeys = (List<string>)method("ForcedKeys", typeof(List<string>), ForcedKeys, auxData);

		}

		public virtual bool RunOnChild<T>(string name, BabelModelAction<T> method, T auxData)
		{
			switch(name)
			{
				case "Target": Target = (string)method("Target", typeof(string), Target, auxData); return true;
				case "Count": Count = (int?)method("Count", typeof(int?), Count, auxData); return true;
				case "ForcedKeys": ForcedKeys = (List<string>)method("ForcedKeys", typeof(List<string>), ForcedKeys, auxData); return true;
				default: return false;
			}
		}
		#endregion
	}

	[TestClass]
	public class JSONSerializerAdditionalTest
	{
		static List<IpmMsgRequest> GetData()
		{
			return new List<IpmMsgRequest>() { new IpmMsgRequest{
					Count = 5,
					ForcedKeys = new List<string> { "a", "bbb", null },
					Target = "some string"
				},	new IpmMsgRequest{
					Count = 6,
					ForcedKeys = new List<string> { "b", "ccb", null },
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
			using(var ms = (MemoryStream)ser.Serialize(data))
			{
				babelData = ms.ToArray();
				babelJson = Encoding.UTF8.GetString(babelData);
			}
			Assert.AreEqual("{\"a\":[{\"Target\":\"some string\",\"Count\":5,\"ForcedKeys\":[\"a\",\"bbb\",null]},{\"Target\":\"some string 2\",\"Count\":6,\"ForcedKeys\":[\"b\",\"ccb\",null]}],\"b\":null,\"Sdfd\":61}", babelJson);
		}

		public class ExecQueryRequest : Concur.Babel.Mvc.IBabelRequest
		{
			/// <summary>
			///  RequestObject specify what to run, and where
			/// </summary>
			public DbRequest Query;

			#region IBabelRequest
			public void RunOnChildren<T>(BabelModelAction<T> method, T auxData, bool runOnAll = true)
			{
				Query = (DbRequest)method("query", typeof(DbRequest), Query, auxData);
			}

			public bool RunOnChild<T>(string name, BabelModelAction<T> method, T auxData)
			{
				switch(name)
				{
					case "query": Query = (DbRequest)method("query", typeof(DbRequest), Query, auxData); return true;
					default: return false;
				}
			}

			public void SetDefaults()
			{
			}
			#endregion
		}

		[TestMethod]
		public void StringDeserializationTest()
		{
			const string TEST_JSON = "{\"query\":{\"Info\":{\"Blocks\":[]},\"ShardInfo\":{\"ConnectionName\":\"devgsa.gov\",\"ApplicationGroup\":\"Government\"},\"Sql\":\"/* QUERY PAGE: Query from acsontos@outtask.com(Andrew Stephen Csontos) */ Select distinct tperson.\\\"obt-companyid\\\", voucher.tanum, voucher.vchnum, ticksub.ticknum, ticksub.locator, voucher.docnum, voucher.\\\"tran-status\\\", ticksub.depdate, ticksub.\\\"res-status\\\", voucher.\\\"cur-stat\\\", voucher.lname, voucher.fname, voucher.doctype\\r\\nFrom voucher\\r\\nInner join tperson\\r\\n\\tOn voucher.ssn = tperson.ssn \\r\\ninner join ticksub on voucher.ssn = ticksub.ssn\\r\\n\\tand (voucher.doctype = ticksub.doctype \\r\\n\\t\\t or ticksub.doctype = \\'#VAUTH#\\')\\r\\n\\tand voucher.vchnum = ticksub.vchnum\\r\\n\\tand voucher.\\\"adj-level\\\" = ticksub.\\\"adj-level\\\"\\r\\ninner join trip\\r\\n\\ton voucher.ssn = trip.ssn\\r\\n\\tand voucher.doctype = trip.doctype\\r\\n\\tand voucher.vchnum = trip.vchnum\\r\\n\\tand voucher.\\\"adj-level\\\" = trip.\\\"adj-level\\\" \\r\\n\\tand (\\r\\n\\t\\t(voucher.mintrip = trip.tripnum )\\r\\n\\t\\tor \\r\\n\\t\\t(voucher.mintrip = 0 and trip.tripnum = 1)\\r\\n\\t)\\r\\nWhere voucher.gtmdoctype in (\\'AUTH\\', \\'VCH\\')\\r\\n\\tand voucher.\\\"tran-status\\\" in (\\'COMPLETE\\', \\'LOCKED\\')\\r\\n\\tand ticksub.\\\"res-status\\\" in (\\'BOOKED\\')\\r\\n\\tand ticksub.\\\"res-type\\\" in (\\'COMM-CARR\\',\\'COMM-RAIL\\')\\r\\n\\tand ticksub.\\\"depdate\\\" between \\'2013-01-01\\' AND \\'2014-01-01\\'\\r\\n\\tand (select prefval from syspref where type = \\'IsBillable\\') = \\'YES\\'\"}}";

			var ser = new BabelJsonSerializer();
			
			
			using(var ms = new MemoryStream(Encoding.UTF8.GetBytes(TEST_JSON)))
			{
				var result = ser.Deserialize<ExecQueryRequest>(ms);
				Assert.AreEqual("/* QUERY PAGE: Query from acsontos@outtask.com(Andrew Stephen Csontos) */ Select distinct tperson.\"obt-companyid\", voucher.tanum, voucher.vchnum, ticksub.ticknum, ticksub.locator, voucher.docnum, voucher.\"tran-status\", ticksub.depdate, ticksub.\"res-status\", voucher.\"cur-stat\", voucher.lname, voucher.fname, voucher.doctype\r\nFrom voucher\r\nInner join tperson\r\n\tOn voucher.ssn = tperson.ssn \r\ninner join ticksub on voucher.ssn = ticksub.ssn\r\n\tand (voucher.doctype = ticksub.doctype \r\n\t\t or ticksub.doctype = '#VAUTH#')\r\n\tand voucher.vchnum = ticksub.vchnum\r\n\tand voucher.\"adj-level\" = ticksub.\"adj-level\"\r\ninner join trip\r\n\ton voucher.ssn = trip.ssn\r\n\tand voucher.doctype = trip.doctype\r\n\tand voucher.vchnum = trip.vchnum\r\n\tand voucher.\"adj-level\" = trip.\"adj-level\" \r\n\tand (\r\n\t\t(voucher.mintrip = trip.tripnum )\r\n\t\tor \r\n\t\t(voucher.mintrip = 0 and trip.tripnum = 1)\r\n\t)\r\nWhere voucher.gtmdoctype in ('AUTH', 'VCH')\r\n\tand voucher.\"tran-status\" in ('COMPLETE', 'LOCKED')\r\n\tand ticksub.\"res-status\" in ('BOOKED')\r\n\tand ticksub.\"res-type\" in ('COMM-CARR','COMM-RAIL')\r\n\tand ticksub.\"depdate\" between '2013-01-01' AND '2014-01-01'\r\n\tand (select prefval from syspref where type = 'IsBillable') = 'YES'", result.Query.Sql);
			}
		}

		[TestMethod]
		public void BoolDeserializationTest()
		{
			var ser = new BabelJsonSerializer();
			using(var ms = new MemoryStream(new byte[] {116, 114, 117, 101}))
			{
				var result = ser.Deserialize<bool?>(ms);
				Assert.AreEqual(true, result);
			}

			using(var ms = new MemoryStream(new byte[] {9, 32, 116, 114, 117, 101, 32, 10, 13}))
			{
				var result = ser.Deserialize<bool?>(ms);
				Assert.AreEqual(true, result);
			}
		}

		[TestMethod]
		public void WhitestpaceDeserializationTest()
		{
			var ser = new BabelJsonSerializer();
			using(var ms = new MemoryStream(new byte[] { 32, 32, 32, 32, 9 }))
			{
				var result = ser.Deserialize<bool?>(ms);
				Assert.IsNull(result);
			}
		}

		[TestMethod]
		public void DecimalFormatsDeserializationTest()
		{
			var ser = new BabelJsonSerializer();
			using(var ms = new MemoryStream(Encoding.UTF8.GetBytes("0.000000000000000")))
			{
				var result = ser.Deserialize<decimal?>(ms);
				Assert.AreEqual(0.0m, result, "Decimal zero");
			}

			using(var ms = new MemoryStream(Encoding.UTF8.GetBytes("0e-8")))
			{
				var result = ser.Deserialize<decimal?>(ms);
				Assert.AreEqual(0.0m, result, "Zero EN negative exponent");
			}

			using(var ms = new MemoryStream(Encoding.UTF8.GetBytes("0E8")))
			{
				var result = ser.Deserialize<decimal?>(ms);
				Assert.AreEqual(0.0m, result, "Zero EN positeve exponent");
			}

			using(var ms = new MemoryStream(Encoding.UTF8.GetBytes("0.00045")))
			{
				var result = ser.Deserialize<decimal?>(ms);
				Assert.AreEqual(0.00045m, result, "Leading zero");
			}

			using(var ms = new MemoryStream(Encoding.UTF8.GetBytes(".00045")))
			{
				var result = ser.Deserialize<decimal?>(ms);
				Assert.AreEqual(0.00045m, result, "No leading zero");
			}

			using(var ms = new MemoryStream(Encoding.UTF8.GetBytes(".0045e-2")))
			{
				var result = ser.Deserialize<decimal?>(ms);
				Assert.AreEqual(0.000045m, result, "No leading zero, exponent");
			}

			using(var ms = new MemoryStream(Encoding.UTF8.GetBytes("0.0045e-2")))
			{
				var result = ser.Deserialize<decimal?>(ms);
				Assert.AreEqual(0.000045m, result, "Leading zero, exponent");
			}

			using(var ms = new MemoryStream(Encoding.UTF8.GetBytes("bad daTA")))
			{
				try
				{
					var result = ser.Deserialize<decimal?>(ms);
					Assert.Fail("Bad data deserialization suppjsed to throw an exception");
				}
				catch(JsonSerializationError err)
				{
					Assert.AreEqual(typeof(InvalidCastException), err.InnerException.GetType(), "Unexpected message on error");
				}
			}
		}

		[TestMethod]
		public void DateTimeSerilaizationTest()
		{
			var ser = new BabelJsonSerializer();
			DateTime? dt = new DateTime(2012, 12, 31,0,0,0, DateTimeKind.Utc);

			using(var str = (MemoryStream)ser.Serialize(dt))
			{
				string babelJson = Encoding.UTF8.GetString(str.ToArray());
				Assert.AreEqual("\"2012-12-31T00:00:00.000Z\"", babelJson);
			}

			using(var ms = new MemoryStream(Encoding.UTF8.GetBytes("\"2012-12-31T00:00:00.000Z\"")))
			{
				var result = ser.Deserialize<DateTime?>(ms);
				Assert.AreEqual(dt, result, "Deserialization with milliseconds");
			}

			using(var ms = new MemoryStream(Encoding.UTF8.GetBytes("\"2012-12-31T00:00:00Z\"")))
			{
				var result = ser.Deserialize<DateTime?>(ms);
				Assert.AreEqual(dt, result, "Deserialization with no milliseconds");
			}
		}
	}


}

