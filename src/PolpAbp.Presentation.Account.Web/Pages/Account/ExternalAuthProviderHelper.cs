namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public static class ExternalAuthProviderHelper
    {
        public static string GetPrettySsoSignUpName(string provider)
        {
            return provider switch { 
                 "AzureAd" => "Sign Up with Microsoft",
                 "Google" => "Sign up with Google",
                 _ => provider
            };
        }
    }
}
