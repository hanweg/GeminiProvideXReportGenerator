using System;
using System.Windows;

namespace GeminiProvideXReportGenerator
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Configure system proxy settings
            System.Net.WebRequest.DefaultWebProxy = System.Net.WebRequest.GetSystemWebProxy();
            System.Net.WebRequest.DefaultWebProxy.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
            
            // Prevent automatic shutdown when windows close
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var configWindow = new StartupConfigWindow();
            var dialogResult = configWindow.ShowDialog();
            
            if (dialogResult == true)
            {
                try
                {
                    var connectionString = $"DSN={configWindow.Dsn}";
                    var mainWindow = new MainWindow(configWindow.ApiKey, connectionString, configWindow.SelectedModelName);
                    
                    // Set shutdown mode back to normal after MainWindow is created
                    this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    this.MainWindow = mainWindow;
                    
                    mainWindow.Show();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Startup Exception: {ex.ToString()}");
                    MessageBox.Show($"An error occurred during startup: {ex.Message}\n\nDetails: {ex.ToString()}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                }
            }
            else
            {
                Shutdown();
            }
        }
    }
}

