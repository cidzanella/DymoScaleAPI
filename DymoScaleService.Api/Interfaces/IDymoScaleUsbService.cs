using DymoScaleService.Api.Services.Communications;

namespace DymoScaleService.Api.Interfaces
{
    public interface IDymoScaleUsbService
    {
        void Disconnect();
        DymoScaleServiceResponse GetWeight();
    }
}