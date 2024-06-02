using AspNetCore.ReCaptcha;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using PolpAbp.Framework.Authorization.Users;
using PolpAbp.Framework.DistributedEvents.Account;
using PolpAbp.Framework.Emailing.Account;
using PolpAbp.Framework.Exceptions.Identity;
using PolpAbp.Framework.Identity;
using PolpAbp.Framework.Identity.Dto;
using PolpAbp.Framework.Security;
using PolpAbp.Framework.Settings;
using PolpAbp.ZeroAdaptors.Authorization.Users;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlTypes;
using System.Web;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Auditing;
using Volo.Abp.Identity;
using Volo.Abp.Identity.Settings;
using Volo.Abp.Settings;
using Volo.Abp.Validation;
using static PolpAbp.Presentation.Account.Pages.Account.PolpAbpExternalAuthPageModel;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [OnlyAnonymous]
    [TenantPrerequisite]
    public partial class MemberRegisterModel : PolpAbpAccountPageModel
    {
        [BindProperty]
        public PostInput Input { get; set; }

        public readonly List<ExternalProviderModel> SsoProviders = new List<ExternalProviderModel>();

        protected readonly IMemberEnrollmentAppService _memberEnrollmentAppService;

        protected MemberRegistrationEnum RegistrationType = MemberRegistrationEnum.RequireEmailActivation;
        protected bool IsNewRegistrationNotyEnabled = false;

        protected readonly IReCaptchaService RecaptchaService;

        public MemberRegisterModel(IReCaptchaService reCaptchaService,
            IMemberEnrollmentAppService memberEnrollmentAppService
            ) : base()
        {
            Input = new PostInput();

            RecaptchaService = reCaptchaService;
            _memberEnrollmentAppService = memberEnrollmentAppService;
        }


        public virtual async Task<IActionResult> OnGetAsync()
        {
            // Load settings
            await LoadSettingsAsync();

            return Page();
        }

        public virtual async Task<IActionResult> OnPostAsync(string action)
        {            
            // Load settings
            await LoadSettingsAsync();

            if (IsRecaptchaEnabled)
            {
                var recaptchaValue = ParseRecaptchResponse();
                var isGood = await RecaptchaService.VerifyAsync(recaptchaValue);
                if (!isGood)
                {
                    // TODO: localization
                    Alerts.Danger("Please verify that you are not a robot.");

                    return Page();
                }
            }

            if (action == "Input")
            {
                try
                {
                    // trim
                    Input.EmailAddress = Input!.EmailAddress!.Trim();
                    Input.Password = Input!.Password!.Trim();
                    Input.ConfirmPassword = Input!.ConfirmPassword!.Trim();

                    // Tenant not set this moment.
                    ValidateModel();

                    var createdRet = await _memberEnrollmentAppService.ProcessUserRegistrationAsync("MVC", new MemberEnrollmentInputDto
                    {
                        EmailAddress = Input.EmailAddress,
                        Password = Input.Password,
                        UserName = Input.EmailAddress,
                        FirstName = Input.FirstName,
                        LastName = Input.LastName
                    });

                    if (createdRet.ErrorCode == (int)UserOnboardingErrorEnum.EmailAlreadyUsed)
                    {
                        var errorDetail = IdentityErrorLikeInterpretor.Translate(UserOnboardingErrorEnum.EmailAlreadyUsed);

                        Alerts.Danger(errorDetail.Description);
                    }
                    else if (createdRet.ErrorCode == (int)UserOnboardingErrorEnum.UserNameUsed)
                    {
                        var errorDetail = IdentityErrorLikeInterpretor.Translate(UserOnboardingErrorEnum.UserNameUsed);

                        Alerts.Danger(errorDetail.Description);
                    }
                    else if (createdRet.ErrorCode == (int)UserOnboardingErrorEnum.PasswordComplexityNotEnough)
                    {
                        var errorDetail = IdentityErrorLikeInterpretor.Translate(UserOnboardingErrorEnum.PasswordComplexityNotEnough);

                        Alerts.Danger(errorDetail.Description);
                    }
                    else if (createdRet.ErrorCode == (int)UserOnboardingErrorEnum.MemberLicenseShortage)
                    {
                        var errorDetail = IdentityErrorLikeInterpretor.Translate(UserOnboardingErrorEnum.MemberLicenseShortage);

                        Alerts.Danger(errorDetail.Description);
                    }
                    else if (!createdRet.UserId.HasValue)
                    {
                        Alerts.Danger("Something went wrong. Please try it later.");
                    }
                    else
                    {
                        Alerts.Success($"Welcome aboard, {Input.FirstName}!  Your account has been created successfully.");
                        // Success 
                        if (createdRet.NextActionStatus == UserOnboardingNextActionEnum.WaitAdminApprovel)
                        {
                            Alerts.Warning("The organization's administrator will review your request shortly. In the meantime, feel free to browse our website and learn more about what we do.");
                        }
                        else if (createdRet.NextActionStatus == UserOnboardingNextActionEnum.ActivateEmail)
                        {
                            Alerts.Warning("We've just sent a confirmation email to you.  Click the link in the email to activate your account and get started.");
                        }
                        else
                        {
                            var user = await UserManager.FindByIdAsync(createdRet.UserId!.Value.ToString());
                            await SignInManager.SignInAsync(user, true);
                        }

                        return RedirectToPage("./MemberRegisterSuccess");
                    }

                }
                catch (AbpValidationException ex)
                {
                    // Handle this error.
                    foreach (var a in ex.ValidationErrors)
                    {
                        Alerts.Add(Volo.Abp.AspNetCore.Mvc.UI.Alerts.AlertType.Danger, a.ErrorMessage);
                    }
                }
                catch (UserFriendlyException ex)
                {
                    Alerts.Add(Volo.Abp.AspNetCore.Mvc.UI.Alerts.AlertType.Danger, ex.Message);
                }
            }

            return Page();
        }

        protected override void ValidateModel()
        {
            var passwordValidator = new PasswordValidator(PwdComplexity, L, ModelState);
            if (passwordValidator.ValidateComplexity(Input.Password))
            {
                passwordValidator.ValidateConfirmPassword(Input.Password, Input.ConfirmPassword);
            }

            base.ValidateModel();
        }

        protected override async Task LoadSettingsAsync()
        {
            await base.LoadSettingsAsync();
            await ReadInRecaptchaEnabledAsync();
            await ReadInPasswordComplexityAsync();

            // Load
            RegistrationType = (MemberRegistrationEnum)(await SettingProvider.GetAsync<int>(FrameworkSettings.Account.RegistrationApprovalType));
            IsNewRegistrationNotyEnabled = await SettingProvider.GetAsync<bool>(FrameworkSettings.Account.IsNewRegistrationNotyEnabled);

            await ReadInExternalAuthProviderSettingsAsync();
            // Build up the login providers 
            var providers = await GetAllExternalProviders();

            var candidates = providers
                .Where(x => AllowedProviderName.Any(y => x.DisplayName.Contains(y)));
            foreach (var z in candidates)
            {
                var p = Configuration[$@"PolpAbp:ExternalLogin:{z.AuthenticationScheme}:RegisterPage"];
                if (!string.IsNullOrEmpty(p))
                {
                    SsoProviders.Add(new ExternalProviderModel(z)
                    {
                        RegisterPage = p
                    });
                }
            }
        }

        protected override async Task ReadInRecaptchaEnabledAsync()
        {
            await base.ReadInRecaptchaEnabledAsync();
            if (IsRecaptchaEnabled)
            {
                IsRecaptchaEnabled = await SettingProvider.GetAsync<bool>(FrameworkSettings.Security.UseCaptchaOnRegistration);
            }
        }

        public class PostInput : IHasConfirmPassword
        {
            // Admin information below
            // todo: Some email address is not allowe?
            [Required]
            [EmailAddress]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxEmailLength))]
            public string? EmailAddress { get; set; }

            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPasswordLength))]
            [DataType(DataType.Password)]
            [DisableAuditing]
            public string? Password { get; set; }

            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPasswordLength))]
            [DataType(DataType.Password)]
            [DisableAuditing]
            public string? ConfirmPassword { get; set; }

            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxNameLength))]
            public string? FirstName { get; set; }

            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxSurnameLength))]
            public string? LastName { get; set; }

        }
    }
}
