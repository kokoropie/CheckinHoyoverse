using System.Text.Json.Serialization;

namespace CheckinHoyoverse.HSR
{
    public class HomeJson
    {
        [JsonPropertyName("retcode")]
        public int retcode { get; set; }

        [JsonPropertyName("message")]
        public string? message { get; set; }

        [JsonPropertyName("data")]
        public DataHome? data { get; set; }
    }

    public class Award
    {
        [JsonPropertyName("icon")]
        public string? icon { get; set; }

        [JsonPropertyName("name")]
        public string? name { get; set; }

        [JsonPropertyName("cnt")]
        public int cnt { get; set; }
    }

    public class DataHome
    {
        [JsonPropertyName("month")]
        public int month { get; set; }

        [JsonPropertyName("awards")]
        public List<Award>? awards { get; set; }

        [JsonPropertyName("biz")]
        public string? biz { get; set; }

        [JsonPropertyName("resign")]
        public bool resign { get; set; }

        [JsonPropertyName("short_extra_award")]
        public ShortExtraAward? short_extra_award { get; set; }
    }

    public class ShortExtraAward
    {
        [JsonPropertyName("has_extra_award")]
        public bool has_extra_award { get; set; }

        [JsonPropertyName("start_time")]
        public string? start_time { get; set; }

        [JsonPropertyName("end_time")]
        public string? end_time { get; set; }

        [JsonPropertyName("list")]
        public List<object>? list { get; set; }

        [JsonPropertyName("start_timestamp")]
        public string? start_timestamp { get; set; }

        [JsonPropertyName("end_timestamp")]
        public string? end_timestamp { get; set; }
    }
}
