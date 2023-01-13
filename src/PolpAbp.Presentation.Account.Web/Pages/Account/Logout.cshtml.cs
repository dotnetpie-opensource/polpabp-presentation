using Microsoft.AspNetCore.Mvc;
using PolpAbp.Framework.Mvc.Interceptors;
using Volo.Abp.Identity;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public class LogoutModel : PolpAbpAccountPageModel
    {
        private readonly ILogoutInterceptor _interceptor;
        
        public LogoutModel(ILogoutInterceptor interceptor)
        {
            _interceptor = interceptor;
        }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Will be able to handle regardless the user is authentictaed or not.
            await IdentitySecurityLogManager.SaveAsync(new IdentitySecurityLogContext()
            {
                Identity = IdentitySecurityLogIdentityConsts.Identity,
                Action = IdentitySecurityLogActionConsts.Logout
            });

            await _interceptor.BeforeSignOutAsync(HttpContext);

            await SignInManager.SignOutAsync();

            await _interceptor.AfterSignOutAsync(HttpContext);
            
            if (!string.IsNullOrEmpty(ReturnUrl))
            {
                return RedirectSafely(ReturnUrl, ReturnUrlHash);
            }

            return RedirectToPage("./LoggedOut");
        }

        public virtual Task<IActionResult> OnPostAsync()
        {
            return Task.FromResult<IActionResult>(Page());
        }
    }
}
