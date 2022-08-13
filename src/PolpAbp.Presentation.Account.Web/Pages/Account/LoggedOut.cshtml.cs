using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static PolpAbp.Presentation.Account.Web.Pages.Account.LoginModel;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public class LoggedOutModel : PolpAbpAccountPageModel
    {
        public virtual Task<IActionResult> OnGetAsync()
        {
            return Task.FromResult<IActionResult>(Page());
        }
    }
}
