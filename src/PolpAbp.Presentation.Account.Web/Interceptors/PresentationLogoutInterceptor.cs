using PolpAbp.Framework.Mvc.Cookies;
using PolpAbp.Framework.Mvc.Interceptors;
using Volo.Abp.DependencyInjection;

namespace PolpAbp.Presentation.Account.Web.Interceptors
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(ILogoutInterceptor))]
    public class PresentationLogoutInterceptor : ILogoutInterceptor, ITransientDependency
    {
        private readonly IAppCookieManager _cookieManager;

        public PresentationLogoutInterceptor(IAppCookieManager cookieManager)
        {
            _cookieManager = cookieManager;
        }

        public Task AfterSignOutAsync(HttpContext httpContext)
        {
            // Clean cookies. 
            _cookieManager.ClearTenantCookie(httpContext.Response);
            return Task.CompletedTask;
        }

        public Task BeforeSignOutAsync(HttpContext httpContext)
        {
            return Task.CompletedTask;
        }
    }
}
