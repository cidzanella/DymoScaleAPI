using DymoScaleService.Api.Services.Communications;
using HidLibrary;
using Microsoft.Extensions.Logging;

namespace DymoScaleService.Api.Services
{

    public class DymoScaleService
    {

        private const string _NO_USB_SCALE_FOUND = "No USB scale found.";
        private const string _NO_USB_SCALE_CONNECTED = "No USB scale connected.";

        private enum ScaleStatus
        {
            Fault=1, 
            StableAtZero=2, 
            InMotion=3, 
            Stable=4, 
            UnderZero=5, 
            OverWeight=6, 
            RequiresCalibration=7, 
            RequiresReZeroing=8
        }

        private IDictionary<ScaleStatus, string> scaleStatusDictionary= new Dictionary<ScaleStatus, string>()
        {
            {ScaleStatus.Fault, "Falha"},
            {ScaleStatus.StableAtZero,"Estável em Zero"},
            {ScaleStatus.InMotion, "Em movimento"},
            {ScaleStatus.Stable, "Estável"},
            {ScaleStatus.UnderZero, "Negativo"},
            {ScaleStatus.OverWeight,"Excesso de peso"},
            {ScaleStatus.RequiresCalibration,"Recalibrar"},
            {ScaleStatus.RequiresReZeroing,"Zerar balança"}
        };

        private HidDevice? usbScale;

        private ILogger _logger;

        public DymoScaleService(ILoggerFactory loggerFactory ) => _logger = loggerFactory.CreateLogger<DymoScaleService>();

        //Get USB device list for the Dymo 25lb Postal Scale product/vendor ID numbers - change these depdending on what scale adopted
        private HidDevice? GetUsbScale()
        {
            HidDevice[] usbScaleList;

            try
            {
                usbScaleList = HidDevices.Enumerate(0x0922).Cast<HidDevice>().ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"@DymoScaleService.GetUsbScale - {ex.Message} - {ex.InnerException}");
                return null;
            }

            if (usbScaleList.Length <= 0)
            {
                _logger.LogInformation(_NO_USB_SCALE_FOUND);
                return null;
            }

            return usbScaleList[0];
        }

        private bool Connect()
        {
            int waitTries = 0;

            usbScale = GetUsbScale();
            if (usbScale == null)
                return false;

            try
            {
                usbScale.OpenDevice();
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"@DymoScaleService.Connect - {ex.Message} - {ex.InnerException}");
                return false;
            }

            // sometimes the scale is not ready immedietly after Open() so wait till its ready
            while (!usbScale.IsConnected && waitTries < 10)
            {
                Thread.Sleep(50);
                waitTries++;
            }
            return usbScale.IsConnected;
        }

        public void Disconnect()
        {
            if (!(bool)(usbScale?.IsConnected))
                return;
            
            usbScale.CloseDevice();
            usbScale.Dispose();
        }

        // Byte 0 == Report ID?
        // Byte 1 == Scale Status (1 == Fault, 2 == Stable @ 0, 3 == In Motion, 4 == Stable, 5 == Under 0, 6 == Over Weight, 7 == Requires Calibration, 8 == Requires Re-Zeroing)
        // Byte 2 == Weight Unit
        // Byte 3 == Data Scaling (decimal placement)
        // Byte 4 == Weight LSB
        // Byte 5 == Weight MSB
        public DymoScaleServiceResponse GetWeight()
        {
            {
                const string _LOG_PREFIX = "@DymoScaleService.GetWeight";

                HidDeviceData inData;

                bool readingInGrams = false;
                decimal weightFromScale, weightInGrams;
                string scaleMessage;

                if (usbScale == null)
                {
                    scaleMessage = $"{_LOG_PREFIX} - {_NO_USB_SCALE_FOUND}";
                    _logger.LogInformation(scaleMessage);
                    return new DymoScaleServiceResponse(scaleMessage);
                }

                if (!usbScale.IsConnected)
                {
                    scaleMessage = $"{_LOG_PREFIX} - {_NO_USB_SCALE_CONNECTED}";
                    _logger.LogInformation(scaleMessage);
                    return new DymoScaleServiceResponse(scaleMessage);
                }

                try
                {
                    inData = usbScale.Read(250);
                }
                catch (Exception ex) 
                {
                    _logger.LogCritical($"{_LOG_PREFIX}.usbScale.Read - {ex.Message} - {ex.InnerException}.");
                    return new DymoScaleServiceResponse($"{_LOG_PREFIX} - Internal Server Error."); //exception middleware should handle this?
                }

                ScaleStatus scaleStatus = (ScaleStatus)inData.Data[1];
                switch (scaleStatus)
                {
                    case ScaleStatus.Stable:
                    case ScaleStatus.StableAtZero:
                        isStable = true;
                        break;
                    case ScaleStatus.InMotion:
                    case ScaleStatus.Fault:
                    case ScaleStatus.UnderZero:
                    case ScaleStatus.OverWeight:
                    case ScaleStatus.RequiresCalibration:
                    case ScaleStatus.RequiresReZeroing:
                        scaleStatusDictionary.TryGetValue(scaleStatus, out string statusMessage);
                        _logger.LogInformation($"{_LOG_PREFIX} - ScaleStatus: {statusMessage}");
                        return new DymoScaleServiceResponse(statusMessage);
                }

                // switch on selected weight unit
                switch (Convert.ToInt16(inData.Data[2]))
                {
                    case 2:  // Scale reading in g
                        weightInGrams = (Convert.ToDecimal(inData.Data[4]) + Convert.ToDecimal(inData.Data[5]) * 256);
                        readingInGrams = true;
                        break;
                    case 11: // Ounces
                        weightFromScale = (Convert.ToDecimal(inData.Data[4]) + Convert.ToDecimal(inData.Data[5]) * 256) / 10;
                        weightInGrams = weightFromScale * (decimal)28.3495;
                        readingInGrams = false;
                        break;
                    case 12: // Pounds
                        weightFromScale = (Convert.ToDecimal(inData.Data[4]) + Convert.ToDecimal(inData.Data[5]) * 256) / 10;
                        weightInGrams = weightFromScale * (decimal)453.592;
                        readingInGrams = false;
                        break;
                }

                return new DymoScaleServiceResponse(weightInGrams, readingInGrams);
            }

        }


    }
}
