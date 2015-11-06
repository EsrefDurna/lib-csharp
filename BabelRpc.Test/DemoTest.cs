using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BabelRpc;
using BabelRpc.Demo;
using System.IO;

namespace BabelRpc.Test
{
	[TestClass]
	public class DemoTest
	{
		const string BASE_URL = "http://localhost/BabelRpcTestMvcService/";
		const string BAD_200_BASE_URL = "http://localhost/BabelRpcTestMvcService/Home";

		const string JOKE = "Why did the chicken commit his code?";
		const string ANSWER = "To avoid the fork in the tree.";

		[TestMethod]
		public void TestMethod()
		{
			var client = new LogControlClient(BASE_URL);
			client.Headers.Add("Foo", FOO);	
			client.Headers.Add("RequestId", REQUEST_ID);
			int onResponseCount = 0;

			client.OnComplete += (data) =>
				{
					onResponseCount ++;
					CheckLog(data.Url, data.Request, data.Response, data.Status);
				};

			client.OnStart += (d)=>
			{
				System.Diagnostics.Debug.WriteLine("Sending request for method {0} (Full URL: {1}) {2}", d.Url, d.Method, d.Stage);
				System.Diagnostics.Debug.WriteLine("Request headers {0} (ms)", d.RequestHeaders);
			};

			client.OnComplete += (d) => {
				System.Diagnostics.Debug.WriteLine("Sending request for method {0} (Full URL: {1}) {2}", d.Url, d.Method, d.Stage);
				System.Diagnostics.Debug.WriteLine("Request: {0}", Encoding.UTF8.GetString(d.Request));
				System.Diagnostics.Debug.WriteLine("Response: {0}", Encoding.UTF8.GetString(d.Response));
				System.Diagnostics.Debug.WriteLine("Request duration: {0} ms", d.DurationMilliseconds);
				string requestHeadersString = string.Join("\r\n", d.RequestHeaders.AllKeys.Select(k => { return k + ":" + d.RequestHeaders[k]; }).ToArray());
				System.Diagnostics.Debug.WriteLine("Request headers: {0}", requestHeadersString);
				string responseHeadersString = string.Join("\r\n", d.ResponseHeaders.AllKeys.Select(k => { return k + ":" + d.ResponseHeaders[k]; }).ToArray());
				System.Diagnostics.Debug.WriteLine("Response headers: {0}", responseHeadersString);
			};

			client.OnFailure += (d)=>
			{
				System.Diagnostics.Debug.WriteLine("Sending request for method {0} (Full URL: {1}) {2}", d.Url, d.Method, d.Stage);
				System.Diagnostics.Debug.WriteLine("Request: {0}", Encoding.UTF8.GetString(d.Request));
				System.Diagnostics.Debug.WriteLine("Response: {0}", Encoding.UTF8.GetString(d.Response));
				System.Diagnostics.Debug.WriteLine("Request duration: {0} (ms)", d.DurationMilliseconds);
				string requestHeadersString = string.Join("\r\n", d.RequestHeaders.AllKeys.Select(k => { return k + ":" + d.RequestHeaders[k]; }).ToArray());
				System.Diagnostics.Debug.WriteLine("Request headers: {0}", requestHeadersString);
				string responseHeadersString = string.Join("\r\n", d.ResponseHeaders.AllKeys.Select(k => { return k + ":" + d.ResponseHeaders[k]; }).ToArray());
				System.Diagnostics.Debug.WriteLine("Response headers: {0}", responseHeadersString);
				System.Diagnostics.Debug.WriteLine("Transport specific error code: {0}", d.Status);
			};

			client.AddJoke(new Joke() { Question = JOKE, Answer = ANSWER, DateAdded = DateTime.Now });
			client.SetLogStatus(Logs.ErrorLog, State.ON);
			client.SetLogStatus(Logs.MessageLog, State.OFF);
			Info toSet = new Info();
			toSet.Status["TransLog1"] = State.ON;
			toSet.Status["TransLog2"] = State.OFF;
			toSet.Status["TransLog3"] = State.ON;
			toSet.Status["TransLog4"] = State.OFF;
			var finfo = client.SetMulti(toSet);
			CheckResult(finfo, "1");
			//Check default 
			Assert.AreEqual(1, finfo.LogLevel, "LogLevel should be set to 1 by default");
			
			Info info = client.GetLoggingStatus();
			CheckResult(info, "2");

			Assert.AreEqual(5, onResponseCount, "OnResponse event called wrong number of times");
		}

