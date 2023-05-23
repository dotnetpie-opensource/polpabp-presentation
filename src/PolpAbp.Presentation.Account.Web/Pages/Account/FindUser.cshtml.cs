using AspNetCore.ReCaptcha;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PolpAbp.Framework.Mvc.Cookies;
using System.ComponentModel.DataAnnotations;
using System.Web;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;
using Volo.Abp.Auditing;
using Volo.Abp.TenantManagement;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [OnlyAnonymous]
    [DisableAuditing]
    public class FindUserModel : PolpAbpAccountPageModel
    {

        [BindProperty(SupportsGet = true)]
        public string? TenantName { get; set; }

        [BindProperty]
        public PostInput Input { get; set; }

        [BindProperty]
        public PostResolution Resolution { get; set; }

        [BindProperty]
        public PostSelect Selection { get; set; }

        public List<SelectListItem> TenantList { get; set; }

        // DI
        protected readonly ITenantRepository TenantRepository;
        protected readonly IReCaptchaService RecaptchaService;
        protected readonly IAppCookieManager CookieManager;

        public string NormalizedTenantName => TenantName ?? string.Empty;

        public FindUserModel(
            ITenantRepository tenantRepository,
            IReCaptchaService reCaptchaService,
            IAppCookieManager cookieManager) : base()
        {
            TenantRepository = tenantRepository;
            RecaptchaService = reCaptchaService;
            CookieManager = cookieManager;

            Input = new PostInput();
            Selection = new PostSelect();
            Resolution = new PostResolution();
            TenantList = new List<SelectListItem>();
        }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Load settings
            await LoadSettingsAsync();

            // If the query parameter carries some info, this step of locating the tenant 
            // may be skipped.
            if (!string.IsNullOrEmpty(NormalizedTenantName))
            {
                await AttemptBuildTenantsByNameAync(NormalizedTenantName);
            }
            // Render page 
            return Page();
        }

        public virtual async Task<IActionResult> OnPostAsync(string action)
        {
            // Load settings
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
                    // Tenant not set this moment.
                    ValidateModel();

                    await AttemptBuildTenantsByNameAync(Input.TenantName!);

                    if (TenantList.Count == 0)
                    {
                        // TODO: localization
                        Alerts.Danger("Invalid organization.");
                    }

                    // Skip the selection if not needed.
                    if (TenantList.Count == 1)
                    {
                        // Set up the tenant and move on.
                        CookieManager.SetTenantCookieValue(Response, TenantList.First().Value);

                        if (!string.IsNullOrEmpty(ReturnUrl))
                        {
                            // Be smart
                            if (ReturnUrl.ToLower().EndsWith("/account/login"))
                            {
                                return RedirectToPage("./Login");
                            }

                            return Redirect(ReturnUrl);
                        }
                        else
                        {
                            return RedirectToPage("./Login");
                        }
                    }
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
            else if (action == "Selection")
            {
                if (Selection != null && Selection.Id.HasValue)
                {
                    // Set up the tenant and move on.
                    CookieManager.SetTenantCookieValue(Response, Selection.Id.Value.ToString());

                    if (!string.IsNullOrEmpty(ReturnUrl))
                    {
                        // Be smart
                        if (ReturnUrl.ToLower().EndsWith("/account/login"))
                        {
                            return RedirectToPage("./Login");
                        }

                        return Redirect(ReturnUrl);
                    }
                    else
                    {
                        return RedirectToPage("./Login");
                    }
                }
            }

            return Page();
        }

        protected async Task AttemptBuildTenantsByNameAync(string name)
        {
            var tenants = new List<Tenant>();

            var x = await TenantRepository.GetListAsync(filter: name, includeDetails: false);
            // We do not expose other tenant names
            if (x.Count <= 3)
            {
                foreach (var y in x)
                {
                    tenants.Add(y);
                }
            }

            TenantList = tenants.Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Name }).ToList();
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

        protected override async Task LoadSettingsAsync()
        {
            await base.LoadSettingsAsync();
            await ReadInRecaptchaEnabledAsync();
        }

        public class PostInput
        {
            [Required]
            [MinLength(1)]
            public string? TenantName { get; set; }
        }

        public class PostSelect
        {
            [SelectItems(nameof(TenantList))]
            public Guid? Id { get; set; }
        }

        public class PostResolution
        {
            public int OptionId { get; set; }
        }

    }
}
