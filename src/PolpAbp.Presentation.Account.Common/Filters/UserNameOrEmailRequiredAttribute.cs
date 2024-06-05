using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

// On purpose, we do not include Filter in the namespace.
// So we do not need additional namespace in the pages.
namespace PolpAbp.Presentation.Account
{
    public class UserNameOrEmailRequiredAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _redirectUrl;

        // We may use DI. However, for the performance person, 
        // we let the caller to provide the input.
        public UserNameOrEmailRequiredAttribute(string redirectUrl = "/Account/Login") {
            _redirectUrl = redirectUrl;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userName = context.HttpContext.Request.Query["UserName"];
            if (string.IsNullOrEmpty(userName))
            {
                var email = context.HttpContext.Request.Query["EmailAddress"];
                if (string.IsNullOrEmpty(email))
                {
                    var originalQueryString = context.HttpContext.Request.QueryString;
                    context.Result = new RedirectResult(_redirectUrl + originalQueryString ?? "");
                }
            }
        }
    }
}
