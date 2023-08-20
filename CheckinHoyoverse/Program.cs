using ConsoleTables;
using Microsoft.Win32;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace CheckinHoyoverse
{
    internal class Program
    {
        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }
        delegate bool SetConsoleCtrlEventHandler(CtrlType sig);

        [DllImport("Kernel32")]
        static extern bool SetConsoleCtrlHandler(SetConsoleCtrlEventHandler handler, bool add);
        [DllImport("Kernel32")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("User32")]
        static extern bool ShowWindow(IntPtr hWnd, int cmdShow);

        static readonly string appName = "Checkin Hoyoverse";
        static readonly string configFile = "config";
        static readonly string logFolder = "log";
        static readonly string logFile = $"{DateTime.Now.Year}-{DateTime.Now.Month:00}-{DateTime.Now.Day:00}";
        static readonly string key = "KagaAkatsuki0705";
        static readonly string keyStartup = $"{appName} startup";
        static readonly RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        static ConfigJson? config = null;
        static bool isStartup = ((string) rk.GetValue(keyStartup, "")).Length > 0;

        static async Task Main(string[] args)
        {   
            await Init(args);
            while (true)
            {
                Console.Clear();
                Console.WriteLine($"------ Menu ------");
                Console.WriteLine("A. Checkin");
                Console.WriteLine("B. List account");
                Console.WriteLine("C. Add account");
                Console.WriteLine("D. Edit account");
                Console.WriteLine("E. Remove account");
                Console.WriteLine("F. {0} check in when start with windows", isStartup ? "Disable" : "Enable");
                Console.WriteLine($"G. Show log {logFile}");
                Console.WriteLine("H. Show log folder");
                Console.WriteLine("I. Clear log");
                Console.WriteLine($"J. Change language check in ({config.lang})");
                Console.WriteLine("K. Reset config (without data)");
                Console.WriteLine("L. Reset config (with data)");
                Console.WriteLine("M. Export data");
                Console.WriteLine("N. Import data");
                Console.WriteLine("X. Close");
                Console.WriteLine("Z. Close (without saving)");
                Console.Write("Enter your choice: ");

                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.A:
                        await Checkin();
                        break;

                    case ConsoleKey.B:
                        List();
                        break;

                    case ConsoleKey.C:
                        Add();
                        break;

                    case ConsoleKey.D:
                        Edit();
                        break;

                    case ConsoleKey.E:
                        Remove();
                        break;

                    case ConsoleKey.F:
                        Startup();
                        break;

                    case ConsoleKey.G:
                        ShowLog();
                        break;

                    case ConsoleKey.H:
                        ShowLogFolder();
                        break;

                    case ConsoleKey.I:
                        ClearLog();
                        break;

                    case ConsoleKey.J:
                        await ChangeLanguage();
                        break;

                    case ConsoleKey.K:
                        Reset();
                        break;

                    case ConsoleKey.L:
                        Reset(true);
                        break;

                    case ConsoleKey.M:
                        ExportData();
                        break;

                    case ConsoleKey.N:
                        ImportData();
                        break;

                    case ConsoleKey.T:
                        Console.Clear();
                        Console.WriteLine(JsonSerializer.Serialize(config));
                        Console.ReadKey();
                        break;

                    case ConsoleKey.X:
                        Save();
                        Console.WriteLine("Closing...");
                        Log($"Close app", $"{logFile}.action.log", false);
                        return;

                    case ConsoleKey.Z:
                        Console.Clear();
                        Console.WriteLine("Closing...");
                        Log($"Close app without saving", $"{logFile}.action.log", false);
                        return;
                }
            }
        }

        static async Task Init(string[] args)
        {
            Log($"Start app", $"{logFile}.action.log", false);
            Console.Title = "Checkin Hoyoverse";
            if (args.Contains<string>("-autorun"))
            {
                IntPtr hWnd = GetConsoleWindow();
                ShowWindow(hWnd, 0);
                while (true)
                {
                    Console.Clear();
                    try
                    {
                        Load();
                        await Checkin(false);
                        break;
                    }
                    catch (Exception ex)
                    {
                        foreach (string err in ex.ToString().Split("\n"))
                        {
                            Log($"[ERROR]: {err}", $"{logFile}.action.log", false);
                        }
                        Log($"Have something wrong.", $"{logFile}.log");
                        Log($"Try again after 5 seconds.", $"{logFile}.log");
                        Log("-------------------------------------", $"{logFile}.log");
                        for (int i = 1; i <= 5; i++)
                        {
                            Console.Write($"{i} ");
                            Thread.Sleep(1000);
                        }
                    }
                }
                Environment.Exit(0);
            }
            Ping p = new Ping();
            try
            {
                Console.WriteLine("Checking internet...");
                PingReply reply = p.Send("8.8.8.8");
                if (reply.Status != IPStatus.Success)
                {
                    Console.WriteLine("Please check your internet connection.");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    Environment.Exit(0);
                }
            } catch {
                Console.WriteLine("Have something wrong.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                Environment.Exit(0);
            }
            SetConsoleCtrlHandler(Handler, true);
            Load();
        }

        static void Load()
        {
            Console.Clear();
            Console.WriteLine("Loading...");
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, appName);
            if (Directory.Exists(appFolder)) {
                string configFilePath = Path.Combine(appFolder, configFile);
                if (File.Exists(configFilePath)) {
                    using (StreamReader reader = new StreamReader(configFilePath))
                    {
                        string encoded = reader.ReadToEnd();
                        string json = AES.Decrypt(Convert.FromBase64String(encoded), key);
                        config = JsonSerializer.Deserialize<ConfigJson>(json);
                        reader.Close();
                        config.current_user_agent = new Random().Next(0, config.userAgent.Count - 1);
                    }
                } else {
                    Create();
                    Load();
                }
            } else {
                Create();
                Load();
            }
        }

        static void Create()
        {
            Console.Clear();
            Console.WriteLine("Creating...");
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, appName);
            if (!Directory.Exists(appFolder)) {
                Directory.CreateDirectory(appFolder);
            }
            string configFilePath = Path.Combine(appFolder, configFile);

            using (StreamWriter writer = new StreamWriter(configFilePath))
            {
                ConfigJson newConfig = new ConfigJson();

                newConfig.url.gi.info = "https://sg-hk4e-api.hoyolab.com/event/sol/info";
                newConfig.url.gi.sign = "https://sg-hk4e-api.hoyolab.com/event/sol/sign";
                newConfig.url.gi.home = "https://sg-hk4e-api.hoyolab.com/event/sol/home";
                newConfig.url.gi.act_id = "e202102251931481";

                newConfig.url.hi3.info = "https://sg-public-api.hoyolab.com/event/mani/info";
                newConfig.url.hi3.sign = "https://sg-public-api.hoyolab.com/event/mani/sign";
                newConfig.url.hi3.home = "https://sg-public-api.hoyolab.com/event/mani/home";
                newConfig.url.hi3.act_id = "e202110291205111";

                newConfig.url.hsr.info = "https://sg-public-api.hoyolab.com/event/luna/os/info";
                newConfig.url.hsr.sign = "https://sg-public-api.hoyolab.com/event/luna/os/sign";
                newConfig.url.hsr.home = "https://sg-public-api.hoyolab.com/event/luna/os/home";
                newConfig.url.hsr.act_id = "e202303301540311";

                newConfig.userAgent.Add("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.164 Safari/537.36");
                newConfig.userAgent.Add("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.107 Safari/537.36");
                newConfig.userAgent.Add("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Firefox/89.0.2");
                newConfig.userAgent.Add("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Firefox/91.0");
                newConfig.userAgent.Add("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:89.0) Gecko/20100101 Firefox/89.0");
                newConfig.userAgent.Add("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.1 Safari/605.1.15");
                newConfig.userAgent.Add("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Edg/91.0.864.59");
                newConfig.userAgent.Add("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.107 Safari/537.36 OPR/77.0.4054.277");
                newConfig.userAgent.Add("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                newConfig.userAgent.Add("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.131 Safari/537.36");

                newConfig.current_user_agent = 0;

                newConfig.lang = "en-us";
                newConfig.api_lang = "https://bbs-api-os.hoyolab.com/community/misc/wapi/langs";

                string json = JsonSerializer.Serialize(newConfig);

                string encoded = Convert.ToBase64String(AES.Encrypt(json, key));
                writer.Write(encoded);
                writer.Close();
            }
        }

        static void Save()
        {
            Console.Clear();
            Console.WriteLine("Saving...");
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, appName);
            if (!Directory.Exists(appFolder)) {
                Directory.CreateDirectory(appFolder);
            }
            string configFilePath = Path.Combine(appFolder, configFile);

            using (StreamWriter writer = new StreamWriter(configFilePath))
            {
                string json = JsonSerializer.Serialize(config);
                string encoded = Convert.ToBase64String(AES.Encrypt(json, key));
                writer.Write(encoded);
                writer.Close();
            }
        }

        static void Log(string strValue = "", string file = "example.log", bool console = true)
        {
            if (console) Console.WriteLine(strValue);

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, appName);
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            string logFolderPath = Path.Combine(appFolder, logFolder);
            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
            }
            string logFilePath = Path.Combine(logFolderPath, file);

            WriteLog(strValue, logFilePath);
        }

        static void WriteLog(string strValue = "", string logFilePath = "")
        {
            try
            {
                StreamWriter sw;
                if (!File.Exists(logFilePath))
                { sw = File.CreateText(logFilePath); }
                else
                { sw = File.AppendText(logFilePath); }

                sw.WriteLine("[{0}] {1}", $"{DateTime.Now.Hour:00}:{DateTime.Now.Minute:00}:{DateTime.Now.Second:00}", strValue);

                sw.Flush();
                sw.Close();
            }
            catch (Exception ex)
            {

            }
        }

        static bool Handler(CtrlType signal)
        {
            switch (signal)
            {
                case CtrlType.CTRL_BREAK_EVENT:
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                    Save();
                    Console.WriteLine("Closing...");
                    Log($"Close app", $"{logFile}.action.log", false);
                    Environment.Exit(0);
                    return false;

                default:
                    return false;
            }
        }

        static async Task Checkin(bool readKey = true)
        {
            Console.Clear();
            Console.WriteLine("------ Checkin ------");
            int i = 1;
            foreach (Data data in config.data)
            {
                Log($"{i}. Start checking {data.name}", $"{logFile}.log");
                Log($"{i}. Start checking {data.name}", $"{logFile}.action.log", false);
                string[] cookies = data.cookies.Split(";");

                if (data.gi)
                {
                    using (HttpClientHandler handler = new HttpClientHandler())
                    {
                        handler.CookieContainer = new CookieContainer();
                        foreach (string cookie in cookies)
                        {
                            string[] nameValue = cookie.Split("=");
                            handler.CookieContainer.Add(new Uri(config.url.gi.info), new Cookie(nameValue[0].Trim(), cookie.Substring(nameValue[0].Length + 1).Trim()));
                        }

                        using (HttpClient client = new HttpClient(handler))
                        {
                            client.DefaultRequestHeaders.UserAgent.ParseAdd(config.userAgent[config.current_user_agent]);
                            Log("- Checking Genshin Impact...", $"{logFile}.log");
                            Log("- Checking Genshin Impact...", $"{logFile}.action.log", false);

                            UriBuilder uriInfo = new UriBuilder(config.url.gi.info);
                            uriInfo.Query = String.Format("act_id={0}&lang={1}", config.url.gi.act_id, config.lang);

                            Log($"- [REQUEST:GET] {uriInfo}", $"{logFile}.action.log", false);
                            HttpResponseMessage responseInfo = await client.GetAsync(uriInfo.ToString());

                            if (responseInfo.IsSuccessStatusCode)
                            {
                                string responseContentInfo = await responseInfo.Content.ReadAsStringAsync();
                                Log($"- [RESPONSE] {responseContentInfo}", $"{logFile}.action.log", false);
                                GI.InfoJson infoJson = JsonSerializer.Deserialize<GI.InfoJson>(responseContentInfo);
                                if (infoJson.retcode == 0)
                                {
                                    if (infoJson.data.is_sign)
                                    {
                                        Log("-- Traveler, you've already checked in today~", $"{logFile}.log");
                                    }
                                    else
                                    {
                                        UriBuilder uriHome = new UriBuilder(config.url.gi.home);
                                        uriHome.Query = String.Format("act_id={0}&lang={1}", config.url.gi.act_id, config.lang);
                                        Log($"- [REQUEST:GET] {uriHome}", $"{logFile}.action.log", false);
                                        HttpResponseMessage responseHome = await client.GetAsync(uriHome.ToString());
                                        string responseContentHome = await responseHome.Content.ReadAsStringAsync();
                                        Log($"- [RESPONSE] {responseContentHome}", $"{logFile}.action.log", false);
                                        GI.HomeJson homeJson = JsonSerializer.Deserialize<GI.HomeJson>(responseContentHome);

                                        UriBuilder uriSign = new UriBuilder(config.url.gi.sign);
                                        uriSign.Query = String.Format("lang={0}", config.lang);
                                        string jsonContentSign = "{\"act_id\":\"" + config.url.gi.act_id + "\"}";
                                        HttpContent contentSign = new StringContent(jsonContentSign, Encoding.UTF8, "application/responseContentInfo");
                                        Log($"- [REQUEST:POST] {config.url.gi.sign}", $"{logFile}.action.log", false);
                                        Log($"- [REQUEST:POST:CONTENT] {jsonContentSign}", $"{logFile}.action.log", false);
                                        HttpResponseMessage responseSign = await client.PostAsync(uriSign.ToString(), contentSign);
                                        if (responseSign.IsSuccessStatusCode)
                                        {
                                            string responseContentSign = await responseSign.Content.ReadAsStringAsync();
                                            Log($"- [RESPONSE] {responseContentSign}", $"{logFile}.action.log", false);
                                            GI.SignJson signJson = JsonSerializer.Deserialize<GI.SignJson>(responseContentSign);
                                            if (signJson.retcode != 0)
                                            {
                                                Log($"[SIGN]: {signJson.message}", $"{logFile}.log");
                                            }
                                            else
                                            {
                                                if (signJson.data != null && signJson.data.gt_result.is_risk)
                                                {
                                                    Log("-- [RISK]: It's risk. Please check in by yourself, Traveler. You must to pass the challenge.", $"{logFile}.log");
                                                    foreach (var header in responseSign.Headers)
                                                    {
                                                        Log($"-- [HEADER]: {header.Key}: {header.Value}", $"{logFile}.action.log", false);
                                                    }
                                                }
                                                else
                                                {
                                                    Log("-- Traveler, you successfully checked in today~", $"{logFile}.log");
                                                    if (homeJson.retcode == 0)
                                                    {
                                                        Log($"-- {homeJson.data.awards[infoJson.data.total_sign_day].name} x{homeJson.data.awards[infoJson.data.total_sign_day].cnt}", $"{logFile}.log");
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Log($"-- HTTP request failed with status code: {responseSign.StatusCode}", $"{logFile}.log");
                                            foreach (var header in responseSign.Headers)
                                            {
                                                Log($"-- [HEADER]: {header.Key}: {header.Value}", $"{logFile}.action.log", false);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Log($"[INFO]: {infoJson.message}", $"{logFile}.log");
                                    Log($"[INFO]: {infoJson.message}", $"{logFile}.action.log", false);
                                    foreach (var header in responseInfo.Headers)
                                    {
                                        Log($"-- [HEADER]: {header.Key}: {header.Value}", $"{logFile}.action.log", false);
                                    }
                                }
                            }
                            else
                            {
                                Log($"-- HTTP request failed with status code: {responseInfo.StatusCode}", $"{logFile}.log");
                                foreach (var header in responseInfo.Headers)
                                {
                                    Log($"-- [HEADER]: {header.Key}: {header.Value}", $"{logFile}.action.log", false);
                                }
                            }
                        }
                    }
                }

                if (data.hi3)
                {
                    using (HttpClientHandler handler = new HttpClientHandler())
                    {
                        handler.CookieContainer = new CookieContainer();
                        foreach (string cookie in cookies)
                        {
                            string[] nameValue = cookie.Split("=");
                            handler.CookieContainer.Add(new Uri(config.url.hi3.info), new Cookie(nameValue[0].Trim(), nameValue[1].Trim()));
                        }

                        using (HttpClient client = new HttpClient(handler))
                        {
                            client.DefaultRequestHeaders.UserAgent.ParseAdd(config.userAgent[config.current_user_agent]);
                            Log("- Checking Honkai Impact 3...", $"{logFile}.log");
                            Log("- Checking Honkai Impact 3...", $"{logFile}.action.log", false);

                            UriBuilder uri = new UriBuilder(config.url.hi3.info);
                            uri.Query = String.Format("act_id={0}&lang={1}", config.url.hi3.act_id, config.lang);

                            Log($"- [REQUEST:GET] {uri}", $"{logFile}.action.log", false);
                            HttpResponseMessage responseInfo = await client.GetAsync(uri.ToString());

                            if (responseInfo.IsSuccessStatusCode)
                            {
                                string json = await responseInfo.Content.ReadAsStringAsync();
                                Log($"- [RESPONSE] {json}", $"{logFile}.action.log", false);
                                HI3.InfoJson infoJson = JsonSerializer.Deserialize<HI3.InfoJson>(json);
                                if (infoJson.retcode == 0)
                                {
                                    if (infoJson.data.is_sign)
                                    {
                                        Log("-- Captain, you have already signed in~", $"{logFile}.log");
                                    }
                                    else
                                    {
                                        UriBuilder uriHome = new UriBuilder(config.url.hi3.home);
                                        uriHome.Query = String.Format("act_id={0}&lang={1}", config.url.hi3.act_id, config.lang);
                                        Log($"- [REQUEST:GET] {uriHome}", $"{logFile}.action.log", false);
                                        HttpResponseMessage responseHome = await client.GetAsync(uriHome.ToString());
                                        string responseContentHome = await responseHome.Content.ReadAsStringAsync();
                                        Log($"- [RESPONSE] {responseContentHome}", $"{logFile}.action.log", false);
                                        HI3.HomeJson homeJson = JsonSerializer.Deserialize<HI3.HomeJson>(responseContentHome);

                                        UriBuilder uriSign = new UriBuilder(config.url.hi3.sign);
                                        uriSign.Query = String.Format("lang={0}", config.lang);
                                        string jsonContentSign = "{\"act_id\":\"" + config.url.hi3.act_id + "\"}";
                                        HttpContent contentSign = new StringContent(jsonContentSign, Encoding.UTF8, "application/responseContentInfo");
                                        Log($"- [REQUEST:POST] {config.url.hi3.sign}", $"{logFile}.action.log", false);
                                        Log($"- [REQUEST:POST:CONTENT] {jsonContentSign}", $"{logFile}.action.log", false);
                                        HttpResponseMessage responseSign = await client.PostAsync(uriSign.ToString(), contentSign);
                                        if (responseSign.IsSuccessStatusCode)
                                        {
                                            string responseContentSign = await responseSign.Content.ReadAsStringAsync();
                                            Log($"- [RESPONSE] {responseContentSign}", $"{logFile}.action.log", false);
                                            HI3.SignJson signJson = JsonSerializer.Deserialize<HI3.SignJson>(responseContentSign);
                                            if (signJson.retcode != 0)
                                            {
                                                Log($"[SIGN]: {signJson.message}", $"{logFile}.log");
                                            }
                                            else
                                            {
                                                //if (signJson.data.gt_result.is_risk)
                                                //{
                                                //    Log("-- [RISK]: It's risk. Please check in by yourself, Captain. You must to pass the challenge.");
                                                //}
                                                //else
                                                //{
                                                Log("-- Captain, you successfully checked in today~", $"{logFile}.log");
                                                if (homeJson.retcode == 0)
                                                {
                                                    Log($"-- {homeJson.data.awards[infoJson.data.total_sign_day].name} x{homeJson.data.awards[infoJson.data.total_sign_day].cnt}", $"{logFile}.log");
                                                }
                                                //}
                                            }
                                        }
                                        else
                                        {
                                            Log($"-- HTTP request failed with status code: {responseSign.StatusCode}", $"{logFile}.log");
                                            foreach (var header in responseSign.Headers)
                                            {
                                                Log($"-- [HEADER]: {header.Key}: {header.Value}", $"{logFile}.action.log", false);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Log($"[INFO]: {infoJson.message}", $"{logFile}.log");
                                    Log($"[INFO]: {infoJson.message}", $"{logFile}.action.log", false);
                                    foreach (var header in responseInfo.Headers)
                                    {
                                        Log($"-- [HEADER]: {header.Key}: {header.Value}", $"{logFile}.action.log", false);
                                    }
                                }
                            }
                            else
                            {
                                Log($"-- HTTP request failed with status code: {responseInfo.StatusCode}", $"{logFile}.log");
                                foreach (var header in responseInfo.Headers)
                                {
                                    Log($"-- [HEADER]: {header.Key}: {header.Value}", $"{logFile}.action.log", false);
                                }
                            }
                        }
                    }
                }

                if (data.hsr)
                {
                    using (HttpClientHandler handler = new HttpClientHandler())
                    {
                        handler.CookieContainer = new CookieContainer();
                        foreach (string cookie in cookies)
                        {
                            string[] nameValue = cookie.Split("=");
                            handler.CookieContainer.Add(new Uri(config.url.hsr.info), new Cookie(nameValue[0].Trim(), nameValue[1].Trim()));
                        }

                        using (HttpClient client = new HttpClient(handler))
                        {
                            client.DefaultRequestHeaders.UserAgent.ParseAdd(config.userAgent[config.current_user_agent]);
                            Log("- Checking Honkai: Star Rail...", $"{logFile}.log");
                            Log("- Checking Honkai: Star Rail...", $"{logFile}.action.log", false);

                            UriBuilder uriInfo = new UriBuilder(config.url.hsr.info);
                            uriInfo.Query = String.Format("act_id={0}&lang={1}", config.url.hsr.act_id, config.lang);

                            Log($"- [REQUEST:GET] {uriInfo}", $"{logFile}.action.log", false);
                            HttpResponseMessage responseInfo = await client.GetAsync(uriInfo.ToString());

                            if (responseInfo.IsSuccessStatusCode)
                            {
                                string json = await responseInfo.Content.ReadAsStringAsync();
                                Log($"- [RESPONSE] {json}", $"{logFile}.action.log", false);
                                HSR.InfoJson infoJson = JsonSerializer.Deserialize<HSR.InfoJson>(json);
                                if (infoJson.retcode == 0)
                                {
                                    if (infoJson.data.is_sign)
                                    {
                                        Log("-- Trailblazer, you've already checked in today~", $"{logFile}.log");
                                    }
                                    else
                                    {
                                        UriBuilder uriHome = new UriBuilder(config.url.hsr.home);
                                        uriHome.Query = String.Format("act_id={0}&lang={1}", config.url.hsr.act_id, config.lang);
                                        Log($"- [REQUEST:GET] {uriHome}", $"{logFile}.action.log", false);
                                        HttpResponseMessage responseHome = await client.GetAsync(uriHome.ToString());
                                        string responseContentHome = await responseHome.Content.ReadAsStringAsync();
                                        Log($"- [RESPONSE] {responseContentHome}", $"{logFile}.action.log", false);
                                        HSR.HomeJson homeJson = JsonSerializer.Deserialize<HSR.HomeJson>(responseContentHome);

                                        UriBuilder uriSign = new UriBuilder(config.url.hsr.sign);
                                        uriSign.Query = String.Format("lang={0}", config.lang);
                                        string jsonContentSign = "{\"act_id\":\"" + config.url.hsr.act_id + "\"}";
                                        HttpContent contentSign = new StringContent(jsonContentSign, Encoding.UTF8, "application/responseContentInfo");
                                        Log($"- [REQUEST:POST] {config.url.hsr.sign}", $"{logFile}.action.log", false);
                                        Log($"- [REQUEST:POST:CONTENT] {jsonContentSign}", $"{logFile}.action.log", false);
                                        HttpResponseMessage responseSign = await client.PostAsync(uriSign.ToString(), contentSign);
                                        if (responseSign.IsSuccessStatusCode)
                                        {
                                            string responseContentSign = await responseSign.Content.ReadAsStringAsync();
                                            Log($"- [RESPONSE] {responseContentSign}", $"{logFile}.action.log", false);
                                            HSR.SignJson signJson = JsonSerializer.Deserialize<HSR.SignJson>(responseContentSign);
                                            if (signJson.retcode != 0)
                                            {
                                                Log($"[SIGN]: {signJson.message}", $"{logFile}.log");
                                            }
                                            else
                                            {
                                                //if (signJson.data.gt_result.is_risk)
                                                //{
                                                //    Log("-- [RISK]: It's risk. Please check in by yourself, Trailblazer. You must to pass the challenge.");
                                                //}
                                                //else
                                                //{
                                                Log("-- Trailblazer, you successfully checked in today~", $"{logFile}.log");
                                                if (homeJson.retcode == 0)
                                                {
                                                    Log($"-- {homeJson.data.awards[infoJson.data.total_sign_day].name} x{homeJson.data.awards[infoJson.data.total_sign_day].cnt}", $"{logFile}.log");
                                                }
                                                //}
                                            }
                                        }
                                        else
                                        {
                                            Log($"-- HTTP request failed with status code: {responseSign.StatusCode}", $"{logFile}.log");
                                            foreach (var header in responseSign.Headers)
                                            {
                                                Log($"-- [HEADER]: {header.Key}: {header.Value}", $"{logFile}.action.log", false);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Log($"[INFO]: {infoJson.message}", $"{logFile}.log");
                                    Log($"[INFO]: {infoJson.message}", $"{logFile}.action.log", false);
                                    foreach (var header in responseInfo.Headers)
                                    {
                                        Log($"-- [HEADER]: {header.Key}: {header.Value}", $"{logFile}.action.log", false);
                                    }
                                }
                            }
                            else
                            {
                                Log($"-- HTTP request failed with status code: {responseInfo.StatusCode}", $"{logFile}.log");
                                foreach (var header in responseInfo.Headers)
                                {
                                    Log($"-- [HEADER]: {header.Key}: {header.Value}", $"{logFile}.action.log", false);
                                }
                            }
                        }
                    }
                }
                Log("", $"{logFile}.log");

                i++;
            }

            Log("-------------------------------------", $"{logFile}.log");

            Console.WriteLine("\nPress any key to back...");
            if (readKey) Console.ReadKey();
        }

        static void List()
        {
            Console.Clear();

            ConsoleTable table = new ConsoleTable($"Name ({config.data.Count})", "GI", "HI3", "HSR");
            config.data.ForEach(data => {
                table.AddRow(data.name, data.gi ? "Yes" : "No", data.hi3 ? "Yes" : "No", data.hsr ? "Yes" : "No");
            });
            table.Write(Format.MarkDown);

            Console.WriteLine("\nPress any key to back...");
            Console.ReadKey();
        }

        static void Add()
        {
            Data newData = new Data();

            while (true)
            {
                Console.Clear();
                Console.WriteLine("------ Add ------");

                Console.Write("Name: ");
                newData.name = Console.ReadLine();

                if (newData.name.Length == 0) continue;

                if (!config.data.Exists(data => data.name.Equals(newData.name))) break;
                else
                {
                    Console.Write("This name exists.");
                    Console.ReadKey();
                }
            }

            Console.Write("Cookies: ");
            newData.cookies = Console.ReadLine();
            Console.Write("Have Genshin Impact (Y/N)? [Y] ");
            newData.gi = Console.ReadKey().Key != ConsoleKey.N;
            Console.Write("\r" + new String(' ', Console.WindowWidth));
            Console.Write("\rHave Honkai Impact 3 (Y/N)? [Y] ");
            newData.hi3 = Console.ReadKey().Key != ConsoleKey.N;
            Console.Write("\r" + new String(' ', Console.WindowWidth));
            Console.Write("\rHave Honkai: Star Rail (Y/N)? [Y] ");
            newData.hsr = Console.ReadKey().Key != ConsoleKey.N;

            config.data.Add(newData);
            Log($"Add account {newData.name}", $"{logFile}.action.log", false);
            Log(newData.ToString(), $"{logFile}.action.log", false);
        }

        static void Edit()
        {
            Console.Clear();
            Console.WriteLine("------ Edit ------");
            Console.Write("Search name (empty to back): ");
            string name = Console.ReadLine();
            if (name.Length == 0) return;

            //DataHome searchData = config.data.Find(data => data.name == name);
            List<Data> searchData = config.data.FindAll(data => data.name.ToLower().Contains(name.ToLower()));
            if (searchData.Count > 0) {
                Data currentData = null;
                if (searchData.Count == 1)
                {
                    currentData = searchData.First();
                } else
                {
                    while (true)
                    {
                        Console.Clear();
                        Console.WriteLine("------ Edit ------");
                        for (int i = 0; i < searchData.Count; i++)
                            Console.WriteLine("{0}. {1}", i + 1, searchData[i].name);
                        Console.WriteLine("{0}. {1}", 0, "Back");
                        Console.Write("Choose one: ");
                        string ch = Console.ReadLine();
                        if (ch.Length == 0 || !double.TryParse(ch, out _)) continue;
                        int c = int.Parse(ch);
                        if (c == 0) return;
                        if (c < 1 || c > searchData.Count)
                        {
                            Console.Clear();
                            Console.WriteLine("------ Edit ------");
                            Console.WriteLine("The choice doesn't exist");
                            Console.WriteLine("Press any key to continue...");
                            Console.ReadKey();
                        } else
                        {
                            currentData = searchData[c - 1];
                            break;
                        }
                    }
                }

                Console.Clear();
                Console.WriteLine("------ Edit {0} ------", currentData.name);
                Console.WriteLine("Empty for no changing.");

                Log($"Edit account {currentData.name}", $"{logFile}.action.log", false);
                Log($"[FROM] {currentData}", $"{logFile}.action.log", false);

                Console.Write("Cookies: ");
                string newCookies = Console.ReadLine();
                if (newCookies.Length > 0)
                    currentData.cookies = newCookies;

                Console.Write("Have Genshin Impact (Y/N)? [{0}] ", currentData.gi ? "Y" : "N");
                ConsoleKey gi = Console.ReadKey().Key;
                if ((gi == ConsoleKey.Y && !currentData.gi) || (gi == ConsoleKey.N && currentData.gi))
                    currentData.gi = !currentData.gi;

                Console.Write("\r" + new String(' ', Console.WindowWidth));
                Console.Write("\rHave Honkai Impact 3 (Y/N)? [{0}] ", currentData.hi3 ? "Y" : "N");
                ConsoleKey hi3 = Console.ReadKey().Key;
                if ((hi3 == ConsoleKey.Y && !currentData.hi3) || (hi3 == ConsoleKey.N && currentData.hi3))
                    currentData.hi3 = !currentData.hi3;

                Console.Write("\r" + new String(' ', Console.WindowWidth));
                Console.Write("\rHave Honkai: Star Rail (Y/N)? [{0}] ", currentData.hsr ? "Y" : "N");
                ConsoleKey hsr = Console.ReadKey().Key;
                if ((hsr == ConsoleKey.Y && !currentData.hsr) || (hsr == ConsoleKey.N && currentData.hsr))
                    currentData.hsr = !currentData.hsr;

                Log($"[TO] {currentData}", $"{logFile}.action.log", false);
            } else {
                Console.Clear();
                Console.WriteLine("------ Edit ------");
                Console.WriteLine("This name doesn't exist.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                Edit();
            }
        }

        static void Remove()
        {
            Console.Clear();
            Console.WriteLine("------ Remove ------");
            Console.Write("Search name: ");
            string name = Console.ReadLine();
            List<Data> searchData = config.data.FindAll(data => data.name.ToLower().Contains(name.ToLower()));
            if (searchData.Count > 0)
            {
                Data currentData = null;
                if (searchData.Count == 1)
                {
                    Log($"Remove account {searchData.First().name}", $"{logFile}.action.log", false);
                    Log($"{searchData.First()}", $"{logFile}.action.log", false);
                    config.data.Remove(searchData.First());
                }
                else if (searchData.Count >= 2)
                {
                    while (true)
                    {
                        Console.Clear();
                        Console.WriteLine("------ Remove ------");
                        for (int i = 0; i < searchData.Count; i++)
                            Console.WriteLine("{0}. {1}", i + 1, searchData[i].name);
                        Console.WriteLine("{0}. {1}", 0, "Back");
                        Console.Write("Choose one: ");
                        string ch = Console.ReadLine();
                        if (ch.Length == 0 || !double.TryParse(ch, out _)) continue;
                        int c = int.Parse(ch);
                        if (c == 0) return;
                        if (c < 1 || c > searchData.Count)
                        {
                            Console.Clear();
                            Console.WriteLine("------ Remove ------");
                            Console.WriteLine("The choice doesn't exist");
                            Console.WriteLine("Press any key to continue...");
                            Console.ReadKey();
                        }
                        else
                        {
                            Log($"Remove account {searchData[c - 1].name}", $"{logFile}.action.log", false);
                            Log($"{searchData[c - 1]}", $"{logFile}.action.log", false);
                            config.data.Remove(searchData[c - 1]);
                            break;
                        }
                    }
                }
            }
        }

        static void Startup()
        {
            Console.Clear();
            isStartup = !isStartup;
            Log(String.Format("{0} startup", isStartup ? "Enable" : "Disable"), $"{logFile}.action.log", false);
            if (isStartup) rk.SetValue(keyStartup, $"\"{Environment.ProcessPath}\" -autorun");
            else rk.DeleteValue(keyStartup, false);
        }

        static void ShowLog()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, appName);
            string logFolderPath = Path.Combine(appFolder, logFolder);
            string logFilePath = Path.Combine(logFolderPath, $"{logFile}.log");
            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
            }
            if (!File.Exists(logFilePath))
            {
                File.CreateText(logFilePath).Close();
            }
            Process.Start("explorer", logFilePath);
        }

        static void ClearLog()
        {
            Console.Clear();
            Console.WriteLine("Clearing log...");
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, appName);
            string logFolderPath = Path.Combine(appFolder, logFolder);
            if (Directory.Exists(logFolderPath))
            {
                DirectoryInfo di = new DirectoryInfo(logFolderPath);
                di.Delete(true);
                di.Create();
            }
            Console.WriteLine("Succeed!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void ShowLogFolder()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, appName);
            string logFolderPath = Path.Combine(appFolder, logFolder);
            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
            }
            Process.Start("explorer", logFolderPath);
        }

        static async Task ChangeLanguage()
        {
            Console.Clear();
            Console.WriteLine("------ Change Language Check In ------");

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(config.userAgent[config.current_user_agent]);

                Log($"- [REQUEST:GET] {config.api_lang}", $"{logFile}.action.log", false);
                Console.WriteLine("Getting languagues list...");
                HttpResponseMessage responseMessage = await client.GetAsync(config.api_lang);

                if (responseMessage.IsSuccessStatusCode)
                {
                    string json = await responseMessage.Content.ReadAsStringAsync();
                    Log($"- [RESPONSE] {json}", $"{logFile}.action.log", false);
                    LangsJson langsJson = JsonSerializer.Deserialize<LangsJson>(json);
                    if (langsJson.retcode == 0)
                    {
                        while (true)
                        {
                            Console.Clear();
                            Console.WriteLine("------ Change Language Check In ------");
                            for (int i = 0; i < langsJson.data.langs.Count; i++)
                                Console.WriteLine("{0}. {1} ({2})", i + 1, langsJson.data.langs[i].name, langsJson.data.langs[i].value);
                            Console.WriteLine("{0}. {1}", 0, "Back");
                            Console.Write("Choose one: ");
                            string ch = Console.ReadLine();
                            if (ch.Length == 0 || !double.TryParse(ch, out _)) continue;
                            int c = int.Parse(ch);
                            if (c == 0) return;
                            if (c < 1 || c > langsJson.data.langs.Count)
                            {
                                continue;
                            }
                            else
                            {
                                config.lang = langsJson.data.langs[c - 1].value;
                                break;
                            }
                        }
                    }
                    else
                    {
                        Log($"[INFO]: {langsJson.message}", $"{logFile}.log");
                        Log($"[INFO]: {langsJson.message}", $"{logFile}.action.log", false);
                        foreach (var header in responseMessage.Headers)
                        {
                            Log($"-- [HEADER]: {header.Key}: {header.Value}", $"{logFile}.action.log", false);
                        }
                    }
                }
                else
                {
                    Log($"-- HTTP request failed with status code: {responseMessage.StatusCode}", $"{logFile}.log");
                    foreach (var header in responseMessage.Headers)
                    {
                        Log($"-- [HEADER]: {header.Key}: {header.Value}", $"{logFile}.action.log", false);
                    }
                }
            }
        }

        static void Reset(bool data = false)
        {
            Console.Clear();
            Console.WriteLine("Resetting...");

            if (data)
            {
                Console.Write("Are you sure to clear account (Y/N)? [N]");
                if (Console.ReadKey().Key != ConsoleKey.Y)
                {
                    return;
                }
                Console.Clear();
                Console.WriteLine("Resetting...");
            }

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, appName);
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            string configFilePath = Path.Combine(appFolder, configFile);

            using (StreamWriter writer = new StreamWriter(configFilePath))
            {
                config.url.gi.info = "https://sg-hk4e-api.hoyolab.com/event/sol/info";
                config.url.gi.sign = "https://sg-hk4e-api.hoyolab.com/event/sol/sign";
                config.url.gi.home = "https://sg-hk4e-api.hoyolab.com/event/sol/home";
                config.url.gi.act_id = "e202102251931481";

                config.url.hi3.info = "https://sg-public-api.hoyolab.com/event/mani/info";
                config.url.hi3.sign = "https://sg-public-api.hoyolab.com/event/mani/sign";
                config.url.hi3.home = "https://sg-public-api.hoyolab.com/event/mani/home";
                config.url.hi3.act_id = "e202110291205111";

                config.url.hsr.info = "https://sg-public-api.hoyolab.com/event/luna/os/info";
                config.url.hsr.sign = "https://sg-public-api.hoyolab.com/event/luna/os/sign";
                config.url.hsr.home = "https://sg-public-api.hoyolab.com/event/luna/os/home";
                config.url.hsr.act_id = "e202303301540311";

                config.userAgent.Clear();

                config.userAgent.Add("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.164 Safari/537.36");
                config.userAgent.Add("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.107 Safari/537.36");
                config.userAgent.Add("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Firefox/89.0.2");
                config.userAgent.Add("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Firefox/91.0");
                config.userAgent.Add("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:89.0) Gecko/20100101 Firefox/89.0");
                config.userAgent.Add("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.1 Safari/605.1.15");
                config.userAgent.Add("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Edg/91.0.864.59");
                config.userAgent.Add("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.107 Safari/537.36 OPR/77.0.4054.277");
                config.userAgent.Add("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                config.userAgent.Add("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.131 Safari/537.36");

                config.current_user_agent = 0;

                config.lang = "en-us";
                config.api_lang = "https://bbs-api-os.hoyolab.com/community/misc/wapi/langs";

                if (data)
                {
                    Console.WriteLine("Resetting data...");
                    config.data.Clear();
                }

                string json = JsonSerializer.Serialize(config);

                string encoded = Convert.ToBase64String(AES.Encrypt(json, key));
                writer.Write(encoded);
                writer.Close();
            }

            Console.WriteLine("Reset successful!");
            Console.WriteLine("Press any key to back...");
            Console.ReadKey();
        }

        static void ExportData()
        {
            Console.Clear();
            Console.WriteLine("------ Export Data ------");

            Console.WriteLine("Selecting path...");

            string path = String.Empty;

            Thread t = new Thread((ThreadStart)(() =>
            {
                SaveFileDialog ofd = new SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    FilterIndex = 2,
                    RestoreDirectory = true
                };

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    path = ofd.FileName;
                }
            }));

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            if (!path.Equals(String.Empty))
            {
                using (StreamWriter writer = new StreamWriter(path))
                {
                    writer.Write(JsonSerializer.Serialize(config.data));
                    writer.Close();
                    Console.WriteLine("Exported data");
                    Console.WriteLine("\nPress any key to back...");
                    Console.ReadKey();
                }
            }
            else
            {
                Console.WriteLine("Please select a file");
            }

            Console.ReadKey();
        }

        static void ImportData()
        {
            Console.Clear();
            Console.WriteLine("------ Import Data ------");

            Console.WriteLine("Selecting path...");

            string path = String.Empty;

            Thread t = new Thread((ThreadStart) (() =>
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    FilterIndex = 2,
                    RestoreDirectory = true
                };

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    path = ofd.FileName;
                }
            }));

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            if (!path.Equals(String.Empty))
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    config.data = JsonSerializer.Deserialize<List<Data>>(reader.ReadToEnd());
                    Console.WriteLine("Imported data");
                }
            }
            else
            {
                Console.WriteLine("Please select a file");
            }

            Console.WriteLine("\nPress any key to back...");
            Console.ReadKey();

            Console.ReadKey();
        }
    }
}