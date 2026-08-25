using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ReturnVehicle.Models;

namespace GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ReturnVehicle.Ports
{
    /// <summary>
    /// Output port for the Return Vehicle use case.
    /// </summary>
    public interface IReturnVehicleOutputPort : IOutputPortStandard<ReturnVehicleOutput>, IOutputPortNotFound
    {
    }
}
