// On purpose we do not include extensions in the namespace
namespace PolpAbp.Presentation.Account.Web
{
    public static class CookieExtensions
    {
        public const string TenantCookieName = "PolpAbpTenantId";
        public const string MfaCookieName = "MultiFactorAuth";

        #region Tenant
        public static bool HasTenantCookie(this HttpRequest request)
        {
            var c = request.Cookies[TenantCookieName];
            return c != null;
        }
        public static string ReadTenantCookieValue(this HttpRequest request)
        {
            var c = request.Cookies[TenantCookieName];
            return c ?? string.Empty;
        }

        public static void SetTenantCookieValue(this HttpResponse response, string value, string domain = null, TimeSpan? span = null)
        {
            var options = new CookieOptions();
            if (!string.IsNullOrWhiteSpace(domain))
            {
                options.Domain = domain;    
            }
            if (span.HasValue)
            {
                var now = DateTimeOffset.UtcNow;
                var expired = now + span;
                options.Expires = expired;
            }
            // Remember 
            response.Cookies.Append(TenantCookieName, value, options);
        }
        #endregion

        #region mfa
        public static bool HasMfaCookie(this HttpRequest request)
        {
            var mfaCookie = request.Cookies[MfaCookieName];
            return mfaCookie != null;
        }
        public static string ReadMfaCookieValue(this HttpRequest request)
        {
            var mfaCookie = request.Cookies[MfaCookieName];
            return mfaCookie ?? string.Empty;
        }

        public static void SetMfaCookieValue(this HttpResponse response, string value, string domain, TimeSpan? span = null)
        {
            var options = new CookieOptions
            {
                Domain = domain
            };
            if (span.HasValue)
            {
                var now = DateTimeOffset.UtcNow;
                var expired = now + span;
                options.Expires = expired;
            }
            // Remember 
            response.Cookies.Append(MfaCookieName, value, options);
        }
        #endregion


        // Clean all cookie
        public static void ClearCookies(this HttpResponse response, string domain)
        {
            var options = new CookieOptions
            {
                Domain = domain,
            };

            response.Cookies.Delete(TenantCookieName, options);
        }

    }
}
