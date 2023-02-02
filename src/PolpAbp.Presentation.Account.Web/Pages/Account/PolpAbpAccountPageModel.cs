using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PolpAbp.Framework.Security;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Localization;
using Volo.Abp.AspNetCore.ExceptionHandling;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.ExceptionHandling;
using Volo.Abp.Identity;
using Volo.Abp.Identity.Settings;
using Volo.Abp.Settings;
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

    protected IAccountAppService AccountAppService => LazyServiceProvider.LazyGetRequiredService<IAccountAppService>();
    protected SignInManager<IdentityUser> SignInManager => LazyServiceProvider.LazyGetRequiredService<SignInManager<IdentityUser>>();
    protected IdentityUserManager UserManager => LazyServiceProvider.LazyGetRequiredService<IdentityUserManager>();
    protected IdentitySecurityLogManager IdentitySecurityLogManager => LazyServiceProvider.LazyGetRequiredService<IdentitySecurityLogManager>();
    protected IOptions<IdentityOptions> IdentityOptions => LazyServiceProvider.LazyGetRequiredService<IOptions<IdentityOptions>>();
    protected IExceptionToErrorInfoConverter ExceptionToErrorInfoConverter => LazyServiceProvider.LazyGetRequiredService<IExceptionToErrorInfoConverter>();

    protected IConfiguration Configuration => LazyServiceProvider.LazyGetRequiredService<IConfiguration>();

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

    /// <summary>
    /// Reads the setting about the recaptcha into the property.
    /// May be overriden for the tenant specific settings in the future.
    /// </summary>
    /// <returns></returns>
    protected virtual Task ReadInRecaptchaEnabledAsync()
    {
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
}
