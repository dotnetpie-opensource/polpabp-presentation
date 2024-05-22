using Microsoft.AspNetCore.Mvc;
using PolpAbp.Framework.Extensions;
using PolpAbp.Framework.Mvc.Cookies;
using System.ComponentModel.DataAnnotations;
using System.Web;
using Volo.Abp.Auditing;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [OnlyAnonymous]
    [DisableAuditing]
    public class LoginModel : LoginModelBase
    {
        [BindProperty]
        public LoginInputModel Input { get; set; }

        [BindProperty]
        public PostResolution Resolution { get; set; }

        private readonly IAppCookieManager _cookieManager;

        public LoginModel(IAppCookieManager cookieManager) : base()
        {
            _cookieManager = cookieManager;

            Input = new LoginInputModel();
            Resolution = new PostResolution();
        }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Load settings
            await LoadSettingsAsync();

            if (!IsEmailGloballyUnique && !CurrentTenant.IsAvailable)
            {
                // Find user.
                return RedirectToPage("./FindUser", new
                {
                    returnUrl = ReturnUrl,
                    returnUrlHash = ReturnUrlHash
                });
            }

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

                    if (IsEmailGloballyUnique)
                    {
                        var anyUsers = await FindByEmailBeyondTenantAsync(Input.UserNameOrEmailAddress);
                        user = anyUsers.FirstOrDefault();
                    }
                    else if (IsUserNameEnabled)
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
                        _cookieManager.SetTenantCookieValue(Response, user.TenantId!.Value.ToString());

                        if (!user.IsExternal)
                        {
                            var a = Input.IsUsingEmailAddress ? string.Empty : user.UserName;
                            var b = Input.IsUsingEmailAddress ? user.Email : string.Empty;
                            return RedirectToPage("./LocalLogin", new
                            {
                                // todo: Maybe use Id
                                UserName = a,
                                EmailAddress = b,
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
                                        UserName = Input.IsUsingEmailAddress ? string.Empty : user.UserName,
                                        EmailAddress = Input.IsUsingEmailAddress ? user.Email : string.Empty,
                                        returnUrl = ReturnUrl,
                                        returnUrlHash = ReturnUrlHash
                                    });
                                }
                            }

                            // Fall back to the general case.
                            return RedirectToPage("./ExternalLogin", new
                            {
                                // todo: Maybe use Id
                                UserName = Input.IsUsingEmailAddress ? string.Empty : user.UserName,
                                EmailAddress = Input.IsUsingEmailAddress ? user.Email : string.Empty,
                                returnUrl = ReturnUrl,
                                returnUrlHash = ReturnUrlHash
                            });

                        }
                    }
                    else
                    {
                        _cookieManager.SetTenantCookieValue(Response, string.Empty);

                        Alerts.Danger(L["InvalidUserNameOrPassword"]);

                        if (IsEmailGloballyUnique)
                        {
                            return Page();
                        }

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

            return Page();
        }

        public class LoginInputModel
        {
            public bool IsUsingEmailAddress { get; set; }
            [Required]
            [MinLength(1)]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxEmailLength))]
            public string UserNameOrEmailAddress { get; set; }

            public LoginInputModel()
            {
                UserNameOrEmailAddress = string.Empty;
            }
        }

        public class PostResolution
        {
            public int OptionId { get; set; }
        }
    }
}
