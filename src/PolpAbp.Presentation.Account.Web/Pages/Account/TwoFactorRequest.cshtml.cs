using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PolpAbp.Framework;
using PolpAbp.Framework.Emailing.Account;
using PolpAbp.Framework.Globalization;
using System.Security.Claims;
using Volo.Abp.Sms;

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
        protected readonly IAmbientSmsSender SmsSender;

        protected string SmsSenderName
        {
            get
            {
                return Configuration.GetValue<string>("PolpAbp:Framework:SmsSenderName", string.Empty);
            }
        }

        public TwoFactorRequestModel(IFrameworkAccountEmailer accountEmailer,
            IAmbientSmsSender smsSender,
            IPhoneNumberService phoneNumberService) : base()
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

                    var b = L["TwoFactorCode_Sms", token];
                    var bodyStr = b.ToString();
                    if (!string.IsNullOrEmpty(SmsSenderName))
                    {
                        bodyStr = SmsSenderName + ": " + bodyStr;
                    }
                    TempData["PolpAbp.Account.TwoFactorCode.Provider"] = TokenOptions.DefaultPhoneProvider;
                    
                    // Note that the underlying sms sender will be responsible
                    // for checking if the phone number is good or not.
                    //
                    // todo: Check if the organization configuration.
                    // todo: Settings
                    // var allowedCountry =
                    var msg = new SmsMessage(user.PhoneNumber, bodyStr);
                    await SmsSender.SendAsync(msg);

                    var errorCodeStr = msg.Properties.GetOrDefault("ErrorCode");
                    int.TryParse(errorCodeStr.ToString(), out int errorCode);
                    if (errorCode != 0)
                    {
                        Alerts.Danger("The system cannot send the text message to the given number. Please choose a different way for getting the code.");
                        return Page();
                    }

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
