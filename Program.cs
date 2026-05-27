using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using DiscordRPC;
using DiscordRPC.Logging;

namespace MullvadDiscordActivity;

public class MullvadConnectionData
{
    public bool Connected { get; set; }
    public string Endpoint { get; set; }
    public string Country { get; set; }
    public string City { get; set; }
    public string HostName { get; set; }
    
}

public class Program
{
    public const string DISCORD_APP_ID = "YOUR_DISCORD_APP_ID";
    public const string REAL_IP = "127.0.0.1";
        
    private static DiscordRpcClient client;

    public static void Main(string[] args)
    {

        client = new DiscordRpcClient(DISCORD_APP_ID)
        {
            Logger = new ConsoleLogger()
        };

        client.OnReady += (sender, e) =>
        {
            Console.WriteLine("Connected to discord with user {0}", e.User.Username);

            MullvadConnectionData data = GetStatus();

            if (data.HostName == null)
                data.HostName = "None";
            
            string status = data.Connected ? $"Connected to {data.HostName}" : "Disconnected";

            List<Button> buttons = new List<Button>()
            {
                new Button() { Label = $"Real IP: " + REAL_IP, Url = "https://google.com" }
            };
            
            if (data.Connected)
                buttons.Add(new Button() { Label = "Mullvad IP: " + data.Endpoint, Url = "https://google.com" });

            client.SetPresence(new RichPresence()
            {
                Details = status,
                State = $"{data.City} {data.Country}",
                Assets = new Assets()
                {
                    LargeImageKey = "default",
                    LargeImageText = "Mullvad Large Image",
                    SmallImageKey = "default",
                },
                Buttons = buttons.ToArray()
            });
        };

        client.OnPresenceUpdate += (sender, e) =>
        {
            Console.WriteLine("Presence updated!");
            Console.WriteLine(e.Presence);
            Console.WriteLine(e.Name);
        };

        client.OnError += (sender, e) =>
        {
            Console.WriteLine("Error: {0} - {1}", e.Code, e.Message);
        };

        client.Initialize();
        
        Console.ReadLine();
        client.Dispose();
    }
    
    private static MullvadConnectionData GetStatus()
    {
        JsonNode data = JsonSerializer.Deserialize<JsonNode>(RunCommandWithBash("mullvad status --json"));
        MullvadConnectionData connectionData = new MullvadConnectionData
        {
            Connected = data["state"].ToString() == "connected" ? true : false,
            Endpoint = data["details"]["location"]["ipv4"].ToString(),
            Country = data["details"]["location"]["country"].ToString(),
            City = data["details"]["location"]["city"].ToString(),
            HostName = data["state"].ToString() == "connected" ? data["details"]["location"]["hostname"].ToString() : "None",
        };
        return connectionData;
    }
    
    public static string RunCommandWithBash(string command)
    {
        var psi = new ProcessStartInfo();
        psi.FileName = "/bin/bash";
        psi.Arguments = $"-c \"{command}\"";
        psi.RedirectStandardOutput = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        using var process = Process.Start(psi);

        process.WaitForExit();

        var output = process.StandardOutput.ReadToEnd();

        return output;
    }
}
