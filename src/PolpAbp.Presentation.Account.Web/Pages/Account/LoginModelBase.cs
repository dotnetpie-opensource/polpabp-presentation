using System.Web;
using Microsoft.AspNetCore.Mvc;
using PolpAbp.Framework.Settings;
using Volo.Abp.Settings;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public abstract class LoginModelBase : PolpAbpAccountPageModel
    {
        [BindProperty(SupportsGet = true)]
        public string? UserNameOrEmailAddress { get; set; }

        public string NormalizedUserNameOrEmailAddress => HttpUtility.UrlDecode(UserNameOrEmailAddress ?? string.Empty);

        public bool IsUserNameEnabled { get; set; }


        protected override async Task LoadSettingsAsync()
        {
            await base.LoadSettingsAsync();
            // Use host ...
            IsUserNameEnabled = await SettingProvider.IsTrueAsync(FrameworkSettings.IsUserNameEnabled);
        }
      
    }
}
