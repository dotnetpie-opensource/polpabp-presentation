using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;
using Volo.Abp.MultiTenancy;

// On purpose, we do not include Filter in the namespace.
// So we do not need additional namespace in the pages.
namespace PolpAbp.Presentation.Account
{
    public class TenantPrerequisiteAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _redirectUrl;

        public TenantPrerequisiteAttribute(string redirectUrl = "/Account/SelectOrganization")
        {
            _redirectUrl = redirectUrl;
        }   

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var currentTenant = context.HttpContext.RequestServices.GetService(typeof(ICurrentTenant)) as ICurrentTenant;

            // todo: Tenant ... 
            if (!currentTenant!.IsAvailable)
            {
                // Encode 
                var retUrl = context.HttpContext.Request.GetEncodedUrl();
                var dstUrl = QueryHelpers.AddQueryString(_redirectUrl, "ReturnUrl", retUrl);
                context.Result = new RedirectResult(dstUrl);
            }

        }
    }
}