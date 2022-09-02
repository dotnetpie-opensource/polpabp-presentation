using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Auditing;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public class RegisterConfirmModel : RegisterModelBase
    {
        [BindProperty]
        public PostInput Input { get; set; }

        public RegisterConfirmModel() : base()
        {
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

            // Shortcut
            if (IsRegistrationDisabled)
            {
                Alerts.Warning("Registration is not available now. Please try it later!");
                return Page();
            }


            if (action == "Input")
            {
                ValidateModel();
                
                try
                {
                    await RegisterTenantAsync();

                    TempData.Clear();
                    // Success and then instructions
                    return RedirectToPage("./RegisterSuccess");

                }
                catch (Exception e)
                {
                    Alerts.Danger(GetLocalizeExceptionMessage(e));
                    return Page();
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

            IdentityUser admin = null;

            // Create data for tenant.
            // 1. The admin
            using (CurrentTenant.Change(tenant.Id, tenant.Name))
            {
                await DataSeeder.SeedAsync(new DataSeedContext(tenant.Id)
                    .WithProperty("AdminEmail", Input.AdminEmailAddress)
                    .WithProperty("AdminPassword", Input.Password));

                admin = await UserManager.FindByEmailAsync(Input.AdminEmailAddress);
            }
            // Send out a confirmation email, regardless the current tenant.
            // Send it instantly, because the user is waiting for it.
            await AccountEmailer.SendEmailActivationLinkAsync(admin!.Id);
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
