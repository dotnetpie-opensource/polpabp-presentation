using AspNetCore.ReCaptcha;
using Microsoft.AspNetCore.Mvc;
using PolpAbp.Framework.Settings;
using System.ComponentModel.DataAnnotations;
using System.Web;
using Volo.Abp.Account;
using Volo.Abp.Auditing;
using Volo.Abp.Identity;
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

        protected readonly IReCaptchaService RecaptchaService;

        public MemberRegisterModel(IReCaptchaService reCaptchaService) : base()
        {
            Input = new PostInput();
            RecaptchaService = reCaptchaService;
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

                    // Tenant not set this moment.
                    ValidateModel();

                    // trim
                    Input.EmailAddress = Input.EmailAddress.Trim();
                    Input.Password = Input.Password.Trim();

                    // Check if this email is available or not
                    var userInfo = await UserManager.FindByEmailAsync(Input.EmailAddress);
                    if (userInfo != null)
                    {
                        Alerts.Warning("Please log into your account.");
                        return RedirectToPage("./Login", new
                        {
                            email = HttpUtility.UrlEncode(userInfo.Email)
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
                    await UserManager.UpdateAsync(user);

                    // todo: decide if we can activate this user ..

                    // todo: Message 


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
        }

        protected override async Task ReadInPasswordComplexityAsync()
        {
            PwdComplexity.RequireDigit = await SettingProvider.GetAsync<bool>(FrameworkSettings.TenantAccountPassComplexityRequireDigit);
            PwdComplexity.RequireLowercase = await SettingProvider.GetAsync<bool>(FrameworkSettings.TenantAccountPassComplexityRequireLowercase);
            PwdComplexity.RequireUppercase = await SettingProvider.GetAsync<bool>(FrameworkSettings.TenantAccountPassComplexityRequireUppercase);
            PwdComplexity.RequireNonAlphanumeric = await SettingProvider.GetAsync<bool>(FrameworkSettings.TenantAccountPassComplexityRequireNonAlphanumeric);
            PwdComplexity.RequiredLength = await SettingProvider.GetAsync<int>(FrameworkSettings.TenantAccountPassComplexityRequiredLength);
        }

        public class PostInput : IHasConfirmPassword
        {
            // Admin information below
            // todo: Some email address is not allowe?
            [Required]
            [EmailAddress]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxEmailLength))]
            public string EmailAddress { get; set; }

            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPasswordLength))]
            [DataType(DataType.Password)]
            [DisableAuditing]
            public string Password { get; set; }

            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxPasswordLength))]
            [DataType(DataType.Password)]
            [DisableAuditing]
            public string ConfirmPassword { get; set; }

            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxNameLength))]
            public string FirstName { get; set; }

            [Required]
            [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxSurnameLength))]
            public string LastName { get; set; }

        }
    }
}
