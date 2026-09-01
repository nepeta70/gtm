namespace GtMotive.Estimate.Microservice.Api.Authorization
{
    /// <summary>
    /// Constants for authorization policies used in [Authorize(Policy = ...)] attributes.
    /// </summary>
    public static class AuthorizationPolicies
    {
        public const string CanCreateVehicle = nameof(CanCreateVehicle);
        public const string CanRentVehicle = nameof(CanRentVehicle);
    }
}
