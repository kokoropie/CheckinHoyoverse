using System.Text.Json.Serialization;

namespace CheckinHoyoverse.HSR
{
    public class InfoJson
    {
        [JsonPropertyName("retcode")]
        public int retcode { get; set; }

        [JsonPropertyName("message")]
        public string? message { get; set; }

        [JsonPropertyName("data")]
        public DataInfo? data { get; set; }
    }

    public class DataInfo
    {
        [JsonPropertyName("total_sign_day")]
        public int total_sign_day { get; set; }

        [JsonPropertyName("today")]
        public string? today { get; set; }

        [JsonPropertyName("is_sign")]
        public bool is_sign { get; set; }

        [JsonPropertyName("is_sub")]
        public bool is_sub { get; set; }

        [JsonPropertyName("region")]
        public string? region { get; set; }

        [JsonPropertyName("sign_cnt_missed")]
        public int sign_cnt_missed { get; set; }

        [JsonPropertyName("short_sign_day")]
        public int short_sign_day { get; set; }
    }
}
