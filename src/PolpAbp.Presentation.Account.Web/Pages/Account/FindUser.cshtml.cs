using System.ComponentModel.DataAnnotations;
using System.Web;
using AspNetCore.ReCaptcha;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PolpAbp.Framework.Identity;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [OnlyAnonymous]
    public class FindUserModel : PolpAbpAccountPageModel
    {

        [BindProperty(SupportsGet = true)]
        public string? TenantOrEmailAddress { get; set; }

        [BindProperty]
        public PostInput Input { get; set; }

        [BindProperty]
        public PostSelect Selection { get; set; }

        public List<SelectListItem> TenantList { get; set; }

        protected const string CachedEmailAddressKey = "FindUserEmailAddress";

        // DI
        protected readonly IDataFilter DataFilter;
        protected readonly IIdentityUserRepositoryExt IdentityUserRepositoryExt;
        protected readonly ITenantRepository TenantRepository;
        protected readonly IReCaptchaService RecaptchaService;

        public string NormalizedTenantOrEmailAddress => HttpUtility.UrlDecode(TenantOrEmailAddress ?? string.Empty);

        public FindUserModel(IDataFilter dataFilter,
            IIdentityUserRepositoryExt identityUserRepositoryExt,
            ITenantRepository tenantRepository,
            IReCaptchaService reCaptchaService) : base()
        {
            DataFilter = dataFilter;
            IdentityUserRepositoryExt = identityUserRepositoryExt;
            TenantRepository = tenantRepository;
            RecaptchaService = reCaptchaService;

            Input = new PostInput();
            Selection = new PostSelect();
            TenantList = new List<SelectListItem>();
        }

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

                    // Return all tenants 
                    await AttemptBuildTenantsAync(Input.TenantOrEmailAddress!); // after validation not null
                    if (TenantList.Any() && ValidationHelper.IsValidEmailAddress(Input.TenantOrEmailAddress))
                    {
                        TempData[CachedEmailAddressKey] = Input.TenantOrEmailAddress;
                    }
                    else
                    {
                        // Clean up the entry
                        var _ = TempData[CachedEmailAddressKey];
                    }

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
                    Response.SetTenantCookieValue(Selection.Id.Value.ToString());

                    if (!string.IsNullOrEmpty(ReturnUrl))
                    {
                        // Be smart
                        if (ReturnUrl.ToLower().EndsWith("/account/login"))
                        {
                            return RedirectToPage("./Login", new
                            {
                                UserNameOrEmailAddress = HttpUtility.UrlEncode(TempData[CachedEmailAddressKey]!.ToString())
                            });
                        }

                        return Redirect(ReturnUrl);
                    }
                    else
                    {
                        if (TempData.Peek(CachedEmailAddressKey) != null)
                        {
                            return RedirectToPage("./Login", new
                            {
                                UserNameOrEmailAddress = HttpUtility.UrlEncode(TempData[CachedEmailAddressKey]!.ToString())
                            });
                        }
                        // To Login by default.
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

        protected override async Task LoadSettingsAsync()
        {
            await base.LoadSettingsAsync();
            await ReadInRecaptchaEnabledAsync();
        }

        public class PostInput
        {
            [Required]
            public string? TenantOrEmailAddress { get; set; }
        }

        public class PostSelect
        {
            [SelectItems(nameof(TenantList))]
            public Guid? Id { get; set; }
        }

    }
}
