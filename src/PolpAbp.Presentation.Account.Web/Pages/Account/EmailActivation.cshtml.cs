using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
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

        public EmailActivationModel(ITenantResolveResultAccessor tenantResolveResultAccessor,
            ITenantRepository tenantRepository) : base()
        {
            TenantResolveResultAccessor = tenantResolveResultAccessor;
            TenantRepository = tenantRepository;
        }

        public async virtual Task<IActionResult> OnGet()
        {
            var tenant = await TenantRepository.GetAsync(TenantId);
            if (tenant != null)
            {
                TenantName = tenant.Name;
                using (CurrentTenant.Change(tenant.Id))
                {
                    var user = await UserManager.GetByIdAsync(UserId);
                    if (user != null)
                    {
                        if (user.EmailConfirmed)
                        {
                            State = ActivationState.Already;
                        }
                        else
                        {
                            // TODO: Activation
                            try
                            {
                                await UserManager.ConfirmEmailAsync(user, ConfirmationCode);
                                State = ActivationState.Success;
                            }
                            catch (Exception e)
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