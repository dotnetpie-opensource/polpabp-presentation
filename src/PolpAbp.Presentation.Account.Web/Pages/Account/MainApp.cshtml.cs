using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [Authorize]
    public class MainAppModel : PolpAbpAccountPageModel
    {
        public void OnGet()
        {
        }
    }
}
