using Microsoft.AspNetCore.Mvc;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public class ResetPasswordConfirmationModel : PolpAbpAccountPageModel
    {
        public virtual Task<IActionResult> OnGetAsync()
        {
            ReturnUrl = GetRedirectUrl(ReturnUrl, ReturnUrlHash);

            return Task.FromResult<IActionResult>(Page());
        }
    }
}
