using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PolpAbp.Presentation.Account.Web.Filters
{
    public class OnlyAnonymousAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context?.HttpContext?.User != null)
            {
                if (context.HttpContext.User.Identity.IsAuthenticated)
                {
                    var query = context.HttpContext.Request.QueryString;
                    context.Result = new RedirectResult("/Account/Login" + query);
                }
            }
        }
    }
}
