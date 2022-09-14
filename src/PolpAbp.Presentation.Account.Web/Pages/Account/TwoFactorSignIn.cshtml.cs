using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PolpAbp.Framework;
using PolpAbp.Framework.Emailing.Account;
using PolpAbp.Presentation.Account.Web.Settings;
using Scriban;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Volo.Abp.Auditing;
using Volo.Abp.Emailing;
using Volo.Abp.Identity;
using Volo.Abp.Settings;
using Volo.Abp.TenantManagement;
using Volo.Abp.TextTemplating;
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

        public List<TwoFactorProvider> TwoFactorCodeProviders { get; set; }

        protected readonly IFrameworkAccountEmailer AccountEmailer;


        public TwoFactorSignInModel(IFrameworkAccountEmailer accountEmailer) : base()
        {
            AccountEmailer = accountEmailer;

            Input = new PostInput();
            RememberMe = false;
            TwoFactorCodeProviders = new List<TwoFactorProvider>();
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

                    var provider = TempData.Peek("PolpAbp.Account.TwoFactorCode.Provider") as string;

                    // todo: Provider is wrong.

                    var ret = await SignInManager.TwoFactorSignInAsync(provider, Input.Code, Input.RememberMe, Input.RememberClient);
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
            else if (action == "SendCodeByEmail")
            {
                var userId = await RetrieveTwoFactorUserIdAsync();
                if (userId.HasValue)
                {
                    var user = await UserManager.FindByIdAsync(userId.ToString());
                    var token = await UserManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);

                    await AccountEmailer.SendTwoFactorCodeAsync(user.Id, token);

                    TempData["PolpAbp.Account.TwoFactorCode.Provider"] = TokenOptions.DefaultEmailProvider;

                    Alerts.Success(L["TwoFactorCode_SentSuccess"].Value);
                }
            }
            else if (action == "SendCodeByPhone")
            {
                var userId = await RetrieveTwoFactorUserIdAsync();
                if (userId.HasValue)
                {
                    var user = await UserManager.FindByIdAsync(userId.ToString());
                    var token = await UserManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider);

                    TempData["PolpAbp.Account.TwoFactorCode.Provider"] = TokenOptions.DefaultPhoneProvider;

                    // todo: Send via phone
                    await AccountEmailer.SendTwoFactorCodeAsync(user.Id, token);

                    Alerts.Success(L["TwoFactorCode_SentSuccess"].Value);
                }
            }

            return Page();
        }

        protected override async Task LoadSettingsAsync()
        {
            await base.LoadSettingsAsync();
            // todo: Load more providers 
            TwoFactorCodeProviders.Clear();

            var userId = await RetrieveTwoFactorUserIdAsync();
            var user = await UserManager.FindByIdAsync(userId.ToString());
            if (user != null) {

                var providers = await UserManager.GetValidTwoFactorProvidersAsync(user);
                foreach (var p in providers)
                {
                    if (string.Equals(p, TokenOptions.DefaultEmailProvider, StringComparison.CurrentCultureIgnoreCase))
                    {
                        TwoFactorCodeProviders.Add(new TwoFactorProvider
                        {
                            Name = p,
                            Action = "SendCodeByEmail",
                            Display = "Send to " + user.NormalizedEmail.MaskEmailAddress()
                        });
                    }
                    else if (string.Equals(p, TokenOptions.DefaultPhoneProvider, StringComparison.CurrentCultureIgnoreCase))
                    {
                        TwoFactorCodeProviders.Add(new TwoFactorProvider
                        {
                            Name = p,
                            Action = "SendCodeByPhone",
                            Display = "Send to " + user.PhoneNumber.MaskPhoneNumber()
                        });
                    }
                }
            }
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

        public class TwoFactorProvider
        {
            public string Name { get; set; }
            public string Action { get; set; }
            public string Display { get; set; }
        }
    }
}
