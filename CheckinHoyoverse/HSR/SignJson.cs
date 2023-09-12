using System.Text.Json.Serialization;

namespace CheckinHoyoverse.HSR
{
    public class SignJson
    {
        [JsonPropertyName("retcode")]
        public int retcode { get; set; }

        [JsonPropertyName("message")]
        public string? message { get; set; }

        [JsonPropertyName("data")]
        public DataSign? data { get; set; }
    }

    public class DataSign
    {
        [JsonPropertyName("code")]
        public string? code { get; set; }

        [JsonPropertyName("first_bind")]
        public bool first_bind { get; set; }

        [JsonPropertyName("gt_result")]
        public GT_Result? gt_result { get; set; }
    }

    public class GT_Result
    {
        [JsonPropertyName("challenge")]
        public string? challenge { get; set; }

        [JsonPropertyName("gt")]
        public string? gt { get; set; }

        [JsonPropertyName("is_risk")]
        public bool is_risk { get; set; }

        [JsonPropertyName("risk_code")]
        public int risk_code { get; set; }

        [JsonPropertyName("success")]
        public int success { get; set; }
    }
}
