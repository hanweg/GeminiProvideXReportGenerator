using Mscc.GenerativeAI;
using System;
using System.Threading.Tasks;

namespace GeminiProvideXReportGenerator.Services
{
    public class GeminiService
    {
        private readonly GenerativeModel _model;
        private readonly string _apiKey;
        private readonly string _modelName;

        public GeminiService(string apiKey, string modelName = "models/gemini-1.5-pro")
        {
            _apiKey = apiKey;
            _modelName = modelName;
            try
            {
                // Check if we can resolve the host first
                var host = "generativelanguage.googleapis.com";
                try
                {
                    var addresses = System.Net.Dns.GetHostAddresses(host);
                    System.Diagnostics.Debug.WriteLine($"Successfully resolved {host} to {addresses.Length} addresses");
                }
                catch (Exception dnsEx)
                {
                    System.Diagnostics.Debug.WriteLine($"DNS resolution failed for {host}: {dnsEx.Message}");
                    throw new InvalidOperationException($"Cannot resolve Google API host. Check network/DNS settings: {dnsEx.Message}");
                }

                // Initialize with the selected model name
                var googleAi = new GoogleAI(_apiKey);
                _model = googleAi.GenerativeModel(modelName);
                
                System.Diagnostics.Debug.WriteLine($"Gemini service initialized successfully with model: {modelName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing Gemini with model {modelName}: {ex}");
                throw;
            }
        }

        public async Task<string> SendMessage(string message)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Sending message to Gemini: {message}");
                
                var response = await _model.GenerateContent(message);
                var result = response?.Text ?? "No response from Gemini";
                
                System.Diagnostics.Debug.WriteLine($"Gemini response: {result}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calling Gemini API: {ex}");
                
                // Check for common issues
                if (ex.Message.Contains("No such host"))
                {
                    return "Error: Unable to connect to Gemini API. Please check your internet connection and firewall settings.";
                }
                else if (ex.Message.Contains("401") || ex.Message.Contains("403"))
                {
                    return "Error: Invalid API key. Please check your Gemini API key.";
                }
                else
                {
                    return $"Error communicating with Gemini: {ex.Message}";
                }
            }
        }
    }
}
