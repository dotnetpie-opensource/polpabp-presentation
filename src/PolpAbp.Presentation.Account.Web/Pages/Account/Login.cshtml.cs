using Microsoft.AspNetCore.Mvc;
using PolpAbp.Presentation.Account.Web.Settings;
using System.ComponentModel.DataAnnotations;
using System.Web;
using Volo.Abp.Identity;
using Volo.Abp.Settings;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [TenantPrerequisite]
    public class LoginModel : PolpAbpAccountPageModel
    {
        [BindProperty(SupportsGet = true)]
        public string? UserNameOrEmail { get; set; }

        public string NormalizedUserNameOrEmail => HttpUtility.UrlDecode(UserNameOrEmail ?? string.Empty);

        public bool IsUserNameEnabled { get; set; }

        [BindProperty]
        public LoginInputModel Input { get; set; }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Load settings
            await LoadSettingsAsync();

            IdentityUser? user = null;

            if (!string.IsNullOrEmpty(UserNameOrEmail))
            {
                if (IsUserNameEnabled)
                {
                    user = await UserManager.FindByNameAsync(NormalizedUserNameOrEmail);
                }
                else if (ValidationHelper.IsValidEmailAddress(NormalizedUserNameOrEmail))
                {
                    user = await UserManager.FindByEmailAsync(NormalizedUserNameOrEmail);
                }
            }

            if (user != null)
            {
                if (!user.IsExternal)
                {
                    return RedirectToPage("./LocalLogin", new
                    {
                        // todo: Maybe use Id
                        Email = HttpUtility.UrlEncode(user.Email),
                        ReturnUrl = ReturnUrl,
                        ReturnUrlHash = ReturnUrlHash
                    });
                }
                else
                {
                    return RedirectToPage("./ExternalLogin", new
                    {
                        // todo: Maybe use Id
                        Email = HttpUtility.UrlEncode(user.Email),
                        ReturnUrl = ReturnUrl,
                        ReturnUrlHash = ReturnUrlHash
                    });

                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            // Load settings
            await LoadSettingsAsync();

            if (action == "Input")
            {

                ValidateModel();
                if (!IsUserNameEnabled)
                {
                    if (!ValidationHelper.IsValidEmailAddress(NormalizedUserNameOrEmail))
                    {
                        Alerts.Warning("Please type a valid email address");
                        return Page();
                    }
                }

                IdentityUser? user = null;

                if (IsUserNameEnabled)
                {
                    user = await UserManager.FindByNameAsync(NormalizedUserNameOrEmail);
                }
                else if (ValidationHelper.IsValidEmailAddress(NormalizedUserNameOrEmail))
                {
                    user = await UserManager.FindByEmailAsync(NormalizedUserNameOrEmail);
                }

                if (user != null)
                {
                    if (!user.IsExternal)
                    {
                        return RedirectToPage("./LocalLogin", new
                        {
                            // todo: Maybe use Id
                            Email = HttpUtility.UrlEncode(user.Email),
                            ReturnUrl = ReturnUrl,
                            ReturnUrlHash = ReturnUrlHash
                        });
                    }
                    else
                    {
                        return RedirectToPage("./ExternalLogin", new
                        {
                            // todo: Maybe use Id
                            Email = HttpUtility.UrlEncode(user.Email),
                            ReturnUrl = ReturnUrl,
                            ReturnUrlHash = ReturnUrlHash
                        });

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
                    UserNameOrEmail = NormalizedUserNameOrEmail,
                    ReturnUrl = ReturnUrl,
                    ReturnUrlHash = ReturnUrlHash
                });
            }

            return Page();
        }

        protected async Task LoadSettingsAsync()
        {
            // Use host ...
            IsUserNameEnabled = await SettingProvider.IsTrueAsync(AccountWebSettingNames.IsUserNameEnabled);
        }

        public class LoginInputModel
        {
            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxEmailLength))]
            public string UserNameOrEmailAddress { get; set; }
        }
    }
}
