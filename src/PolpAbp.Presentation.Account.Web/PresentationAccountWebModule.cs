﻿using PolpAbp.Framework;
using PolpAbp.Framework.Mvc;
using PolpAbp.Presentation.Contracts;
using Volo.Abp.Account;
using Volo.Abp.Account.Localization;
using Volo.Abp.AspNetCore.Mvc.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AutoMapper;
using Volo.Abp.Emailing;
using Volo.Abp.ExceptionHandling;
using Volo.Abp.Identity.AspNetCore;
using Volo.Abp.Modularity;
using Volo.Abp.Sms;
using Volo.Abp.TenantManagement;
using Volo.Abp.UI.Navigation;
using Volo.Abp.VirtualFileSystem;

namespace PolpAbp.Presentation.Account.Web
{
    [DependsOn(
        typeof(AbpAutoMapperModule),
        typeof(AbpExceptionHandlingModule),
        typeof(AbpTenantManagementDomainModule),
        typeof(AbpAccountApplicationContractsModule),
        typeof(AbpEmailingModule),
        typeof(AbpSmsModule),
        typeof(AbpIdentityAspNetCoreModule),
        typeof(AbpAspNetCoreMvcUiThemeSharedModule),
        typeof(AbpAspNetCoreMvcUiMultiTenancyModule),
        typeof(PolpAbpFrameworkDomainModule),
        typeof(PolpAbpFrameworkApplicationContractsModule),
        typeof(PolpAbpFrameworkMvcModule),
        typeof(PolpAbpFrameworkAbpExtensionsModule),
        typeof(PresentationContractsModule),
        typeof(PresentationAccountCommonModule)
    )]
    public class PresentationAccountWebModule : AbpModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.PreConfigure<AbpMvcDataAnnotationsLocalizationOptions>(options =>
            {
                options.AddAssemblyResource(typeof(AccountResource), typeof(PresentationAccountWebModule).Assembly);
            });

            PreConfigure<IMvcBuilder>(mvcBuilder =>
            {
                mvcBuilder.AddApplicationPartIfNotExists(typeof(PresentationAccountWebModule).Assembly);
            });
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.AddEmbedded<PresentationAccountWebModule>();
            });


            Configure<AbpNavigationOptions>(options =>
            {
                options.MenuContributors.Add(new PresentationAccountUserMenuContributor());
            });

            context.Services.AddAutoMapperObjectMapper<PresentationAccountWebModule>();
            
            Configure<AbpAutoMapperOptions>(options =>
            {
                options.AddProfile<PresentationAccountWebAutomapperProfile>(validate: true);
            });

        }

    }
}