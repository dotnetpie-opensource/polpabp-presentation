using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

// On purpose, we do not include Filter in the namespace.
// So we do not need additional namespace in the pages.
namespace PolpAbp.Presentation.Account.Web
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
                    // todo: ? main or MainApp ???
                    context.Result = new RedirectResult("/main");
                }
            }
        }
    }
}
