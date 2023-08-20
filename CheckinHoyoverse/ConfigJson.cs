using System.Text.Json.Serialization;

namespace CheckinHoyoverse
{
    public class ConfigJson
    {
        [JsonPropertyName("data")]
        public List<Data> data { get; set; }

        [JsonPropertyName("url")]
        public Url url { get; set; }

        [JsonPropertyName("userAgent")]
        public List<string> userAgent { get; set; }

        [JsonPropertyName("current_user_agent")]
        public int current_user_agent { get; set; }

        [JsonPropertyName("lang")]
        public string lang { get; set; }

        [JsonPropertyName("api_lang")]
        public string api_lang { get; set; }

        public ConfigJson() {
            data = new List<Data>();
            url = new Url();
            userAgent = new List<string>();
            current_user_agent = 0;
            lang = "en-us";
            api_lang = "https://bbs-api-os.hoyolab.com/community/misc/wapi/langs";
        }
    }

    public class Data
    {
        [JsonPropertyName("name")]
        public string name { get; set; }

        [JsonPropertyName("cookies")]
        public string cookies { get; set; }

        [JsonPropertyName("gi")]
        public bool gi { get; set; }

        [JsonPropertyName("hi3")]
        public bool hi3 { get; set; }

        [JsonPropertyName("hsr")]
        public bool hsr { get; set; }

        public Data()
        {
            name = string.Empty;
            cookies = string.Empty;
            gi = false; 
            hi3 = false; 
            hsr = false;
        }

        public override string ToString()
        {
            return $"{name} | {gi} | {hi3} | {hsr} | {cookies}";
        }
    }

    public class Url
    {
        [JsonPropertyName("gi")]
        public Gi gi { get; set; }

        [JsonPropertyName("hi3")]
        public Hi3 hi3 { get; set; }

        [JsonPropertyName("hsr")]
        public Hsr hsr { get; set; }

        public Url()
        {
            gi = new Gi();
            hi3 = new Hi3();
            hsr = new Hsr();
        }
    }

    public class Gi
    {
        [JsonPropertyName("info")]
        public string info { get; set; }

        [JsonPropertyName("sign")]
        public string sign { get; set; }

        [JsonPropertyName("home")]
        public string home { get; set; }

        [JsonPropertyName("act_id")]
        public string act_id { get; set; }

        public Gi()
        {
            info = string.Empty;
            sign = string.Empty;
            home = string.Empty;
            act_id = string.Empty;
        }
    }

    public class Hi3
    {
        [JsonPropertyName("info")]
        public string info { get; set; }

        [JsonPropertyName("sign")]
        public string sign { get; set; }

        [JsonPropertyName("home")]
        public string home { get; set; }

        [JsonPropertyName("act_id")]
        public string act_id { get; set; }

        public Hi3()
        {
            info = string.Empty;
            sign = string.Empty;
            home = string.Empty;
            act_id = string.Empty;
        }
    }

    public class Hsr
    {
        [JsonPropertyName("info")]
        public string info { get; set; }

        [JsonPropertyName("sign")]
        public string sign { get; set; }

        [JsonPropertyName("home")]
        public string home { get; set; }

        [JsonPropertyName("act_id")]
        public string act_id { get; set; }

        public Hsr()
        {
            info = string.Empty;
            sign = string.Empty;
            home = string.Empty;
            act_id = string.Empty;
        }
    }
}
