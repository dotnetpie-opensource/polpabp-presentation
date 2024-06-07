using Microsoft.AspNetCore.Mvc;
using PolpAbp.Presentation.Account.Web.Etos;
using PolpAbp.Framework;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using PolpAbp.Framework.DistributedEvents.Account;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    // This page runs regardless the tenant.
    // This page runs regardless whether a user is logged into 
    // the system or not.
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

        public string? MaskedEmailAddress { get; set; }

        public ActivationState State { get; set; }

        protected readonly ITenantResolveResultAccessor TenantResolveResultAccessor;
        protected readonly ITenantRepository TenantRepository;

        public EmailActivationModel(ITenantResolveResultAccessor tenantResolveResultAccessor,
            ITenantRepository tenantRepository) : base()
        {
            TenantResolveResultAccessor = tenantResolveResultAccessor;
            TenantRepository = tenantRepository;
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
                        EmailAddress = user.Email;
                        MaskedEmailAddress = EmailAddress.MaskEmailAddress();

                        if (user.EmailConfirmed && user.IsActive)
                        {
                            return RedirectToPage("/Account/EmailActivationSuccess");
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

                                    // Raise an event
                                    await DistributedEventBus.PublishAsync(new AccountStateChangeEto
                                    {
                                        TenantId = user.TenantId!.Value,
                                        AccountId = user.Id,
                                        ChangeId = AccountStateChangeEnum.ActivatedOnItsOwn
                                    });
                                }
                                State = ActivationState.Success;

                                // It's ok if the following runs into some error.
                                await DistributedEventBus.PublishAsync(new EmailActivationSuccessEto
                                {
                                    UserId = user.Id,
                                    TenantId = tenant.Id
                                });

                                return RedirectToPage("/Account/EmailActivationSuccess");

                            }
                            else if (confirmRet.Errors.Any(a => a.Code.ToLower().Contains("invalidtoken")))
                            {
                                State = ActivationState.InvalidToken;

                            }
                            else
                            {
                                State = ActivationState.Failure;
                            }
                        }
                    }
                }
            }

            return RedirectToPage("/Account/EmailActivationError");
        }

        public enum ActivationState
        {
            Undefined = 0,
            Already = 1,
            Success = 2,
            InvalidToken = 10,
            Failure = 100
        }

    }
}