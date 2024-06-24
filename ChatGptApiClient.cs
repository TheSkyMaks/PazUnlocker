using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace PazUnlocker.ChatGpt
{
    public class ChatGptApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly List<Message> _messages;
        private List<Message> GetLastMessages(int numberOfMessages = 5)
        {
            var lastMessages = new List<Message>();
            while (numberOfMessages > 0)
            {
                var currentIndex = _messages.Count - numberOfMessages;
                numberOfMessages--;
                var currentMessage = _messages.ElementAtOrDefault(currentIndex);
                if (currentIndex < 0 || currentMessage == null)
                {
                    continue;
                }
                lastMessages.Add(currentMessage);
            }
            return lastMessages;
        }

        private readonly string _chatEndPoint = "https://api.openai.com/v1/chat/completions";
        private readonly string _textEndPoint = "https://api.openai.com/v1/completions";

        private readonly Dictionary<string, bool> _models = new Dictionary<string, bool>()
        {
            { "gpt-3.5-turbo", true},
            { "davinci", false}
        };

        private Message CreateUserMessage(string content)
        {
            return new Message() { Role = "user", Content = content };
        }

        public ChatGptApiClient(string apiKey)
        {
            _messages = new List<Message>();
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_chatEndPoint);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<string> SendPrompt(string modelId = "gpt-3.5-turbo", int tryNumber = 0)
        {
            try
            {
                var requestData = new Request()
                {
                    ModelId = modelId,
                    Messages = GetLastMessages()
                };
                var endpoint = _models[modelId] ? _chatEndPoint : _textEndPoint;
                using var response = await _httpClient.PostAsJsonAsync(endpoint, requestData);

                response.EnsureSuccessStatusCode();

                var responseData = await response.Content.ReadFromJsonAsync<ResponseData>();
                var choices = responseData?.Choices ?? new List<Choice>();

                if (choices.Count == 0)
                {
                    return string.Empty;
                }

                var choice = choices[0];
                var responseMessage = choice.Message;
                _messages.Add(responseMessage);

                var responseText = responseMessage.Content.Trim();
                return responseText;
            }
            catch (Exception ex)
            {
                tryNumber++;
                if (tryNumber < 3 && ex.Message == "Response status code does not indicate success: 429 (Too Many Requests).")
                {
                    Thread.Sleep(60000);
                    return SendPrompt("gpt-3.5-turbo", tryNumber).GetAwaiter().GetResult();
                }

                return "#Error#" + ex.Message;
            }
        }

        public string GetAnswerFromChatGpt(string content)
        {
            var message = CreateUserMessage(content);
            _messages.Add(message);
            string response = SendPrompt().GetAwaiter().GetResult();
            return response;
        }

        private class Message
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = "";

            [JsonPropertyName("content")]
            public string Content { get; set; } = "";
        }

        private class Request
        {
            [JsonPropertyName("model")]
            public string ModelId { get; set; } = "";

            [JsonPropertyName("messages")]
            public List<Message> Messages { get; set; } = new();
        }

        private class ResponseData
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = "";

            [JsonPropertyName("object")]
            public string Object { get; set; } = "";

            [JsonPropertyName("created")]
            public ulong Created { get; set; }

            [JsonPropertyName("choices")]
            public List<Choice> Choices { get; set; } = new();

            [JsonPropertyName("usage")]
            public Usage Usage { get; set; } = new();
        }

        private class Choice
        {
            [JsonPropertyName("index")]
            public int Index { get; set; }

            [JsonPropertyName("message")]
            public Message Message { get; set; } = new();

            [JsonPropertyName("finish_reason")]
            public string FinishReason { get; set; } = "";
        }

        private class Usage
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }

            [JsonPropertyName("total_tokens")]
            public int TotalTokens { get; set; }
        }
    }
}
