﻿using Volo.Abp.Account;
using Volo.Abp.Account.Localization;
using Volo.Abp.AspNetCore.Mvc.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AutoMapper;
using Volo.Abp.ExceptionHandling;
using Volo.Abp.Identity.AspNetCore;
using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace PolpAbp.Presentation.Account.Web
{
    [DependsOn(
    typeof(AbpAccountApplicationContractsModule),
    typeof(AbpIdentityAspNetCoreModule),
    typeof(AbpAspNetCoreMvcUiThemeSharedModule),
    typeof(AbpExceptionHandlingModule),
    typeof(AbpAutoMapperModule)
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

            context.Services.AddAutoMapperObjectMapper<PresentationAccountWebModule>();
            Configure<AbpAutoMapperOptions>(options =>
            {
                options.AddProfile<PresentationAccountWebAutomapperProfile>(validate: true);
            });

        }

    }
}