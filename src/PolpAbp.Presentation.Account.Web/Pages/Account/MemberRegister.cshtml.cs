using Microsoft.AspNetCore.Mvc;
using static PolpAbp.Presentation.Account.Pages.Account.PolpAbpExternalAuthPageModel;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [OnlyAnonymous]
    [TenantPrerequisite]
    public partial class MemberRegisterModel : PolpAbpAccountPageModel
    {
        public readonly List<ExternalProviderModel> SsoProviders = new List<ExternalProviderModel>();

        public MemberRegisterModel() : base()
        {
        }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Load settings
            await LoadSettingsAsync();

            return Page();
        }


        protected override async Task LoadSettingsAsync()
        {
            await base.LoadSettingsAsync();

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
