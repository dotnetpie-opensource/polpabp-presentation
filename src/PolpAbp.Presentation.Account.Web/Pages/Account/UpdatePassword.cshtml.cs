using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PolpAbp.Framework.Authorization.Users.Events;
using PolpAbp.Framework.Security;
using PolpAbp.Framework.Settings;
using System.ComponentModel.DataAnnotations;
using System.Transactions;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Identity;
using Volo.Abp.Settings;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [Authorize]
    public class UpdatePasswordModel : PolpAbpAccountPageModel
    {
        [BindProperty]
        public PostInput Input { get; set; }

        private readonly ILocalEventBus _localEventBus;

        public UpdatePasswordModel(ILocalEventBus localEventBus) {
            _localEventBus = localEventBus;

            Input = new PostInput();
        }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Load settings
            await LoadSettingsAsync();

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
                    // trim
                    Input.Password = Input!.Password!.Trim();
                    Input.ConfirmPassword = Input!.ConfirmPassword!.Trim();

                    ValidateModel();

                    var userInfo = await UserManager.GetUserAsync(User);

                    (await UserManager.RemovePasswordAsync(userInfo)).CheckErrors();
                    (await UserManager.AddPasswordAsync(userInfo, Input.Password)).CheckErrors();

                    userInfo.RemoveShouldChangePasswordOnNextLogin();
                    await UserManager.UpdateAsync(userInfo);

                    await _localEventBus.PublishAsync(new PasswordChangedEvent
                    {
                        TenantId = userInfo.TenantId,
                        UserId = userInfo.Id
                    });

                    Alerts.Info("Your password has been updated successfully.");

                    var mainPage = Configuration["PolpAbp:Account:MainEntry"];
                    // Following the same logic from the local login.
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

            return Page();
        }

        protected override void ValidateModel()
        {
            var passwordValidator = new PasswordValidator(PwdComplexity, L, ModelState);
            if (passwordValidator.ValidateComplexity(Input.Password))
            {
                passwordValidator.ValidateConfirmPassword(Input.Password, Input.ConfirmPassword);
            }

            base.ValidateModel();
        }

        protected override async Task LoadSettingsAsync()
        {
            await base.LoadSettingsAsync();
            await ReadInPasswordComplexityAsync();
        }

        protected override async Task ReadInPasswordComplexityAsync()
        {
            await base.ReadInPasswordComplexityAsync();

            PwdComplexity.RequireDigit = await SettingProvider.GetAsync<bool>(FrameworkSettings.AccountPassComplexityRequireDigit, PwdComplexity.RequireDigit);
            PwdComplexity.RequireLowercase = await SettingProvider.GetAsync<bool>(FrameworkSettings.AccountPassComplexityRequireLowercase, PwdComplexity.RequireLowercase);
            PwdComplexity.RequireUppercase = await SettingProvider.GetAsync<bool>(FrameworkSettings.AccountPassComplexityRequireUppercase, PwdComplexity.RequireUppercase);
            PwdComplexity.RequireNonAlphanumeric = await SettingProvider.GetAsync<bool>(FrameworkSettings.AccountPassComplexityRequireNonAlphanumeric, PwdComplexity.RequireNonAlphanumeric);
            PwdComplexity.RequiredLength = await SettingProvider.GetAsync<int>(FrameworkSettings.AccountPassComplexityRequiredLength, PwdComplexity.RequiredLength);
        }

        public class PostInput : IHasConfirmPassword
        {
            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPasswordLength))]
            [DataType(DataType.Password)]
            [DisableAuditing]
            public string? Password { get; set; }

            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPasswordLength))]
            [DataType(DataType.Password)]
            [DisableAuditing]
            public string? ConfirmPassword { get; set; }
        }
    }
}
