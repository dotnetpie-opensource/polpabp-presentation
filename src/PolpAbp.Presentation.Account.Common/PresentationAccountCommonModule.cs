using Volo.Abp.Account;
using Volo.Abp.Identity.AspNetCore;
using Volo.Abp.Modularity;
using Volo.Abp.AspNetCore.Mvc.UI;
using PolpAbp.Framework;

namespace PolpAbp.Presentation.Account;

[DependsOn(
    typeof(AbpIdentityAspNetCoreModule),
    typeof(AbpAccountApplicationContractsModule),
    typeof(AbpAspNetCoreMvcUiModule),
    typeof(PolpAbpFrameworkDomainSharedModule),
    typeof(PolpAbpFrameworkCoreSharedModule),
    typeof(PolpAbpFrameworkApplicationContractsModule)
    )]
public class PresentationAccountCommonModule : AbpModule
{
}