		public void TestDefaultOverride()
		{
			var client = new LogControlClient(BASE_URL);
			Info toSet = new Info();
			toSet.Status["TransLog1"] = State.ON;
			toSet.Status["TransLog2"] = State.OFF;
			toSet.Status["TransLog3"] = State.ON;
			toSet.Status["TransLog4"] = State.OFF;
			var finfo = client.SetMulti(toSet, 5);
			CheckResult(finfo, "1");
			//Check default 
			Assert.AreEqual(5, finfo.LogLevel, "Default logLevel should be overridden");
		}

		private static void CheckLog(string url, byte[] request, byte[] response, string status)
		{
			System.Diagnostics.Debug.WriteLine("{3}>{0}: {1} -> {2}", url, System.Text.Encoding.ASCII.GetString(request), System.Text.Encoding.ASCII.GetString(response), status);
			switch(url)
			{
				case BASE_URL + "LogControl/AddJoke":
					string decoded = Encoding.UTF8.GetString(request);
					// This could use to be a bit stronger but full deserialization requires a type only available in the controller which isn't here.
					Assert.IsTrue(decoded.Contains(JOKE));
					Assert.IsTrue(decoded.Contains(ANSWER));
					Assert.AreEqual(0, response.Length, "Unexpected response for AddJoke");
					break;
				case BASE_URL + "LogControl/SetLogStatus":
					Assert.IsTrue(request.Length > 1, "Unexpected request length in SetLogStatus");
					Assert.AreEqual(0, response.Length, "Unexpected response for SetLogStatus");
					break;
				case BASE_URL + "LogControl/SetMulti":
					Assert.IsTrue(request.Length > 1, "Unexpected request length in SetMulti");
					Assert.IsTrue(response.Length > 1, "Unexpected response SetMulti");
					break;
				case BASE_URL + "LogControl/GetLoggingStatus":
					Assert.AreEqual("{}", System.Text.Encoding.ASCII.GetString(request), "Unexpected request length in SetMulti");
					Assert.IsTrue(response.Length > 1, "Unexpected response SetMulti");
					break;
				default:
					Assert.Fail("Unexpected url {0}", url);
					break;
			}
			Assert.AreEqual("OK", status, "Unexpected status");
		}

		private static void CheckResult(Info finfo, string key)
		{
			Assert.IsTrue(finfo.Jokes.Count > 0, key + ": Has jokes");
			Assert.IsTrue(finfo.Status.Count > 0, key + ": Has status");
			Assert.AreEqual(finfo.Status["TransLog1"], State.ON, key + ": TransLog1");
			Assert.AreEqual(finfo.Status["TransLog2"], State.OFF, key + ": TransLog2");
			Assert.AreEqual(finfo.Status["TransLog3"], State.ON, key + ": TransLog3");
			Assert.AreEqual(finfo.Status["TransLog4"], State.OFF, key + ": TransLog4");

			Assert.AreEqual(string.Format("Foo: {0}; RequrestId: {1}", FOO, REQUEST_ID), finfo.Jokes.First(j=>j.Question == "Status?").Answer, "Headers transmited wrong");
		}

