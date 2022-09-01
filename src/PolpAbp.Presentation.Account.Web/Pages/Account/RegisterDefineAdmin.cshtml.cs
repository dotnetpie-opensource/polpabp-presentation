using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Auditing;
using Volo.Abp.Identity;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [OnlyAnonymous]
    public class RegisterDefineAdminModel : RegisterModelBase
    {
        [BindProperty]
        public PostInput Input { get; set; }

        public RegisterDefineAdminModel(): base()
        {
            Input = new PostInput();
        }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            await LoadSettingsAsync();

            Input.AdminEmailAddress = TempData.Peek(nameof(PostInput.AdminEmailAddress))?.ToString() ?? string.Empty;
            Input.Password = TempData.Peek(nameof(PostInput.Password))?.ToString() ?? string.Empty;
            Input.ConfirmPassword = Input.Password;

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
    
                TempData[nameof(PostInput.AdminEmailAddress)] = Input.AdminEmailAddress;
                TempData[nameof(PostInput.Password)] = Input.Password;

                return RedirectToPage("./RegisterConfirm");
            }
            else if (action == "Cancel")
            {
                return RedirectToPage("./Register");
            }

            return Page();
        }

        public class PostInput
        {
            [Required]
            [EmailAddress]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxEmailLength))]
            public string? AdminEmailAddress { get; set; }

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
