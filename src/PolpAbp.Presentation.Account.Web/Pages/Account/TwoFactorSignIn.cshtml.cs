using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Auditing;
using Volo.Abp.Identity;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [UnauthenticatedUser]
    [CurrentTenantRequired]
    public class TwoFactorSignInModel : LoginModelBase
    {
        [BindProperty(SupportsGet = true)]
        public bool RememberMe { get; set; }

        [BindProperty]
        public PostInput Input { get; set; }

        public TwoFactorSignInModel() : base()
        {
            Input = new PostInput();
            RememberMe = false;
        }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Load settings
            await LoadSettingsAsync();
            Input.RememberMe = RememberMe;

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

                        var mainPage = Configuration["PolpAbp:Account:MainEntry"];
                        return RedirectToPage(mainPage, new
                        {
                            returnUrl = ReturnUrl,
                            returnUrlHash = ReturnUrlHash
                        });
                    }

                    // Error messages 
                    Alerts.Danger(@"Oops! The code isn't right. No problem, just enter the code we sent to your phone or email and try again. Click ""Resend Code"" below if you need a new one.");

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

            return Page();
        }

        public class PostInput
        {
            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPasswordLength))]
            [DisableAuditing]
            public string? Code { get; set; }

            // Remember this client.
            public bool RememberClient { get; set; }

            // todo: Get this value from the previous step.
            // Remember this cookie across the session.
            // Still useful after the browser is closed 
            public bool RememberMe { get; set; }

            public bool IsRecoveryCode { get; set; }
        }
    }
}