		const string FOO = "12124214\n\r2345hfgfh & ;;;";
		const string REQUEST_ID = "234";
		[TestMethod]
		public void TestAsyncMethod()
		{
			var client = new LogControlClient(BASE_URL);
			client.Headers.Add("Foo", FOO);
			client.Headers.Add("RequestId", REQUEST_ID);
			int onResponseCount = 0;

			client.OnComplete += (data) => {
				onResponseCount++;
				CheckLog(data.Url, data.Request, data.Response, data.Status);
			};
			var t1 = client.AddJokeAsync(new Joke() { Question = JOKE, Answer = ANSWER, DateAdded = DateTime.Now });
			var t2 = client.SetLogStatusAsync(Logs.ErrorLog, State.ON);
			var t3 = client.SetLogStatusAsync(Logs.MessageLog, State.OFF);
			Info toSet = new Info();
			toSet.Status["TransLog1"] = State.ON;
			toSet.Status["TransLog2"] = State.OFF;
			toSet.Status["TransLog3"] = State.ON;
			toSet.Status["TransLog4"] = State.OFF;
			System.Threading.Tasks.Task.WaitAll(t1, t2, t3);

			var t4 = client.SetMultiAsync(toSet);
			var t5 = client.GetLoggingStatusAsync();
			CheckResult(t4.Result, "1");
			CheckResult(t5.Result, "2");
			Assert.AreEqual(5, onResponseCount, "OnResponse event called wrong number of times");
		}

		[TestMethod]
		public void TestSync200Error()
		{
			Test200Error((client, info) => { client.SetMulti(info); });
		}

		[TestMethod]
		public void TestAsync200Error()
		{
			Test200Error((client, info) => 
			{
				try
				{
					var t = client.SetMultiAsync(info).Result;
				}
				catch(AggregateException ae)
				{
					Assert.AreEqual(1, ae.InnerExceptions.Count, "Should be one inner exception");
					throw ae.InnerExceptions[0];
				}
			});
		}


		void Test200Error(Action<LogControlClient, Info> requestAction)
		{
			var client = new LogControlClient(BAD_200_BASE_URL);
			client.Headers.Add("Foo", FOO);
			client.Headers.Add("RequestId", REQUEST_ID);
			bool onCompleteCalled = false;

			client.OnStart += (d) => {
				System.Diagnostics.Debug.WriteLine("Sending request for method {0} (Full URL: {1}) {2}", d.Url, d.Method, d.Stage);
				System.Diagnostics.Debug.WriteLine("Request headers {0} (ms)", d.RequestHeaders);
			};

			client.OnComplete += (d) => {
				onCompleteCalled = true;
				System.Diagnostics.Debug.WriteLine("Sending request for method {0} (Full URL: {1}) {2}", d.Url, d.Method, d.Stage);
				System.Diagnostics.Debug.WriteLine("Request: {0}", Encoding.UTF8.GetString(d.Request));
				System.Diagnostics.Debug.WriteLine("Response: {0}", Encoding.UTF8.GetString(d.Response));
				System.Diagnostics.Debug.WriteLine("Request duration: {0} ms", d.DurationMilliseconds, null);
				string requestHeadersString = string.Join("\r\n", d.RequestHeaders.AllKeys.Select(k => { return k + ":" + d.RequestHeaders[k]; }).ToArray());
				System.Diagnostics.Debug.WriteLine("Request headers: {0}", requestHeadersString, null);
				string responseHeadersString = string.Join("\r\n", d.ResponseHeaders.AllKeys.Select(k => { return k + ":" + d.ResponseHeaders[k]; }).ToArray());
				System.Diagnostics.Debug.WriteLine("Response headers: {0}", responseHeadersString, null);
			};

			client.OnFailure += (d) => {
				Assert.Fail("OnFailure shouldn't be called on HTTP 200");
			};


			Info toSet = new Info();
			toSet.Status["TransLog1"] = State.ON;
			toSet.Status["TransLog2"] = State.OFF;
			toSet.Status["TransLog3"] = State.ON;
			toSet.Status["TransLog4"] = State.OFF;
			try
			{
				requestAction(client, toSet);
				Assert.Fail("Error should be thrown");
			}
			catch(BabelRequestException err)
			{
				Assert.AreEqual(1, err.Errors.Count, "Should contain 1 error");
				Assert.AreEqual("RESPONSE_FORMAT_ERROR", err.Errors[0].Code, "Should have proper code");

			}
			Assert.IsTrue(onCompleteCalled, "OnComplete should be called");
		}

