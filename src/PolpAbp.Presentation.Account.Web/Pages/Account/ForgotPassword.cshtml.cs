using Microsoft.AspNetCore.Mvc;
using PolpAbp.Framework.Emailing.Account;
using PolpAbp.Framework.Mvc.Interceptors;
using System.ComponentModel.DataAnnotations;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [OnlyAnonymous]
    public class ForgotPasswordModel : LoginModelBase
    {
        [BindProperty]
        public InputModel Input { get; set; }

        protected readonly IEmailingInterceptor EmailingInterceptor;
        protected readonly IFrameworkAccountEmailer FrameworkAccountEmailer;

        public ForgotPasswordModel(
            IEmailingInterceptor emailingInterceptor,
            IFrameworkAccountEmailer frameworkAccountEmailer) : base()
        {
            EmailingInterceptor= emailingInterceptor;
            FrameworkAccountEmailer = frameworkAccountEmailer;

            Input = new InputModel();
        }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Load settings
            await LoadSettingsAsync();

            if (!IsEmailGloballyUnique && !CurrentTenant.IsAvailable)
            {
                Alerts.Danger("Please locate your organization first!");

                // Find user.
                return RedirectToPage("./FindUser", new
                {
                    returnUrl = ReturnUrl,
                    returnUrlHash = ReturnUrlHash
                });
            }

            if (!string.IsNullOrEmpty(NormalizedEmailAddress))
            {
                Input.EmailAddress = NormalizedEmailAddress;
            }

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

                    IdentityUser? user = null;

                    if (IsEmailGloballyUnique)
                    {
                        var anyUsers = await FindByEmailBeyondTenantAsync(Input.EmailAddress);
                        user = anyUsers.FirstOrDefault();
                    }
                    else 
                    {
                        user = await UserManager.FindByEmailAsync(Input.EmailAddress);
                    }

                    if (user != null)
                    {
                        // todo: Should we use the background ??
                        // In that case, the email may not be sent instantly.
                        var cc = await EmailingInterceptor.GetForgotPasswordEmailCcAsync(user.Id);
                        var resetToken = await UserManager.GeneratePasswordResetTokenAsync(user);
                        await FrameworkAccountEmailer.SendPasswordResetLinkWithCcAsync(
                            user.TenantId!.Value,
                            user.Id,
                            user.Email,
                            resetToken,
                            "MVC", // appname
                            cc :cc,
                            returnUrl: ReturnUrl,
                            returnUrlHash: ReturnUrlHash);
                    }
                }
                catch (UserFriendlyException e)
                {
                    Alerts.Danger(GetLocalizeExceptionMessage(e));
                    return Page();
                }
                catch (AbpValidationException ex)
                {
                    // Handle this error.
                    foreach (var a in ex.ValidationErrors)
                    {
                        Alerts.Add(Volo.Abp.AspNetCore.Mvc.UI.Alerts.AlertType.Danger, a.ErrorMessage);
                    }

                    return Page();
                }

            }
            else if (action == "Cancel")
            {
                return RedirectToPage("./Login", new
                {
                    UserName = UserName,
                    EmailAddress = EmailAddress,
                    ReturnUrl = ReturnUrl,
                    ReturnUrlHash = ReturnUrlHash
                });
            }

            // For security reason, we will redirect the user to another page regardless 
            // whether the user exits or not.
            return RedirectToPage(
                "./PasswordResetLinkSent",
                new
                {
                    returnUrl = ReturnUrl,
                    returnUrlHash = ReturnUrlHash
                });
        }

        public class InputModel
        {
            [Required]
            [MinLength(1)]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxEmailLength))]
            public string EmailAddress { get; set; }

            public InputModel()
            {
                EmailAddress = string.Empty;
            }
        }
    }
}
