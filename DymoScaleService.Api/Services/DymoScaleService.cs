using HidLibrary;

namespace DymoScaleService.Api.Services
{
    public class DymoScaleService
    {

        private HidDevice? usbScale;

        public DymoScaleService()
        {
            usbScale = GetUsbScale();
        }

        private HidDevice? GetUsbScale()
        {
            HidDevice[] usbScaleList;

            //The next line contains the product/vendor ID numbers for the Dymo 25lb Postal Scale, change these depdending on what scale you're using 
            //return HidDevices.Enumerate(0x0922, 0x8004).Cast<HidDevice>().ToArray();                
            usbScaleList = HidDevices.Enumerate(0x0922).Cast<HidDevice>().ToArray();

            if (usbScaleList.Length <= 0)
                return null;

            return usbScaleList[0];
        }

        public bool Connect()
        {
            int waitTries = 0;

            usbScale = GetUsbScale();
            if (usbScale == null) return false;

            usbScale.OpenDevice();

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
            if ((bool)(usbScale?.IsConnected))
            {
                usbScale.CloseDevice();
                usbScale.Dispose();
            }
        }

        public decimal GetWeight()
        {
            // Byte 0 == Report ID?
            // Byte 1 == Scale Status (1 == Fault, 2 == Stable @ 0, 3 == In Motion, 4 == Stable, 5 == Under 0, 6 == Over Weight, 7 == Requires Calibration, 8 == Requires Re-Zeroing)
            // Byte 2 == Weight Unit
            // Byte 3 == Data Scaling (decimal placement)
            // Byte 4 == Weight LSB
            // Byte 5 == Weight MSB
            {
                HidDeviceData inData;

                bool isStable = false;

                if (usbScale == null)
                    return 0; //Console.WriteLine("No Scale found.");

                if (!usbScale.IsConnected)
                    return 0; //Console.WriteLine("No Scale Connected.");

                inData = usbScale.Read(250);

                isStable = inData.Data[1] == 0x4;

                //check if selected weight unit is grams (2)
                if (isStable && Convert.ToInt16(inData.Data[2]) == 2)
                    return (Convert.ToDecimal(inData.Data[4]) + Convert.ToDecimal(inData.Data[5]) * 256); // Scale reading in g

                return 0;
            }

        }


    }
}
