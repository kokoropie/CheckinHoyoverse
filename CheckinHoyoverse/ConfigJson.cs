using System;
using System.Text.Json.Serialization;

namespace CheckinHoyoverse
{
    public class ConfigJson
    {
        [JsonPropertyName("data")]
        public List<Data> data { get; set; }

        [JsonPropertyName("version")]
        public string version { get; set; }

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
            version = string.Empty;
            url = new Url();
            userAgent = new List<string>();
            userAgent.Add("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.164 Safari/537.36");
            userAgent.Add("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.107 Safari/537.36");
            userAgent.Add("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Firefox/89.0.2");
            userAgent.Add("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Firefox/91.0");
            userAgent.Add("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:89.0) Gecko/20100101 Firefox/89.0");
            userAgent.Add("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.1 Safari/605.1.15");
            userAgent.Add("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Edg/91.0.864.59");
            userAgent.Add("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.107 Safari/537.36 OPR/77.0.4054.277");
            userAgent.Add("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            userAgent.Add("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.131 Safari/537.36");
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

        [JsonPropertyName("tot")]
        public bool tot { get; set; }

        public Data()
        {
            name = string.Empty;
            cookies = string.Empty;
            gi = false; 
            hi3 = false; 
            hsr = false;
            tot = false;
        }

        public override string ToString()
        {
            return $"{name} | {gi} | {hi3} | {hsr} | {tot} | {cookies}";
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

        [JsonPropertyName("tot")]
        public Tot tot { get; set; }

        public Url()
        {
            gi = new Gi();
            hi3 = new Hi3();
            hsr = new Hsr();
            tot = new Tot();
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
            info = "https://sg-hk4e-api.hoyolab.com/event/sol/info";
            sign = "https://sg-hk4e-api.hoyolab.com/event/sol/sign";
            home = "https://sg-hk4e-api.hoyolab.com/event/sol/home";
            act_id = "e202102251931481";
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
            info = "https://sg-public-api.hoyolab.com/event/mani/info";
            sign = "https://sg-public-api.hoyolab.com/event/mani/sign";
            home = "https://sg-public-api.hoyolab.com/event/mani/home";
            act_id = "e202110291205111";
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
            info = "https://sg-public-api.hoyolab.com/event/luna/os/info";
            sign = "https://sg-public-api.hoyolab.com/event/luna/os/sign";
            home = "https://sg-public-api.hoyolab.com/event/luna/os/home";
            act_id = "e202303301540311";
        }
    }

    public class Tot
    {
        [JsonPropertyName("info")]
        public string info { get; set; }

        [JsonPropertyName("sign")]
        public string sign { get; set; }

        [JsonPropertyName("home")]
        public string home { get; set; }

        [JsonPropertyName("act_id")]
        public string act_id { get; set; }

        public Tot()
        {
            info = "https://sg-public-api.hoyolab.com/event/luna/os/info";
            sign = "https://sg-public-api.hoyolab.com/event/luna/os/sign";
            home = "https://sg-public-api.hoyolab.com/event/luna/os/home";
            act_id = "e202202281857121";
        }
    }
}
