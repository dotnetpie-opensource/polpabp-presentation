using AspNetCore.ReCaptcha;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using PolpAbp.Framework.Identity;
using PolpAbp.Presentation.Account.Web.Settings;
using System.ComponentModel.DataAnnotations;
using System.Web;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;
using Volo.Abp.AspNetCore.Mvc.UI.MultiTenancy.Localization;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
using Volo.Abp.TenantManagement;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public class FindUserModel : PolpAbpAccountPageModel
    {

        [BindProperty(SupportsGet = true)]
        public string? TenantOrEmailAddress { get; set; }

        [BindProperty]
        public PostInput Input { get; set; }

        [BindProperty]
        public PostSelect Selection { get; set; }

        public List<SelectListItem> TenantList { get; set; }

        public bool IsRecaptchaEnabled { get; set; }

        // DI
        public IDataFilter DataFilter { get; set; }
        public IIdentityUserRepositoryExt IdentityUserRepositoryExt { get; set; }
        public ITenantRepository TenantRepository { get; set; }
        public IReCaptchaService RecaptchaService { get; set; }

        public string NormalizedTenantOrEmailAddress => HttpUtility.UrlDecode(TenantOrEmailAddress ?? string.Empty);

        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Load settings
            await LoadSettingsAsync();

            // If the query parameter carries some info, this step of locating the tenant 
            // may be skipped.
            if (!string.IsNullOrEmpty(NormalizedTenantOrEmailAddress))
            {
                await AttemptBuildTenantsAync(NormalizedTenantOrEmailAddress);
            }
            // Render page 
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            // Load settings
            await LoadSettingsAsync();

            if (action == "Input")
            {

                if (IsRecaptchaEnabled)
                {
                    _ = HttpContext.Request.Form.TryGetValue(PresentationAccountWebConstants.RecaptchaReponseKey, out var reCaptchaResponse);
                    var isGood = await RecaptchaService.VerifyAsync(reCaptchaResponse);
                    if (!isGood)
                    {
                        // TODO: localization
                        Alerts.Danger("Please verify that you are not a robot.");

                        return Page();
                    }
                }

                // Tenant not set this moment.
                ValidateModel();

                // Return all tenants
                await AttemptBuildTenantsAync(Input.TenantOrEmailAddress);

                if (TenantList.Count == 0)
                {
                    // TODO: localization
                    Alerts.Danger("Invalid organization or email address.");
                }

                /*
                if (TenantList.Count == 1)
                {
                    // Set up the tenant and move on.
                    Response.SetTenantCookieValue(TenantList.First().Value);

                    if (!string.IsNullOrEmpty(ReturnUrl))
                    {
                        return Redirect(ReturnUrl);
                    }
                    else
                    {
                        // To Login 
                        return RedirectToPage("./Login");
                    }
                } */
            }
            else if (action == "Selection")
            {
                if (Selection != null && Selection.Id.HasValue)
                {
                    // Set up the tenant and move on.
                    Response.SetTenantCookieValue(Selection.Id.Value.ToString());

                    if (!string.IsNullOrEmpty(ReturnUrl))
                    {
                        return Redirect(ReturnUrl);
                    }
                    else
                    {
                        // To Login 
                        return RedirectToPage("./Login");
                    }
                }
            }


            return Page();
        }

        protected async Task AttemptBuildTenantsAync(string tenantOrEmail)
        {
            var tenants = new List<Tenant>();

            if (ValidationHelper.IsValidEmailAddress(tenantOrEmail))
            {
                var users = await FindByEmailBeyondTenantAsync(tenantOrEmail);
                var ids = users.Select(a => a.TenantId);
                await FindByTenantIdsAsync(ids, tenants);
            } 
            else
            {
                var x = await TenantRepository.FindByNameAsync(tenantOrEmail, false);
                if (x != null)
                {
                    tenants.Add(x);
                }
            }

            TenantList = tenants.Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Name }).ToList();
        }


        protected async Task<List<IdentityUser>> FindByEmailBeyondTenantAsync(string email)
        {
            using (DataFilter.Disable<IMultiTenant>())
            {
                // Find lookup the user in the ID
                var user = await IdentityUserRepositoryExt.FindUsersByEmailAsync(email);
                return user;
            }
        }

        protected async Task FindByTenantIdsAsync(IEnumerable<Guid?> ids, List<Tenant> tenants)
        {
            foreach(var a in ids)
            {
                if (a.HasValue)
                {
                    var b = await TenantRepository.GetAsync(a.Value);
                    tenants.Add(b);
                }
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
            public string TenantOrEmailAddress { get; set; }
        }

        public class PostSelect
        {
            [SelectItems(nameof(TenantList))]
            public Guid? Id { get; set; }
        }

    }
}
