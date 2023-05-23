using System.Web;
using Microsoft.AspNetCore.Mvc;
using PolpAbp.Framework.Settings;
using Volo.Abp.Settings;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public abstract class LoginModelBase : PolpAbpAccountPageModel
    {
        [BindProperty(SupportsGet = true)]
        public string? UserName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? EmailAddress { get; set; }

        public string NormalizedUserName => UserName ?? string.Empty;

        public string NormalizedEmailAddress => EmailAddress ?? string.Empty;

        public bool IsUserNameEnabled { get; set; }


        protected override async Task LoadSettingsAsync()
        {
            await base.LoadSettingsAsync();
            // Use host ...
            IsUserNameEnabled = await SettingProvider.IsTrueAsync(FrameworkSettings.Account.IsUserNameEnabled);
        }
      
    }
}
