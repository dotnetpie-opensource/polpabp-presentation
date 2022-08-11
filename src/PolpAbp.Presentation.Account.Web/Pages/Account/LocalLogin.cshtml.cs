using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Web;
using Volo.Abp;
using Volo.Abp.Account.Settings;
using Volo.Abp.Auditing;
using Volo.Abp.Identity;
using Volo.Abp.Identity.AspNetCore;
using Volo.Abp.Settings;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public class LocalLoginModel : LoginModelBase
    {
        [BindProperty]
        public PostInput Input { get; set; }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Load settings
            await LoadSettingsAsync();

            Input = new PostInput();
            Input.UserNameOrEmailAddress = NormalizedUserNameOrEmailAddress;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            // Load settings
            await LoadSettingsAsync();

            if (action == "Input")
            {
                // On purpose use it here to allow the other actions.
                await CheckLocalLoginAsync();

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

                if (user == null)
                {
                    Alerts.Danger(L["InvalidUserNameOrPassword"]);
                    return Page();
                }

                // todo: Login 
                var result = await SignInManager.PasswordSignInAsync(
                    user!.UserName,
                    Input.Password,
                    Input.RememberMe,
                    true
                    );

                await IdentitySecurityLogManager.SaveAsync(new IdentitySecurityLogContext()
                {
                    Identity = IdentitySecurityLogIdentityConsts.Identity,
                    Action = result.ToIdentitySecurityLogAction(),
                    UserName = Input.UserNameOrEmailAddress
                });

                if (result.RequiresTwoFactor)
                {
                    // todo: tfa
                    // return await TwoFactorLoginResultAsync();
                }

                if (result.IsLockedOut)
                {
                    Alerts.Warning(L["UserLockedOutMessage"]);
                    return Page();
                }

                if (result.IsNotAllowed)
                {
                    Alerts.Warning(L["LoginIsNotAllowed"]);
                    return Page();
                }

                if (!result.Succeeded)
                {
                    Alerts.Danger(L["InvalidUserNameOrPassword"]);
                    return Page();
                }

                // todo: Make MainApp configurable ...
                return RedirectToPage("./MainApp", new
                {
                    returnUrl = ReturnUrl,
                    returnUrlHash = ReturnUrlHash
                });

            }
            else if (action == "Cancel")
            {
                // Need to reload the page.
                return RedirectToPage("./Login", new
                {
                    UserNameOrEmailAddress = NormalizedUserNameOrEmailAddress,
                    ReturnUrl = ReturnUrl,
                    ReturnUrlHash = ReturnUrlHash
                });
            }

            return Page();
        }

        protected virtual async Task CheckLocalLoginAsync()
        {
            if (!await SettingProvider.IsTrueAsync(AccountSettingNames.EnableLocalLogin))
            {
                throw new UserFriendlyException(L["LocalLoginDisabledMessage"]);
            }
        }

        public class PostInput
        {
            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxEmailLength))]
            public string UserNameOrEmailAddress { get; set; }

            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPasswordLength))]
            [DataType(DataType.Password)]
            [DisableAuditing]
            public string Password { get; set; }

            public bool RememberMe { get; set; }
        }
    }
}
