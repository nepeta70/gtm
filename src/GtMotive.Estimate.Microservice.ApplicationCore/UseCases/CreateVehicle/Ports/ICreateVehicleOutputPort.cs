using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.CreateVehicle.Models;

namespace GtMotive.Estimate.Microservice.ApplicationCore.UseCases.CreateVehicle.Ports
{
    /// <summary>
    /// Output port for the Create Vehicle use case.
    /// </summary>
    public interface ICreateVehicleOutputPort : IOutputPortStandard<CreateVehicleOutput>
    {
    }
}
