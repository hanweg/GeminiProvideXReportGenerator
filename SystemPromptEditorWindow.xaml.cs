using System.IO;
using System.Windows;

namespace GeminiProvideXReportGenerator
{
    public partial class SystemPromptEditorWindow : Window
    {
        private const string PROMPT_FILE_PATH = "SystemPrompt.txt";
        private readonly string _defaultPrompt;
        public string SystemPrompt { get; private set; } = string.Empty;
        public bool PromptChanged { get; private set; } = false;

        public SystemPromptEditorWindow(string currentPrompt, string defaultPrompt)
        {
            InitializeComponent();
            _defaultPrompt = defaultPrompt;
            SystemPrompt = currentPrompt;
            PromptTextBox.Text = currentPrompt;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SystemPrompt = PromptTextBox.Text;
                
                // Save to file
                File.WriteAllText(PROMPT_FILE_PATH, SystemPrompt);
                
                PromptChanged = true;
                StatusText.Text = "System prompt saved successfully.";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                
                DialogResult = true;
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error saving prompt: {ex.Message}";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset the system prompt to default? This will lose all your customizations.",
                "Reset System Prompt",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                PromptTextBox.Text = _defaultPrompt;
                StatusText.Text = "System prompt reset to default.";
                StatusText.Foreground = System.Windows.Media.Brushes.Blue;
            }
        }

        public static string LoadSystemPrompt(string defaultPrompt)
        {
            try
            {
                if (File.Exists(PROMPT_FILE_PATH))
                {
                    return File.ReadAllText(PROMPT_FILE_PATH);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading system prompt: {ex.Message}");
            }
            
            return defaultPrompt;
        }

        public static void SaveSystemPrompt(string prompt)
        {
            try
            {
                File.WriteAllText(PROMPT_FILE_PATH, prompt);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving system prompt: {ex.Message}");
            }
        }
    }
}