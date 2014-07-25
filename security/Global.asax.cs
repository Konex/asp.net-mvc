using System.Data.Entity;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using FluentValidation.Mvc;
using log4net;

namespace vDieu.Web
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : HttpApplication
    {
        private static readonly ILog log = LogManager.GetLogger("MvcApplication");
        protected void Application_Start()
        {
            log4net.Config.XmlConfigurator.Configure();
            log.Info("Application Starting...");
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<Dal.vDbContext, Dal.Migrations.Configuration>());
            // lines below set off database initialization immediately.
            // Or it would be initialized upon first db access.
            using (var context = new Dal.vDbContext())
            {
                context.Database.Initialize(false);
            }

            AreaRegistration.RegisterAllAreas();
            
            FilterConfig.RegisterWebApiFilters(GlobalConfiguration.Configuration.Filters);
            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            Bootstrapper.Initialise();
            AuthConfig.RegisterAuth();

            Task.Factory.StartNew(() =>
            {
                AutomapperConfigWeb.Initialize();
                FluentValidationModelValidatorProvider.Configure();
            });
            log.Info("Application Started");
        }

        void Application_Error(HttpApplication sender, System.EventArgs e)
        {
            var error = sender.Server.GetLastError();
            if (error != null)
            {
                log.Error("Application Error", error);
            }
        }
    }
}