namespace DymoScaleService.Api.Services.Communications
{
    public class DymoScaleServiceResponse
    {
        public bool Success { get; protected set; }
        public bool Connected { get; protected set; }
        public int ResponseCode { get; protected set; }
        public string Message { get; protected set; }
        public bool ReadingInGrams { get; protected set; }
        public decimal Weight { get; protected set; }

        public DymoScaleServiceResponse(bool success, bool connected, int responseCode, string message, bool readingInGrams, decimal weight)
        {
            Success = success;
            Connected = connected;
            ResponseCode = responseCode;
            Message = message;
            ReadingInGrams = readingInGrams;
            Weight = weight;
        }

        public DymoScaleServiceResponse(decimal weight, bool readingInGrams) : this(true, true, 200, string.Empty, readingInGrams, weight)
        {
        }

        public DymoScaleServiceResponse(string message) : this(false, false, 404, message, false, 0)
        {
        }



    }
}
