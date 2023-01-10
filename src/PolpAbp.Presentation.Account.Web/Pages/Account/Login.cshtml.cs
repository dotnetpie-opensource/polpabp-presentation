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

        [BindProperty]
        public PostResolution Resolution { get; set; }

        public LoginModel() : base()
        {
            Input = new LoginInputModel();
            Resolution = new PostResolution();
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

                    if (IsUserNameEnabled)
                    {
                        if (Input.IsUsingEmailAddress)
                        {
                            user = await UserManager.FindByEmailAsync(Input.UserNameOrEmailAddress);
                        }
                        else
                        {
                            user = await UserManager.FindByNameAsync(Input.UserNameOrEmailAddress);
                        }
                    }
                    else
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
                                UserName = Input.IsUsingEmailAddress ? string.Empty : HttpUtility.UrlEncode(user.UserName),
                                EmailAddress = Input.IsUsingEmailAddress ? HttpUtility.UrlEncode(user.Email) : string.Empty,
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
                                        UserName = Input.IsUsingEmailAddress ? string.Empty : HttpUtility.UrlEncode(user.UserName),
                                        EmailAddress = Input.IsUsingEmailAddress ? HttpUtility.UrlEncode(user.Email) : string.Empty,
                                        returnUrl = ReturnUrl,
                                        returnUrlHash = ReturnUrlHash
                                    });
                                }
                            }

                            // Fall back to the general case.
                            return RedirectToPage("./ExternalLogin", new
                            {
                                // todo: Maybe use Id
                                UserName = Input.IsUsingEmailAddress ? string.Empty : HttpUtility.UrlEncode(user.UserName),
                                EmailAddress = Input.IsUsingEmailAddress ? HttpUtility.UrlEncode(user.Email) : string.Empty,
                                returnUrl = ReturnUrl,
                                returnUrlHash = ReturnUrlHash
                            });

                        }
                    }
                    else
                    {
                        return RedirectToPage("./FindUser", new
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
            else if (action == "Resolution")
            {
                // Re-render the page.
                return Page();
            }
            else if (action == "ResetTenant")
            {
                // Remove tenant cookies
                Response.SetTenantCookieValue(String.Empty);

                // Need to reload the page.
                return RedirectToPage("./FindUser", new
                {
                    returnUrl = ReturnUrl,
                    returnUrlHash = ReturnUrlHash
                });
            }

            return Page();
        }

        public class LoginInputModel
        {
            public bool IsUsingEmailAddress { get; set; }
            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxEmailLength))]
            public string? UserNameOrEmailAddress { get; set; }
        }

        public class PostResolution
        {
            public int OptionId { get; set; }
        }
    }
}
