using AspNetCore.ReCaptcha;
using Microsoft.AspNetCore.Mvc;
using PolpAbp.Framework.Identity;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Auditing;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [UnauthenticatedUser]
    [CurrentTenantEmpty]
    public class RegisterConfirmModel : RegisterModelBase
    {
        [BindProperty]
        public PostInput Input { get; set; }

        protected readonly IReCaptchaService RecaptchaService;
        protected readonly IRegisteredTenantDataSeeder TenantDataSeeder;

        public RegisterConfirmModel(IReCaptchaService recaptchaService,
            IRegisteredTenantDataSeeder registeredTenantDataSeeder) : base()
        {
            RecaptchaService = recaptchaService;
            TenantDataSeeder = registeredTenantDataSeeder;

            Input = new PostInput();
        }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            await LoadSettingsAsync();

            Input.TenantName = TempData.Peek(nameof(PostInput.TenantName))?.ToString() ?? string.Empty;
            Input.AdminEmailAddress = TempData.Peek(nameof(PostInput.AdminEmailAddress))?.ToString() ?? string.Empty;
            Input.Password = TempData.Peek(nameof(PostInput.Password))?.ToString() ?? string.Empty;

            // Render page 
            return Page();
        }

        public virtual async Task<IActionResult> OnPostAsync(string action)
        {
            await LoadSettingsAsync();

            if (action == "Input")
            {
                if (IsRecaptchaEnabled)
                {
                    var recaptchaValue = ParseRecaptchResponse();
                    var isGood = await RecaptchaService.VerifyAsync(recaptchaValue);
                    if (!isGood)
                    {
                        // TODO: localization
                        Alerts.Danger("Please verify that you are not a robot.");

                        return Page();
                    }
                }


                try
                {
                    ValidateModel();
                    await RegisterTenantAsync();

                    TempData.Clear();
                    // Success and then instructions
                    return RedirectToPage("./RegisterSuccess");
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
                return RedirectToPage("./RegisterDefineAdmin");
            }

            return Page();
        }

        protected async Task RegisterTenantAsync()
        {
            // todo: Verify if the tenant name is available or not.
            var tenant = await TenantManager.CreateAsync(Input.TenantName);
            await TenantRepository.InsertAsync(tenant, true); // Save automatically

            IdentityUser? admin = null;

            // Create data for tenant.
            // 1. The admin
            using (CurrentTenant.Change(tenant.Id, tenant.Name))
            {
                await DataSeeder.SeedAsync(new DataSeedContext(tenant.Id)
                    .WithProperty("AdminEmail", Input.AdminEmailAddress)
                    .WithProperty("AdminPassword", Input.Password));

                admin = await UserManager.FindByEmailAsync(Input.AdminEmailAddress);
                // The admin has to confirm the email before being active.
                admin.SetIsActive(false);
                admin.Surname = "admin";
                await UserManager.UpdateAsync(admin);
            }

            // Tenant seeder
            await TenantDataSeeder.SeedAsync(tenant.Id);

            // Send out a confirmation email, regardless the current tenant.
            // Send it instantly, because the user is waiting for it.
            await AccountEmailer.SendEmailActivationLinkAsync(admin!.Id);

            // Global events will be raised by the built-in entity events. 
        }

        protected override async Task LoadSettingsAsync()
        {
            await base.LoadSettingsAsync();
            // Use host ...
            await ReadInRecaptchaEnabledAsync();
        }

        public class PostInput 
        {
            [Required]
            [MinLength(4)]
            [MaxLength(256)]
            public string? TenantName { get; set; }

            [Required]
            [EmailAddress]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxEmailLength))]
            public string? AdminEmailAddress { get; set; }

            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPasswordLength))]
            [DataType(DataType.Password)]
            [DisableAuditing]
            public string? Password { get; set; }
        }
    }
}
