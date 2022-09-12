using Microsoft.AspNetCore.Mvc;
using PolpAbp.Framework.Emailing.Account;
using System.ComponentModel.DataAnnotations;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [TenantPrerequisite]
    [OnlyAnonymous]
    public class ResendActivationLinkModel : LoginModelBase
    {
        [BindProperty]
        public InputModel Input { get; set; }

        protected readonly IFrameworkAccountEmailer AccountEmailer;

        public ResendActivationLinkModel(IFrameworkAccountEmailer frameworkAccountEmailer) : base()
        {
            AccountEmailer = frameworkAccountEmailer;

            Input = new InputModel();
        }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Load settings
            await LoadSettingsAsync();

            Input.UserNameOrEmailAddress = NormalizedUserNameOrEmailAddress;

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

                    if (!IsUserNameEnabled)
                    {
                        if (!ValidationHelper.IsValidEmailAddress(Input.UserNameOrEmailAddress))
                        {
                            Alerts.Warning("Please type a valid email address");
                            return Page();
                        }
                    }

                    IdentityUser? user = null;

                    if (IsUserNameEnabled)
                    {
                        user = await UserManager.FindByNameAsync(Input.UserNameOrEmailAddress);
                    }
                    else if (ValidationHelper.IsValidEmailAddress(Input.UserNameOrEmailAddress))
                    {
                        user = await UserManager.FindByEmailAsync(Input.UserNameOrEmailAddress);
                    }

                    if (user != null && !user.EmailConfirmed)
                    {


                        // Send it instantly, because the user is waiting for it.
                        await AccountEmailer.SendEmailActivationLinkAsync(user.Id);

                    }
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
                catch (UserFriendlyException e)
                {
                    Alerts.Danger(GetLocalizeExceptionMessage(e));
                    return Page();
                }
            }
            else if (action == "Cancel")
            {
                return RedirectToPage("./Login", new
                {
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
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxEmailLength))]
            public string UserNameOrEmailAddress { get; set; }
        }
    }
}
