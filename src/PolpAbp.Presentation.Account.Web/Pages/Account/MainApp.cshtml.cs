using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PolpAbp.Framework;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [Authorize]
    public class MainAppModel : PolpAbpAccountPageModel
    {
        public UserAccountOutputDto UserAccountInfo { get; protected set; }

        public MainAppModel() : base()
        {
            UserAccountInfo = new UserAccountOutputDto();
        }


        public virtual async Task<IActionResult> OnGetAsync()
        {
            var mainPage = Configuration["PolpAbp:Account:MainEntry"];
            if (!mainPage.Contains("MainApp"))
            {
                return RedirectToPage(mainPage, new
                {
                    returnUrl = ReturnUrl,
                    returnUrlHash = ReturnUrlHash
                });
            }

            var userInfo = await UserManager.GetUserAsync(User);

            // todo: Use automapper?
            UserAccountInfo.Organization = CurrentTenant.Name;
            UserAccountInfo.Name = userInfo.Name;
            UserAccountInfo.Surname = userInfo.Surname;
            UserAccountInfo.Email = userInfo.Email;

            return Page();
        }

        public class UserAccountOutputDto
        {
            public string? Name { get; set; }
            public string? Surname { get; set; }
            public string? Email { get; set; }

            public string? Organization { get; set; }

            public string FullName => UtitlityExtensions.ComposeFullName(Name, Surname);

        }
    }
}
