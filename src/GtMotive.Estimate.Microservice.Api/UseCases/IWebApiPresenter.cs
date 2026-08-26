using Microsoft.AspNetCore.Http;

namespace GtMotive.Estimate.Microservice.Api.UseCases
{
    public interface IWebApiPresenter
    {
        IResult Result { get; }
    }
}