		[TestMethod] 
		public void TestSync500Error()
		{
			Test500Errors((client, info) => { client.SetMulti(info); });
		}


		[TestMethod] 
		public void TestAsync500Error()
		{
			Test500Errors((client, info) =>
				{
					try
					{
						var t = client.SetMultiAsync(info).Result;
					}
					catch (AggregateException ae)
					{
						Assert.AreEqual(1, ae.InnerExceptions.Count, "Should be one inner exception");
						throw ae.InnerExceptions[0];
					}
				});
		}

		void Test500Errors(Action<LogControlClient, Info> requestAction)
		{
			Test500Error(requestAction, 1, "BadRequest", 1, "INVALID_REQUEST", "Value cannot be null.\r\nParameter name: userId", "Value cannot be null.\r\nParameter name: userId", "BabelApplicationException");
			Test500Error(requestAction, 2, "BadRequest", 1, "INVALID_REQUEST", "This is ArgumentException", "This is ArgumentException", "BabelApplicationException");
			Test500Error(requestAction, 3, "BadRequest", 1, "INVALID_REQUEST", "This is ApplicationException", "This is ApplicationException", "BabelApplicationException");
			Test500Error(requestAction, 4, "InternalServerError", 1, "INTERNAL_ERROR", "Attempted to divide by zero.", "Attempted to divide by zero.", "BabelException");
			Test500Error(requestAction, 5, "InternalServerError", 1, "TEST_EXCEPTION", "Test exception 1", "Test exception 1", "BabelException");
			Test500Error(requestAction, 6, "InternalServerError", 2, "TEST_ERROR", "This is a test error 1", "This is a test error 1, This is a test error 2", "BabelException");
		}

		void Test500Error(Action<LogControlClient, Info> requestAction, int errType, string expectedHttpStatus, int expectedErrorCount, string expectedErrorCode, string expectedErrorMessage, string expectedExceptionMessage, string expectedExceptionTypeName)
		{
			var client = new LogControlClient(BASE_URL, 9);
			client.Headers.Add("ShouldError", errType.ToString());
			
			bool onFailureCalled = false;
			bool onStartCalled = false;

			client.OnStart += (d) => {
				onStartCalled = true;
				System.Diagnostics.Debug.WriteLine("Sending request for method {0} (Full URL: {1}) {2}", d.Url, d.Method, d.Stage);
				System.Diagnostics.Debug.WriteLine("Request headers {0} (ms)", d.RequestHeaders);
			};

			client.OnComplete += (d) => {
				Assert.Fail("OnComplete shouldn't be called");
			};

			client.OnFailure += (d) => {
				Assert.AreEqual(expectedHttpStatus, d.Status, "Wrong HTTP Status");
				onFailureCalled = true;
			};


			Info toSet = new Info();
			toSet.Status["TransLog1"] = State.ON;
			try
			{
				requestAction(client, toSet);
				Assert.Fail("Error should be thrown");
			}
			catch(BabelException err)
			{
				Assert.AreEqual(expectedExceptionTypeName, err.GetType().Name, "Bad exception type");
				Assert.AreEqual(expectedErrorCount, err.Errors.Count, "Wrong error count");
				Assert.AreEqual(expectedErrorCode, err.Errors[0].Code, "Wrong error code");
				Assert.AreEqual(expectedErrorMessage, err.Errors[0].Message, "Wrong error code");
				Assert.AreEqual(expectedExceptionMessage, err.Message, "Wrong error code");
			}
			Assert.IsTrue(onStartCalled, "OnStart should be called");
			Assert.IsTrue(onFailureCalled, "OnComplete should be called");
		}
	}
}
