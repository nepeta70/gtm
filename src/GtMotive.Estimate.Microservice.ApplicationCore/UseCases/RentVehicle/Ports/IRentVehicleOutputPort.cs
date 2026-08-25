using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.RentVehicle.Models;

namespace GtMotive.Estimate.Microservice.ApplicationCore.UseCases.RentVehicle.Ports
{
    /// <summary>
    /// Output port for the Rent Vehicle use case.
    /// </summary>
    public interface IRentVehicleOutputPort : IOutputPortStandard<RentVehicleOutput>, IOutputPortNotFound
    {
    }
}
