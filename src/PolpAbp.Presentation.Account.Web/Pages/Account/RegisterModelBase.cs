using PolpAbp.Framework.Emailing.Account;
using Volo.Abp.Data;
using Volo.Abp.TenantManagement;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public class RegisterModelBase : PolpAbpAccountPageModel
    {

        // DI
        protected ITenantManager TenantManager => LazyServiceProvider.LazyGetRequiredService<ITenantManager>();
        protected ITenantRepository TenantRepository => LazyServiceProvider.LazyGetRequiredService<ITenantRepository>();
        protected  IDataSeeder DataSeeder => LazyServiceProvider.LazyGetRequiredService<IDataSeeder>();

        protected IFrameworkAccountEmailer AccountEmailer => LazyServiceProvider.LazyGetRequiredService<IFrameworkAccountEmailer>();

        // System determined, regardless the tenant.
        protected bool IsRegistrationDisabled
        {
            get
            {
                return Configuration.GetValue<bool>("PolpAbpFramework:RegistrationDisabled");
            }
        }

    }
}
