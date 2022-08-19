using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PolpAbp.Presentation.Account.Web.Settings;
using System.Web;
using Volo.Abp.Settings;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public abstract class LoginModelBase : PolpAbpAccountPageModel
    {
        [BindProperty(SupportsGet = true)]
        public string? UserNameOrEmailAddress { get; set; }

        public string NormalizedUserNameOrEmailAddress => HttpUtility.UrlDecode(UserNameOrEmailAddress ?? string.Empty);

        public bool IsUserNameEnabled { get; set; }

        protected IConfiguration Configuration => LazyServiceProvider.LazyGetRequiredService<IConfiguration>();

        protected virtual async Task LoadSettingsAsync()
        {
            // Use host ...
            IsUserNameEnabled = await SettingProvider.IsTrueAsync(AccountWebSettingNames.IsUserNameEnabled);
        }

        protected bool IsBackgroundEmail
        {
            get
            {
                // todo: Do we need to introduce the module-specific settings?
                return Configuration.GetValue<bool>("PolpAbpFramework:BackgroundEmail");
            }
        }
    }
}
