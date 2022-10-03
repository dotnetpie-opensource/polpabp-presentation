// On purpose we do not include extensions in the namespace
using Microsoft.AspNetCore.Http;

namespace PolpAbp.Presentation.Account
{
    public static class CookieExtensions
    {
        // todo: Configurable
        public const string TenantCookieName = "PolpAbpTenantId";
        public const string MfaCookieName = "MultiFactorAuth";

        public static void SetNamedCookie(this HttpResponse response, string name, string value, string? domain = null, TimeSpan? span = null)
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
            response.Cookies.Append(name, value, options);
        }

        // Clean all cookie
        public static void ClearCookies(this HttpResponse response, string domain)
        {
            var options = new CookieOptions
            {
                Domain = domain,
            };

            response.Cookies.Delete(TenantCookieName, options);
        }

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

        public static void SetTenantCookieValue(this HttpResponse response, string value, string? domain = null, TimeSpan? span = null)
        {
            response.SetNamedCookie(TenantCookieName, value, domain, span);
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
            response.SetNamedCookie(MfaCookieName, value, domain, span);
        }
        #endregion



    }
}
