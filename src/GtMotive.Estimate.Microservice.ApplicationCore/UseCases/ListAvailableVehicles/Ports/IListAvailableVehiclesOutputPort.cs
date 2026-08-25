using GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ListAvailableVehicles.Models;

namespace GtMotive.Estimate.Microservice.ApplicationCore.UseCases.ListAvailableVehicles.Ports
{
    /// <summary>
    /// Output port for the List Available Vehicles use case.
    /// </summary>
    public interface IListAvailableVehiclesOutputPort : IOutputPortStandard<ListAvailableVehiclesOutput>
    {
    }
}
