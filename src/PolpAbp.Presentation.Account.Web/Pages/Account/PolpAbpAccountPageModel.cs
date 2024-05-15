using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PolpAbp.Framework.Identity;
using PolpAbp.Framework.Security;
using PolpAbp.Framework.Settings;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Localization;
using Volo.Abp.AspNetCore.ExceptionHandling;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Data;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ExceptionHandling;
using Volo.Abp.Identity;
using Volo.Abp.Identity.Settings;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
using static PolpAbp.Presentation.Account.Pages.Account.PolpAbpExternalAuthPageModel;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace PolpAbp.Presentation.Account.Web.Pages.Account;

public abstract class PolpAbpAccountPageModel : AbpPageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrlHash { get; set; }

    public bool IsRecaptchaEnabled { get; set; }

    public PasswordComplexitySetting PwdComplexity { get; private set; }

    public bool IsExternalAuthEnabled = false;
    protected string[] AllowedProviderName = new string[0] { };

    protected IAccountAppService AccountAppService => LazyServiceProvider.LazyGetRequiredService<IAccountAppService>();
    protected SignInManager<IdentityUser> SignInManager => LazyServiceProvider.LazyGetRequiredService<SignInManager<IdentityUser>>();
    protected IdentityUserManager UserManager => LazyServiceProvider.LazyGetRequiredService<IdentityUserManager>();
    protected IdentitySecurityLogManager IdentitySecurityLogManager => LazyServiceProvider.LazyGetRequiredService<IdentitySecurityLogManager>();
    protected IOptions<IdentityOptions> IdentityOptions => LazyServiceProvider.LazyGetRequiredService<IOptions<IdentityOptions>>();
    protected IExceptionToErrorInfoConverter ExceptionToErrorInfoConverter => LazyServiceProvider.LazyGetRequiredService<IExceptionToErrorInfoConverter>();
    protected IDataFilter DataFilter => LazyServiceProvider.LazyGetRequiredService<IDataFilter>();
    protected IIdentityUserRepositoryExt IdentityUserRepositoryExt => LazyServiceProvider.LazyGetRequiredService<IIdentityUserRepositoryExt>();

    protected IConfiguration Configuration => LazyServiceProvider.LazyGetRequiredService<IConfiguration>();
    protected IDistributedEventBus DistributedEventBus => LazyServiceProvider.LazyGetRequiredService<IDistributedEventBus>();

    protected IAuthenticationSchemeProvider SchemeProvider => LazyServiceProvider.LazyGetRequiredService<IAuthenticationSchemeProvider>();

    /// <summary>
    /// Currently the behavior is determined by design, regardless the tenant.
    /// </summary>
    protected bool IsBackgroundEmailEnabled
    {
        get
        {
            // todo: Do we need to introduce the module-specific settings?
            return Configuration.GetValue<bool>("PolpAbp:Framework:BackgroundEmailEnabled");
        }
    }

    protected bool IsEmailGloballyUnique
    {
        get
        {
            // todo: Do we need to introduce the module-specific settings?
            return Configuration.GetValue<bool>("PolpAbp:Framework:IsEmailGloballyUnique");
        }
    }

    protected PolpAbpAccountPageModel()
    {
        PwdComplexity = new PasswordComplexitySetting();
        LocalizationResourceType = typeof(AccountResource);
        ObjectMapperContext = typeof(PresentationAccountWebModule);
    }

    protected virtual Task LoadSettingsAsync()
    {
        return Task.CompletedTask;
    }

    protected async Task ReadInExternalAuthProviderSettingsAsync()
    {
        IsExternalAuthEnabled = await SettingProvider.GetAsync<bool>(FrameworkSettings.Account.Sso.IsEnabled);
        var a = await SettingProvider.GetOrNullAsync(FrameworkSettings.Account.Sso.Providers);
        if (string.IsNullOrEmpty(a))
        {
            AllowedProviderName = new string[] { };
        }
        else
        {
            AllowedProviderName = a.Split(",");
        }
    }

    protected virtual async Task<List<ExternalProviderModel>> GetAllExternalProviders()
    {
        var schemes = await SchemeProvider.GetAllSchemesAsync();

        // todo: Remove some 
        return schemes
            .Where(x => x.DisplayName != null)
            .Select(x => new ExternalProviderModel(x.DisplayName!, x.Name))
            .ToList();
    }

    /// <summary>
    /// Reads the setting about the recaptcha into the property.
    /// May be overriden for the tenant specific settings in the future.
    /// </summary>
    /// <returns></returns>
    protected virtual Task ReadInRecaptchaEnabledAsync()
    {
        IsRecaptchaEnabled = Configuration.GetValue<bool>("PolpAbp:Framework:RecaptchaEnabled");
        return Task.CompletedTask;
    }

    protected string ParseRecaptchResponse()
    {
        var k = Configuration.GetValue<string>("PolpAbp:Framework:RecaptchaResponseKey")??PresentationAccountWebConstants.RecaptchaReponseKey;
        HttpContext.Request.Form.TryGetValue(k, out var response);
        return response;
    }

    // Helpers for validating passwords 
    protected async Task ReadInPasswordComplexityAsync()
    {
        PwdComplexity.RequireDigit = await SettingProvider.GetAsync<bool>(IdentitySettingNames.Password.RequireDigit);
        PwdComplexity.RequireLowercase = await SettingProvider.GetAsync<bool>(IdentitySettingNames.Password.RequireLowercase);
        PwdComplexity.RequireUppercase = await SettingProvider.GetAsync<bool>(IdentitySettingNames.Password.RequireUppercase);
        PwdComplexity.RequireNonAlphanumeric = await SettingProvider.GetAsync<bool>(IdentitySettingNames.Password.RequireNonAlphanumeric);
        PwdComplexity.RequiredLength = await SettingProvider.GetAsync<int>(IdentitySettingNames.Password.RequiredLength);
        PwdComplexity.RequiredUniqueChars = await SettingProvider.GetAsync<int>(IdentitySettingNames.Password.RequiredUniqueChars);
    }

    protected virtual void CheckCurrentTenant(Guid? tenantId)
    {
        if (CurrentTenant.Id != tenantId)
        {
            throw new ApplicationException($"Current tenant is different than given tenant. CurrentTenant.Id: {CurrentTenant.Id}, given tenantId: {tenantId}");
        }
    }

    protected virtual void CheckIdentityErrors(IdentityResult identityResult)
    {
        if (!identityResult.Succeeded)
        {
            throw new UserFriendlyException("Operation failed: " + identityResult.Errors.Select(e => $"[{e.Code}] {e.Description}").JoinAsString(", "));
        }

        //identityResult.CheckErrors(LocalizationManager); //TODO: Get from old Abp
    }

    protected virtual string GetLocalizeExceptionMessage(Exception exception)
    {
        if (exception is ILocalizeErrorMessage || exception is IHasErrorCode)
        {
            return ExceptionToErrorInfoConverter.Convert(exception).Message;
        }

        return exception.Message;
    }

    protected async Task<List<IdentityUser>> FindByEmailBeyondTenantAsync(string email)
    {
        using (DataFilter.Disable<IMultiTenant>())
        {
            var user = await IdentityUserRepositoryExt.FindUsersByEmailAsync(email);
            return user;
        }
    }

    protected async Task<IdentityUser> FindByIdBeyondTenantAsync(Guid id)
    {
        using (DataFilter.Disable<IMultiTenant>())
        {
            // Find lookup the user in the ID
            var user = await IdentityUserRepositoryExt.FindAsync(id);
            return user;
        }
    }
}
