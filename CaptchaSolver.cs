using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PazUnlocker.Console;

public class CaptchaSolver
{
    private HttpClient _httpClient;
    private string ApiKey;
    private string SiteKey;
    private string SiteUrl;

    public CaptchaSolver()
    {
        _httpClient = new HttpClient();
        var json = File.ReadAllText("appConfig.json");
        dynamic jsonObj = JsonConvert.DeserializeObject(json);
        if (jsonObj["CaptchaApiKey"] != null)
        {
            ApiKey = jsonObj["CaptchaApiKey"];
        }
    }

    public void SetSiteKey(string key)
    {
        SiteKey = key;
    }

    public void SetSiteUrl(string url)
    {
        SiteUrl = url;
    }

    public async Task<(string, string)> Solve()
    {
        string taskId = await CreateTask();
        if (!string.IsNullOrEmpty(taskId))
        {
            System.Console.WriteLine($"Got taskId: {taskId} / {SiteKey} / Getting result...");
            var solution = await GetTaskResult(taskId);
            if (!string.IsNullOrEmpty(solution.Item1))
            {
                System.Console.WriteLine($"Captcha solution: {solution.Item1} | {solution.Item2}");
                return solution;
            }
        }

        return (null, null);
    }

    private async Task<string> CreateTask()
    {
        var payload = new
        {
            clientKey = ApiKey,
            task = new
            {
                type = "ReCaptchaV2TaskProxyLess",
                websiteKey = SiteKey,
                websiteURL = SiteUrl
            }
        };

        var response = await _httpClient.PostAsync(
            "https://api.capsolver.com/createTask",
            new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
        );

        string responseString = await response.Content.ReadAsStringAsync();
        JObject jsonResponse = JsonConvert.DeserializeObject<JObject>(responseString);

        if (jsonResponse["errorId"]?.Value<int>() == 0)
        {
            return jsonResponse["taskId"]?.Value<string>();
        }
        else
        {
            System.Console.WriteLine("Failed to create task: " + jsonResponse);
            return null;
        }
    }

    private async Task<(string, string)> GetTaskResult(string taskId)
    {
        while (true)
        {
            await Task.Delay(3000);

            var payload = new
            {
                clientKey = ApiKey,
                taskId = taskId
            };

            var response = await _httpClient.PostAsync(
                "https://api.capsolver.com/getTaskResult",
                new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            );

            string responseString = await response.Content.ReadAsStringAsync();
            JObject jsonResponse = JsonConvert.DeserializeObject<JObject>(responseString);

            string status = jsonResponse["status"]?.Value<string>();
            if (status == "ready")
            {
                var gRecaptchaResponse = jsonResponse["solution"]?["gRecaptchaResponse"]?.Value<string>();
                var userAgent = jsonResponse["solution"]?["userAgent"]?.Value<string>();
                return (userAgent, gRecaptchaResponse);
            }

            if (status == "failed" || jsonResponse["errorId"]?.Value<int>() != 0)
            {
                System.Console.WriteLine("Solve failed! response: " + responseString);
                return (null, null);
            }
        }
    }
}