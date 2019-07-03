using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Airbag.Utils
{
    public class RouteWhitelistMatcher
    {
        private static bool UrlMatches(string pattern, string url) =>
            Regex.IsMatch(url, "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$");

        public static bool IsWhitelisted(HttpContext ctx, IEnumerable<string> whitelistedRoutes) =>
            ctx.Request.Path.HasValue &&
            whitelistedRoutes.Any(whitelistedRoute => UrlMatches(whitelistedRoute, ctx.Request.Path.Value));
    }
}