using Volo.Abp.Account.Localization;
using Volo.Abp.Localization;
using Volo.Abp.Settings;

namespace PolpAbp.Presentation.Account.Web.Settings
{
    public class AccountSettingDefinitionProvider : SettingDefinitionProvider
    {
        public override void Define(ISettingDefinitionContext context)
        {
            context.Add(
                new SettingDefinition(
                    AccountWebSettingNames.IsTenantRegistrationDisabled,
                    "false",
                    L("DisplayName:Presentation.Account.Web.IsTenantRegistrationDisabled"),
                    L("Description:Presentation.Account.Web.IsTenantRegistrationDisabled"), isVisibleToClients: true)
            );

            context.Add(
                new SettingDefinition(
                    AccountWebSettingNames.IsTenantRecaptchaDisabled,
                    "false",
                    L("DisplayName:Presentation.Account.Web.IsTenantRecaptchaDisabled"),
                    L("Description:Presentation.Account.Web.IsTenantRecaptchaDisabled"), isVisibleToClients: true)
            );

            context.Add(
                new SettingDefinition(
                    AccountWebSettingNames.IsTenantUserNameEnabled,
                    "false",
                    L("DisplayName:Presentation.Account.Web.IsTenantUserNameEnabled"),
                    L("Description:Presentation.Account.Web.IsTenantUserNameEnabled"), isVisibleToClients: true)
            );
        }

        private static LocalizableString L(string name)
        {
            return LocalizableString.Create<AccountResource>(name);
        }
    }
}
