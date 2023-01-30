using System.Net;
using System.Text.Json.Serialization;

namespace Precision.Contracts.Response
{
    public class GenericResponse
    {
        public HttpStatusCode statusCode { get; set; }
        public bool success { get; set; }
        public string message {get; set;}

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? errors {get; set;}
    }
}