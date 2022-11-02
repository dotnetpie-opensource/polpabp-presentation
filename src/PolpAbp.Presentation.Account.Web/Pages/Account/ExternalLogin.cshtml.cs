using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PolpAbp.Presentation.Account.Pages.Account;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public class ExternalLoginModel : PolpAbpExternalAuthPageModel
    {
        public readonly List<ExternalProviderModel> SsoProviders;

        public ExternalLoginModel(IAuthenticationSchemeProvider schemeProvider) : base(schemeProvider)
        {
            SsoProviders = new List<ExternalProviderModel>();
        }

        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Load settings
            await LoadSettingsAsync();

            var providers = await GetAllExternalProviders();

            var candidates = providers;
            // todo: 
                // .Where(x => AllowedProviderName.Any(y => string.Equals(y, x.AuthenticationScheme, StringComparison.CurrentCultureIgnoreCase)));
            foreach(var z in candidates)
            {
                var p = Configuration[$@"PolpAbp:ExternalLogin:{z.AuthenticationScheme}:LoginPage"];
                if (!string.IsNullOrEmpty(p))
                {
                    SsoProviders.Add(new ExternalProviderModel(z) 
                    {
                        LoginPage = p
                    });
                }
            }

            return Page();
        }
    }
}
