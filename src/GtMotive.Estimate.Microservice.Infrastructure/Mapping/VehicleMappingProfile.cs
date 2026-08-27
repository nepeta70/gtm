using AutoMapper;
using GtMotive.Estimate.Microservice.Domain.Entities;

namespace GtMotive.Estimate.Microservice.Infrastructure.Fleet.MongoDb
{
    public class VehicleMappingProfile : Profile
    {
        public VehicleMappingProfile()
        {
            CreateMap<VehicleDocument, Vehicle>();
            CreateMap<Vehicle, VehicleDocument>()
                .ForMember(dest => dest.LicensePlate, opt => opt.MapFrom(src => src.LicensePlate.Value));
        }
    }
}
