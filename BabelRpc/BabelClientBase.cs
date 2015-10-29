using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;

namespace BabelRpc
{
	public interface IResponseHandlerData
	{
		/// <summary>
		/// Stage of the request sending 
		/// </summary>
		EventKind Stage { get; }

		/// <summary>
		/// Full URL to the resource
		/// </summary>
		string Url { get; }

		/// <summary>
		/// Remote method name
		/// </summary>
		string Method { get; }

		/// <summary>
		/// Request data
		/// </summary>
		byte[] Request { get; }

		/// <summary>
		/// Response data
		/// </summary>
		byte[] Response { get; }

		/// <summary>
		/// Request headers
		/// </summary>
		NameValueCollection RequestHeaders { get; }

		/// <summary>
		/// Response headers
		/// </summary>
		NameValueCollection ResponseHeaders { get; }

		/// <summary>
		/// Protocol specific status code
		/// </summary>
		string Status { get; }

		/// <summary>
		/// Babel exception - only set by OnFailure event 
		/// </summary>
		BabelException Error { get; }

		/// <summary>
		/// Duration of a current stage:
		/// BeforeSend - serialization time
		/// OnConplet and OnFailure - request time
		/// </summary>
		double DurationMilliseconds { get; }
	}


	public delegate void ResponseHandler(IResponseHandlerData data);

	public enum EventKind { Start, Complete, Failure }
	public delegate void OnEventHandler(string eventName, EventKind stage, string parm);

	/// <summary>
	/// Defines babel transport. The implementation should be thread safe
	/// </summary>
	public interface IBabelTransport
	{
		T GetResponse<T>(string method, object data, NameValueCollection debugInfo);
		void Send(string method, object data, NameValueCollection debugInfo);

		Task<T> GetResponseAsync<T>(string method, object data, NameValueCollection debugInfo);
		Task SendAsync(string method, object data, NameValueCollection debugInfo);

		/// <summary>
		/// How many times the system should retry if the transport to the service is unavailable
		/// </summary>
		int RetryCount { get; set; }

		/// <summary>
		/// How long to wait between retries
		/// </summary>
		TimeSpan RetryDelay { get; set; }

		event ResponseHandler OnStart;
		/// <summary>
		/// Indicates that there was a failure to transport the payload, will occur on every retry if retries are enabled.
		/// </summary>
		event ResponseHandler OnFailure;
		/// <summary>
		/// Indicates the transport was successful
		/// </summary>
		event ResponseHandler OnComplete;
	}

	/// <summary>
	/// Defines [de]serializer for Babel models. Implementation should be thread safe 
	/// </summary>
	public interface IBabelSerializer
	{
		T Deserialize<T>(Stream str);
		Stream Serialize(object data);
	}
			
	/// <summary>
	/// Base class for Babel client implementation. Thread safe.
	/// </summary>
	public abstract class BabelClientBase
	{
		public BabelClientBase(IBabelTransport transport)
		{
			if(transport == null) throw new ArgumentNullException("transport");
			m_transport = transport;
			m_proxyName = this.GetType().Name;
		}

		protected IBabelTransport m_transport;
		protected string m_proxyName;

		public event ResponseHandler OnComplete
		{
			add { m_transport.OnComplete += value; }
			remove { m_transport.OnComplete -= value; }
		}

		public event ResponseHandler OnFailure
		{
			add { m_transport.OnFailure += value; }
			remove { m_transport.OnFailure -= value; }
		}

		public event ResponseHandler OnStart
		{
			add { m_transport.OnStart += value; }
			remove { m_transport.OnStart -= value; }
		}

		public int RetryCount { get { return m_transport.RetryCount; } set { m_transport.RetryCount = value; } }

		public TimeSpan RetryDelay { get { return m_transport.RetryDelay; } set { m_transport.RetryDelay = value; } }

		/// <summary>
		/// Collection to store optional context info
		/// </summary>
		public readonly System.Collections.Specialized.NameValueCollection Headers = new System.Collections.Specialized.NameValueCollection();
					
		/// <summary>
		/// Makes synchronous HTTP request and deserializes the received stream using XML
		/// </summary>
		/// <param name="method">Web service method name</param>
		/// <param name="data">Method parameters as a class</param>
		protected T MakeRequestAndDeserialize<T>(string method, object data)
		{
			if (string.IsNullOrEmpty(method)) throw new ArgumentNullException("method");
			return m_transport.GetResponse<T>(method, data, Headers);
		}

		/// <summary>
		/// Makes synchronous HTTP request and deserializes the received stream using XML
		/// </summary>
		/// <param name="method">Web service method name</param>
		/// <param name="data">Method parameters as a class</param>
		protected Task<T> MakeRequestAndDeserializeAsync<T>(string method, object data)
		{
			if(string.IsNullOrEmpty(method)) throw new ArgumentNullException("method");
			return m_transport.GetResponseAsync<T>(method, data, Headers);
		}

		protected void Send(string method, object data)
		{
			if(string.IsNullOrEmpty(method)) throw new ArgumentNullException("method");
			m_transport.Send(method, data, Headers);
		}

		protected Task SendAsync(string method, object data)
		{
			if(string.IsNullOrEmpty(method)) throw new ArgumentNullException("method");
			return m_transport.SendAsync(method, data, Headers);
		}
	}
}
