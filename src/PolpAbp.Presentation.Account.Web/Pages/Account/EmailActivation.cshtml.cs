using Microsoft.AspNetCore.Mvc;
using PolpAbp.Presentation.Account.Web.Etos;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    // This page runs regardless the tenant.
    // It will use the 
    [OnlyAnonymous]
    public class EmailActivationModel : LoginModelBase
    {
        [HiddenInput]
        [BindProperty(SupportsGet = true)]
        public Guid TenantId { get; set; }

        [Required]
        [HiddenInput]
        [BindProperty(SupportsGet = true)]
        public Guid UserId { get; set; }

        [Required]
        [HiddenInput]
        [BindProperty(SupportsGet = true)]
        public string? ConfirmationCode { get; set; }

        public string? TenantName { get; set; }

        public string? EmailName { get; set; }

        public ActivationState State { get; set; }

        protected readonly ITenantResolveResultAccessor TenantResolveResultAccessor;
        protected readonly ITenantRepository TenantRepository;
        protected readonly IDistributedEventBus DistributedEventBus;

        public EmailActivationModel(ITenantResolveResultAccessor tenantResolveResultAccessor,
            ITenantRepository tenantRepository,
            IDistributedEventBus distributedEventBus) : base()
        {
            TenantResolveResultAccessor = tenantResolveResultAccessor;
            TenantRepository = tenantRepository;
            DistributedEventBus = distributedEventBus;
        }

        public async virtual Task<IActionResult> OnGet()
        {
            await LoadSettingsAsync();

            var tenant = await TenantRepository.GetAsync(TenantId);
            if (tenant != null)
            {
                TenantName = tenant.Name;
                using (CurrentTenant.Change(tenant.Id))
                {
                    var user = await UserManager.GetByIdAsync(UserId);
                    if (user != null)
                    {
                        if (user.EmailConfirmed && user.IsActive)
                        {
                            State = ActivationState.Already;
                        }
                        else
                        {
                            // The following call is supposed to verify if the token is valid or not. 
                            var confirmRet = await UserManager.ConfirmEmailAsync(user, ConfirmationCode);
                            if (confirmRet.Succeeded)
                            {
                                // On purpose, we check active after confirming the email ...
                                if (!user.IsActive)
                                {
                                    user.SetIsActive(true);
                                    await UserManager.UpdateAsync(user);
                                }
                                State = ActivationState.Success;

                                // It's ok if the following runs into some error.
                                await DistributedEventBus.PublishAsync(new EmailActivationSuccessEto
                                {
                                    UserId = user.Id,
                                    TenantId = tenant.Id
                                });
                            }
                            else
                            {
                                State = ActivationState.Failure;
                            }
                        }
                    }
                }
            }

            return Page();
        }

        public enum ActivationState
        {
            Undefined = 0,
            Already = 1,
            Success = 2,
            Failure = 3
        }

    }
}