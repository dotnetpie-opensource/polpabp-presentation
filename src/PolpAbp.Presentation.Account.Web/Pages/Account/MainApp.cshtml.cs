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
            await LoadSettingsAsync();

            var mainPage = Configuration["PolpAbp:Account:MainEntry"];
            if (!mainPage.Contains("MainApp"))
            {
                return RedirectToPage(mainPage, new
                {
                    returnUrl = ReturnUrl,
                    returnUrlHash = ReturnUrlHash
                });
            }

            await BuildOutputModelsAsync();

            return Page();
        }

        protected async Task BuildOutputModelsAsync()
        {
            var userInfo = await UserManager.GetUserAsync(User);

            // todo: Use automapper?
            UserAccountInfo.TenantId = userInfo.TenantId;
            UserAccountInfo.TenantName = CurrentTenant.Name;
            UserAccountInfo.Name = userInfo.Name;
            UserAccountInfo.Surname = userInfo.Surname;
            UserAccountInfo.Email = userInfo.Email;
            UserAccountInfo.UserName = userInfo.UserName;
        }

        public class UserAccountOutputDto
        {
            public string? Name { get; set; }
            public string? Surname { get; set; }
            public string? Email { get; set; }
            public string? UserName { get; set; }
            public Guid? TenantId { get; set; }    

            public string? TenantName { get; set; }

            public string FullName => UtitlityExtensions.ComposeFullName(Name, Surname);

        }
    }
}
