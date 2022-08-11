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
                    AccountWebSettingNames.IsHostRecaptchaEnabled,
                    "false",
                    L("DisplayName:Presentation.Account.Web.IsHostRecaptchaEnabled"),
                    L("Description:Presentation.Account.Web.IsHostRecaptchaEnabled"), isVisibleToClients: true)
            );

            context.Add(
                new SettingDefinition(
                    AccountWebSettingNames.IsTenantRecaptchaEnabled,
                    "false",
                    L("DisplayName:Presentation.Account.Web.IsTenantRecaptchaEnabled"),
                    L("Description:Presentation.Account.Web.IsTenantRecaptchaEnabled"), isVisibleToClients: true)
            );

            context.Add(
                new SettingDefinition(
                    AccountWebSettingNames.IsUserNameEnabled,
                    "false",
                    L("DisplayName:Presentation.Account.Web.IsUserNameEnabled"),
                    L("Description:Presentation.Account.Web.IsUserNameEnabled"), isVisibleToClients: true)
            );
        }

        private static LocalizableString L(string name)
        {
            return LocalizableString.Create<AccountResource>(name);
        }
    }
}
