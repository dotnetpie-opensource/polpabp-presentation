using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;
using PolpAbp.Framework.Identity;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public class PasswordValidator
    {
        private readonly PasswordComplexity _setting;
        private readonly IStringLocalizer L;
        private readonly ModelStateDictionary ModelState;

        public PasswordValidator(PasswordComplexity settings, IStringLocalizer l, ModelStateDictionary modelState)
        {
            _setting = settings;
            L = l;
            ModelState = modelState;
        }

        public bool ValidateComplexity(string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Password", "Password is required");
                return false;
            }

            if (password.Length < _setting.RequiredLength)
            {
                ModelState.AddModelError("Password", L["The field {0} must be a string with a minimum length of {1}.", "Password", _setting.RequiredLength]);
                return false;
            }

            if (_setting.RequireUppercase && !password.Any(char.IsUpper))
            {
                ModelState.AddModelError("Password", "An upper case letter is required in the password!");
                return false;
            }

            if (_setting.RequireLowercase && !password.Any(char.IsLower))
            {
                ModelState.AddModelError("Password", "A lower case letter is required in the password!");
                return false;
            }

            if (_setting.RequireDigit && !password.Any(char.IsNumber))
            {
                ModelState.AddModelError("Password", "A number (letter) is required in the password!");
                return false;
            }

            if (_setting.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError("Password", "A special symbol is required in the password!");
                return false;
            }

            return true;
        }

        public bool ValidateConfirmPassword(string? password, string? confirmPassword)
        {
            if (!Equals(password, confirmPassword))
            {
                ModelState.AddModelError("ConfirmPassword", L["'{0}' and '{1}' do not match.", "Confirm Password", "Password"]);
                return false;
            }

            return true;
        }
    }
}
