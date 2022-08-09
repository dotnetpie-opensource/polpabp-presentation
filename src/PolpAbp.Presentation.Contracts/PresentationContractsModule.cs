using Volo.Abp.Account;
using Volo.Abp.Account.Localization;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace PolpAbp.Presentation.Contracts
{
    [DependsOn(
        typeof(AbpAccountApplicationContractsModule)
    )]
    public class PresentationContractsModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.AddEmbedded<PresentationContractsModule>("PolpAbp.Presentation");
            });

            Configure<AbpLocalizationOptions>(options =>
            {
                // Extend account resource.
                options.Resources
                    .Get<AccountResource>()
                    .AddVirtualJson("/Localization/Resources");
            });
        }
    }
}
