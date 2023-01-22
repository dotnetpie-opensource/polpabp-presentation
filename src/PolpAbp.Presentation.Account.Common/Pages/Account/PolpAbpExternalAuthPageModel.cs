using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PolpAbp.Framework.Emailing.Account;
using PolpAbp.Framework.Settings;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Localization;
using Volo.Abp.AspNetCore.ExceptionHandling;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.ExceptionHandling;
using Volo.Abp.Identity;
using Volo.Abp.Security.Claims;
using Volo.Abp.Settings;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace PolpAbp.Presentation.Account.Pages.Account;

public abstract class PolpAbpExternalAuthPageModel : AbpPageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrlHash { get; set; }

    protected IAccountAppService AccountAppService => LazyServiceProvider.LazyGetRequiredService<IAccountAppService>();
    protected SignInManager<IdentityUser> SignInManager => LazyServiceProvider.LazyGetRequiredService<SignInManager<IdentityUser>>();
    protected IdentityUserManager UserManager => LazyServiceProvider.LazyGetRequiredService<IdentityUserManager>();
    protected IdentitySecurityLogManager IdentitySecurityLogManager => LazyServiceProvider.LazyGetRequiredService<IdentitySecurityLogManager>();
    protected IOptions<IdentityOptions> IdentityOptions => LazyServiceProvider.LazyGetRequiredService<IOptions<IdentityOptions>>();
    protected IExceptionToErrorInfoConverter ExceptionToErrorInfoConverter => LazyServiceProvider.LazyGetRequiredService<IExceptionToErrorInfoConverter>();

    protected readonly IAuthenticationSchemeProvider SchemeProvider;

    protected IConfiguration Configuration => LazyServiceProvider.LazyGetRequiredService<IConfiguration>();
    protected IFrameworkAccountEmailer AccountEmailer => LazyServiceProvider.LazyGetRequiredService<IFrameworkAccountEmailer>();

    protected MemberRegistrationEnum RegistrationType = MemberRegistrationEnum.RequireEmailActivation;
    protected bool IsNewRegistrationNotyEnabled = false;

    public bool IsExternalAuthEnabled = false;
    protected string[] AllowedProviderName = new string[0] { };

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

    protected PolpAbpExternalAuthPageModel(IAuthenticationSchemeProvider schemeProvider)
    {
        SchemeProvider = schemeProvider;
        LocalizationResourceType = typeof(AccountResource);
    }

    protected virtual async Task LoadSettingsAsync()
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

        // Load
        RegistrationType = (MemberRegistrationEnum)(await SettingProvider.GetAsync<int>(FrameworkSettings.Account.RegistrationApprovalType));
        IsNewRegistrationNotyEnabled = await SettingProvider.GetAsync<bool>(FrameworkSettings.Account.IsNewRegistrationNotyEnabled);
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

    protected static bool IsEmailRetrievedFromExternalLogin(ExternalLoginInfo externalLoginInfo)
    {
        return externalLoginInfo.Principal.FindFirstValue(AbpClaimTypes.Email) != null;
    }

    protected static bool IsUserNameRetrievedFromExternalLogin(ExternalLoginInfo externalLoginInfo)
    {
        return externalLoginInfo.Principal.FindFirstValue("preferred_username") != null;
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

    public class ExternalProviderModel
    {
        public string DisplayName { get; set; }
        public string AuthenticationScheme { get; set; }
        public string? LoginPage { get; set; }

        public ExternalProviderModel(string name, string scheme)
        {
            DisplayName = name;
            AuthenticationScheme = scheme;
        }

        public ExternalProviderModel(ExternalProviderModel that)
        {
            DisplayName = that.DisplayName;
            AuthenticationScheme = that.AuthenticationScheme;
        }
    }

}
