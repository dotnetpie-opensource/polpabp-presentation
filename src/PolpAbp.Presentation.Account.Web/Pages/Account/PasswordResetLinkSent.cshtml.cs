using Microsoft.AspNetCore.Mvc;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public class PasswordResetLinkSentModel : PolpAbpAccountPageModel
    {
        public virtual Task<IActionResult> OnGetAsync()
        {
            return Task.FromResult<IActionResult>(Page());
        }

        public virtual Task<IActionResult> OnPostAsync(string action)
        {
            if (action == "Cancel")
            {
                // Need to reload the page.
                return Task.FromResult<IActionResult>(RedirectToPage("./Login", new
                {
                    ReturnUrl = ReturnUrl,
                    ReturnUrlHash = ReturnUrlHash
                }));
            }

            return Task.FromResult<IActionResult>(Page());
        }
    }
}
