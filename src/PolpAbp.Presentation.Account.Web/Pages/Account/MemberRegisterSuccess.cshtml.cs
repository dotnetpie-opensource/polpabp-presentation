using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PolpAbp.Framework.Exceptions.Identity;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [CurrentTenantRequired]
    public class MemberRegisterSuccessModel : PolpAbpAccountPageModel
    {
        public bool IsAuthenticated = false; 
        public string TenantName = string.Empty;

        [BindProperty(SupportsGet = true)]
        public UserOnboardingNextActionEnum NextAction { get; set; }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Load settings
            await LoadSettingsAsync();

            this.IsAuthenticated = CurrentUser.IsAuthenticated;
            this.TenantName = CurrentTenant.Name ?? string.Empty;

            return Page();
        }
    }
}
