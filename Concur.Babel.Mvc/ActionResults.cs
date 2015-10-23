using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Web.Mvc;

namespace Concur.Babel.Mvc
{
	public abstract class BabelActionResult : ActionResult
	{
		public int HttpStatusCode = 200;
		public abstract string SerializeToString();
		public abstract string ContentType { get; }
		protected readonly object m_objectToSerialize;

		/// <summary>
		/// Creates a new instance of the XmlResult class.
		/// </summary>
		/// <param name="objectToSerialize">The object to serialize to XML.</param>
		public BabelActionResult(object objectToSerialize)
		{
			m_objectToSerialize = objectToSerialize;
		}

		/// <summary>
		/// Serialises the object that was passed into the constructor to JSON and writes the corresponding JSON to the result stream.
		/// </summary>
		/// <param name="context">The controller context for the current request.</param>
		public override void ExecuteResult(ControllerContext context)
		{
			if(m_objectToSerialize != null)
			{
				context.HttpContext.Response.ContentType = ContentType;
				context.HttpContext.Response.CacheControl = "no-cache";
				context.HttpContext.Response.AddHeader("Pragma", "no-cache");
				context.HttpContext.Response.TrySkipIisCustomErrors = true;
				context.HttpContext.Response.Output.Write(SerializeToString());
			}
			context.HttpContext.Response.StatusCode = HttpStatusCode;
		}
	}

	/// <summary>
	/// Action result that serializes the specified object into XML and outputs it to the response stream.
	/// <example>
	/// <![CDATA[
	/// public XmlResult AsXml() {
	///		List<Person> people = _peopleService.GetPeople();
	///		return XmlResult(people);
	/// }
	/// ]]>
	/// </example>
	/// </summary>
	public class BabelXmlResult : BabelActionResult
	{
		/// <summary>
		/// Creates a new instance of the XmlResult class.
		/// </summary>
		/// <param name="objectToSerialize">The object to serialize to XML.</param>
		public BabelXmlResult(object objectToSerialize)
			: base(objectToSerialize)
		{
		}

		/// <summary>
		/// The object to be serialized to XML.
		/// </summary>
		public object ObjectToSerialize
		{
			get { return m_objectToSerialize; }
		}

		private string m_serializationResults;
		public override string SerializeToString()
		{
			if(m_serializationResults == null)
			{
				if(m_objectToSerialize == null)
				{
					m_serializationResults = "";
				}
				else
				{
					m_serializationResults = Encoding.UTF8.GetString(m_objectToSerialize.ToXmlBytes());
				}
			}
			return m_serializationResults;
		}

		public override string ContentType { get { return "text/xml"; } }
	}


	/// <summary>
	/// Action result that serializes the specified object into JSON and outputs it to the response stream using ISO date.
	/// </summary>
	public class BabelJsonResult : BabelActionResult
	{
		/// <summary>
		/// Creates a new instance of the CQJsonResult class.
		/// </summary>
		/// <param name="objectToSerialize">The object to serialize to JSON.</param>
		public BabelJsonResult(object objectToSerialize) : base(objectToSerialize)
		{
		}

		/// <summary>
		/// The object to be serialized to JSON.
		/// </summary>
		public object ObjectToSerialize
		{
			get { return m_objectToSerialize; }
		}


		private static BabelJsonSerializer s_serializer = new BabelJsonSerializer();
		
		private string m_serializationResults;
		public override string SerializeToString()
		{
			if(m_serializationResults == null)
			{
				if(m_objectToSerialize == null)
				{
					m_serializationResults = "";
				}
				else
				{
					using(var strm = (MemoryStream)s_serializer.Serialize(m_objectToSerialize))
					{
						m_serializationResults = (new UTF8Encoding(false)).GetString(strm.ToArray());
					}
				}
			}
			return m_serializationResults;
		}

		public override string ContentType { get { return "application/json"; } }
	}
}
