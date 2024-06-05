using Microsoft.AspNetCore.Mvc;
using static PolpAbp.Presentation.Account.Pages.Account.PolpAbpExternalAuthPageModel;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [UnauthenticatedUser]
    [CurrentTenantRequired]
    public partial class MemberRegisterModel : PolpAbpAccountPageModel
    {
        [BindProperty(SupportsGet = true)]
        public bool AutoRedirect { get; set; }

        public readonly List<ExternalProviderModel> SsoProviders = new List<ExternalProviderModel>();

        public MemberRegisterModel() : base()
        {
        }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Load settings
            await LoadSettingsAsync();

            // If there is only method and autoRedirect is set, we skip this page.
            if (AutoRedirect && IsRegistrationEnabled)
            {
                if (IsExternalAuthEnforced)
                {
                    if (SsoProviders.Count() == 1)
                    {
                        // Only 1 option 
                        return RedirectToPage(SsoProviders.First().RegisterPage);
                    }
                }
                else
                {
                    if (SsoProviders.Count() == 0)
                    {
                        return RedirectToPage("/Account/CreateMember");
                    }
                }
            }

            return Page();
        }


        protected override async Task LoadSettingsAsync()
        {
            await base.LoadSettingsAsync();

            await LoadRegistrationSettingsAsync();

            await ReadInExternalAuthProviderSettingsAsync();
            // Build up the login providers 
            var providers = await GetAllExternalProviders();

            var candidates = providers
                .Where(x => AllowedProviderName.Any(y => x.DisplayName.Contains(y)));
            foreach (var z in candidates)
            {
                var p = Configuration[$@"PolpAbp:ExternalLogin:{z.AuthenticationScheme}:RegisterPage"];
                if (!string.IsNullOrEmpty(p))
                {
                    SsoProviders.Add(new ExternalProviderModel(z)
                    {
                        RegisterPage = p
                    });
                }
            }
        }

    }
}
