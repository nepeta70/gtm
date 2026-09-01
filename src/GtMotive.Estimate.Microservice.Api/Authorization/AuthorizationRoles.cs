namespace GtMotive.Estimate.Microservice.Api.Authorization
{
    /// <summary>
    /// Constants for role names used in RequireRole() and token claims.
    /// </summary>
    public static class AuthorizationRoles
    {
        public const string Admin = nameof(Admin);
        public const string User = nameof(User);
    }
}
