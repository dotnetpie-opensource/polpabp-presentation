using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PolpAbp.Presentation.Account.Web.Settings;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Volo.Abp.Auditing;
using Volo.Abp.Identity;
using Volo.Abp.Settings;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [OnlyAnonymous]
    [TenantPrerequisite]
    public class TwoFactorSignInModel : LoginModelBase
    {
        [BindProperty(SupportsGet = true)]
        public bool RememberMe { get; set; }

        [BindProperty]
        public PostInput Input { get; set; }

        public List<string> TwoFactorCodeProviders { get; set; }

        public TwoFactorSignInModel() : base()
        {
            Input = new PostInput();
            RememberMe = false;
            TwoFactorCodeProviders = new List<string>();
        }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Load settings
            await LoadSettingsAsync();

            return Page();
        }

        public virtual async Task<IActionResult> OnPostAsync(string action)
        {
            // Load settings
            await LoadSettingsAsync();

            if (action == "Input")
            {
                try
                {
                    ValidateModel();

                    var ret = await SignInManager.TwoFactorSignInAsync(null, Input.Code, Input.RememberMe, Input.RememberClient);
                    if (ret.Succeeded)
                    {

                        // todo: Make MainApp configurable ...
                        return RedirectToPage("./MainApp", new
                        {
                            returnUrl = ReturnUrl,
                            returnUrlHash = ReturnUrlHash
                        });
                    }
                }
                catch (AbpValidationException ex)
                {
                    // Handle this error.
                    foreach (var a in ex.ValidationErrors)
                    {
                        Alerts.Add(Volo.Abp.AspNetCore.Mvc.UI.Alerts.AlertType.Danger, a.ErrorMessage);
                    }
                }
            } 
            else if (action == "EmailCode")
            {
                var userId = await RetrieveTwoFactorUserIdAsync();
                if (userId.HasValue)
                {
                    var user = await UserManager.FindByIdAsync(userId.ToString());
                    // await UserManager.GenerateTwoFactorTokenAsync(user);
                }
            }

            return Page();
        }

        protected override async Task LoadSettingsAsync()
        {
            await base.LoadSettingsAsync();
            // todo: Load more providers 
            TwoFactorCodeProviders.Clear();
            TwoFactorCodeProviders.Add("email");
        }


        private async Task<Guid?> RetrieveTwoFactorUserIdAsync()
        {
            var result = await HttpContext.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme);
            if (result?.Principal != null)
            {
                var userId = result.Principal.FindFirstValue(ClaimTypes.Name);
                if (Guid.TryParse(userId, out var id))
                {
                    return id;
                }
            }

            return null;
        }

        public class PostInput
        {
            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPasswordLength))]
            [DataType(DataType.Password)]
            [DisableAuditing]
            public string Code { get; set; }

            // Remember this client.
            public bool RememberClient { get; set; }

            // todo: Get this value from the previous step.
            // Remember this cookie across the session.
            // Still useful after the browser is closed 
            public bool RememberMe { get; set; }
        }
    }
}
