using Microsoft.AspNetCore.Mvc;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public class PasswordResetLinkSentModel : PolpAbpAccountPageModel
    {
        public virtual Task<IActionResult> OnGetAsync()
        {
            return Task.FromResult<IActionResult>(Page());
        }
    }
}
