using ContosoUniversity.DAL;
using System.Web.Mvc;
using Unity;
using Unity.Lifetime; // Required for PerRequestLifetimeManager
using Unity.Mvc5;

namespace ContosoUniversity
{
    public static class UnityConfig
    {
        public static void RegisterComponents()
        {
            var container = new UnityContainer();

            // Register ISchoolContext to be resolved with SchoolContext
            // Use PerRequestLifetimeManager to ensure a new SchoolContext is created for each HTTP request
            // and disposed of when the request ends.
            container.RegisterType<ISchoolContext, SchoolContext>(new PerRequestLifetimeManager());

            DependencyResolver.SetResolver(new UnityDependencyResolver(container));
        }
    }
}
