using System.Security.Claims;
using System.Threading.Tasks;
using GtMotive.Estimate.Microservice.Domain.Interfaces;

namespace GtMotive.Estimate.Microservice.Infrastructure.Authorization
{
    /// <summary>
    /// Development stub that authorizes all requests.
    /// </summary>
    public sealed class NoOpAuthorizationService : IAuthorizationService
    {
        public Task<bool> Authorize(ClaimsPrincipal user, object resource, string policyName)
        {
            return Task.FromResult(true);
        }
    }
}
