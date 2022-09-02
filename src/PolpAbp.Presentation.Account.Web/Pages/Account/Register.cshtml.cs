using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [OnlyAnonymous]
    public class RegisterModel : RegisterModelBase
    {
        [BindProperty]
        public PostInput Input { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool IsExternalLogin { get; set; }

        public RegisterModel() : base()
        {
            Input = new PostInput();
        }


        public virtual async Task<IActionResult> OnGetAsync()
        {
            await LoadSettingsAsync();

            Input.TenantName = TempData.Peek(nameof(PostInput.TenantName))?.ToString() ?? string.Empty;
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
                // Validate model
                ValidateModel();

                if (!string.Equals(TempData.Peek(nameof(PostInput.TenantName))?.ToString(), Input.TenantName))
                {
                    // Clean up data 
                    _ = TempData["Password"];
                }

                TempData[nameof(PostInput.TenantName)] = Input.TenantName;

                // Success and then instructions
                return RedirectToPage("./RegisterDefineAdmin");
            }

            return Page();
        }

        public class PostInput
        {
            [Required]
            [MinLength(4)]
            [MaxLength(256)]
            public string? TenantName { get; set; }
        }
    }
}
