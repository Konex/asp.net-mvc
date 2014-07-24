using System.Web.Http;
using System.Web.Mvc;
using Microsoft.Practices.Unity;
using Unity.Mvc5;
using vDieu.Dal;
using vDieu.Dal.Contracts;
using vDieu.Service;
using WebService.Service;
using WebService.Service.contracts;

namespace vDieu.Web
{
  public static class Bootstrapper
  {
    public static IUnityContainer Initialise()
    {
      var container = BuildUnityContainer();

      DependencyResolver.SetResolver(new UnityDependencyResolver(container));
      GlobalConfiguration.Configuration.DependencyResolver = new Unity.WebApi.UnityDependencyResolver(container);

      return container;
    }

    private static IUnityContainer BuildUnityContainer()
    {
      var container = new UnityContainer();

      // register all your components with the container here
      // it is NOT necessary to register your controllers

      // e.g. container.RegisterType<ITestService, TestService>();    
      RegisterTypes(container);

      return container;
    }

    public static void RegisterTypes(IUnityContainer container)
    {
        container.RegisterInstance(GlobalConfiguration.Configuration);

        container.RegisterType<IMyService, MyService>();   
        container.RegisterType<IUnitOfWork, UnitOfWork>(new PerRequestLifetimeManager());
        container.RegisterType<IDbContext, vDbContext>(new PerRequestLifetimeManager());
    }
  }
}