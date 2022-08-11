namespace PolpAbp.Presentation.Account.Web.Settings
{
    public class AccountWebSettingNames
    {
        /// <summary>
        /// Decides if Recaptcha is enabled for the host.
        /// </summary>
        public const string IsHostRecaptchaEnabled = "PolpAbp.Presentation.Account.Web.IsHostRecaptchaEnabled";

        // Decides if Recaptcha is enabled for the tenant.
        public const string IsTenantRecaptchaEnabled = "PolpAbp.Presentation.Account.Web.IsTenantRecaptchaEnabled";

        /// <summary>
        /// Decides if the user name is used for login for the tenant.
        /// </summary>
        public const string IsUserNameEnabled = "PolpAbp.Presentation.Account.Web.IsUserNameEnabled";
    }
}
