using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

// On purpose, we do not include Filter in the namespace.
// So we do not need additional namespace in the pages.
namespace PolpAbp.Presentation.Account
{
    public class OnlyAnonymousAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _redirectUrl;

        // We may use DI. However, for the performance person, 
        // we let the caller to provide the input.
        public OnlyAnonymousAttribute(string redirectUrl = "/Account/MainApp") {
            _redirectUrl = redirectUrl;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context?.HttpContext?.User?.Identity != null)
            {
                if (context.HttpContext.User.Identity.IsAuthenticated)
                {
                    var query = context.HttpContext.Request.QueryString;
                    context.Result = new RedirectResult(_redirectUrl + query??"");
                }
            }
        }
    }
}
