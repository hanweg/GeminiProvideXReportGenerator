using Microsoft.Win32;
using Mscc.GenerativeAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GeminiProvideXReportGenerator
{
    public class GeminiModelInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public List<string> SupportedGenerationMethods { get; set; } = new();
        public bool SupportsGenerateContent => SupportedGenerationMethods.Contains("generateContent");
    }

    public partial class StartupConfigWindow : Window
    {
        public string ApiKey { get; private set; } = string.Empty;
        public string Dsn { get; private set; } = string.Empty;
        public string SelectedModelName { get; private set; } = "models/gemini-1.5-pro";

        public StartupConfigWindow()
        {
            InitializeComponent();
            PopulateDsnComboBox();
            
            // Set default model in dropdown
            var defaultModel = new GeminiModelInfo 
            { 
                Name = "models/gemini-1.5-pro", 
                DisplayName = "Gemini 1.5 Pro (Default)"
            };
            ModelComboBox.Items.Add(defaultModel);
            ModelComboBox.SelectedIndex = 0;
        }

        private void PopulateDsnComboBox()
        {
            var dsnList = new List<string>();
            // User DSNs
            var userDsnKey = Registry.CurrentUser.OpenSubKey("Software\\ODBC\\ODBC.INI\\ODBC Data Sources");
            if (userDsnKey != null)
            {
                foreach (var valueName in userDsnKey.GetValueNames())
                {
                    dsnList.Add(valueName);
                }
            }

            // System DSNs
            var systemDsnKey = Registry.LocalMachine.OpenSubKey("Software\\ODBC\\ODBC.INI\\ODBC Data Sources");
            if (systemDsnKey != null)
            {
                foreach (var valueName in systemDsnKey.GetValueNames())
                {
                    dsnList.Add(valueName);
                }
            }

            DsnComboBox.ItemsSource = dsnList;
        }

        private void ApiKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var hasApiKey = !string.IsNullOrWhiteSpace(ApiKeyTextBox.Text);
            FetchModelsButton.IsEnabled = hasApiKey;
            
            if (!hasApiKey)
            {
                ModelStatusText.Text = "Enter API key to fetch available models";
            }
            else
            {
                ModelStatusText.Text = "Click 'Fetch Available Models' to load model list";
            }
        }

        private async void FetchModelsButton_Click(object sender, RoutedEventArgs e)
        {
            var apiKey = ApiKeyTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                MessageBox.Show("Please enter an API key first.", "No API Key", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ModelStatusText.Text = "Fetching models...";
            FetchModelsButton.IsEnabled = false;
            ModelComboBox.IsEnabled = false;

            try
            {
                var models = await FetchAvailableModels(apiKey);
                
                // Filter for models that support generateContent
                var generativeModels = models
                    .Where(m => m.SupportsGenerateContent)
                    .OrderBy(m => m.DisplayName)
                    .ToList();

                if (generativeModels.Any())
                {
                    ModelComboBox.Items.Clear();
                    foreach (var model in generativeModels)
                    {
                        ModelComboBox.Items.Add(model);
                    }
                    
                    // Try to select gemini-1.5-pro or the first model
                    var defaultModel = generativeModels.FirstOrDefault(m => m.Name == "models/gemini-1.5-pro") 
                                    ?? generativeModels.First();
                    ModelComboBox.SelectedItem = defaultModel;
                    
                    ModelComboBox.IsEnabled = true;
                    ModelStatusText.Text = $"Found {generativeModels.Count} compatible models";
                }
                else
                {
                    ModelStatusText.Text = "No compatible models found";
                    ModelStatusText.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                ModelStatusText.Text = $"Error fetching models: {ex.Message}";
                ModelStatusText.Foreground = System.Windows.Media.Brushes.Red;
                
                // Keep the default model
                ModelComboBox.IsEnabled = true;
            }
            finally
            {
                FetchModelsButton.IsEnabled = true;
            }
        }

        private Task<List<GeminiModelInfo>> FetchAvailableModels(string apiKey)
        {
            try
            {
                // For now, return a hardcoded list of known models since the API may not support listing
                // This list is based on the models you provided
                var models = new List<GeminiModelInfo>
                {
                    new GeminiModelInfo { Name = "models/gemini-1.0-pro-vision-latest", DisplayName = "Gemini 1.0 Pro Vision", SupportedGenerationMethods = new List<string> { "generateContent", "countTokens" } },
                    new GeminiModelInfo { Name = "models/gemini-1.5-pro-latest", DisplayName = "Gemini 1.5 Pro Latest", SupportedGenerationMethods = new List<string> { "generateContent", "countTokens" } },
                    new GeminiModelInfo { Name = "models/gemini-1.5-pro", DisplayName = "Gemini 1.5 Pro", SupportedGenerationMethods = new List<string> { "generateContent", "countTokens" } },
                    new GeminiModelInfo { Name = "models/gemini-1.5-flash-latest", DisplayName = "Gemini 1.5 Flash Latest", SupportedGenerationMethods = new List<string> { "generateContent", "countTokens" } },
                    new GeminiModelInfo { Name = "models/gemini-1.5-flash", DisplayName = "Gemini 1.5 Flash", SupportedGenerationMethods = new List<string> { "generateContent", "countTokens" } },
                    new GeminiModelInfo { Name = "models/gemini-1.5-flash-8b", DisplayName = "Gemini 1.5 Flash 8B", SupportedGenerationMethods = new List<string> { "createCachedContent", "generateContent", "countTokens" } },
                    new GeminiModelInfo { Name = "models/gemini-2.5-pro", DisplayName = "Gemini 2.5 Pro", SupportedGenerationMethods = new List<string> { "generateContent", "countTokens", "createCachedContent", "batchGenerateContent" } },
                    new GeminiModelInfo { Name = "models/gemini-2.5-flash", DisplayName = "Gemini 2.5 Flash", SupportedGenerationMethods = new List<string> { "generateContent", "countTokens", "createCachedContent", "batchGenerateContent" } },
                    new GeminiModelInfo { Name = "models/gemini-2.0-flash", DisplayName = "Gemini 2.0 Flash", SupportedGenerationMethods = new List<string> { "generateContent", "countTokens", "createCachedContent", "batchGenerateContent" } },
                    new GeminiModelInfo { Name = "models/gemini-2.0-flash-lite", DisplayName = "Gemini 2.0 Flash Lite", SupportedGenerationMethods = new List<string> { "generateContent", "countTokens", "createCachedContent", "batchGenerateContent" } },
                    new GeminiModelInfo { Name = "models/gemini-2.0-pro-exp", DisplayName = "Gemini 2.0 Pro Experimental", SupportedGenerationMethods = new List<string> { "generateContent", "countTokens", "createCachedContent", "batchGenerateContent" } },
                    new GeminiModelInfo { Name = "models/gemini-2.0-flash-thinking-exp", DisplayName = "Gemini 2.0 Flash Thinking", SupportedGenerationMethods = new List<string> { "generateContent", "countTokens", "createCachedContent", "batchGenerateContent" } }
                };
                
                // Test if we can connect with the API key by creating a simple model
                try
                {
                    var googleAi = new GoogleAI(apiKey);
                    var testModel = googleAi.GenerativeModel("models/gemini-1.5-pro");
                    // If we get here, the API key is valid
                }
                catch (Exception)
                {
                    throw new Exception("Invalid API key or network error");
                }
                
                return Task.FromResult(models);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching models: {ex}");
                throw;
            }
        }

        private string FormatModelDisplayName(string modelName)
        {
            // Remove "models/" prefix if present
            var displayName = modelName.StartsWith("models/") ? modelName.Substring(7) : modelName;
            
            // Make the name more readable
            displayName = displayName.Replace("-", " ");
            displayName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(displayName);
            
            // Special formatting for known models
            if (modelName.Contains("gemini-2.5-flash"))
                return "Gemini 2.5 Flash (Fast)";
            else if (modelName.Contains("gemini-2.5-pro"))
                return "Gemini 2.5 Pro (Advanced)";
            else if (modelName.Contains("gemini-2.0-flash"))
                return "Gemini 2.0 Flash";
            else if (modelName.Contains("gemini-1.5-pro"))
                return "Gemini 1.5 Pro";
            else if (modelName.Contains("gemini-1.5-flash"))
                return "Gemini 1.5 Flash";
            
            return displayName;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            
            if (string.IsNullOrWhiteSpace(ApiKeyTextBox.Text))
            {
                MessageBox.Show("Please enter a valid API key.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DsnComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a DSN.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ModelComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a Gemini model.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ApiKey = ApiKeyTextBox.Text;
            Dsn = DsnComboBox.SelectedItem as string ?? string.Empty;
            
            // Get the selected model
            if (ModelComboBox.SelectedItem is GeminiModelInfo selectedModel)
            {
                SelectedModelName = selectedModel.Name;
            }
            
            DialogResult = true;
        }
    }
}