using horizonYellTracker;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

string? webhook = null;
string? atUser = null;
List<string> storedQueries = new List<string>();
List<string> storedBazQueries = new List<string>();
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
        storedBazQueries = config.BazaarQueries;
        atUser = config.DiscordUserId;
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
Console.WriteLine(@"Tracks yells and bazaar items and sends them to your Discord webhook if they contain your query.
Yells update every 30 sec. Bazaar items show cheapest item(up to 3 items each search), and update every 5 min.
Resets previous matches every hour.");
Console.WriteLine();

if(webhook != null)
{
    Console.WriteLine("Current webhook:");
    Console.WriteLine(webhook);
    Console.WriteLine("Change current webhook? y/n");
    var webhookChangeInput = Console.ReadLine();
    if(!string.IsNullOrEmpty(webhookChangeInput) && webhookChangeInput.ToLower() == "y") webhook = null;
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
        if(!string.IsNullOrEmpty(userConfirm) && userConfirm.ToLower() == "y") unconfirmedWebhook = false;
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

if (atUser != null)
{
    Console.WriteLine("Current Discord user ID:");
    Console.WriteLine(atUser);
    Console.WriteLine("Change current ID? y/n");
    var userIDInput = Console.ReadLine();
    if (!string.IsNullOrEmpty(userIDInput) && userIDInput.ToLower() == "y") atUser = null;
}

if (string.IsNullOrEmpty(atUser))
{
    string? idToCheck = null;
    bool unconfirmedId = true;
    bool addUser = true;

    if (string.IsNullOrEmpty(config.DiscordUserId))
    {
        Console.WriteLine("Add @user?");
        var addUserInput = Console.ReadLine();
        if (string.IsNullOrEmpty(addUserInput) || addUserInput == "n") addUser = false;
    }

    if (addUser)
    {
        Console.WriteLine("Enter Discord ID:(This can be found by typing '\\@YourDiscordUserName' into Discord or left blank)");
        idToCheck = Console.ReadLine();

        while (unconfirmedId)
        {
            var discordUserRegex = @"<@[0-9]+>";
            if (string.IsNullOrEmpty(idToCheck)) break;
            else if (Regex.Match(idToCheck, discordUserRegex).Success)
            {
                atUser = idToCheck;
                unconfirmedId = false;
            }
            else
            {
                Console.WriteLine("Invalid Discord ID.");
                unconfirmedId = true;
            }
        }
    }
    else atUser = "";


}

bool moreInput = true;
List<string> queries = new List<string>();
List<string> bazaarQueries = new List<string>();

if (storedQueries.Any())
{
    Console.WriteLine("");
    Console.WriteLine("Previous queries:");
    foreach (var query in storedQueries)
    {
        if (query != null) Console.WriteLine($"> {query}");
    }
    if(storedBazQueries.Any())
    {
        Console.WriteLine("Previous bazaar queries:");
        foreach (var query in storedBazQueries)
        {
            if (query != null) Console.WriteLine($"> {query}");
        }
    }
    Console.WriteLine("");
    Console.WriteLine("Use previous queries? y/n");
    var usePrev = Console.ReadLine();
    if(!string.IsNullOrEmpty(usePrev) && usePrev.ToLower() == "y")
    {
        queries = storedQueries;
        bazaarQueries = storedBazQueries;
        Console.WriteLine("Add more queries? y/n");
        var addQueryInput = Console.ReadLine();
        if (addQueryInput != null && addQueryInput.ToLower() == "n") moreInput = false;
    }
}

if (moreInput)
{
    Console.WriteLine();
    Console.WriteLine("Add your queries(regex capable). Add to bazaar queries as well with the '!' prefix. When your finished leave blank and hit enter.");
    Console.WriteLine();
}

var configBazQueries = new List<string>();

while (moreInput)
{
    Console.WriteLine("Add query:");
    var input = Console.ReadLine();
    if (string.IsNullOrEmpty(input))
    {
        moreInput = false;
        break;
    }
    else
    {
        if (input.StartsWith("!"))
        {
            var bazaarQ = input[1..];
            bazaarQueries.Add(bazaarQ);
            queries.Add(bazaarQ);
        }
        else queries.Add(input);
    }
}


config.Queries = queries;
config.BazaarQueries = bazaarQueries;
config.Webhook = webhook;
config.DiscordUserId = atUser;
var configJson = JsonConvert.SerializeObject(config);
File.WriteAllText(path, configJson);



HttpClient client = new HttpClient();
HttpClient dClient = new HttpClient();
client.BaseAddress = new Uri("https://api.horizonxi.com/api/v1");
dClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
HttpResponseMessage yellsResponse;
HttpResponseMessage bazaarResponse;
HashSet<string> matches = new HashSet<string>();
HashSet<BazaarItem> bazMatches = new HashSet<BazaarItem>(new BazaarItemComparer());
DateTime matchListStart = DateTime.Now;
DateTime bazListStart = DateTime.Now;
DateTime bazLastChecked = DateTime.Now.AddMinutes(-5);
TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

while (true)
{
    yellsResponse = await client.GetAsync($"{client.BaseAddress}/misc/yells");
        if (yellsResponse.IsSuccessStatusCode)
    {
        var json = yellsResponse.Content.ReadAsStringAsync().Result;
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
                var payload = new
                {
                    username = "Yell Tracker",
                    content = $"{formattedYell} {atUser}",
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

        if (bazaarQueries.Any())
        {
            bazaarResponse = await client.GetAsync($"{client.BaseAddress}/items/bazaar");
            var bazJson = bazaarResponse.Content.ReadAsStringAsync().Result;
            var bazList = JsonConvert.DeserializeObject<List<BazaarItem>>(bazJson);
            var foundBaz = new List<BazaarItem>();
            if (bazList != null && bazList.Any() && DateTime.Now >= bazLastChecked.AddMinutes(5))
            {
                bazLastChecked = DateTime.Now;
                var formattedBazQueries = bazaarQueries.Select(i => i.Replace(" ", "_").Replace("\\s", "_"));
                foundBaz = bazList.Where(b => formattedBazQueries.Any(i => Regex.Match(b.Name, i, RegexOptions.IgnoreCase).Success)).OrderBy(x => x.Bazaar).ToList();
                var filterBaz = new HashSet<BazaarItem>(new BazaarItemComparer());
                filterBaz.UnionWith(foundBaz);
                var cheapest = filterBaz.Take(3);
                if (DateTime.Now >= bazListStart.AddHours(1))
                {
                    bazMatches = new HashSet<BazaarItem>(new BazaarItemComparer());
                    bazListStart = DateTime.Now;
                }
                bazMatches.UnionWith(cheapest);

                Console.WriteLine("");
                Console.WriteLine("Baazar query matches:");
                foreach (var item in cheapest)
                {
                    var itemString = $">>> [{item.Charname}](<https://horizonxi.com/players/{item.Charname}>) has **{textInfo.ToTitleCase(item.Name.Replace("_", " "))}**({item.Bazaar}g) listed in Bazaar.";
                    Console.WriteLine(itemString);
                    var payload = new
                    {
                        username = "Bazaar Tracker",
                        content = $"{itemString} {atUser}",
                    };
                    var contentToSend = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                    dClient.PostAsync(webhook, contentToSend).Wait();
                }
            }
        }
    }
    else throw new Exception($"API Error: {yellsResponse.StatusCode}");
    Thread.Sleep(30000);
}