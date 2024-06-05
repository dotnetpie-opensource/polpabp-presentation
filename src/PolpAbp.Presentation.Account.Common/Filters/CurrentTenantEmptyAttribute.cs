using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Volo.Abp.MultiTenancy;

// On purpose, we do not include Filter in the namespace.
// So we do not need additional namespace in the pages.
namespace PolpAbp.Presentation.Account
{
    public class CurrentTenantEmptyAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _redirectUrl;

        public CurrentTenantEmptyAttribute(string redirectUrl = "/Account/Login")
        {
            _redirectUrl= redirectUrl;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var currentTenant = context.HttpContext.RequestServices.GetService(typeof(ICurrentTenant)) as ICurrentTenant;

            // todo: Tenant ... 
            if (currentTenant!.IsAvailable)
            {
                context.Result = new RedirectResult(_redirectUrl);
            }

        }
    }
}