namespace DymoScaleService.Api.Services.Communications
{
    public class DymoScaleServiceResponse
    {
        public bool Success { get; protected set; }
        public bool Connected { get; protected set; }
        public int ResponseCode { get; protected set; }
        public int ErrorCode { get; }
        public string Message { get; protected set; }
        public bool ReadingInGrams { get; protected set; }
        public decimal Weight { get; protected set; }

        public DymoScaleServiceResponse(bool success, bool connected, int responseCode, int errorCode, string message, bool readingInGrams, decimal weight)
        {
            Success = success;
            Connected = connected;
            ResponseCode = responseCode;
            ErrorCode = errorCode;
            Message = message;
            ReadingInGrams = readingInGrams;
            Weight = weight;
        }

        public DymoScaleServiceResponse(decimal weight, bool readingInGrams) : this(true, true, 200, 0, string.Empty, readingInGrams, weight)
        {
        }

        public DymoScaleServiceResponse(string message, int errorCode) : this(false, false, 404, errorCode, message, false, 0)
        {
        }



    }
}
