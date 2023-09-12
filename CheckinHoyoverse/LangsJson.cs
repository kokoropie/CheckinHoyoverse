using System.Text.Json.Serialization;

namespace CheckinHoyoverse
{
    public class LangsJson
    {
        [JsonPropertyName("retcode")]
        public int retcode { get; set; }

        [JsonPropertyName("message")]
        public string? message { get; set; }

        [JsonPropertyName("data")]
        public LangsData? data { get; set; }
    }

    public class LangsData
    {
        [JsonPropertyName("langs")]
        public List<Lang>? langs { get; set; }
    }

    public class Lang
    {
        [JsonPropertyName("name")]
        public string? name { get; set; }

        [JsonPropertyName("value")]
        public string? value { get; set; }

        [JsonPropertyName("label")]
        public string? label { get; set; }

        [JsonPropertyName("alias")]
        public List<string>? alias { get; set; }
    }
}
