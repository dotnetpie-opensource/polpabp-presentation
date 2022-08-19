using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PolpAbp.Presentation.Account.Web.Pages.Account;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
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
        public string ConfirmationCode { get; set; }

        public string TenantName { get; set; }

        public string EmailName { get; set; }

        public ActivationState State { get; set; }

        protected virtual ITenantResolveResultAccessor TenantResolveResultAccessor { get; }
        private readonly ITenantRepository _tenantRepository;

        public EmailActivationModel(ITenantResolveResultAccessor tenantResolveResultAccessor,
            ITenantRepository tenantRepository)
        {
            TenantResolveResultAccessor = tenantResolveResultAccessor;
            _tenantRepository = tenantRepository;
        }

        public async virtual Task<IActionResult> OnGet()
        {
            var tenant = await _tenantRepository.GetAsync(TenantId);
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