using Volo.Abp.EventBus;

namespace PolpAbp.Presentation.Account.Web.Etos
{
    [EventName(nameof(EmailActivationSuccessEto))]
    public class EmailActivationSuccessEto
    {
        public Guid? TenantId { get; set; }
        public Guid UserId { get; set; }
    }
}
