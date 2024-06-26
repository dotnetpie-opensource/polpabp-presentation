﻿namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public static class ExternalAuthProviderHelper
    {
        public static string GetPrettySsoSignUpName(string provider)
        {
            return provider switch { 
                 "AzureAd" => "Join with Microsoft SSO",
                 "Google" => "Join with Google SSO",
                 _ => provider
            };
        }
    }
}
