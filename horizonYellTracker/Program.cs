using horizonYellTracker;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

string? webhook = null;
List<string> storedQueries = new List<string>();
var cur = Directory.GetCurrentDirectory();
var path = Path.Combine(Directory.GetCurrentDirectory(), "config.txt");
Config config = new();
if (File.Exists(path))
{
    var foundConfig = File.ReadAllText(path);
    var tempConfig = JsonConvert.DeserializeObject<Config>(foundConfig);
    if(tempConfig != null)
    {
        config = tempConfig;
        if (config.Webhook != null) webhook = config.Webhook;
        storedQueries = config.Queries;
    }
}
Console.WriteLine(@"
_____.___.      .__  .__    ___________                     __                 
\__  |   | ____ |  | |  |   \__    ___/___________    ____ |  | __ ___________ 
 /   |   |/ __ \|  | |  |     |    |  \_  __ \__  \ _/ ___\|  |/ // __ \_  __ \
 \____   \  ___/|  |_|  |__   |    |   |  | \// __ \\  \___|    <\  ___/|  | \/
 / ______|\___  >____/____/   |____|   |__|  (____  /\___  >__|_ \\___  >__|   
 \/           \/                                  \/     \/     \/    \/         _author: Senah 
");
Console.WriteLine("Tracks yells and sends them to your Discord webhook if they contain your query. Updates every 30 sec. Resets matched yells every hour.");
Console.WriteLine();

if(webhook != null)
{
    Console.WriteLine(webhook);
    Console.WriteLine("Change current webhook? y/n");
    var webhookChangeInput = Console.ReadLine();
    if(webhookChangeInput != null && webhookChangeInput.ToLower() == "y") webhook = null;
}

while (webhook == null)
{
    string? webhookToCheck = null;
    bool unconfirmedWebhook = true;
    while (unconfirmedWebhook)
    {
        Console.WriteLine("Enter Discord webhook:");
        webhookToCheck = Console.ReadLine();
        Console.WriteLine("Is this correct? y/n");
        var userConfirm = Console.ReadLine();
        if(userConfirm != null && userConfirm.ToLower() == "y") unconfirmedWebhook = false;
    }

    var discordWebhookRegex = @"https://discord.com/api/webhooks/[0-9]+\S+";
    if (webhookToCheck != null && Regex.Match(webhookToCheck, discordWebhookRegex).Success)
    {
        webhook = webhookToCheck;
    }
    else
    {
        Console.WriteLine("Invalid Discord webhook.");
        unconfirmedWebhook = true;
    }
}

bool moreInput = true;
List<string> queries = new List<string>();

if (storedQueries.Any())
{
    Console.WriteLine("");
    Console.WriteLine("Previous queries:");
    foreach (var query in storedQueries)
    {
        if (query != null) Console.WriteLine($"> {query}");
    }
    Console.WriteLine("");
    Console.WriteLine("Use previous queries? y/n");
    var usePrev = Console.ReadLine();
    if(usePrev != null && usePrev.ToLower() == "y")
    {
        queries = storedQueries;
        Console.WriteLine("Add more queries? y/n");
        var addQueryInput = Console.ReadLine();
        if (addQueryInput != null && addQueryInput.ToLower() == "n") moreInput = false;
    }
}

if (moreInput)
{
    Console.WriteLine();
    Console.WriteLine("Add your queries(regex capable), and when your finished leave blank and hit enter.");
    Console.WriteLine();
}

while (moreInput)
{
    Console.WriteLine("Add yell query:");
    var input = Console.ReadLine();
    if (string.IsNullOrEmpty(input))
    {
        moreInput = false;
        break;
    }
    else
    {
        queries.Add(input);
    }
}

config.Queries = queries;
config.Webhook = webhook;
var configJson = JsonConvert.SerializeObject(config);
File.WriteAllText(path, configJson);

HttpClient client = new HttpClient();
client.BaseAddress = new Uri("https://api.horizonxi.com/api/v1/misc/yells");
client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
HttpResponseMessage response;
HashSet<string> matches = new HashSet<string>();
DateTime matchListStart = DateTime.Now;

while (true)
{
    response = await client.GetAsync(client.BaseAddress);
    if (response.IsSuccessStatusCode)
    {
        var json = response.Content.ReadAsStringAsync().Result;
        var yellList = JsonConvert.DeserializeObject<List<Yell>>(json);
        List<string> currentMatches = new List<string>();
        if (yellList != null) currentMatches = yellList.Where(y => queries.Any(q => Regex.Match(y.Message, @$"\b{q}\b", RegexOptions.IgnoreCase).Success)).Select(yell => $">>> [{yell.Speaker}: ](<https://horizonxi.com/players/{yell.Speaker}>): {yell.Message}").ToList();
        if (DateTime.Now >= matchListStart.AddHours(1))
        {
            matches = new HashSet<string>();
            matchListStart = DateTime.Now;
        }
        HashSet<string> newMatches = currentMatches.Where(c => !matches.Contains(c)).ToHashSet();
        matches.UnionWith(newMatches);
        if (newMatches.Any())
        {
            Console.WriteLine($"[{DateTime.Now.ToLocalTime()}]");
            foreach (string yell in newMatches)
            {
                var formattedYell = yell.Replace("«", "<:at1:1097736824291606648>").Replace("»", "<:at2:1097736958932959253>"); ;
                foreach (string q in queries)
                {
                    var regex = $@"\b({q})\b";
                    formattedYell = Regex.Replace(formattedYell, regex, m => $"__**{m.Groups[1].Value}**__", RegexOptions.IgnoreCase);
                }
                Console.WriteLine(yell);
                HttpClient dClient = new HttpClient();
                dClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var payload = new
                {
                    username = "Yell Tracker",
                    content = formattedYell,
                };
                var contentToSend = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                dClient.PostAsync(webhook, contentToSend).Wait();
            }
            Console.WriteLine("Discord Updated!");
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now.ToLocalTime()}]");
            Console.WriteLine(@">>>No new matches.");
        }
    }
    else throw new Exception($"API Error: {response.StatusCode.ToString()}");
    Thread.Sleep(30000);
}