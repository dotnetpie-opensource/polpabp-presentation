﻿using System.Web;
using Microsoft.AspNetCore.Mvc;
using PolpAbp.Framework.Settings;
using Volo.Abp.Settings;

namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public abstract class LoginModelBase : PolpAbpAccountPageModel
    {
        [BindProperty(SupportsGet = true)]
        public string? UserName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? EmailAddress { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool IsUsingUserName { get; set; }

        public string NormalizedUserName => UserName ?? string.Empty;

        public string NormalizedEmailAddress => EmailAddress ?? string.Empty;

        protected override async Task LoadSettingsAsync()
        {
            await base.LoadSettingsAsync();
            // Recaptcha 
            await ReadInRecaptchaEnabledAsync();
        }

        protected override async Task ReadInRecaptchaEnabledAsync()
        {
            await base.ReadInRecaptchaEnabledAsync();

            if (IsRecaptchaEnabled)
            {
                IsRecaptchaEnabled = await SettingProvider.GetAsync<bool>(FrameworkSettings.Security.UseCaptchaOnLogin);
            }
        }

    }
}
