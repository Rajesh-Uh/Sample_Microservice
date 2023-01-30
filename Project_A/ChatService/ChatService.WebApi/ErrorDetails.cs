using Newtonsoft.Json;

namespace Precision.WebApi
{
    public class ErrorDetails
    {
        public string ErrorCode { get; set; }
        public object ErrorBody { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }


}