using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PolpAbp.Framework.Emailing.Account;
using PolpAbp.Presentation.Account.Web.Settings;
using System.ComponentModel.DataAnnotations;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Settings;
using Volo.Abp.TenantManagement;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [OnlyAnonymous]
    public class RegisterModel : PolpAbpAccountPageModel
    {
        [BindProperty]
        public PostInput Input { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool IsExternalLogin { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ExternalLoginAuthSchema { get; set; }

        public bool IsRecaptchaEnabled { get; set; }

        // DI
        public ITenantManager TenantManager { get; set; }
        public ITenantRepository TenantRepository { get; set; }
        public IDataSeeder DataSeeder { get; set; }

        protected IFrameworkAccountEmailer AccountEmailer => LazyServiceProvider.LazyGetRequiredService<IFrameworkAccountEmailer>();

        public virtual Task<IActionResult> OnGetAsync()
        {
            // Render page 
            return Task.FromResult(Page() as IActionResult);
        }

        public virtual async Task<IActionResult> OnPostAsync(string action)
        {
            // Shortcut
            if (IsRegistrationDisabled)
            {
                Alerts.Warning("Registertion is not available now. Please try it later!");
                return Page();
            }

            if (action == "Input")
            {
                try
                {
                    await RegisterTenantAsync();

                    // Success and then instructions
                    return RedirectToPage("./RegisterSuccess");

                }
                catch (Exception e)
                {
                    Alerts.Danger(GetLocalizeExceptionMessage(e));
                    return Page();
                }
            }

            return Page();
        }

        protected async Task RegisterTenantAsync()
        {
            // todo: Verify if the tenant name is available or not.
            var tenant = await TenantManager.CreateAsync(Input.TenantName);
            await TenantRepository.InsertAsync(tenant, true); // Save automatically

            // Create data for tenant.
            // 1. The admin
            using (CurrentTenant.Change(tenant.Id, tenant.Name))
            {
                await DataSeeder.SeedAsync(new DataSeedContext(tenant.Id)
                    .WithProperty("AdminEmail", Input.AdminEmailAddress)
                    .WithProperty("AdminPassword", Input.Password));

            }
            // Send out a confirmation email, regardless the current tenant.
            // Send it instantly, because the user is waiting for it.
            await AccountEmailer.SendEmailActivationLinkAsync(Input.AdminEmailAddress);

        }

        protected bool IsRegistrationDisabled
        {
            get
            {
                return Configuration.GetValue<bool>("PolpAbpFramework:RegistrationDisabled");
            }
        }

        protected async Task LoadSettingsAsync()
        {
            // Use host ...
            IsRecaptchaEnabled = await SettingProvider.IsTrueAsync(AccountWebSettingNames.IsHostRecaptchaEnabled);
        }

        public class PostInput
        {
            [Required]
            [MinLength(4)]
            [MaxLength(256)]
            public string TenantName { get; set; }

            [Required]
            [EmailAddress]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxEmailLength))]
            public string AdminEmailAddress { get; set; }

            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPasswordLength))]
            [DataType(DataType.Password)]
            [DisableAuditing]
            public string Password { get; set; }

            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPasswordLength))]
            [DataType(DataType.Password)]
            [DisableAuditing]
            public string ConfirmPassword { get; set; }

        }
    }
}
