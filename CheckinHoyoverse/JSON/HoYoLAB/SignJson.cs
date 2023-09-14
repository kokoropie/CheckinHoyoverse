using System.Text.Json.Serialization;

namespace CheckinHoyoverse.JSON.HoYoLAB
{
    public class SignJson
    {
        [JsonPropertyName("retcode")]
        public int retcode { get; set; }

        [JsonPropertyName("message")]
        public string? message { get; set; }

        [JsonPropertyName("data")]
        public object? data { get; set; }
    }
}
