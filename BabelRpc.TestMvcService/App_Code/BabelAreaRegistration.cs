using System.Web.Mvc;

namespace BabelRpc.Demo
{
	public class BabelAreaRegistration : AreaRegistration
	{
		public override string AreaName
		{
			get
			{
				return "babel";
			}
		}

		public override void RegisterArea(AreaRegistrationContext context)
		{
			context.MapRoute("babel", "Babel/{controller}/{action}", new { action = "GetLoggingStatus" });
		}
	}
}
