using Localization.Resources.AbpUi;
using Volo.Abp.Account.Localization;
using Volo.Abp.UI.Navigation;

namespace PolpAbp.Presentation.Account.Web;

public class PresentationAccountUserMenuContributor : IMenuContributor
{
    public virtual Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name != StandardMenus.User)
        {
            return Task.CompletedTask;
        }

        var uiResource = context.GetLocalizer<AbpUiResource>();
        var accountResource = context.GetLocalizer<AccountResource>();

        // It it okay to use /Account/MainApp here, because it will be redirected to the real entry page.
        context.Menu.AddItem(new ApplicationMenuItem("Account.MainApp", accountResource["MainApp"], url: "/auth/redirect", icon: "fa fa-browser", order: 1000));
        context.Menu.AddItem(new ApplicationMenuItem("Account.Logout", uiResource["Logout"], url: "/account/sign-out", icon: "fa fa-power-off", order: int.MaxValue - 1000));

        return Task.CompletedTask;
    }
}
