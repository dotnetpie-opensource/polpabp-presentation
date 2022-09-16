using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
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
using Volo.Abp.Sms;
using Volo.Abp.TenantManagement;
using Volo.Abp.TextTemplating;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [OnlyAnonymous]
    [TenantPrerequisite]
    public class TwoFactorRequestModel : LoginModelBase
    {
        [BindProperty(SupportsGet = true)]
        public bool RememberMe { get; set; }

        public List<TwoFactorProvider> TwoFactorCodeProviders { get; set; }

        protected readonly IFrameworkAccountEmailer AccountEmailer;
        protected readonly ISmsSender SmsSender;

        protected string SmsSenderName
        {
            get
            {
                return Configuration.GetValue<string>("PolpAbp:Framework:SmsSenderName", string.Empty);
            }
        }

        public TwoFactorRequestModel(IFrameworkAccountEmailer accountEmailer,
            ISmsSender smsSender) : base()
        {
            AccountEmailer = accountEmailer;
            SmsSender = smsSender;

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

            if (action == "SendCodeByEmail")
            {
                var userId = await RetrieveTwoFactorUserIdAsync();
                if (userId.HasValue)
                {
                    var user = await UserManager.FindByIdAsync(userId.ToString());
                    var token = await UserManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);

                    await AccountEmailer.SendTwoFactorCodeAsync(user.Id, token);

                    TempData["PolpAbp.Account.TwoFactorCode.Provider"] = TokenOptions.DefaultEmailProvider;

                    Alerts.Success(L["TwoFactorCode_SentSuccess"].Value);

                    return RedirectToPage("./TwoFactorSignIn", new
                    {
                        returnUrl = ReturnUrl,
                        returnUrlHash = ReturnUrlHash,
                        RememberMe = RememberMe
                    });
                }
            }
            else if (action == "SendCodeByPhone")
            {
                var userId = await RetrieveTwoFactorUserIdAsync();
                if (userId.HasValue)
                {
                    var user = await UserManager.FindByIdAsync(userId.ToString());
                    var token = await UserManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider);

                    var body = L["TwoFactorCode_Sms", SmsSenderName, token];
                    TempData["PolpAbp.Account.TwoFactorCode.Provider"] = TokenOptions.DefaultPhoneProvider;

                    // todo: Check if the organization configuration.
                    // todo: Settings
                    // var allowedCountry =
                    var msg = new SmsMessage(user.PhoneNumber, body);
                    msg.Properties.Add("CountryCode", 236);
                    // todo: Send via phone
                    await SmsSender.SendAsync(msg);

                    Alerts.Success(L["TwoFactorCode_SentSuccess"].Value);

                    return RedirectToPage("./TwoFactorSignIn", new
                    {
                        returnUrl = ReturnUrl,
                        returnUrlHash = ReturnUrlHash,
                        RememberMe = RememberMe
                    });
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
            if (user != null)
            {

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

        public class TwoFactorProvider
        {
            public string? Name { get; set; }
            public string? Action { get; set; }
            public string? Display { get; set; }
        }
    }
}
