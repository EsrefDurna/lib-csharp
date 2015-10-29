using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace BabelRpcTestMvcService
{
	public class HomeController : Controller
	{
		public ActionResult Index()
		{
			return new ContentResult { Content = "BabelRpcTestMvcService Home Page", ContentEncoding = Encoding.UTF8, ContentType = "text/plain" };
		}

		public ActionResult LogControl(string id)
		{
			return new ContentResult { Content = "Wrong result", ContentEncoding = Encoding.UTF8, ContentType = "text/plain" };
		}
	}
}
