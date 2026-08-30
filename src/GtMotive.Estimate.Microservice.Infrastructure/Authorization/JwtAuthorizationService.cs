using System.Security.Claims;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.Domain.Interfaces;

namespace GtMotive.Estimate.Microservice.Infrastructure.Authorization
{
    /// <summary>
    /// Simple policy-based authorization that works with JWTs issued elsewhere.
    /// - "Authenticated" policy requires an authenticated principal.
    /// - "role:roleName" checks the role claim (for example, "role:Admin") or ClaimTypes.Role.
    /// - For unknown policies, returns false.
    ///
    /// This class does not perform token validation. Token validation is handled by ASP.NET Core.
    /// Authentication handlers. This service only evaluates policies against the ClaimsPrincipal.
    /// </summary>
    public sealed class JwtAuthorizationService : IAuthorizationService
    {
        public Task<bool> Authorize(ClaimsPrincipal user, object resource, string policyName)
        {
            if (user is null)
            {
                return Task.FromResult(false);
            }

            if (string.IsNullOrWhiteSpace(policyName))
            {
                return Task.FromResult(user.Identity?.IsAuthenticated == true);
            }

            if (policyName.Equals("Authenticated", System.StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(user.Identity?.IsAuthenticated == true);
            }

            const string rolePrefix = "role:";
            if (policyName.StartsWith(rolePrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                var role = policyName[rolePrefix.Length..];
                if (string.IsNullOrEmpty(role))
                {
                    return Task.FromResult(false);
                }

                // Check standard role claim and ClaimTypes.Role.
                if (user.IsInRole(role))
                {
                    return Task.FromResult(true);
                }

                foreach (var c in user.Claims)
                {
                    if ((c.Type == "role" || c.Type == ClaimTypes.Role) && c.Value == role)
                    {
                        return Task.FromResult(true);
                    }
                }

                return Task.FromResult(false);
            }

            // Unknown policy - be conservative and deny.
            return Task.FromResult(false);
        }
    }
}
