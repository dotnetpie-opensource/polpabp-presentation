using PolpAbp.Framework.Emailing.Account;
using PolpAbp.Presentation.Account.Web.Settings;
using Volo.Abp.Data;
using Volo.Abp.Settings;
using Volo.Abp.TenantManagement;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public class RegisterModelBase : PolpAbpAccountPageModel
    {
        public bool IsRecaptchaEnabled { get; set; }

        // DI
        protected ITenantManager TenantManager => LazyServiceProvider.LazyGetRequiredService<ITenantManager>();
        protected ITenantRepository TenantRepository => LazyServiceProvider.LazyGetRequiredService<ITenantRepository>();
        protected  IDataSeeder DataSeeder => LazyServiceProvider.LazyGetRequiredService<IDataSeeder>();

        protected IFrameworkAccountEmailer AccountEmailer => LazyServiceProvider.LazyGetRequiredService<IFrameworkAccountEmailer>();


        protected bool IsRegistrationDisabled
        {
            get
            {
                return Configuration.GetValue<bool>("PolpAbpFramework:RegistrationDisabled");
            }
        }

        protected async Task LoadSettingsAsync()
        {
            // Use host ...
            IsRecaptchaEnabled = await SettingProvider.IsTrueAsync(AccountWebSettingNames.IsHostRecaptchaEnabled);
        }
    }
}
