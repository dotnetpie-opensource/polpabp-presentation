using AspNetCore.ReCaptcha;
using Microsoft.AspNetCore.Mvc;
using PolpAbp.Framework.Emailing.Account;
using PolpAbp.Framework.Security;
using PolpAbp.Framework.Settings;
using System.ComponentModel.DataAnnotations;
using System.Web;
using Volo.Abp.Account;
using Volo.Abp.Auditing;
using Volo.Abp.Identity;
using Volo.Abp.Identity.Settings;
using Volo.Abp.Settings;
using Volo.Abp.Validation;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    [OnlyAnonymous]
    [TenantPrerequisite]
    public partial class MemberRegisterModel : PolpAbpAccountPageModel
    {
        [BindProperty]
        public PostInput Input { get; set; }

        protected MemberRegistrationEnum RegistrationType = MemberRegistrationEnum.RequireEmailActivation;
        protected bool IsNewRegistrationNotyEnabled = false;

        protected readonly IReCaptchaService RecaptchaService;
        protected readonly IFrameworkAccountEmailer AccountEmailer;


        public MemberRegisterModel(IReCaptchaService reCaptchaService,
            IFrameworkAccountEmailer accountEmailer) : base()
        {
            Input = new PostInput();

            RecaptchaService = reCaptchaService;
            AccountEmailer = accountEmailer;
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

                    // Check if this email is available or not
                    var userInfo = await UserManager.FindByEmailAsync(Input.EmailAddress);
                    if (userInfo != null)
                    {
                        Alerts.Warning("Please log into your account.");
                        return RedirectToPage("./Login", new
                        {
                            email = userInfo.Email
                        });
                    }

                    var userDto = await AccountAppService.RegisterAsync(
                        new RegisterDto
                        {
                            AppName = "MVC",
                            EmailAddress = Input.EmailAddress,
                            Password = Input.Password,
                            UserName = Input.EmailAddress // use Email address
                        });

                    var user = await UserManager.GetByIdAsync(userDto.Id);
                    user.Name = Input.FirstName;
                    user.Surname = Input.LastName;
                    // The above registration will init IsActive to be true,
                    // We have to reverse the behavior.
                    // The following logic will decide whether the new member
                    // will be active or not.
                    user.SetIsActive(false);
                    await UserManager.UpdateAsync(user);

                    if (RegistrationType == MemberRegistrationEnum.AutoActive)
                    {
                        // Make the user be active now.
                        user.SetIsActive(true);
                        await UserManager.UpdateAsync(user);

                        if (IsNewRegistrationNotyEnabled)
                        {
                            await AccountEmailer.SendMemberRegistrationNotyAsync(user!.Id);
                        }

                        Alerts.Success("Your account is ready for use. Please login!");
                    }
                    else if (RegistrationType == MemberRegistrationEnum.RequireAdminApprovel)
                    {
                        // todo: Send out an email to the admin for the approval.
                        await AccountEmailer.SendMemberRegistrationApprovalAsync(user!.Id);

                        Alerts.Success("Your account is almost ready. Your registation request has been sent to the administrators of your organization. Please wait for their approval.");
                    }
                    else
                    {
                        // todo: Send out an activation email.
                        // Send out a confirmation email, regardless the current tenant.
                        // Send it instantly, because the user is waiting for it.
                        await AccountEmailer.SendEmailActivationLinkAsync(user!.Id);

                        if (IsNewRegistrationNotyEnabled)
                        {
                            await AccountEmailer.SendMemberRegistrationNotyAsync(user!.Id);
                        }

                        Alerts.Success("Your account is almost ready. An email has been sent to your email box. Please check your email box to confirm your account.");
                    }

                    return RedirectToPage("./MemberRegisterSuccess");
                }
                catch (AbpValidationException ex)
                {
                    // Handle this error.
                    foreach (var a in ex.ValidationErrors)
                    {
                        Alerts.Add(Volo.Abp.AspNetCore.Mvc.UI.Alerts.AlertType.Danger, a.ErrorMessage);
                    }
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
        }

        protected override async Task ReadInRecaptchaEnabledAsync()
        {
            IsRecaptchaEnabled = await SettingProvider.GetAsync<bool>(FrameworkSettings.Security.UseCaptchaOnRegistration);
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
