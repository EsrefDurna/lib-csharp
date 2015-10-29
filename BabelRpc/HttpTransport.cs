using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace BabelRpc
{
	internal sealed class HttpResponseHandlerData : IResponseHandlerData
	{
		internal HttpResponseHandlerData(string url, string method, byte[] request, byte[] response, NameValueCollection rqHeaders)
		{
			Stage = EventKind.Start;
			Url = url;
			Method = method;
			Request = request;
			Response = response;
			RequestHeaders = rqHeaders;
			/*Status = null;
			Error = null;
			ResponseHeaders = null;
			DurationMilliseconds = 0;*/
		}

		public EventKind Stage { get; internal set; }
		public string Url { get; private set; }
		public string Method { get; private set; }
		public byte[] Request { get; internal set; }
		public byte[] Response { get; internal set; }
		public NameValueCollection RequestHeaders { get; private set; }
		public NameValueCollection ResponseHeaders { get; internal set; }
		public string Status { get; internal set; }
		public BabelException Error { get; internal set; }
		public double DurationMilliseconds { get; internal set; }
	}

	/// <summary>
	/// HTTP POST Babel support. This class is thread safe
	/// </summary>
	public class HttpTransport : IBabelTransport
	{
		static HttpTransport()
		{
			System.Net.ServicePointManager.Expect100Continue = false;
			System.Net.ServicePointManager.UseNagleAlgorithm = false;
			System.Net.ServicePointManager.DefaultConnectionLimit = 100;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="serializer">Serializer</param>
		/// <param name="baseUrl">Service base URL</param>
		/// <param name="timeoutSeconds">HTTP request timeout in seconds</param>
		/// <param name="contentType">Content type</param>
		/// <param name="acceptType">Expected return type</param>
		/// <param name="retryCount">How many times the transport should be retried</param>
		/// <param name="retryDelay">How long to wait before trying again</param>
		public HttpTransport(IBabelSerializer serializer, string baseUrl, int timeoutSeconds, string contentType, string acceptType, int retryCount = 0, TimeSpan retryDelay = new TimeSpan())// This is equivalent to TimeSpan.Zero but can be referenced here. 
		{
			if (string.IsNullOrWhiteSpace(baseUrl)) throw new ArgumentException("baseUrl should be a valid URL", "baseUrl");
			m_baseUrl = baseUrl;
			m_timeoutSeconds = timeoutSeconds;
			m_contentType = contentType;
			m_acceptType = acceptType;
			m_serializer = serializer;
			RetryCount = retryCount;
			RetryDelay = retryDelay;
		}

		public int RetryCount { get; set; }
		public TimeSpan RetryDelay { get; set; }

		protected readonly string m_baseUrl;
		protected readonly int m_timeoutSeconds;
		protected readonly string m_contentType;
		protected readonly string m_acceptType;
		protected IBabelSerializer m_serializer;

		public event ResponseHandler OnComplete;
		public event ResponseHandler OnFailure;
		public event ResponseHandler OnStart;

		#region Synchronous methods
		/// <summary>
		/// Posts request synchronously. Expects data to be returned.
		/// </summary>
		/// <typeparam name="T">Result type</typeparam>
		/// <param name="method">Service method</param>
		/// <param name="data">Data to serialize and send </param>
		/// <param name="debugInfo">Extra context info to send as headers</param>
		/// <returns>Deserialized response data</returns>
		public T GetResponse<T>(string method, object data, NameValueCollection debugInfo)
		{
			var resp = SendInternal(method, data, debugInfo);
			using(var outStream = resp.GetStream())
			{
				try
				{
					return m_serializer.Deserialize<T>(outStream);
				}
				catch(Exception err)
				{
					throw resp.GetException("Invalid response format", "RESPONSE_FORMAT_ERROR", err);
				}
			}
		}

		public static string ConcatUrl(string val1, string val2)
		{
			return val1[val1.Length - 1] == '/' ? val1 + val2 : val1 + "/" + val2;
		}

		/// <summary>
		/// Synchronously sends request that doesn't return any data
		/// </summary>
		/// <param name="method">Service method</param>
		/// <param name="data">Data to serialize and send </param>
		/// <param name="debugInfo">Extra context info to send as headers</param>
		public void Send(string method, object data, NameValueCollection debugInfo)
		{
			SendInternal(method, data, debugInfo);
		}

		private BabelWebResponse SendInternal(string method, object data, NameValueCollection debugInfo)
		{
			string url = m_baseUrl + "/" + method;
			var eventData = new HttpResponseHandlerData(url, method, null, null, debugInfo);
			if (OnStart != null)
			{
				OnStart(eventData);
			}
			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			byte[] binData;
			using (var inStream = m_serializer.Serialize(data))
			{
				binData = inStream.GetBytes();
			}

			eventData.Request = binData;
			BabelWebResponse resp = null;
			int retryCount = 0;
			do
			{
				if (retryCount > 0)
				{
					if(OnFailure != null)
					{
						sw.Stop();
						eventData.Stage = EventKind.Failure;
						eventData.Response = null;
						eventData.DurationMilliseconds = (int)sw.ElapsedMilliseconds;
						if(resp != null)
						{
							eventData.ResponseHeaders = resp.GetResponseHeaders();
							eventData.Status = resp.HttpStatus.ToString();
						}
						eventData.Error = null;
						OnFailure(eventData);
					}
					Thread.Sleep(this.RetryDelay);
				}
				resp = MakeHTTPRequest("POST", url, binData, m_contentType, m_acceptType, m_timeoutSeconds, debugInfo);
				retryCount++;
			} while (ConsiderRetry(resp) && retryCount <= RetryCount);

			ProcessResponceCompletion(eventData, sw, resp);
			return resp;
		}

		private void ProcessResponceCompletion(HttpResponseHandlerData eventData, System.Diagnostics.Stopwatch sw, BabelWebResponse resp)
		{
			if(resp.HasError)
			{
				if(OnFailure != null)
				{
					sw.Stop();
					eventData.Stage = EventKind.Failure;
					eventData.Response = resp.GetBuffer();
					eventData.DurationMilliseconds = (int)sw.ElapsedMilliseconds;
					eventData.ResponseHeaders = resp.GetResponseHeaders();
					eventData.Status = resp.HttpStatus.ToString();
					OnFailure(eventData);
				}

				ServiceError se = null;
				try
				{
					using(var strm = resp.GetStream())
					{
						se = m_serializer.Deserialize<ServiceError>(strm);
					}
				}
				catch
				{
					throw resp.GetError(); //This is not a valid service error
				}

				if(se == null) throw resp.GetError(); //This is something like timeout  - no babel error

				var kind = GetErrorKind(resp.HttpStatus);
				throw BabelException.FromServiceError(se, kind);
			}
			else
			{
				if(OnComplete != null)
				{
					sw.Stop();
					eventData.Stage = EventKind.Complete;
					eventData.Response = resp.GetBuffer();
					eventData.DurationMilliseconds = (int)sw.ElapsedMilliseconds;
					eventData.ResponseHeaders = resp.GetResponseHeaders();
					eventData.Status = resp.HttpStatus.ToString();
					OnComplete(eventData);
				}
			}
		}

		/// <summary>
		/// Checks if the response was a failure and due to a reason that should be retried
		/// </summary>
		/// <param name="response">The response to consider a try for</param>
		/// <returns>If there could be a retry</returns>
		private static bool ConsiderRetry(BabelWebResponse response)
		{
			return response.HasError && s_retryableCodes.Contains(response.HttpStatus);
		}
		// Not entirely certain on this list of status codes.
		private static readonly List<HttpStatusCode> s_retryableCodes = new List<HttpStatusCode> { HttpStatusCode.GatewayTimeout, HttpStatusCode.ServiceUnavailable, HttpStatusCode.RequestTimeout };

		/// <summary>
		/// Checks if the response was a failure and due to a reason that should be retried
		/// </summary>
		/// <param name="response">The response to consider a try for</param>
		/// <returns>If there could be a retry</returns>
		private static bool ConsiderRetry(HttpResponseMessage response)
		{
			return !response.IsSuccessStatusCode && s_retryableCodes.Contains(response.StatusCode);
		}

		protected static ErrorKind GetErrorKind(HttpStatusCode status)
		{
			switch (status)
			{
				case HttpStatusCode.BadRequest:
				case HttpStatusCode.Conflict:
					return ErrorKind.InvalidRequest;
				default:
					return ErrorKind.Unknown;
			}
		}

		/// <summary>
		/// Makes synchronous HTTP request. 500 and other HTTP errors will not cause exception to be thrown. Result status code and error flag should be used to process these situations.
		/// </summary>
		/// <param name="httpMethod">"POST" or "GET"</param>
		/// <param name="url">URL</param>
		/// <param name="postData">Data to send</param>
		/// <param name="contentType">Content type ("text/xml; charset=utf-8")</param>
		/// <param name="acceptType">The value of the Accept HTTP header(null (default), "text/xml", ...)</param>
		/// <param name="timeoutInSeconds">HTTP timeout in seconds</param>
		/// <param name="headers">Optional request headers</param>
		/// <returns>Response string</returns>
		protected virtual BabelWebResponse MakeHTTPRequest(string httpMethod, string url, byte[] postData, string contentType = null, string acceptType = null, int? timeoutInSeconds = null, NameValueCollection headers = null)
		{
			if (string.IsNullOrEmpty(url)) throw new ArgumentNullException("url");

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			if (headers != null)
			{
				for (int i = 0; i < headers.Count; i++)
				{
					string v = headers.Get(i);
					string n = headers.GetKey(i);
					if (v != null) v = Uri.EscapeDataString(v);
					request.Headers.Add(n, v);
				}
			}

			if (!string.IsNullOrEmpty(contentType)) { request.ContentType = contentType; }
			if (!string.IsNullOrEmpty(acceptType)) { request.Accept = acceptType; }
			if (timeoutInSeconds.HasValue && timeoutInSeconds > 0) { request.Timeout = timeoutInSeconds.Value * 1000; }

			request.Method = httpMethod;

			//This fixes a bug where you get a 411 Content Length required if you POST without populating the RequestStream() or setting the content-length.
			if (postData == null || postData.Length == 0)
			{
				request.ContentLength = 0;
			}

			HttpWebResponse response = null;
			BabelWebResponse res;
			try
			{
				if (postData != null && postData.Length > 0)
				{
					using (var requestStream = request.GetRequestStream())
					{
						requestStream.Write(postData, 0, postData.Length);
					}
				}

				response = (HttpWebResponse)request.GetResponse();
				using (var receiveStream = response.GetResponseStream())
				{
					res = new BabelWebResponse(response.StatusCode, url, null, response.Headers, receiveStream.GetBytes());
				}
			}
			catch (WebException err)
			{
				WebResponse errResponse = err.Response;

				NameValueCollection outHeaders = null;
				if (response != null) { outHeaders = response.Headers; }
				else if (errResponse != null) { outHeaders = errResponse.Headers; }

				if (errResponse != null)
				{
					var hwr = errResponse as HttpWebResponse;
					HttpStatusCode statusCode = hwr != null ? hwr.StatusCode : HttpStatusCode.InternalServerError;
					using (var errReceiveStream = errResponse.GetResponseStream())
					{
						byte[] msg = errReceiveStream.GetBytes();
						res = new BabelWebResponse(statusCode, url, err, outHeaders, msg);
					}
				}
				else
				{
					HttpStatusCode statusCode = response != null ? response.StatusCode : HttpStatusCode.InternalServerError;
					res = new BabelWebResponse(statusCode, url, err, outHeaders, new byte[0]); // .. or should it be null instead of the error text
				}
			}
			finally
			{
				if (response != null) response.Close();
			}
			return res;
		}

		#endregion

		#region Asynchronous methods
		/// <summary>
		/// Posts request asynchronously. Expects data to be returned.
		/// </summary>
		/// <typeparam name="T">Result type</typeparam>
		/// <param name="method">Service method</param>
		/// <param name="data">Data to serialize and send </param>
		/// <param name="debugInfo">Extra context info to send as headers</param>
		/// <returns>Deserialized response data</returns>
		public Task<T> GetResponseAsync<T>(string method, object data, NameValueCollection debugInfo)
		{
			return SendInternalAsync<T>(method, data, debugInfo, true);
		}

		/// <summary>
		/// Asynchronously sends request that doesn't return any data
		/// </summary>
		/// <param name="method">Service method</param>
		/// <param name="data">Data to serialize and send </param>
		/// <param name="debugInfo">Extra context info to send as headers</param>
		public Task SendAsync(string method, object data, NameValueCollection debugInfo)
		{
			return SendInternalAsync<string>(method, data, debugInfo,false); 
		}

				private Task<T> SendInternalAsync<T>(string method, object data, NameValueCollection debugInfo, bool hasResult)
		{
			string url = m_baseUrl + "/" + method;
			var eventData = new HttpResponseHandlerData(url, method, null, null, debugInfo);
			if(OnStart != null)
			{
				OnStart(eventData);
			}
			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			byte[] binData;
			using(var inStream = m_serializer.Serialize(data))
			{
				binData = inStream.GetBytes();
			}

			eventData.Request = binData;

			return MakeHTTPRequestAsyncWithRetry("POST", url, binData, m_contentType, m_acceptType, m_timeoutSeconds, debugInfo, sw, eventData, RetryCount).ContinueWith((t)=>
			{
				var resp = t.Result;
				ProcessResponceCompletion(eventData, sw, resp);
				if(hasResult)
				{
					try
					{
						using(var strm = resp.GetStream())
						{
							return m_serializer.Deserialize<T>(strm);
						}
					}
					catch(Exception err)
					{
						var respStatus = resp == null ? HttpStatusCode.RequestTimeout : resp.HttpStatus;
						throw new BabelRequestException("Invalid response format", "RESPONSE_FORMAT_ERROR", respStatus, m_baseUrl + "/" + method, resp.GetResponseHeaders().ToString(), resp.GetResponseText(), err);
					}
				}
				else
				{
					return (T)(object)null;
				}
			});
		}

		private Task<BabelWebResponse> MakeHTTPRequestAsyncWithRetry(string httpMethod, string url, byte[] postData, string contentType, string acceptType, int? timeoutInSeconds, NameValueCollection headers, System.Diagnostics.Stopwatch sw, HttpResponseHandlerData eventData, int retriesLeft)
		{
			return MakeHTTPRequestAsync("POST", url, postData, m_contentType, m_acceptType, m_timeoutSeconds, headers).ContinueWith<Task<BabelWebResponse>>((tResponse) => 
			{
				var resp = tResponse.Result;

				if(retriesLeft > 0 && ConsiderRetry(resp))
				{
					if(OnFailure != null)
					{
						sw.Stop();
						eventData.Stage = EventKind.Failure;
						eventData.Response = null;
						eventData.DurationMilliseconds = (int)sw.ElapsedMilliseconds;
						eventData.ResponseHeaders = resp.GetResponseHeaders();
						eventData.Status = resp.HttpStatus.ToString();
						eventData.Error = null;
						OnFailure(eventData);
					}
					return Task.Delay(this.RetryDelay).ContinueWith(t => MakeHTTPRequestAsyncWithRetry("POST", url, postData, m_contentType, m_acceptType, m_timeoutSeconds, headers, sw, eventData, retriesLeft - 1)).Unwrap();
				}

				return tResponse;
			}).Unwrap();
		}

		protected virtual Task<BabelWebResponse> MakeHTTPRequestAsync(string httpMethod, string url, byte[] postData, string contentType, string acceptType, int? timeoutInSeconds, NameValueCollection headers)
		{
			if(string.IsNullOrEmpty(url)) throw new ArgumentNullException("url");

			var request = (HttpWebRequest)WebRequest.Create(url);
			if(headers != null)
			{
				foreach(string key in headers)
				{
					string lck = key.ToLowerInvariant();
					//Don't including restricted headers - they will throw the error. One way would be to write
					switch(lck)
					{
						case "content-length": 
						case "content-type": 
						case "accept":  
						case "connection": 
						case "host":  
						case "user-agent": 
						case "date": 
						case "proxy-connection": 
						case "expect":  
						case "if-modified-since":
						case "range": 
						case "referer": 
						case "transfer-encoding": break;
						default:
							request.Headers.Add(key, Uri.EscapeDataString(headers[key])); break;
					}
				}
			}

			if(!string.IsNullOrEmpty(contentType)) { request.ContentType = contentType; }
			if(!string.IsNullOrEmpty(acceptType)) { request.Accept = acceptType; }
			if(timeoutInSeconds.HasValue && timeoutInSeconds > 0) { request.Timeout = timeoutInSeconds.Value * 1000; }

			request.Method = httpMethod;

			//This fixes a bug where you get a 411 Content Length required if you POST without populating the RequestStream() or setting the content-length.
			if(postData == null || postData.Length == 0)
			{
				request.ContentLength = 0;
			}

			if(postData != null && postData.Length > 0)
			{
				return request.GetRequestStreamAsync().ContinueWith((t) => {
					using(var requestStream = request.GetRequestStream())
					{
						requestStream.Write(postData, 0, postData.Length);
					}
				}).ContinueWith((t) => {
					return GetResponseWithTimeoutAsync(request, url, timeoutInSeconds);
				}).Unwrap();
			}
			else
			{
				return GetResponseWithTimeoutAsync(request, url, timeoutInSeconds);
			}
		}

		private static Task<BabelWebResponse> GetResponseWithTimeoutAsync(HttpWebRequest request, string url, int? timeoutInSeconds)
		{
			var responseTask = request.GetResponseAsync();
			if(timeoutInSeconds.HasValue && timeoutInSeconds.Value > 0)
			{
				var timeoutTask = Task.WhenAny<WebResponse>(responseTask, Task.Delay(timeoutInSeconds.Value * 1000).ContinueWith<WebResponse>((t) => null));
				return timeoutTask.ContinueWith<Task<WebResponse>>((tt) => {
					var timeoutWaitRes = tt.Result;
					if(timeoutWaitRes != responseTask)
					{
						request.Abort();
						//Now need to eat canceled exception
						try
						{
							responseTask.Wait();
						}
						catch { }
						return Task.FromResult<WebResponse>(null);
					}
					else
					{
						return timeoutWaitRes;
					}

				}).Unwrap().ContinueWith((t) => { return GetResponseAsync(t, url); }); ;

			}
			else
			{
				return responseTask.ContinueWith((t) => { return GetResponseAsync(t, url); });
			}
		}

		private static BabelWebResponse GetResponseAsync(Task<WebResponse> timeoutWaitRes, string url)
		{
			HttpWebResponse response = null;
			try
			{
				response = (HttpWebResponse)timeoutWaitRes.Result;
				if(response == null) //Timeout
				{
					string errMessage = "Request to " + url + " timed out";
					return new BabelWebResponse(HttpStatusCode.GatewayTimeout, url, new TimeoutException(errMessage), null, Encoding.ASCII.GetBytes(errMessage));
				}

				using(var receiveStream = response.GetResponseStream())
				{
					byte[] rcvdData = receiveStream.GetBytes();
					return new BabelWebResponse(response.StatusCode, url, null, response.Headers, rcvdData);
				}
			}
			catch(AggregateException aerr)
			{
				var err = aerr.InnerExceptions.Select((e) => e as WebException).FirstOrDefault((e) => e != null);
				if(err == null) throw;
				WebResponse errResponse = err.Response;

				NameValueCollection outHeaders = null;
				if(errResponse != null)
				{
					outHeaders = errResponse.Headers;
				}
				else if(response != null)
				{
					outHeaders = response.Headers;
				}
			
				if(errResponse != null)
				{
					var hwr = errResponse as HttpWebResponse;
					HttpStatusCode statusCode = hwr != null ? hwr.StatusCode : HttpStatusCode.InternalServerError;
					using(var errReceiveStream = errResponse.GetResponseStream())
					{
						byte[] msg = errReceiveStream.GetBytes();
						return new BabelWebResponse(statusCode, url, new HttpRequestException(Encoding.UTF8.GetString(msg), err), outHeaders, msg);
					}
				}
				else
				{
					HttpStatusCode statusCode = response != null ? response.StatusCode : HttpStatusCode.InternalServerError;
					return new BabelWebResponse(statusCode, url, err, outHeaders, Encoding.ASCII.GetBytes(err.Message)); // .. or should it be null instead of the error text
				}
			}
			finally
			{
				if(response != null) response.Close();
			}
		}
		#endregion

		protected sealed class BabelWebResponse
		{
			public BabelWebResponse(HttpStatusCode status, string url, Exception error, NameValueCollection headers, byte[] data)
			{
				m_status = status;
				m_data = data;
				if (error != null)
				{
					m_hasError = true;
					m_error = error;
				}
				m_headers = headers;
				m_url = url;
			}

			readonly HttpStatusCode m_status = 0;
			readonly bool m_hasError;
			readonly Exception m_error;
			readonly string m_url = "";
			readonly NameValueCollection m_headers;
			readonly byte[] m_data;

			public bool HasError
			{
				get { return m_hasError; }
			}

			public NameValueCollection GetResponseHeaders() { return m_headers ?? new NameValueCollection(); }
			public HttpStatusCode HttpStatus { get { return m_status; } }

			public byte[] GetBuffer() { return m_data; }

			public MemoryStream GetStream() { return new MemoryStream(m_data); } //MemoryStream is just a wrapper around input byte array, so it is OK to use this for anything else
			public string GetResponseText() { return Encoding.UTF8.GetString(m_data); }

			/// <summary>
			/// Throws an error that includes the URL, headers, HTTP status and response text 
			/// </summary>
			public BabelRequestException GetError()
			{
				return GetException("Error connecting to", "REQUEST_ERROR", m_error);
			}

			public BabelRequestException GetException(string errorDescription, string errorCode, Exception innerError)
			{
				return new BabelRequestException(errorDescription, errorCode, m_status, m_url, m_headers == null ? "" : m_headers.ToString(), GetResponseText(), innerError);
			}

			/// <summary>
			/// Throws an error if there is one, capturing information about the headers
			/// </summary>
			public void HandleError()
			{
				if (HasError)
				{
					throw GetError();
				}
			}

			/// <summary>
			/// Returns UTF-8 response string
			/// </summary>
			/// <returns></returns>
			public override string ToString()
			{
				return (m_data == null) ? String.Empty : Encoding.UTF8.GetString(m_data);
			}
		}
	}

	[Serializable]
	public class BabelRequestException : BabelException
	{
		protected WebExceptionStatus m_statusCode;
		protected BabelRequestException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
			: base(info, context)
		{
			m_statusCode = (WebExceptionStatus)info.GetInt32("StatusCode");
		}

		public WebExceptionStatus StatusCode { get { return m_statusCode; } }

		static string FormatMesasage(string errorDescription, HttpStatusCode status, string url, string headers, string responseText)
		{
			return string.Format("{4}: {0}\r\n\r\nHttpStatus: {1}\r\n\r\nHeaders: {2}\r\n\r\nHTTP Response: {3}", url, (int)status, headers, responseText, errorDescription);
		}

		internal BabelRequestException(string errorDescription, string errorCode, HttpStatusCode status, string url, string headers, string responseText, Exception innerError)
			: base(FormatMesasage(errorDescription, status, url, headers, responseText), innerError)
		{
			var we = innerError as WebException;
			m_statusCode = (we != null) ? we.Status : WebExceptionStatus.UnknownError;
			Errors.Add(new Error { Code = errorCode, Message = this.Message, Params = new List<string> { url, ((int)status).ToString(), headers, responseText, errorDescription } });
		}


		[System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.LinkDemand, Flags = System.Security.Permissions.SecurityPermissionFlag.SerializationFormatter)]
		public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
		{
			if(info == null)
				throw new ArgumentNullException("info");

			info.AddValue("StatusCode", (int)m_statusCode);
			base.GetObjectData(info, context);
		}

	}
}
