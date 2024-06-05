using Microsoft.AspNetCore.Mvc;
using PolpAbp.Framework.Extensions;
using PolpAbp.Framework.Mvc.Cookies;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Auditing;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [CurrentTenantRequired]
    [UnauthenticatedUser]
    [DisableAuditing]
    public class LoginModel : LoginModelBase
    {
        [BindProperty]
        public LoginInputModel Input { get; set; }

        public Guid? TenantId { get; set; }

        public LoginModel() : base()
        {
            Input = new LoginInputModel();
        }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Load settings
            await LoadSettingsAsync();
            // TenantId
            TenantId = CurrentTenant.Id;

            if (!string.IsNullOrEmpty(NormalizedUserName))
            {
                Input.UserName = NormalizedUserName;
                Input.IsUsingUserName = false;
            }
            else if (!string.IsNullOrEmpty(NormalizedEmailAddress))
            {
                Input.EmailAddress = NormalizedEmailAddress;
                Input.IsUsingUserName = true;
            }
            else
            {
                Input.UserName = string.Empty;
                Input.EmailAddress = string.Empty;
                Input.IsUsingUserName = false;
            }

            return Page();
        }

        public virtual async Task<IActionResult> OnPostAsync(string action)
        {
            // Load settings
            await LoadSettingsAsync();
            // Tenant Id
            TenantId = CurrentTenant.Id;

            if (action == "Input")
            {

                try
                {
                    ValidateModel();

                    // Extra sanity check 
                    if (Input.IsUsingUserName)
                    {
                        if (string.IsNullOrEmpty(Input.UserName))
                        {
                            Alerts.Danger("We need your username to sign you in! Please enter your username and try again.");
                            return Page();
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(Input.EmailAddress))
                        {
                            Alerts.Danger("We need your email to sign you in! Please enter your username and try again.");
                            return Page();
                        }
                    }

                    Input.EmailAddress = Input.EmailAddress.Trim();
                    Input.UserName  = Input.UserName.Trim();

                    IdentityUser? user = null;

                    if (Input.IsUsingUserName)
                    {
                        user = await UserManager.FindByNameAsync(Input.UserName);
                    }
                    else
                    {
                        user = await UserManager.FindByEmailAsync(Input.EmailAddress);
                    }

                    if (user != null)
                    {
                        if (!user.IsExternal)
                        {
                            return RedirectToPage("./LocalLogin", new
                            {
                                // todo: Maybe use Id
                                UserName = user.UserName ,
                                EmailAddress = user.Email,
                                returnUrl = ReturnUrl,
                                returnUrlHash = ReturnUrlHash
                            });
                        }
                        else
                        {
                            // Figure out the provider name.
                            var providerName = user.GetProperty<string>(ExternalProperties.UserIdentity.SsoScheme);
                            if (!string.IsNullOrEmpty(providerName))
                            {
                                var ssoUrl = Configuration[$@"PolpAbp:ExternalLogin:{providerName}:LoginPage"];
                                if (!string.IsNullOrEmpty(ssoUrl))
                                {
                                    return RedirectToPage(ssoUrl, new
                                    {
                                        // todo: Maybe use Id
                                        UserName = user.UserName,
                                        EmailAddress = user.Email,
                                        returnUrl = ReturnUrl,
                                        returnUrlHash = ReturnUrlHash
                                    });
                                }
                            }

                            // Fall back to the general case.
                            return RedirectToPage("./ExternalLogin", new
                            {
                                // todo: Maybe use Id
                                UserName = user.UserName,
                                EmailAddress = user.Email,
                                returnUrl = ReturnUrl,
                                returnUrlHash = ReturnUrlHash
                            });

                        }
                    }
                    else
                    {
                        Alerts.Danger(L["InvalidUserNameOrPassword"]);
                    }
                }
                catch (AbpValidationException ex)
                {
                    // Handle this error.
                    foreach (var a in ex.ValidationErrors)
                    {
                        Alerts.Danger(a.ErrorMessage);
                    }
                }
            }

            return Page();
        }

        public class LoginInputModel
        {
            public bool IsUsingUserName { get; set; }

            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxUserNameLength))]
            public string UserName { get; set; }

            [Required]
            [EmailAddress]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxEmailLength))]
            public string EmailAddress { get; set; }

        }

    }
}
