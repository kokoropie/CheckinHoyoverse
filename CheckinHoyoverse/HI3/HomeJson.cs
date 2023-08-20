using System.Text.Json.Serialization;

namespace CheckinHoyoverse.HI3
{
    public class HomeJson
    {
        [JsonPropertyName("retcode")]
        public int retcode { get; set; }

        [JsonPropertyName("message")]
        public string message { get; set; }

        [JsonPropertyName("data")]
        public DataHome? data { get; set; }
    }

    public class Award
    {
        [JsonPropertyName("icon")]
        public string icon { get; set; }

        [JsonPropertyName("name")]
        public string name { get; set; }

        [JsonPropertyName("cnt")]
        public int cnt { get; set; }
    }

    public class DataHome
    {
        [JsonPropertyName("month")]
        public int month { get; set; }

        [JsonPropertyName("awards")]
        public List<Award> awards { get; set; }
    }
}
