using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Async;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using System.Linq;

using Concur.Babel.Models;

namespace Concur.Babel.Mvc
{
	public interface IBabelRequest : IBabelModel
	{
		void SetDefaults();
	}

	public abstract class BabelController<T> : BabelController
	{
		protected T m_businessLogic;
		protected abstract T InitBusinessLogic();
		protected override void CustomInitializer(ControllerContext controllerContext, string actionName)
		{
			base.CustomInitializer(controllerContext, actionName);

			m_businessLogic = InitBusinessLogic();
						
			if(m_businessLogic == null)
			{
				throw new InvalidOperationException("InitBusinessLogic should return initialized business logic object. Instead null was returned");
			}
		}
	}

	public abstract class BabelController : Controller
	{
		public BabelController()
		{
			ActionInvoker = new BabelActionInvoker(() => {
				string actionName = RouteData.GetRequiredString("action");
				CustomInitializer(ControllerContext, actionName);
			});
		}

		/// <summary>
		/// This method will be called before invoking an action method.
		/// </summary>
		/// <param name="controllerContext">Controller context</param>
		/// <param name="actionName"></param>
		protected virtual void CustomInitializer(ControllerContext controllerContext, string actionName)
		{
			
		}

		protected virtual string LogError(Exception error)
		{
			return null;
		}

		/// <summary>
		/// Returns decoded header by name
		/// </summary>
		/// <param name="name">Header name</param>
		/// <returns>Decoded header value</returns>
		protected string GetHeader(string name)
		{
			string h = Request.Headers[name];
			if (string.IsNullOrEmpty(h)) return h;
			return Uri.UnescapeDataString(h);
		}

		/// <summary>
		/// Translates error to the Babel error format
		/// </summary>
		/// <param name="error">Exception to translate</param>
		/// <param name="errorKind">Output type of error</param>
		/// <returns></returns>
		protected virtual ServiceError TranslateError(Exception error, out ErrorKind errorKind)
		{
			return ServiceErrorHelper.FromException(error, out errorKind);
		}

		/// <summary>
		/// Serailizes excetion using TranslateError mapping
		/// </summary>
		/// <param name="error"></param>
		/// <param name="headers"></param>
		/// <returns></returns>
		public BabelActionResult SerializeError(Exception error, System.Collections.Specialized.NameValueCollection headers)
		{
			string logId = LogError(error);

			ErrorKind kind;
			var serviceError = TranslateError(error, out kind);
			if(!string.IsNullOrEmpty(logId)) serviceError.AddContext("Logging", "LogId", logId);
			var resp =  ControllerHelper.AutoSerializeResponse (serviceError, headers);

			switch(kind)
			{
				//case ErrorKind.InvalidArgument: resp.HttpStatusCode = 400; break;
				case ErrorKind.InvalidRequest: resp.HttpStatusCode = 400; break;
				default: resp.HttpStatusCode = 500; break;
			}
			return resp;
		}

		private Stream m_inputStream;

		/// <summary>
		/// Rewindable input stream
		/// </summary>
		public Stream InputStream
		{
			set
			{
				m_inputStream = value;
			}
			get
			{
				//That is for testing when no Execute... methods are called, so no InputStream assigned
				return m_inputStream ?? ControllerContext.HttpContext.Request.InputStream;
			}
		}

		protected void SetInputStreamFromContext()
		{
			InputStream = new MemoryStream(ControllerContext.HttpContext.Request.InputStream.GetBytes());
		}

		static BabelJsonSerializer s_jsonSerializer = new BabelJsonSerializer();
		static BabelXmlSerializer s_xmlSerializer = new BabelXmlSerializer();
		protected virtual TR DeserializeRequest<TR>()  where TR : IBabelRequest
		{
			TR res; 
			string contentType = Request.Headers["Content-Type"];
			if(!string.IsNullOrWhiteSpace(contentType))
			{
				if(contentType.ToLowerInvariant().Contains("xml"))
				{
					res = s_xmlSerializer.Deserialize<TR>(InputStream);
				}
				else
				{
					res = s_jsonSerializer.Deserialize<TR>(InputStream);
				}
			}
			else 
			{
				res = s_jsonSerializer.Deserialize<TR>(InputStream);
			}
			if(res == null) throw new BabelApplicationException("Invalid input data format");

			res.SetDefaults();

			return res;
		}
	}

	public class BabelExceptionFilter : IExceptionFilter
	{

		#region IExceptionFilter Members

		public void OnException(ExceptionContext filterContext)
		{
			if(filterContext.Exception != null && !filterContext.ExceptionHandled)
			{
				if(filterContext.RequestContext != null && filterContext.RequestContext.HttpContext != null && filterContext.RequestContext.HttpContext.Request != null)
				{
					var headers = filterContext.RequestContext.HttpContext.Request.Headers;
					var controller = filterContext.Controller as BabelController;
					filterContext.Result = controller.SerializeError(filterContext.Exception, headers);
					filterContext.ExceptionHandled = true;
					//filterContext.Exception = null;
				}
			}
		}

		#endregion
	}

	public class BabelActionInvoker : AsyncControllerActionInvoker
	{
		readonly Action m_customAction;

		public BabelActionInvoker(Action customInitializerAction)
		{
			this.m_customAction = customInitializerAction;
			
		}

		protected override IAsyncResult BeginInvokeActionMethodWithFilters(ControllerContext controllerContext, IList<IActionFilter> filters, ActionDescriptor actionDescriptor, IDictionary<string, object> parameters, AsyncCallback callback, object state)
		{
			if(m_customAction != null)
			{
				m_customAction();
			}
			return base.BeginInvokeActionMethodWithFilters(controllerContext, filters, actionDescriptor, parameters, callback, state);
		}

		protected override ActionExecutedContext InvokeActionMethodWithFilters(ControllerContext controllerContext, IList<IActionFilter> filters, ActionDescriptor actionDescriptor, IDictionary<string, object> parameters)
		{
			if(m_customAction != null)
			{
				m_customAction();
			}
			return base.InvokeActionMethodWithFilters(controllerContext, filters, actionDescriptor, parameters);
		}

		protected override FilterInfo GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
		{
			var fi = base.GetFilters(controllerContext, actionDescriptor);
			fi.ExceptionFilters.Add(new BabelExceptionFilter());
			return fi;
		}
		protected override ActionResult CreateActionResult(ControllerContext controllerContext, ActionDescriptor actionDescriptor, object actionReturnValue)
		{
			if(!(actionReturnValue is ActionResult))
			{
				if(controllerContext.RequestContext != null && controllerContext.RequestContext.HttpContext != null && controllerContext.RequestContext.HttpContext.Request != null)
				{
					var headers = controllerContext.RequestContext.HttpContext.Request.Headers;
					actionReturnValue = ControllerHelper.AutoSerializeResponse(actionReturnValue, headers);
				}
			}
			return base.CreateActionResult(controllerContext, actionDescriptor, actionReturnValue);
		}

		protected override ExceptionContext InvokeExceptionFilters(ControllerContext controllerContext, IList<IExceptionFilter> filters, Exception exception)
		{
			return base.InvokeExceptionFilters(controllerContext, filters, exception);
		}
	}

}
