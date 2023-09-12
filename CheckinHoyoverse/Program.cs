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
        static readonly string version = "1.1.2";
        static readonly RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        static ConfigJson? config = null;
        static bool isStartup = ((string) rk.GetValue(keyStartup, "")).Length > 0;

        static async Task Main(string[] args)
        {   
            await Init(args);

            Console.Clear();
            List<string> menu = new List<string>();
            int option = 1;
            while (true)
            {
                menu.Clear();
                menu.Add("Checkin");
                menu.Add("List account");
                menu.Add("Add account");
                menu.Add("Edit account");
                menu.Add("Remove account");
                menu.Add(string.Format("{0} check in when start with windows", isStartup ? "Disable" : "Enable"));
                menu.Add($"Show log {logFile}");
                menu.Add("Show log folder");
                menu.Add("Clear log");
                menu.Add($"Change language check in ({config.lang})");
                menu.Add("Reset config");
                menu.Add("Export data");
                menu.Add("Import data");
                menu.Add("Close");
                menu.Add("Close (without saving)");
                switch (option = ShowMenu("Menu", menu, option))
                {
                    case 1:
                        await Checkin();
                        break;

                    case 2:
                        List();
                        break;

                    case 3:
                        Add();
                        break;

                    case 4:
                        Edit();
                        break;

                    case 5:
                        Remove();
                        break;

                    case 6:
                        Startup();
                        break;

                    case 7:
                        ShowLog();
                        break;

                    case 8:
                        ShowLogFolder();
                        break;

                    case 9:
                        ClearLog();
                        break;

                    case 10:
                        await ChangeLanguage();
                        break;

                    case 11:
                        Reset();
                        break;

                    case 12:
                        ExportData();
                        break;

                    case 13:
                        ImportData();
                        break;

                    case 14:
                        Save();
                        Console.WriteLine("Closing...");
                        Log($"Close app", $"{logFile}.action.log", false);
                        return;

                    case 15:
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
                Log($"[AUTORUN]", $"{logFile}.action.log", false);
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
                Log($"Close app", $"{logFile}.action.log", false);
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

        static int ShowMenu(string title, List<string> menu)
        {
            return ShowMenu(title, menu, 1);
        }

        static int ShowMenu(string title, List<string> menu, int option)
        {
            bool selected = false;

            while (!selected)
            {
                Console.Clear();
                Console.WriteLine($"------ {title} ------");
                for (int i = 0; i < menu.Count; i++)
                {
                    Console.WriteLine("{0} {1}\u001b[0m", option == i + 1 ? "✅\u001b[32m" : "  ", menu[i]);
                }
                Console.Write("Use ⬆️ and ⬇️ to navigate and press \u001b[31mEnter\u001b[0m key to select");
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.UpArrow:
                        option--;
                        if (option < 1) option = menu.Count;
                        break;

                    case ConsoleKey.DownArrow:
                        option++;
                        if (option > menu.Count) option = 1;
                        break;

                    case ConsoleKey.Enter:
                        selected = true;
                        break;
                }
            }

            return option;
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
                        if (config.version != version)
                        {
                            string currentLang = config.lang;
                            Reset(false);
                            config.lang = currentLang;
                        }
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
                newConfig.version = version;

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
                        if (cookies.Length > 0)
                            foreach (string cookie in cookies)
                            {
                                string[] nameValue = cookie.Split("=");
                                try
                                {
                                    handler.CookieContainer.Add(new Uri(config.url.gi.info), new Cookie(nameValue[0].Trim(), cookie.Substring(nameValue[0].Length + 1).Trim()));
                                } catch { }
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
                        if (cookies.Length > 0)
                            foreach (string cookie in cookies)
                            {
                                string[] nameValue = cookie.Split("=");
                                try
                                {
                                    handler.CookieContainer.Add(new Uri(config.url.hi3.info), new Cookie(nameValue[0].Trim(), nameValue[1].Trim()));
                                } catch { }
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
                        if (cookies.Length > 0)
                            foreach (string cookie in cookies)
                            {
                                string[] nameValue = cookie.Split("=");
                                try
                                {
                                    handler.CookieContainer.Add(new Uri(config.url.hsr.info), new Cookie(nameValue[0].Trim(), nameValue[1].Trim()));
                                } catch { }
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

                if (data.tot)
                {
                    using (HttpClientHandler handler = new HttpClientHandler())
                    {
                        handler.CookieContainer = new CookieContainer();
                        if (cookies.Length > 0)
                            foreach (string cookie in cookies)
                            {
                                string[] nameValue = cookie.Split("=");
                                try
                                {
                                    handler.CookieContainer.Add(new Uri(config.url.tot.info), new Cookie(nameValue[0].Trim(), nameValue[1].Trim()));
                                } catch { }
                            }

                        using (HttpClient client = new HttpClient(handler))
                        {
                            client.DefaultRequestHeaders.UserAgent.ParseAdd(config.userAgent[config.current_user_agent]);
                            Log("- Checking Tears of Themis...", $"{logFile}.log");
                            Log("- Checking Tears of Themis...", $"{logFile}.action.log", false);

                            UriBuilder uriInfo = new UriBuilder(config.url.tot.info);
                            uriInfo.Query = String.Format("act_id={0}&lang={1}", config.url.tot.act_id, config.lang);

                            Log($"- [REQUEST:GET] {uriInfo}", $"{logFile}.action.log", false);
                            HttpResponseMessage responseInfo = await client.GetAsync(uriInfo.ToString());

                            if (responseInfo.IsSuccessStatusCode)
                            {
                                string json = await responseInfo.Content.ReadAsStringAsync();
                                Log($"- [RESPONSE] {json}", $"{logFile}.action.log", false);
                                TOT.InfoJson infoJson = JsonSerializer.Deserialize<TOT.InfoJson>(json);
                                if (infoJson.retcode == 0)
                                {
                                    if (infoJson.data.is_sign)
                                    {
                                        Log("-- Attorney, you've already checked in today~", $"{logFile}.log");
                                    }
                                    else
                                    {
                                        UriBuilder uriHome = new UriBuilder(config.url.tot.home);
                                        uriHome.Query = String.Format("act_id={0}&lang={1}", config.url.tot.act_id, config.lang);
                                        Log($"- [REQUEST:GET] {uriHome}", $"{logFile}.action.log", false);
                                        HttpResponseMessage responseHome = await client.GetAsync(uriHome.ToString());
                                        string responseContentHome = await responseHome.Content.ReadAsStringAsync();
                                        Log($"- [RESPONSE] {responseContentHome}", $"{logFile}.action.log", false);
                                        TOT.HomeJson homeJson = JsonSerializer.Deserialize<TOT.HomeJson>(responseContentHome);

                                        UriBuilder uriSign = new UriBuilder(config.url.tot.sign);
                                        uriSign.Query = String.Format("lang={0}", config.lang);
                                        string jsonContentSign = "{\"act_id\":\"" + config.url.tot.act_id + "\"}";
                                        HttpContent contentSign = new StringContent(jsonContentSign, Encoding.UTF8, "application/responseContentInfo");
                                        Log($"- [REQUEST:POST] {config.url.tot.sign}", $"{logFile}.action.log", false);
                                        Log($"- [REQUEST:POST:CONTENT] {jsonContentSign}", $"{logFile}.action.log", false);
                                        HttpResponseMessage responseSign = await client.PostAsync(uriSign.ToString(), contentSign);
                                        if (responseSign.IsSuccessStatusCode)
                                        {
                                            string responseContentSign = await responseSign.Content.ReadAsStringAsync();
                                            Log($"- [RESPONSE] {responseContentSign}", $"{logFile}.action.log", false);
                                            TOT.SignJson signJson = JsonSerializer.Deserialize<TOT.SignJson>(responseContentSign);
                                            if (signJson.retcode != 0)
                                            {
                                                Log($"[SIGN]: {signJson.message}", $"{logFile}.log");
                                            }
                                            else
                                            {
                                                //if (signJson.data.gt_result.is_risk)
                                                //{
                                                //    Log("-- [RISK]: It's risk. Please check in by yourself, Attorney. You must to pass the challenge.");
                                                //}
                                                //else
                                                //{
                                                Log("-- Attorney, you successfully checked in today~", $"{logFile}.log");
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

                if (data.hoyolab)
                {
                    using (HttpClientHandler handler = new HttpClientHandler())
                    {
                        handler.CookieContainer = new CookieContainer();
                        if (cookies.Length > 0)
                            foreach (string cookie in cookies)
                            {
                                string[] nameValue = cookie.Split("=");
                                try
                                {
                                    handler.CookieContainer.Add(new Uri(config.url.hoyolab.sign), new Cookie(nameValue[0].Trim(), nameValue[1].Trim()));
                                }
                                catch { }
                            }

                        using (HttpClient client = new HttpClient(handler))
                        {
                            client.DefaultRequestHeaders.UserAgent.ParseAdd(config.userAgent[config.current_user_agent]);
                            Log("- Checking HoYoLAB...", $"{logFile}.log");
                            Log("- Checking HoYoLAB...", $"{logFile}.action.log", false);
                            UriBuilder uriSign = new UriBuilder(config.url.hoyolab.sign);
                            HttpContent contentSign = new StringContent("{}", Encoding.UTF8, "application/responseContentInfo");
                            Log($"- [REQUEST:POST] {config.url.hoyolab.sign}", $"{logFile}.action.log", false);
                            Log("- [REQUEST:POST:CONTENT] {}", $"{logFile}.action.log", false);
                            HttpResponseMessage responseSign = await client.PostAsync(uriSign.ToString(), contentSign);
                            if (responseSign.IsSuccessStatusCode)
                            {
                                string responseContentSign = await responseSign.Content.ReadAsStringAsync();
                                Log($"- [RESPONSE] {responseContentSign}", $"{logFile}.action.log", false);
                                HoYoLAB.SignJson signJson = JsonSerializer.Deserialize<HoYoLAB.SignJson>(responseContentSign);
                                if (signJson.retcode == 2001)
                                {
                                    Log("-- You've already checked in today~", $"{logFile}.log");
                                }
                                else if (signJson.retcode == 0)
                                {
                                    Log("-- You successfully checked in today~", $"{logFile}.log");
                                }
                                else
                                {
                                    Log($"[SIGN]: {signJson.message}", $"{logFile}.log");
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

            ConsoleTable table = new ConsoleTable($"Name ({config.data.Count})", "GI", "HI3", "HSR", "TOT", "HoYoLAB");
            config.data.ForEach(data => {
                table.AddRow(data.name, data.gi ? "✓" : "✗", data.hi3 ? "✓" : "✗", data.hsr ? "✓" : "✗", data.tot ? "✓" : "✗", data.hoyolab ? "✓" : "✗");
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
            Console.Write("Have Genshin Impact? [\u001b[33mY\u001b[0m/N]");
            newData.gi = Console.ReadKey().Key != ConsoleKey.N;
            Console.Write("\r" + new String(' ', Console.WindowWidth));
            Console.Write("\rHave Honkai Impact 3? [\u001b[33mY\u001b[0m/N]");
            newData.hi3 = Console.ReadKey().Key != ConsoleKey.N;
            Console.Write("\r" + new String(' ', Console.WindowWidth));
            Console.Write("\rHave Honkai: Star Rail? [\u001b[33mY\u001b[0m/N]");
            newData.hsr = Console.ReadKey().Key != ConsoleKey.N;
            Console.Write("\r" + new String(' ', Console.WindowWidth));
            Console.Write("\rHave Tears of Themis? [\u001b[33mY\u001b[0m/N]");
            newData.tot = Console.ReadKey().Key != ConsoleKey.N;
            Console.Write("\r" + new String(' ', Console.WindowWidth));
            Console.Write("\rHave HoYoLAB? [\u001b[33mY\u001b[0m/N]");
            newData.hoyolab = Console.ReadKey().Key != ConsoleKey.N;

            config.data.Add(newData);
            Log($"Add account {newData.name}", $"{logFile}.action.log", false);
            Log($"[ADD] {newData}", $"{logFile}.action.log", false);
        }

        static void Edit()
        {
            Console.Clear();
            Console.WriteLine("------ Edit ------");
            Console.Write("Search name (empty to back): ");
            string name = Console.ReadLine();
            if (name.Length == 0) return;

            List<Data> searchData = config.data.FindAll(data => data.name.ToLower().Contains(name.ToLower()));
            if (searchData.Count > 0) {
                Data currentData = null;
                if (searchData.Count == 1)
                {
                    currentData = searchData.First();
                } else
                {
                    List<string> menu = new List<string>();
                    for (int i = 0; i < searchData.Count; i++)
                        menu.Add(searchData[i].name);
                    menu.Add("Back");
                    int option = ShowMenu("Edit", menu);
                    if (option == searchData.Count + 1) return;
                    currentData = searchData[option - 1];
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

                Console.Write("Have Genshin Impact? [{0}] ", currentData.gi ? "\u001b[33mY\u001b[0m/N" : "Y/\u001b[33mN\u001b[0m");
                ConsoleKey gi = Console.ReadKey().Key;
                if ((gi == ConsoleKey.Y && !currentData.gi) || (gi == ConsoleKey.N && currentData.gi))
                    currentData.gi = !currentData.gi;

                Console.Write("\r" + new String(' ', Console.WindowWidth));
                Console.Write("\rHave Honkai Impact 3? [{0}] ", currentData.hi3 ? "\u001b[33mY\u001b[0m/N" : "Y/\u001b[33mN\u001b[0m");
                ConsoleKey hi3 = Console.ReadKey().Key;
                if ((hi3 == ConsoleKey.Y && !currentData.hi3) || (hi3 == ConsoleKey.N && currentData.hi3))
                    currentData.hi3 = !currentData.hi3;

                Console.Write("\r" + new String(' ', Console.WindowWidth));
                Console.Write("\rHave Honkai: Star Rail? [{0}] ", currentData.hsr ? "\u001b[33mY\u001b[0m/N" : "Y/\u001b[33mN\u001b[0m");
                ConsoleKey hsr = Console.ReadKey().Key;
                if ((hsr == ConsoleKey.Y && !currentData.hsr) || (hsr == ConsoleKey.N && currentData.hsr))
                    currentData.hsr = !currentData.hsr;

                Console.Write("\r" + new String(' ', Console.WindowWidth));
                Console.Write("\rHave Tears of Themis? [{0}] ", currentData.tot ? "\u001b[33mY\u001b[0m/N" : "Y/\u001b[33mN\u001b[0m");
                ConsoleKey tot = Console.ReadKey().Key;
                if ((tot == ConsoleKey.Y && !currentData.tot) || (tot == ConsoleKey.N && currentData.tot))
                    currentData.tot = !currentData.tot;

                Console.Write("\r" + new String(' ', Console.WindowWidth));
                Console.Write("\rHave HoYoLAB? [{0}] ", currentData.hoyolab ? "\u001b[33mY\u001b[0m/N" : "Y/\u001b[33mN\u001b[0m");
                ConsoleKey hoyolab = Console.ReadKey().Key;
                if ((hoyolab == ConsoleKey.Y && !currentData.hoyolab) || (hoyolab == ConsoleKey.N && currentData.hoyolab))
                    currentData.hoyolab = !currentData.hoyolab;

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
                    List<string> menu = new List<string>();
                    for (int i = 0; i < searchData.Count; i++)
                        menu.Add(searchData[i].name);
                    menu.Add("Back");
                    int option = ShowMenu("Edit", menu);
                    if (option == searchData.Count + 1) return;
                    Log($"Remove account {searchData[option - 1].name}", $"{logFile}.action.log", false);
                    Log($"{searchData[option - 1]}", $"{logFile}.action.log", false);
                    config.data.Remove(searchData[option - 1]);
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
                        List<string> menu = new List<string>();
                        for (int i = 0; i < langsJson.data.langs.Count; i++)
                            menu.Add(string.Format("{0} ({1})", langsJson.data.langs[i].name, langsJson.data.langs[i].value));
                        menu.Add("Back");
                        int option = ShowMenu("Change Language Check In", menu);
                        if (option == langsJson.data.langs.Count + 1) return;
                        config.lang = langsJson.data.langs[option - 1].value;
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

        static void Reset()
        {
            Reset(true);
        }

        static void Reset(bool console)
        {
            bool data = false;
            if (console)
            {
                List<string> menu = new List<string>();
                menu.Add("Without data");
                menu.Add("With data");
                menu.Add("Back");
                switch (ShowMenu("Reset config", menu))
                {
                    case 2:
                        data = true;
                        break;

                    case 3:

                        return;
                    default:
                        break;
                }
            }

            if (data)
            {
                Console.Write("Are you sure to clear account? [Y/\u001b[33mN]");
                if (Console.ReadKey().Key != ConsoleKey.Y)
                {
                    return;
                }
            }
            Console.Clear();
            Console.WriteLine("Resetting...");

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, appName);
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            string configFilePath = Path.Combine(appFolder, configFile);

            using (StreamWriter writer = new StreamWriter(configFilePath))
            {
                ConfigJson newConfig = new ConfigJson();

                if (data)
                {
                    if (console) Console.WriteLine("Resetting data...");
                }
                else
                {
                    newConfig.data = config.data;
                }

                newConfig.version = version;
                config = newConfig;

                string json = JsonSerializer.Serialize(config);

                string encoded = Convert.ToBase64String(AES.Encrypt(json, key));
                writer.Write(encoded);
                writer.Close();
            }

            if (console)
            {
                Console.WriteLine("Reset successful!");
                Console.WriteLine("Press any key to back...");
                Console.ReadKey();
            }
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
                    RestoreDirectory = true,
                    FileName = "checkinhoyoverse.json"
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
                }
            }
            else
            {
                Console.WriteLine("Please select a file");
            }
            Console.WriteLine("\nPress any key to back...");
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
                    try
                    {
                        List<Data> datas = JsonSerializer.Deserialize<List<Data>>(reader.ReadToEnd());
                        foreach (Data dataImport in datas)
                        {
                            if (config.data.Exists(data => data.name.Equals(dataImport.name)))
                            {
                                Console.Write("Do you want to rewrite {0}? [Y/\u001b[33mN]", dataImport.name);
                                if (Console.ReadKey().Key == ConsoleKey.Y)
                                {
                                    int i = config.data.FindIndex(data => data.name.Equals(dataImport.name));
                                    config.data[i] = dataImport;
                                }
                                Console.WriteLine();
                            }
                        }
                        Console.WriteLine("Imported data");
                    } catch
                    {
                        Console.WriteLine("JSON format is not right.");
                    }
                }
            }
            else
            {
                Console.WriteLine("Please select a file");
            }

            Console.WriteLine("\nPress any key to back...");
            Console.ReadKey();
        }
    }
}