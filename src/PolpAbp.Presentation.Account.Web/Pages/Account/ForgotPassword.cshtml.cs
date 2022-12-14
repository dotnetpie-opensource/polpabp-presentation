using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Identity;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [TenantPrerequisite]
    [OnlyAnonymous]
    public class ForgotPasswordModel : LoginModelBase
    {
        [BindProperty]
        public InputModel Input { get; set; }

        public ForgotPasswordModel() : base()
        {
            Input = new InputModel();
        }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Load settings
            await LoadSettingsAsync();

            if (!string.IsNullOrEmpty(NormalizedUserName))
            {
                Input.UserNameOrEmailAddress = NormalizedUserName;
            }
            else if (!string.IsNullOrEmpty(NormalizedEmailAddress))
            {
                Input.UserNameOrEmailAddress = NormalizedEmailAddress;
                Input.IsUsingEmailAddress = true;
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

                    if (Input.IsUsingEmailAddress)
                    {
                        user = await UserManager.FindByEmailAsync(Input.UserNameOrEmailAddress);
                    }
                    else
                    {
                        user = await UserManager.FindByNameAsync(Input.UserNameOrEmailAddress);
                    }

                    if (user != null)
                    {
                        // todo: Should we use the background ??
                        // In that case, the email may not be sent instantly.
                        await AccountAppService.SendPasswordResetCodeAsync(
                            new SendPasswordResetCodeDto
                            {
                                Email = user.Email,
                                AppName = "MVC", //TODO: Const!
                                ReturnUrl = ReturnUrl,
                                ReturnUrlHash = ReturnUrlHash
                            }
                        );

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
            public bool IsUsingEmailAddress { get; set; }

            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxEmailLength))]
            public string? UserNameOrEmailAddress { get; set; }
        }
    }
}
