using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [TenantPrerequisite]
    public class LoginModel : PolpAbpAccountPageModel
    {
        public void OnGet()
        {
        }
    }
}
