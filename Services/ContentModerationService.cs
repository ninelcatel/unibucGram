using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace unibucGram.Services
{
    public class ContentModerationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "AIzaSyABJihDdXfDfEOCQl6mrKfWS7FkHtHJYp0";
        private readonly string _apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

        public ContentModerationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(bool IsAppropriate, string Reason)> CheckContentAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return (true, string.Empty);
            }

            try
            {
                var prompt = $@"Analyze the following text and determine if it contains inappropriate content such as:
- Insults or offensive language
- Hate speech
- Discriminatory language
- Threats or violence
- Sexual content
- Spam or irrelevant content

Text to analyze: ""{content}""

Respond ONLY with a JSON object in this exact format:
{{
  ""isAppropriate"": true/false,
  ""reason"": ""brief explanation if inappropriate, empty string if appropriate""
}}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    }
                };

                var jsonRequest = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiUrl}?key={_apiKey}")
                {
                    Content = httpContent
                };

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    // If API fails, allow the content (fail open)
                    return (true, string.Empty);
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonDocument.Parse(responseContent);
                
                // Extract the text from Gemini response
                var candidatesElement = jsonResponse.RootElement.GetProperty("candidates");
                if (candidatesElement.GetArrayLength() > 0)
                {
                    var firstCandidate = candidatesElement[0];
                    var contentElement = firstCandidate.GetProperty("content");
                    var partsElement = contentElement.GetProperty("parts");
                    
                    if (partsElement.GetArrayLength() > 0)
                    {
                        var textResponse = partsElement[0].GetProperty("text").GetString();
                        
                        // Parse the AI response JSON
                        // Remove markdown code blocks if present
                        textResponse = textResponse?.Trim();
                        if (textResponse?.StartsWith("```json") == true)
                        {
                            textResponse = textResponse.Substring(7);
                        }
                        if (textResponse?.StartsWith("```") == true)
                        {
                            textResponse = textResponse.Substring(3);
                        }
                        if (textResponse?.EndsWith("```") == true)
                        {
                            textResponse = textResponse.Substring(0, textResponse.Length - 3);
                        }
                        textResponse = textResponse?.Trim();
                        
                        var aiResponse = JsonSerializer.Deserialize<JsonElement>(textResponse ?? "{}");
                        
                        bool isAppropriate = aiResponse.GetProperty("isAppropriate").GetBoolean();
                        string reason = aiResponse.TryGetProperty("reason", out var reasonElement) 
                            ? reasonElement.GetString() ?? string.Empty 
                            : string.Empty;
                        
                        return (isAppropriate, reason);
                    }
                }

                // If we can't parse the response, allow the content
                return (true, string.Empty);
            }
            catch
            {
                // If anything goes wrong, allow the content (fail open)
                return (true, string.Empty);
            }
        }
    }
}
