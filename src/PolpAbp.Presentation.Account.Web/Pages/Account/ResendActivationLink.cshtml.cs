using Microsoft.AspNetCore.Mvc;
using PolpAbp.Framework.Emailing.Account;
using PolpAbp.Framework.Mvc.Interceptors;
using System.ComponentModel.DataAnnotations;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [UnauthenticatedUser]
    public class ResendActivationLinkModel : LoginModelBase
    {
        [BindProperty]
        public InputModel Input { get; set; }

        protected readonly IFrameworkAccountEmailer AccountEmailer;
        protected readonly IEmailingInterceptor EmailingInterceptor;

        public ResendActivationLinkModel(
            IFrameworkAccountEmailer frameworkAccountEmailer,
            IEmailingInterceptor emailingInterceptor) : base()
        {
            AccountEmailer = frameworkAccountEmailer;
            EmailingInterceptor = emailingInterceptor;

            Input = new InputModel();
        }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Load settings
            await LoadSettingsAsync();

            if (!IsEmailGloballyUnique && !CurrentTenant.IsAvailable)
            {
                Alerts.Danger(L["Login:TenantIsRequired"]);

                // Find user.
                return RedirectToPage("./FindOrganization", new
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

                    if (user != null && user.IsActive && user.EmailConfirmed)
                    {
                        Alerts.Warning(L["Login:AccountAlreadyActive"]);
                        return Page();
                    }

                    if (user != null && (!user.EmailConfirmed || !user.IsActive))
                    {
                        // Send it instantly, because the user is waiting for it.
                        var cc = await EmailingInterceptor.GetActivationLinkEmailCcAsync(user.Id);
                        await AccountEmailer.SendEmailActivationLinkAsync(user.Id, cc);
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
                    UserName = UserName,
                    EmailAddress = EmailAddress,
                    ReturnUrl = ReturnUrl,
                    ReturnUrlHash = ReturnUrlHash
                });
            }
            // For security reason, we will redirect the user to another page regardless 
            // whether the user exits or not.
            return RedirectToPage(
                "./ResendActivationLinkSuccess",
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
