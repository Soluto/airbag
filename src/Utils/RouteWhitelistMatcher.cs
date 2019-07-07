using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Airbag.Utils
{
    public class RouteWhitelistMatcher
    {
        private readonly Regex[] patterns;

        public RouteWhitelistMatcher(IEnumerable<string> whitelistedRoutes)
        {
            this.patterns = whitelistedRoutes
                .Select(pattern => new Regex("^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$"))
                .ToArray();
        }

        public bool IsMatch(PathString path)
        {
            return path.HasValue &&
                   patterns.Any(pattern => pattern.IsMatch(path));
        }
    }
}