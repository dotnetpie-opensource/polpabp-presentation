using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Web;
using Volo.Abp.Identity;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [TenantPrerequisite]
    public class LoginModel : LoginModelBase
    {

        [BindProperty]
        public LoginInputModel Input { get; set; }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Load settings
            await LoadSettingsAsync();

            // todo: Input has been initialized?
            Input = new LoginInputModel();
            Input.UserNameOrEmailAddress = NormalizedUserNameOrEmailAddress;

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
            public string UserNameOrEmailAddress { get; set; }
        }
    }
}
