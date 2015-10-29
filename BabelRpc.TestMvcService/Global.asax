<%@ Application Language="C#" %>

<%@ Import Namespace="System.Web" %>
<%@ Import Namespace="System.Web.Mvc" %>
<%@ Import Namespace="System.Web.Routing" %>

<script RunAt="server">

	public static void RegisterGlobalFilters(GlobalFilterCollection filters)
	{
		filters.Add(new HandleErrorAttribute());
	}

	public static void RegisterRoutes(RouteCollection routes)
	{
		routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

		routes.MapRoute(
			"Default", // Route name
			"{controller}/{action}/{id}", // URL with parameters
			new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
		);

	}

	protected void Application_Start()
	{
		AreaRegistration.RegisterAllAreas();
				
		RegisterGlobalFilters(GlobalFilters.Filters);
		RegisterRoutes(RouteTable.Routes);

		//http://haacked.com/archive/2008/03/13/url-routing-debugger.aspx
		//RouteDebug.RouteDebugger.RewriteRoutesForTesting(RouteTable.Routes);
	}

	
	protected void Application_Error(Object sender, EventArgs e)
	{

	}

	protected void Application_End(Object sender, EventArgs e)
	{
	}
</script>

