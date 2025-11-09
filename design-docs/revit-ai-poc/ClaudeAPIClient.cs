using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RevitAI.SmartSchedule
{
    /// <summary>
    /// Client for Claude API integration
    /// Sends schedule data to Claude and receives intelligent formatting
    /// </summary>
    public static class ClaudeAPIClient
    {
        private const string CLAUDE_API_URL = "https://api.anthropic.com/v1/messages";
        private const string CLAUDE_MODEL = "claude-sonnet-4-20250514";
        
        // TODO: Move to config file or environment variable
        private static string API_KEY = "YOUR_API_KEY_HERE"; // REPLACE THIS

        /// <summary>
        /// Sends schedule data to Claude for intelligent formatting
        /// </summary>
        public static string FormatSchedule(ScheduleData schedule)
        {
            try
            {
                // Convert to JSON for Claude
                string scheduleJson = ScheduleExtractor.ToJson(schedule);
                
                // Create prompt for Claude
                string prompt = BuildPrompt(schedule, scheduleJson);
                
                // Send to Claude API (synchronous wrapper for async)
                string response = SendToClaudeAsync(prompt).GetAwaiter().GetResult();
                
                return response;
            }
            catch (Exception ex)
            {
                return $"Error calling Claude API: {ex.Message}\n\nStack: {ex.StackTrace}";
            }
        }

        private static string BuildPrompt(ScheduleData schedule, string scheduleJson)
        {
            return $@"You are an expert in architectural documentation and Revit scheduling. 

I have a schedule from a Revit model with the following data:

Schedule Name: {schedule.Name}
Category: {schedule.Category}
Number of Rows: {schedule.RowCount}
Number of Columns: {schedule.ColumnCount}

Raw Data (JSON):
```json
{scheduleJson}
```

Please analyze this schedule and provide:

1. **Summary**: Brief overview of what this schedule represents
2. **Data Quality Assessment**: Any missing data, inconsistencies, or issues
3. **Professional Formatting Suggestions**: How to improve this schedule for construction documentation
4. **Insights**: Any patterns, outliers, or notable findings in the data

Format your response in clear sections with headers. Be specific and actionable.";
        }

        private static async Task<string> SendToClaudeAsync(string prompt)
        {
            using (HttpClient client = new HttpClient())
            {
                // Set headers
                client.DefaultRequestHeaders.Add("x-api-key", API_KEY);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                // Build request body
                var requestBody = new
                {
                    model = CLAUDE_MODEL,
                    max_tokens = 2000,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    }
                };

                string jsonRequest = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                // Send request
                HttpResponseMessage response = await client.PostAsync(CLAUDE_API_URL, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Claude API Error ({response.StatusCode}): {errorContent}");
                }

                // Parse response
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject jsonResponse = JObject.Parse(responseBody);
                
                // Extract the text content from Claude's response
                string claudeResponse = jsonResponse["content"]?[0]?["text"]?.ToString();
                
                if (string.IsNullOrEmpty(claudeResponse))
                {
                    throw new Exception("No response content from Claude API");
                }

                return claudeResponse;
            }
        }

        /// <summary>
        /// Test connection to Claude API
        /// </summary>
        public static bool TestConnection()
        {
            try
            {
                string testResponse = SendToClaudeAsync("Say 'OK' if you can read this.").GetAwaiter().GetResult();
                return testResponse.Contains("OK");
            }
            catch
            {
                return false;
            }
        }
    }
}
