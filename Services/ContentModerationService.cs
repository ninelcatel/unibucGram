using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace unibucGram.Services
{
    public class ContentModerationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiUrl;

        public ContentModerationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["ContentModeration:ApiKey"] ?? throw new ArgumentNullException("ContentModeration:ApiKey not configured");
            _apiUrl = configuration["ContentModeration:ApiUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
        }

        /// <summary>
        /// Converts the AI moderation reason into a user-friendly Romanian message
        /// </summary>
        public static string GetFriendlyErrorMessage(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return "Conținutul tău conține termeni nepotriviți. Te rugăm să reformulezi.";
            }

            var lowerReason = reason.ToLower();

            // Check for specific content violations
            if (lowerReason.Contains("insult") || lowerReason.Contains("insulting") || lowerReason.Contains("offensive"))
            {
                return "⚠️ Conținutul tău conține insulte sau limbaj ofensator. Te rugăm să îți exprimi părerea într-un mod respectuos.";
            }
            else if (lowerReason.Contains("hate") || lowerReason.Contains("discrimin") || lowerReason.Contains("racist") || lowerReason.Contains("sexist"))
            {
                return "⚠️ Conținutul tău conține limbaj discriminatoriu sau incitare la ură. Te rugăm să fii respectuos cu toți utilizatorii.";
            }
            else if (lowerReason.Contains("sexual") || lowerReason.Contains("explicit") || lowerReason.Contains("inappropriate content"))
            {
                return "⚠️ Conținutul tău conține referințe sexuale sau explicite nepotrivite. Te rugăm să menții o atmosferă prietenoasă.";
            }
            else if (lowerReason.Contains("violence") || lowerReason.Contains("violent") || lowerReason.Contains("threat"))
            {
                return "⚠️ Conținutul tău conține referințe violente sau amenințări. Te rugăm să eviți acest tip de limbaj.";
            }
            else if (lowerReason.Contains("spam") || lowerReason.Contains("advertising"))
            {
                return "⚠️ Conținutul tău pare a fi spam sau publicitate nedorită. Te rugăm să contribui cu conținut relevant.";
            }
            else if (lowerReason.Contains("profanity") || lowerReason.Contains("vulgar") || lowerReason.Contains("obscene"))
            {
                return "⚠️ Conținutul tău conține limbaj vulgar sau obscen. Te rugăm să folosești un limbaj decent.";
            }
            else if (lowerReason.Contains("harassment") || lowerReason.Contains("bullying"))
            {
                return "⚠️ Conținutul tău ar putea fi perceput ca hărțuire. Te rugăm să fii respectuos față de ceilalți.";
            }
            else
            {
                // Generic fallback with the reason
                return $"⚠️ Conținutul tău nu respectă regulile comunității noastre. Motiv: {reason}";
            }
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
