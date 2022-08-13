using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp.Identity;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public class LogoutModel : PolpAbpAccountPageModel
    {
        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Will be able to handle regardless the user is authentictaed or not.
            await IdentitySecurityLogManager.SaveAsync(new IdentitySecurityLogContext()
            {
                Identity = IdentitySecurityLogIdentityConsts.Identity,
                Action = IdentitySecurityLogActionConsts.Logout
            });

            await SignInManager.SignOutAsync();
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
