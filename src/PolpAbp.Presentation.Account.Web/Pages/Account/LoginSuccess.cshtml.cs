using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [Authorize]
    public class LoginSuccessModel : PolpAbpAccountPageModel
    {
        public bool IsCordovaEnabled { get; set; }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            await LoadSettingsAsync();
            return Page();
        }

        protected override async Task LoadSettingsAsync()
        {
            await base.LoadSettingsAsync();

            IsCordovaEnabled = Configuration.GetValue<bool>("PolpAbp:Framework:IsCordovaEnabled");
        }
    }
}
