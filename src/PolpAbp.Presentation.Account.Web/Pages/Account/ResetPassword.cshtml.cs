using Microsoft.AspNetCore.Mvc;
using PolpAbp.Framework.Authorization.Users.Events;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Account;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
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


        public virtual Task<IActionResult> OnGetAsync()
        {
            return Task.FromResult<IActionResult>(Page());
        }

        public virtual async Task<IActionResult> OnPostAsync()
        {
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
            catch (AbpValidationException e)
            {
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
            if (!Equals(Password, ConfirmPassword))
            {
                ModelState.AddModelError("ConfirmPassword",
                    L["'{0}' and '{1}' do not match.", "ConfirmPassword", "Password"]);
            }

            base.ValidateModel();
        }
    }
}
