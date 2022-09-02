namespace PolpAbp.Presentation.Account.Web.Settings
{
    public class AccountWebSettingNames
    {
        // Decides if Recaptcha is enabled for the tenant.
        public const string IsTenantRecaptchaDisabled = "PolpAbp.Presentation.Account.Web.IsTenantRecaptchaDisabled";

        /// <summary>
        /// Decides if the user name is used for login for the tenant.
        /// </summary>
        public const string IsTenantUserNameEnabled = "PolpAbp.Presentation.Account.Web.IsTenantUserNameEnabled";

        /// <summary>
        /// Decides if the registration is disabled or not for the tenant .
        /// </summary>
        public const string IsTenantRegistrationDisabled = "PolpAbp.Presentation.Account.Web.IsTenantRegistrationDisabled";

    }
}
