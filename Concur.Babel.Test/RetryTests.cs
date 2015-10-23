using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Concur.Babel
{
	[TestClass]
	public class RetryTests : HttpTransport
	{
		public RetryTests() : base(new BabelJsonSerializer(), "localhost", 0, "", "") { }
		[TestMethod]
		public void ZeroRetriesStillTries(){
			RetryTests transport = new RetryTests();
			var serializer = new Mock<IBabelSerializer>();
			transport.m_serializer = serializer.Object;
			serializer.Setup(x => x.Deserialize<ServiceError>(It.IsAny<Stream>())).Returns(new ServiceError());
			serializer.Setup(x => x.Serialize(It.IsAny<object>())).Returns(new MemoryStream());
			int failues = 0;
			int completes = 0;
			transport.OnFailure += x=>failues++;
			transport.OnComplete += x => completes++;
			transport.RetryCount = 0;
			transport.m_responseGenerator = () => new BabelWebResponse(HttpStatusCode.GatewayTimeout, "", new Exception(), null, new byte[0]);

			try
			{
				transport.Send("method", null, null);
			}
			catch (BabelException) { }
			catch (Exception) { Assert.Fail(); }

			Assert.AreEqual(1, failues);
			Assert.AreEqual(0, completes);
		}

		[TestMethod]
		public void OneRetry()
		{
			RetryTests transport = new RetryTests();
			var serializer = new Mock<IBabelSerializer>();
			transport.m_serializer = serializer.Object;
			serializer.Setup(x => x.Deserialize<ServiceError>(It.IsAny<Stream>())).Returns(new ServiceError());
			serializer.Setup(x => x.Serialize(It.IsAny<object>())).Returns(new MemoryStream());
			int failues = 0;
			int completes = 0;
			transport.OnFailure += x => failues++;
			transport.OnComplete += x => completes++;
			transport.RetryCount = 1;
			transport.m_responseGenerator = () => new BabelWebResponse(HttpStatusCode.GatewayTimeout, "", new Exception(), null, new byte[0]);

			try
			{
				transport.Send("method", null, null);
			}
			catch (BabelException) { }
			catch (Exception) { Assert.Fail(); }

			Assert.AreEqual(2, failues, "# of retries");
			Assert.AreEqual(0, completes, "# of successful");
		}

		[TestMethod]
		public void FailureThenSuccess()
		{
			RetryTests transport = new RetryTests();
			var serializer = new Mock<IBabelSerializer>();
			transport.m_serializer = serializer.Object;
			serializer.Setup(x => x.Deserialize<ServiceError>(It.IsAny<Stream>())).Returns(new ServiceError());
			serializer.Setup(x => x.Serialize(It.IsAny<object>())).Returns(new MemoryStream());
			int failues = 0;
			int completes = 0;
			transport.OnFailure += x => failues++;
			transport.OnComplete += x => completes++;
			transport.RetryCount = 1;
			bool first = true;
			transport.m_responseGenerator = () => {
				if (first) 
				{
					first = false; return new BabelWebResponse(HttpStatusCode.GatewayTimeout, "", new Exception(), null, new byte[0]); 
				}
				else 
				{
					return new BabelWebResponse(HttpStatusCode.OK, "", null, null, new byte[0]); 
				}
			};

			try
			{
				transport.Send("method", null, null);
			}
			catch (BabelException) { }
			catch (Exception) { Assert.Fail(); }

			Assert.AreEqual(1, failues);
			Assert.AreEqual(1, completes);
		}

		/// <summary>
		/// Stub HttpContent object to feed into the response. 
		/// </summary>
		class StubHttpContent : HttpContent
		{
			protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
			{
				var task = Task.Run(() => { });
				return task;
			}

			protected override bool TryComputeLength(out long length)
			{
				length = 0;
				return true;
			}
		}

		[TestMethod]
		public void ZeroRetriesStillTriesAsync()
		{
			RetryTests transport = new RetryTests();
			var serializer = new Mock<IBabelSerializer>();
			transport.m_serializer = serializer.Object;
			serializer.Setup(x => x.Deserialize<ServiceError>(It.IsAny<Stream>())).Returns(new ServiceError());
			serializer.Setup(x => x.Serialize(It.IsAny<object>())).Returns(new MemoryStream());
			int failues = 0;
			int completes = 0;
			transport.OnFailure += x => failues++;
			transport.OnComplete += x => completes++;
			transport.RetryCount = 0;
			transport.m_responseGenerator = () => {return new BabelWebResponse(HttpStatusCode.OK, "", null, null, new byte[0]);};
			try
			{
				var result = transport.SendAsync("method", null, null);
				result.Wait();
			}
			catch (AggregateException) { } // AggregateException is expected comes out of the task library itself
			Assert.AreEqual(0, failues, "Expected no failures");
			Assert.AreEqual(1, completes, "Should run once");
		}

		[TestMethod]
		public void OneRetryAsync()
		{
			RetryTests transport = new RetryTests();
			var serializer = new Mock<IBabelSerializer>();
			transport.m_serializer = serializer.Object;
			serializer.Setup(x => x.Deserialize<ServiceError>(It.IsAny<Stream>())).Returns(new ServiceError());
			serializer.Setup(x => x.Serialize(It.IsAny<object>())).Returns(()=>new MemoryStream());// be sure to create a new stream or else it will have already been closed.
			int failues = 0;
			int completes = 0;
			transport.OnFailure += x => failues++;
			transport.OnComplete += x => completes++;
			transport.RetryCount = 1;
			transport.m_responseGenerator = () => { return new BabelWebResponse(HttpStatusCode.GatewayTimeout, "", new Exception(), null, new byte[0]); };
			try
			{
				var result = transport.SendAsync("method", null, null);
				result.Wait();
			}
			catch (AggregateException) { } // AggregateException is expected comes out of the task library itself

			Assert.AreEqual(2, failues, "Bad number of failures");
			Assert.AreEqual(0, completes, "Should have no completeses");
		}

		[TestMethod]
		public void FailureThenSuccessAsync()
		{
			RetryTests transport = new RetryTests();
			var serializer = new Mock<IBabelSerializer>();
			transport.m_serializer = serializer.Object;
			serializer.Setup(x => x.Deserialize<ServiceError>(It.IsAny<Stream>())).Returns(new ServiceError());
			serializer.Setup(x => x.Serialize(It.IsAny<object>())).Returns(() => new MemoryStream());// be sure to create a new stream or else it will have already been closed.
			int failues = 0;
			int completes = 0;
			transport.OnFailure += x => failues++;
			transport.OnComplete += x => completes++;
			transport.RetryCount = 1;

			bool isFirst = true;
			transport.m_responseGenerator = () => {
				if(isFirst)
				{
					isFirst = false; 
					return new BabelWebResponse(HttpStatusCode.GatewayTimeout, "", new Exception(), null, new byte[0]);
				}
				else
				{
					return new BabelWebResponse(HttpStatusCode.OK, "", null, null, new byte[0]);
				}
			};


			var result = transport.SendAsync("method", null, null);
			result.Wait();

			// No exception expected since the retry was good

			Assert.AreEqual(1, failues);
			Assert.AreEqual(1, completes);
		}

		private Func<BabelWebResponse> m_responseGenerator;

		protected override BabelWebResponse MakeHTTPRequest(string httpMethod, string url, byte[] postData, string contentType = null, string acceptType = null, int? timeoutInSeconds = null, NameValueCollection headers = null)
		{
			return m_responseGenerator();
		}

		protected override Task<BabelWebResponse> MakeHTTPRequestAsync(string httpMethod, string url, byte[] postData, string contentType, string acceptType, int? timeoutInSeconds, NameValueCollection headers)
		{
			return Task.FromResult(m_responseGenerator());
		}
	}
}
