using Microsoft.AspNetCore.Mvc;
using PolpAbp.Framework.Mvc.Cookies;
using PolpAbp.Framework.Settings;
using System.ComponentModel.DataAnnotations;
using Volo.Abp;
using Volo.Abp.Account.Settings;
using Volo.Abp.Auditing;
using Volo.Abp.Identity;
using Volo.Abp.Identity.AspNetCore;
using Volo.Abp.Settings;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [OnlyAnonymous]
    [TenantPrerequisite("/Account/Login")]
    public class LocalLoginModel : LoginModelBase
    {
        [BindProperty]
        public PostInput Input { get; set; }

        protected MemberRegistrationEnum RegistrationApprovalType = MemberRegistrationEnum.RequireEmailActivation;
        protected readonly IAppCookieManager CookieManager;

        public LocalLoginModel(IAppCookieManager cookieManager) : base()
        {
            CookieManager = cookieManager;

            Input = new PostInput();
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
                    // On purpose use it here to allow the other actions.
                    await CheckLocalLoginAsync();

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

                    if (user == null)
                    {
                        Alerts.Danger(L["InvalidUserNameOrPassword"]);
                        return RedirectToPage("./Login", new
                        {
                            UserName = UserName,
                            EmailAddress = EmailAddress,
                            returnUrl = ReturnUrl,
                            returnUrlHash = ReturnUrlHash
                        });
                    }

                    // Check if the user is allowed to login or not 
                    if (user.IsActive == false)
                    {
                        if (RegistrationApprovalType == MemberRegistrationEnum.RequireEmailActivation)
                        {
                            Alerts.Warning(L["Login:EmailConfirmationRequired"]);
                            return Page();
                        }
                        else if (RegistrationApprovalType == MemberRegistrationEnum.RequireAdminApprovel)
                        {
                            Alerts.Warning(L["Login:AdminApprovalPending"]);
                            return Page();
                        }
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
                        // todo: Check if wen directly redirect a user to
                        // the two factor sign in page or request page
                        // depending on if a user set up an authenticator.

                        return RedirectToPage("./TwoFactorRequest", new
                        {
                            RememberMe = Input.RememberMe,
                            returnUrl = ReturnUrl,
                            returnUrlHash = ReturnUrlHash
                        });
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

                    // The point where we have to check if the user 
                    // should update his/her password.
                    if (user.ShouldChangePasswordOnNextLogin())
                    {
                        return RedirectToPage("./UpdatePassword", new
                        {
                            returnUrl = ReturnUrl,
                            returnUrlHash = ReturnUrlHash
                        });
                    }

                    var mainPage = Configuration["PolpAbp:Account:MainEntry"];
                    return RedirectToPage(mainPage, new
                    {
                        returnUrl = ReturnUrl,
                        returnUrlHash = ReturnUrlHash
                    });
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
            else if (action == "Cancel")
            {
                // Need to reload the page.
                return RedirectToPage("./Login", new
                {
                    UserName = UserName,
                    EmailAddress = EmailAddress,
                    ReturnUrl = ReturnUrl,
                    ReturnUrlHash = ReturnUrlHash
                });
            }
            else if (action == "ResetTenant")
            {
                // Remove tenant cookies
                CookieManager.SetTenantCookieValue(Response, string.Empty);

                // Need to reload the page.
                return RedirectToPage("./Login", new
                {
                    UserName = UserName,
                    EmailAddress = EmailAddress,
                    returnUrl = ReturnUrl,
                    returnUrlHash = ReturnUrlHash
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

        protected override async Task LoadSettingsAsync()
        {
            await base.LoadSettingsAsync();
            RegistrationApprovalType = (MemberRegistrationEnum)(await SettingProvider.GetAsync<int>(FrameworkSettings.Account.RegistrationApprovalType));
        }

        public class PostInput
        {
            public bool IsUsingEmailAddress { get; set; }

            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxEmailLength))]
            public string? UserNameOrEmailAddress { get; set; }

            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPasswordLength))]
            [DataType(DataType.Password)]
            [DisableAuditing]
            public string? Password { get; set; }

            public bool RememberMe { get; set; }
        }
    }
}
