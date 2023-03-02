using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public class ResetPasswordSuccessModel : PolpAbpAccountPageModel
    {
        public virtual async Task<IActionResult> OnGetAsync()
        {
            await LoadSettingsAsync();

            return Page();
        }
    }
}
