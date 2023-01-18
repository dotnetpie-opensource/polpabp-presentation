using Microsoft.AspNetCore.Mvc;
using PolpAbp.Framework.Authorization.Users.Events;
using PolpAbp.Framework.Settings;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Account;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Identity;
using Volo.Abp.Settings;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [OnlyAnonymous]
    [TenantPrerequisite]
    public class ResetPasswordModel : PolpAbpAccountPageModel
    {
        [Required]
        [HiddenInput]
        [BindProperty(SupportsGet = true)]
        public Guid UserId { get; set; }

        [Required]
        [HiddenInput]
        [BindProperty(SupportsGet = true)]
        public string? ResetToken { get; set; }

        [Required]
        [BindProperty]
        [DataType(DataType.Password)]
        [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPasswordLength))]
        [DisableAuditing]
        public string? Password { get; set; }

        [Required]
        [BindProperty]
        [DataType(DataType.Password)]
        [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPasswordLength))]
        [DisableAuditing]
        public string? ConfirmPassword { get; set; }


        private readonly ILocalEventBus _localEventBus;
        
        public ResetPasswordModel(ILocalEventBus localEventBus) : base()
        {
            _localEventBus = localEventBus;
        }


        public virtual async Task<IActionResult> OnGetAsync()
        {
            await LoadSettingsAsync();

            return Page();
        }

        public virtual async Task<IActionResult> OnPostAsync()
        {
            await LoadSettingsAsync();

            try
            {
                ValidateModel();

                await AccountAppService.ResetPasswordAsync(
                    new ResetPasswordDto
                    {
                        UserId = UserId,
                        ResetToken = ResetToken,
                        Password = Password
                    }
                );

                await _localEventBus.PublishAsync(new PasswordChangedEvent
                {
                    TenantId = CurrentTenant.Id,
                    UserId = UserId,
                    NewPassword = Password
                });
            }
            catch (AbpIdentityResultException e)
            {
                if (!string.IsNullOrWhiteSpace(e.Message))
                {
                    Alerts.Warning(GetLocalizeExceptionMessage(e));
                    return Page();
                }

                throw;
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

            //TODO: Try to automatically login!
            return RedirectToPage("./ResetPasswordConfirmation", new
            {
                returnUrl = ReturnUrl,
                returnUrlHash = ReturnUrlHash
            });
        }

        protected override void ValidateModel()
        {
            var passwordValidator = new PasswordValidator(PwdComplexity, L, ModelState);
            if (passwordValidator.ValidateComplexity(Password))
            {
                passwordValidator.ValidateConfirmPassword(Password, ConfirmPassword);
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

    }
}
