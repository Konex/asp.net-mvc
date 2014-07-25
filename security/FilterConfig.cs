using System.Web.Mvc;
using vDieu.Web.Filters;

namespace vDieu.Web
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());

            // for all requests
            filters.Add(new RequireHttpsAttribute());
        }

        public static void RegisterWebApiFilters(System.Web.Http.Filters.HttpFilterCollection filters)
        {
	    // only for web api controller actions
            filters.Add(new WebApiRequireHttpsAttribute());
            filters.Add(new TokenValidationAttribute());
        }
    }
}
