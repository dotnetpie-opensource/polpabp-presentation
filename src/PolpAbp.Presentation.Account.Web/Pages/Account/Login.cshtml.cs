using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Web;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [TenantPrerequisite]
    [OnlyAnonymous]
    public class LoginModel : LoginModelBase
    {

        [BindProperty]
        public LoginInputModel Input { get; set; }

        public LoginModel() : base()
        {
            Input = new LoginInputModel();
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

                    if (user != null)
                    {
                        if (!user.IsExternal)
                        {
                            return RedirectToPage("./LocalLogin", new
                            {
                                // todo: Maybe use Id
                                userNameOrEmailAddress = HttpUtility.UrlEncode(IsUserNameEnabled ? user.UserName : user.Email),
                                returnUrl = ReturnUrl,
                                returnUrlHash = ReturnUrlHash
                            });
                        }
                        else
                        {
                            // Figure out the provider name.
                            var providerName = user.GetProperty<string>("SsoScheme");
                            if (!string.IsNullOrEmpty(providerName))
                            {
                                var ssoUrl = Configuration[$@"PolpAbp:ExternalLogin:{providerName}:LoginPage"];
                                if (!string.IsNullOrEmpty(ssoUrl))
                                {
                                    return RedirectToPage(ssoUrl, new
                                    {
                                        // todo: Maybe use Id
                                        userNameOrEmailAddress = HttpUtility.UrlEncode(IsUserNameEnabled ? user.UserName : user.Email),
                                        returnUrl = ReturnUrl,
                                        returnUrlHash = ReturnUrlHash
                                    });
                                }
                            }

                            // Fall back to the general case.
                            return RedirectToPage("./ExternalLogin", new
                            {
                                // todo: Maybe use Id
                                userNameOrEmailAddress = HttpUtility.UrlEncode(IsUserNameEnabled ? user.UserName : user.Email),
                                returnUrl = ReturnUrl,
                                returnUrlHash = ReturnUrlHash
                            });

                        }
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
            else if (action == "ResetTenant")
            {
                // Remove tenant cookies
                Response.SetTenantCookieValue(String.Empty);

                // Need to reload the page.
                return RedirectToPage("./Login", new
                {
                    userNameOrEmailAddress = NormalizedUserNameOrEmailAddress,
                    returnUrl = ReturnUrl,
                    returnUrlHash = ReturnUrlHash
                });
            }

            return Page();
        }

        public class LoginInputModel
        {
            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxEmailLength))]
            public string? UserNameOrEmailAddress { get; set; }
        }
    }
}
