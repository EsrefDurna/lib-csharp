using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Concur.Government.Database;

namespace Concur.Babel.Test
{
	[TestClass]
	public class XmlSerializerAdditionalTests
	{
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
		public void TestExecXmlWrongNode()
		{
			const string XML = @"<DbRequest>
<ShardInfo>
<CompanyId>409</CompanyId>
<ApplicationGroup>Government</ApplicationGroup>
</ShardInfo><Sql>select top 10 * from state</Sql>
</DbRequest>";

			var ser = new BabelXmlSerializer();
			using(var ms = new MemoryStream(Encoding.UTF8.GetBytes(XML)))
			{
				var result = ser.Deserialize<ExecQueryRequest>(ms);
				Assert.IsNotNull(result, "result shouldn't be null");
				Assert.IsNull(result.Query, "Query should be null");
			}
		}

		[TestMethod]
		public void TestExecXml()
		{
			const string XML = @"<A><query>
<ShardInfo>
<CompanyId>409</CompanyId>
<ApplicationGroup>Government</ApplicationGroup>
</ShardInfo>
<Sql>select top 10 * from state</Sql>
</query></A>";

			var ser = new BabelXmlSerializer();
			using(var ms = new MemoryStream(Encoding.UTF8.GetBytes(XML)))
			{
				var result = ser.Deserialize<ExecQueryRequest>(ms);
				Assert.IsNotNull(result, "result shouldn't be null");
				Assert.IsNotNull(result.Query, "Query shouldn't be null");
				Assert.IsNotNull(result.Query.ShardInfo, "ShardInfo shouldn't be null");
				Assert.AreEqual(409, result.Query.ShardInfo.CompanyId, "Company Id");
				Assert.AreEqual("Government", result.Query.ShardInfo.ApplicationGroup, "ApplicationGroup");
				Assert.AreEqual("select top 10 * from state", result.Query.Sql, "SQL");
				//string compRes;
				//Assert.IsTrue(result.Compare(data, out compRes), "Data before serialization don't match data after deserialization: " + compRes ?? "");
			}
		}

		[TestMethod]
		public void TestExecXmlWithJunk()
		{
			const string XML = @"<A><query><z>sdfsf<c><b>34534</b></c>
</z>
<ShardInfo>
<CompanyId>409</CompanyId>
<ApplicationGroup>Government</ApplicationGroup>
</ShardInfo>
<Sql>select top 10 * from state</Sql>
</query></A>";

			var ser = new BabelXmlSerializer();
			using(var ms = new MemoryStream(Encoding.UTF8.GetBytes(XML)))
			{
				var result = ser.Deserialize<ExecQueryRequest>(ms);
				Assert.IsNotNull(result, "result shouldn't be null");
				Assert.IsNotNull(result.Query, "Query shouldn't be null");
				Assert.IsNotNull(result.Query.ShardInfo, "ShardInfo shouldn't be null");
				Assert.AreEqual(409, result.Query.ShardInfo.CompanyId, "Company Id");
				Assert.AreEqual("Government", result.Query.ShardInfo.ApplicationGroup, "ApplicationGroup");
				Assert.AreEqual("select top 10 * from state", result.Query.Sql, "SQL");
				//string compRes;
				//Assert.IsTrue(result.Compare(data, out compRes), "Data before serialization don't match data after deserialization: " + compRes ?? "");
			}
		}
	}
}
