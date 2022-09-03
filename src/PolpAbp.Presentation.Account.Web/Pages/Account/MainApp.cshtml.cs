using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [Authorize]
    public class MainAppModel : PolpAbpAccountPageModel
    {
        public UserAccountOutputDto UserAccountInfo { get; protected set; }


        public virtual async Task<IActionResult> OnGetAsync()
        {
            var userInfo = await UserManager.GetUserAsync(User);

            UserAccountInfo = new UserAccountOutputDto
            {
                Organization = CurrentTenant.Name,
                Name = userInfo.Name,
                Surname = userInfo.Surname,
                Email = userInfo.Email
            };

            return Page();
        }

        public class UserAccountOutputDto
        {
            public string Name { get; set; }
            public string Surname { get; set; }
            public string Email { get; set; }

            public string Organization { get; set; }

            public string FullName => string.IsNullOrEmpty(Name) ? Surname : $"{Name} {Surname}";

        }
    }
}
