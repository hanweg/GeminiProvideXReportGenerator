using GeminiProvideXReportGenerator.Services;using System.Data.Odbc;using System.Windows;using System.Windows.Controls;using System.Windows.Input;namespace GeminiProvideXReportGenerator{    public partial class MainWindow : Window    {        private readonly GeminiService _geminiService;        private readonly OdbcService _odbcService;        private readonly SqliteService _sqliteService;        private readonly string _connectionString;
        private readonly List<(string sender, string message)> _conversationHistory = new();
        private string _currentUserRequest = string.Empty;
        private bool _isWorkingOnTask = false;
        private string _customSystemPrompt = string.Empty;

        public MainWindow(string apiKey, string connectionString, string modelName)
        {
            try
            {
                InitializeComponent();
                
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new ArgumentNullException(nameof(apiKey), "API key cannot be null or empty");
                }
                
                _geminiService = new GeminiService(apiKey, modelName);
                _odbcService = new OdbcService(connectionString);
                _sqliteService = new SqliteService();
                _connectionString = connectionString;
                
                // Load custom system prompt
                var defaultPrompt = GetDefaultSystemPrompt();
                _customSystemPrompt = SystemPromptEditorWindow.LoadSystemPrompt(defaultPrompt);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in MainWindow constructor: {ex.Message}\n\nDetails: {ex.ToString()}");
                throw;
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Don't populate Object Explorer immediately - wait for user action
            // This prevents blocking on ODBC connection during startup
            await Task.Delay(100); // Small delay to ensure window is fully loaded
            _ = Task.Run(() => PopulateObjectExplorerAsync());
        }

        private async Task PopulateObjectExplorerAsync()
        {
            try
            {
                var tableNames = await Task.Run(() => _odbcService.GetTableNames());
                
                // Update UI on the main thread
                await Dispatcher.InvokeAsync(() =>
                {
                    ObjectExplorer.Items.Clear();
                    foreach (var tableName in tableNames)
                    {
                        var item = new TreeViewItem();
                        item.Header = GetTableDisplayName(tableName);
                        item.Tag = tableName; // Store the actual table name
                        ObjectExplorer.Items.Add(item);
                    }
                });
            }
            catch (OdbcException odbcEx)
            {
                System.Diagnostics.Debug.WriteLine($"ODBC Exception: {odbcEx.Message}");
                await Dispatcher.InvokeAsync(() =>
                {
                    ObjectExplorer.Items.Clear();
                    ObjectExplorer.Items.Add(new TreeViewItem { Header = "Connect to database to view tables" });
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"An error occurred while connecting to the database: {ex.Message}", "Database Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }

        private void PopulateObjectExplorer()
        {
            try
            {
                var tableNames = _odbcService.GetTableNames();
                ObjectExplorer.Items.Clear();
                foreach (var tableName in tableNames)
                {
                    var item = new TreeViewItem();
                    item.Header = GetTableDisplayName(tableName);
                    item.Tag = tableName; // Store the actual table name
                    ObjectExplorer.Items.Add(item);
                }
            }
            catch (OdbcException odbcEx)
            {
                System.Diagnostics.Debug.WriteLine($"ODBC Exception: {odbcEx.Message}");
                ObjectExplorer.Items.Clear();
                ObjectExplorer.Items.Add(new TreeViewItem { Header = "Connect to database to view tables" });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while connecting to the database: {ex.Message}", "Database Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private string GetTableDisplayName(string tableName)
        {
            try
            {
                if (!_sqliteService.TableExists(tableName))
                {
                    return $"⚪ {tableName}"; // Not mirrored
                }

                var sqliteCount = _sqliteService.GetTableRowCount(tableName);
                var provideXCount = _odbcService.GetTableRowCount(tableName);

                if (sqliteCount == 0)
                {
                    return $"🔴 {tableName} (0 rows)"; // Empty mirror
                }
                else if (sqliteCount == provideXCount)
                {
                    return $"🟢 {tableName} ({sqliteCount} rows)"; // Fully mirrored
                }
                else
                {
                    return $"🟡 {tableName} ({sqliteCount}/{provideXCount} rows)"; // Partially mirrored
                }
            }
            catch
            {
                return $"⚪ {tableName}"; // Default if we can't determine status
            }
        }

        private void RefreshTables_Click(object sender, RoutedEventArgs e)
        {
            PopulateObjectExplorer();
        }

        private void MirrorTable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedItem = ObjectExplorer.SelectedItem as TreeViewItem;
                
                if (selectedItem != null)
                {
                    var tableName = selectedItem.Tag?.ToString() ?? string.Empty;
                    
                    if (!string.IsNullOrEmpty(tableName))
                    {
                        using (var odbcConnection = new OdbcConnection(_connectionString))
                        {
                            odbcConnection.Open();
                            _sqliteService.MirrorTable(odbcConnection, tableName);
                            
                            MessageBox.Show($"Table '{tableName}' mirrored to SQLite.", "Success");
                            
                            // Refresh the object explorer to show updated status
                            PopulateObjectExplorer();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Table name is empty.", "Error");
                    }
                }
                else
                {
                    MessageBox.Show("No table selected.", "Error");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in MirrorTable_Click: {ex.Message}\n\nDetails: {ex.ToString()}");
                MessageBox.Show($"Error mirroring table: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectTop1000PVX_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedItem = ObjectExplorer.SelectedItem as TreeViewItem;
                if (selectedItem != null)
                {
                    var tableName = selectedItem.Tag?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(tableName))
                    {
                        var query = $"SELECT TOP 1000 * FROM {tableName}";
                        SetQueryEditorText(query);
                        
                        var result = _odbcService.GetTop1000FromProvideX(tableName);
                        ResultsGrid.ItemsSource = result.DefaultView;
                        ResultsLabel.Content = "Results - ProvideX";
                        MessageBox.Show($"Showing top 1000 rows from ProvideX table '{tableName}'. Total rows loaded: {result.Rows.Count}", "Query Result");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error querying ProvideX table: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectTop1000SQLite_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedItem = ObjectExplorer.SelectedItem as TreeViewItem;
                if (selectedItem != null)
                {
                    var tableName = selectedItem.Tag?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(tableName))
                    {
                        if (!_sqliteService.TableExists(tableName))
                        {
                            MessageBox.Show($"Table '{tableName}' has not been mirrored to SQLite yet. Please mirror it first.", "Table Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        var query = $"SELECT * FROM [{tableName}] LIMIT 1000";
                        SetQueryEditorText(query);

                        var result = _sqliteService.GetTop1000FromSqlite(tableName);
                        ResultsGrid.ItemsSource = result.DefaultView;
                        ResultsLabel.Content = "Results - SQLite";
                        MessageBox.Show($"Showing top 1000 rows from SQLite table '{tableName}'. Total rows loaded: {result.Rows.Count}", "Query Result");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error querying SQLite table: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteQuery_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteCurrentQuery();
        }

        private string GetSelectedQuery()
        {
            try
            {
                var selectedTab = QueryTabControl.SelectedItem as TabItem;
                
                if (selectedTab != null)
                {
                    var textBox = selectedTab.Content as TextBox;
                    
                    if (textBox != null)
                    {
                        var queryText = textBox.Text;
                        return queryText ?? string.Empty;
                    }
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in GetSelectedQuery: {ex.Message}");
                return string.Empty;
            }
        }

        private void SetQueryEditorText(string query)
        {
            try
            {
                var selectedTab = QueryTabControl.SelectedItem as TabItem;
                if (selectedTab != null)
                {
                    var textBox = selectedTab.Content as TextBox;
                    if (textBox != null)
                    {
                        textBox.Text = query;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting query text: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendChatMessage();
        }

        private async void ChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SendButton.IsEnabled)
            {
                await SendChatMessage();
            }
        }

        private async Task SendChatMessage()
        {
            try
            {
                var userMessage = ChatInput.Text?.Trim();
                if (string.IsNullOrEmpty(userMessage)) return;

                // Show loading state
                SetLoadingState(true);
                
                // Clear input immediately
                ChatInput.Text = "";
                
                // Track conversation history
                _conversationHistory.Add(("You", userMessage));
                
                // Capture original user request if this is a new task
                if (!_isWorkingOnTask)
                {
                    _currentUserRequest = userMessage;
                    _isWorkingOnTask = true;
                }
                
                // Add user message to chat history with formatting
                AddMessageToChat("You", userMessage, System.Windows.Media.Colors.Blue);

                // Show "Gemini is thinking..." message
                AddMessageToChat("Gemini", "🤔 Thinking...", System.Windows.Media.Colors.LightGray);

                try
                {
                    // Get current query context for Gemini (remove debug messages for cleaner UX)
                    var currentQuery = GetSelectedQuerySilent();
                    var contextMessage = BuildContextMessage(userMessage, currentQuery);

                    // Send to Gemini on background thread
                    var geminiResponse = await Task.Run(async () => 
                        await _geminiService.SendMessage(contextMessage));

                    // Small delay before removing thinking message for better UX
                    await Task.Delay(500);
                    
                    // Remove the "thinking" message
                    RemoveLastMessage();

                    // Track Gemini's response
                    _conversationHistory.Add(("Gemini", geminiResponse));

                    // Add Gemini response to chat history with formatting FIRST
                    AddMessageToChat("Gemini", geminiResponse, System.Windows.Media.Colors.DarkGreen);

                    // Then process Gemini response for potential query generation and actions
                    var processedResponse = await ProcessGeminiResponseAsync(geminiResponse);
                }
                catch (Exception ex)
                {
                    // Remove the "thinking" message
                    RemoveLastMessage();
                    AddMessageToChat("Error", ex.Message, System.Windows.Media.Colors.Red);
                }
                
            }
            catch (Exception ex)
            {
                AddMessageToChat("Error", ex.Message, System.Windows.Media.Colors.Red);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            LoadingIndicator.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            SendButton.IsEnabled = !isLoading;
            ChatInput.IsEnabled = !isLoading;
        }

        private void ScrollChatToBottom()
        {
            // Multiple approaches to ensure scrolling works
            ChatHistory.ScrollToEnd();
            
            // Also try scrolling the document to the end
            if (ChatHistory.Document != null)
            {
                var scrollViewer = GetScrollViewer(ChatHistory);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToBottom();
                }
            }
        }

        private System.Windows.Controls.ScrollViewer? GetScrollViewer(System.Windows.DependencyObject o)
        {
            if (o is System.Windows.Controls.ScrollViewer)
                return (System.Windows.Controls.ScrollViewer)o;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(o, i);
                var result = GetScrollViewer(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void RemoveLastMessage()
        {
            if (ChatHistory.Document.Blocks.Count > 0)
            {
                var lastBlock = ChatHistory.Document.Blocks.LastBlock;
                if (lastBlock != null)
                {
                    ChatHistory.Document.Blocks.Remove(lastBlock);
                }
            }
        }

        private void AddMessageToChat(string sender, string message, System.Windows.Media.Color senderColor)
        {
            var paragraph = new System.Windows.Documents.Paragraph();
            
            // Add timestamp and sender
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var headerRun = new System.Windows.Documents.Run($"[{timestamp}] {sender}: ");
            headerRun.FontWeight = System.Windows.FontWeights.Bold;
            headerRun.Foreground = new System.Windows.Media.SolidColorBrush(senderColor);
            paragraph.Inlines.Add(headerRun);
            
            // Add message content
            var messageRun = new System.Windows.Documents.Run(message);
            paragraph.Inlines.Add(messageRun);
            
            // Add separator line
            paragraph.Inlines.Add(new System.Windows.Documents.LineBreak());
            var separatorRun = new System.Windows.Documents.Run("─────────────────────────────────────────");
            separatorRun.Foreground = System.Windows.Media.Brushes.LightGray;
            paragraph.Inlines.Add(separatorRun);
            
            ChatHistory.Document.Blocks.Add(paragraph);
            
            // Auto-scroll to bottom after adding message
            ScrollChatToBottom();
        }

        private string GetSelectedQuerySilent()
        {
            try
            {
                var selectedTab = QueryTabControl.SelectedItem as TabItem;
                if (selectedTab != null)
                {
                    var textBox = selectedTab.Content as TextBox;
                    if (textBox != null)
                    {
                        return textBox.Text ?? string.Empty;
                    }
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GetDefaultSystemPrompt()
        {
            var prompt = "You are an AI assistant pair programmer helping with SQL query generation and data analysis for a Sage 100 ProvideX ERP system. ";
            prompt += "You have FULL visibility into the application state including:\n";
            prompt += "- Object Explorer showing all tables and their mirror status\n";
            prompt += "- Query Editor with current SQL queries\n";
            prompt += "- Results Grid showing actual query results\n";
            prompt += "- Both ProvideX (original) and SQLite (mirrored) databases\n\n";
            
            prompt += "IMPORTANT: Tables must be mirrored from ProvideX to SQLite before they can be queried in SQLite.\n\n";
            
            prompt += "CRITICAL SQL SYNTAX RULES:\n";
            prompt += "- YOU ARE WORKING WITH SQLite DATABASE - NOT SQL Server!\n";
            prompt += "- SQLite syntax: Use 'SELECT ... LIMIT n' (NOT 'SELECT TOP n')\n";
            prompt += "- ProvideX syntax: Use 'SELECT TOP n' (only when querying ProvideX directly)\n";
            prompt += "- Table names with special chars: Use [brackets] like [SO_SalesOrderHeader]\n";
            prompt += "- Column names are case-sensitive in SQLite\n";
            prompt += "- Always check if a table is mirrored before querying SQLite\n";
            prompt += "- When a query fails with syntax error, immediately fix the SQLite syntax\n\n";
            
            prompt += "AVAILABLE ACTIONS YOU CAN SUGGEST:\n";
            prompt += "- Generate SQL queries (will be offered to user to insert and optionally execute)\n";
            prompt += "- When you identify a table needs mirroring, say 'ACTION: MIRROR tablename'\n";
            prompt += "- CRITICAL: ALWAYS immediately explore tables after mirroring with 'ACTION: EXPLORE tablename' to see actual column names and data\n";
            prompt += "- When you want to execute a query after generating it, say 'ACTION: EXECUTE'\n";
            prompt += "- When you need to examine table structure and data, say 'ACTION: EXPLORE tablename'\n";
            prompt += "- IMPORTANT: When queries return 0 rows, ALWAYS explore the relevant tables to understand the actual data\n";
            prompt += "- DO NOT hesitate or ask for permission - take actions immediately using ACTION commands\n";
            prompt += "- ALWAYS end your responses with ACTION commands when work needs to be done\n";
            prompt += "- After mirroring and exploring tables, IMMEDIATELY generate and execute a query to answer the user's request\n";
            prompt += "- IMPORTANT: Use HISTORY tables for completed transactions (SO_SalesOrderHistoryHeader, AR_InvoiceHistoryHeader, etc.)\n";
            prompt += "- Use current tables (SO_SalesOrderHeader, etc.) only for active/pending transactions\n";
            prompt += "- For invoices, sales history, and completed orders, ALWAYS start with HISTORY tables first\n";
            prompt += "These actions will be automatically performed when detected in your response.\n\n";
            
            return prompt;
        }

        private string BuildContextMessage(string userMessage, string currentQuery)
        {
            // Start with custom system prompt
            var context = _customSystemPrompt + "\n";
            
            // Include conversation history (last 10 exchanges to manage context size)
            if (_conversationHistory.Count > 0)
            {
                context += "=== RECENT CONVERSATION HISTORY ===\n";
                var recentHistory = _conversationHistory.TakeLast(Math.Min(20, _conversationHistory.Count)); // Last 10 exchanges
                foreach (var (sender, message) in recentHistory)
                {
                    context += $"{sender}: {message}\n";
                }
                context += "\n";
            }
            
            // Current query in editor
            if (!string.IsNullOrEmpty(currentQuery))
            {
                context += $"=== CURRENT QUERY IN EDITOR ===\n{currentQuery}\n\n";
            }
            
            // Object Explorer state
            context += "=== OBJECT EXPLORER (Tables) ===\n";
            foreach (TreeViewItem item in ObjectExplorer.Items)
            {
                if (item.Tag != null && item.Header != null)
                {
                    var tableName = item.Tag.ToString();
                    var displayName = item.Header.ToString();
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        context += $"{displayName}\n";
                        
                        // Explain the status indicators
                        if (displayName.Contains("🟢"))
                            context += "  [Fully mirrored to SQLite]\n";
                        else if (displayName.Contains("🟡"))
                            context += "  [Partially mirrored to SQLite]\n";
                        else if (displayName.Contains("🔴"))
                            context += "  [Empty mirror in SQLite]\n";
                        else if (displayName.Contains("⚪"))
                            context += "  [Not mirrored to SQLite - must mirror before querying]\n";
                    }
                }
            }
            context += "\n";
            
            // Results Grid state
            context += "=== CURRENT RESULTS GRID ===\n";
            var dataView = ResultsGrid.ItemsSource as System.Data.DataView;
            if (dataView != null && dataView.Table != null && dataView.Table.Rows.Count > 0)
            {
                var table = dataView.Table;
                context += $"Showing {table.Rows.Count} rows from: {ResultsLabel.Content}\n";
                context += "Columns: " + string.Join(", ", table.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName)) + "\n";
                
                // Show first few rows as sample
                context += "Sample data (first 5 rows):\n";
                for (int i = 0; i < Math.Min(5, table.Rows.Count); i++)
                {
                    var row = table.Rows[i];
                    var values = row.ItemArray.Select(v => v?.ToString() ?? "NULL").Take(5); // First 5 columns
                    context += $"Row {i + 1}: " + string.Join(" | ", values) + "\n";
                }
                
                if (table.Rows.Count > 5)
                    context += $"... and {table.Rows.Count - 5} more rows\n";
            }
            else
            {
                context += "No results currently displayed\n";
                
                // If no results and there's a query, suggest exploration
                if (!string.IsNullOrEmpty(currentQuery))
                {
                    var tablesInQuery = ExtractTableNamesFromQuery(currentQuery);
                    if (tablesInQuery.Any())
                    {
                        context += "\n=== RELATED TABLE SCHEMAS (for current query) ===\n";
                        foreach (var tableName in tablesInQuery.Take(3)) // Limit to 3 tables to avoid context overflow
                        {
                            if (_sqliteService.TableExists(tableName))
                            {
                                context += _sqliteService.GetTableSchema(tableName) + "\n";
                                context += _sqliteService.GetSampleData(tableName, 3) + "\n";
                            }
                        }
                    }
                }
            }
            context += "\n";
            
            context += $"=== USER REQUEST ===\n{userMessage}\n\n";
            
            return context;
        }

        private async Task<string> ProcessGeminiResponseAsync(string response)
        {
            bool queryInserted = false;
            bool shouldExecute = response.Contains("ACTION: EXECUTE", StringComparison.OrdinalIgnoreCase);
            
            // Check for mirror actions first (must happen before exploring)
            var mirrorPattern = @"ACTION:\s*MIRROR\s+(\w+)";
            var mirrorMatches = System.Text.RegularExpressions.Regex.Matches(response, mirrorPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach (System.Text.RegularExpressions.Match match in mirrorMatches)
            {
                var tableName = match.Groups[1].Value;
                AddMessageToChat("System", $"Mirroring table {tableName}...", System.Windows.Media.Colors.Purple);
                
                try
                {
                    using (var odbcConnection = new OdbcConnection(_connectionString))
                    {
                        odbcConnection.Open();
                        _sqliteService.MirrorTable(odbcConnection, tableName);
                        AddMessageToChat("System", $"Table {tableName} mirrored successfully.", System.Windows.Media.Colors.Purple);
                        
                        // Refresh Object Explorer
                        PopulateObjectExplorer();
                    }
                }
                catch (Exception ex)
                {
                    AddMessageToChat("System", $"Error mirroring {tableName}: {ex.Message}", System.Windows.Media.Colors.Red);
                }
            }
            
            // Check for explore actions (after mirroring is complete)
            var explorePattern = @"ACTION:\s*EXPLORE\s+(\w+)";
            var exploreMatches = System.Text.RegularExpressions.Regex.Matches(response, explorePattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach (System.Text.RegularExpressions.Match match in exploreMatches)
            {
                var tableName = match.Groups[1].Value;
                AddMessageToChat("System", $"Exploring table {tableName}...", System.Windows.Media.Colors.Purple);
                
                try
                {
                    var exploration = _sqliteService.ExploreTable(tableName);
                    AddMessageToChat("System", exploration, System.Windows.Media.Colors.DarkBlue);
                }
                catch (Exception ex)
                {
                    AddMessageToChat("System", $"Error exploring {tableName}: {ex.Message}", System.Windows.Media.Colors.Red);
                }
            }
            
            // Check if response contains SQL query
            if (response.Contains("SELECT", StringComparison.OrdinalIgnoreCase) || 
                response.Contains("INSERT", StringComparison.OrdinalIgnoreCase) ||
                response.Contains("UPDATE", StringComparison.OrdinalIgnoreCase) ||
                response.Contains("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                // Look for SQL code blocks first (```sql, ```sqlite, or ```)
                var sqlBlockPattern = @"```(?:\w+)?\s*\n([\s\S]*?)\n```";
                var sqlBlockMatch = System.Text.RegularExpressions.Regex.Match(response, sqlBlockPattern);
                
                if (sqlBlockMatch.Success)
                {
                    var query = sqlBlockMatch.Groups[1].Value.Trim();
                    
                    if (shouldExecute)
                    {
                        // Auto-insert and execute
                        SetQueryEditorText(query);
                        queryInserted = true;
                        AddMessageToChat("System", "Auto-executing generated query...", System.Windows.Media.Colors.Purple);
                        await ExecuteCurrentQuery();
                    }
                    else
                    {
                        // Ask user
                        var result = MessageBox.Show($"Would you like to insert this query into the editor?\n\n{query}\n\nClick Yes to insert, No to skip", 
                                          "Insert Query", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            SetQueryEditorText(query);
                            queryInserted = true;
                            
                            // Ask if they want to execute
                            if (MessageBox.Show("Would you like to execute this query now?", "Execute Query", 
                                              MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                await ExecuteCurrentQuery();
                            }
                        }
                    }
                }
            }
            
            // Check if Gemini executed actions but didn't provide a query to complete the task
            // This happens when Gemini mirrors/explores tables but doesn't continue with the actual query
            bool needsContinuation = false;
            if ((mirrorMatches.Count > 0 || exploreMatches.Count > 0) && !queryInserted && _isWorkingOnTask)
            {
                // Gemini executed actions but didn't provide a query - we need to continue
                needsContinuation = true;
            }
            
            // If Gemini executed actions without providing a query to complete the task, prompt it to continue
            if (needsContinuation && !string.IsNullOrEmpty(_currentUserRequest))
            {
                await Task.Delay(1000); // Small delay to let table explorations complete
                
                var continuePrompt = $"The tables have been mirrored and explored. Please continue with the original request: '{_currentUserRequest}'. Generate and execute the appropriate SQL query.";
                
                AddMessageToChat("System", "📋 Tables ready. Asking Gemini to continue with the query...", System.Windows.Media.Colors.Orange);
                
                try
                {
                    SetLoadingState(true);
                    var contextMessage = BuildContextMessage(continuePrompt, GetSelectedQuerySilent());
                    var geminiResponse = await Task.Run(async () => await _geminiService.SendMessage(contextMessage));
                    
                    _conversationHistory.Add(("Gemini", geminiResponse));
                    AddMessageToChat("Gemini", geminiResponse, System.Windows.Media.Colors.Green);
                    
                    // Process the new response recursively
                    return await ProcessGeminiResponseAsync(geminiResponse);
                }
                catch (Exception ex)
                {
                    AddMessageToChat("System", $"Error getting continuation: {ex.Message}", System.Windows.Media.Colors.Red);
                }
                finally
                {
                    SetLoadingState(false);
                }
            }
            
            return response;
        }

        private async Task ExecuteCurrentQuery()
        {
            try
            {
                var query = GetSelectedQuerySilent();
                if (!string.IsNullOrEmpty(query))
                {
                    var result = _sqliteService.ExecuteQuery(query);
                    ResultsGrid.ItemsSource = result.DefaultView;
                    ResultsLabel.Content = "Results - SQLite";
                    AddMessageToChat("System", $"Query executed successfully. {result.Rows.Count} rows returned.", System.Windows.Media.Colors.Purple);
                    
                    // If query returned 0 rows or few results, trigger Gemini follow-up
                    if (result.Rows.Count == 0)
                    {
                        await HandleEmptyQueryResults(query);
                    }
                    else if (result.Rows.Count > 0)
                    {
                        // Add successful results to conversation history for context
                        var resultSummary = FormatQueryResults(query, result);
                        _conversationHistory.Add(("System", $"Query Result: {resultSummary}"));
                        
                        // If we're working on a task and got partial results, ask Gemini to continue
                        if (_isWorkingOnTask && !string.IsNullOrEmpty(_currentUserRequest))
                        {
                            await HandlePartialResults(query, result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddMessageToChat("System", $"Query execution error: {ex.Message}", System.Windows.Media.Colors.Red);
                // Also add error to conversation history
                _conversationHistory.Add(("System", $"Query Error: {ex.Message}"));
                
                // Trigger investigation for SQL syntax errors
                var query = GetSelectedQuerySilent();
                if (!string.IsNullOrEmpty(query))
                {
                    await HandleQueryError(query, ex.Message);
                }
            }
        }

        private async Task HandleQueryError(string query, string errorMessage)
        {
            AddMessageToChat("System", "Query failed with syntax error. Asking Gemini to fix the SQL syntax...", System.Windows.Media.Colors.Orange);
            
            // Create specific investigation prompt for syntax errors
            var investigationPrompt = $"The query '{query}' failed with this error: {errorMessage}\n\n" +
                "IMPORTANT SYNTAX RULES:\n" +
                "- This is SQLite, NOT SQL Server or ProvideX\n" +
                "- Use 'LIMIT n' instead of 'TOP n'\n" +
                "- Use '[TableName]' brackets for table names with special characters\n" +
                "- Column names are case-sensitive\n" +
                "- Use proper SQLite syntax throughout\n\n" +
                "Please:\n" +
                "1. Identify the syntax error in the failed query\n" +
                "2. Generate a corrected query using proper SQLite syntax\n" +
                "3. Execute the corrected query automatically\n\n" +
                "IMPORTANT: Use ACTION: EXECUTE after generating the corrected query.\n\n" +
                "Remember: Convert any SQL Server syntax to SQLite syntax.";

            await TriggerGeminiSyntaxFix(query, errorMessage, investigationPrompt);
        }

        private async Task HandleEmptyQueryResults(string query)
        {
            var tablesInQuery = ExtractTableNamesFromQuery(query);
            if (tablesInQuery.Any())
            {
                AddMessageToChat("System", "Query returned 0 rows. Exploring tables and asking Gemini to investigate...", System.Windows.Media.Colors.Orange);
                
                var explorationData = "";
                foreach (var tableName in tablesInQuery)
                {
                    if (_sqliteService.TableExists(tableName))
                    {
                        var exploration = _sqliteService.ExploreTable(tableName);
                        AddMessageToChat("System", exploration, System.Windows.Media.Colors.DarkBlue);
                        explorationData += exploration + "\n\n";
                    }
                }

                // Add the empty result to conversation history
                _conversationHistory.Add(("System", $"Query returned 0 rows: {query}"));
                _conversationHistory.Add(("System", $"Table exploration data: {explorationData}"));

                // Trigger Gemini to investigate the issue
                await TriggerGeminiInvestigation(query, explorationData);
            }
        }

        private async Task HandlePartialResults(string query, System.Data.DataTable result)
        {
            try
            {
                // Format the current results for Gemini
                var resultSummary = FormatQueryResults(query, result);
                
                // Create a prompt asking Gemini to continue working on the original task
                var continuePrompt = $@"I executed the query and got {result.Rows.Count} row(s). Here are the current results:

{resultSummary}

Original user request: ""{_currentUserRequest}""

Please continue working on this request. If these results answer the user's question, please provide a summary and indicate the task is complete. If more work is needed (like getting detailed information, joining with other tables, or refining the query), please continue with the appropriate next steps immediately.

You can:
- ACTION: MIRROR tablename (to mirror additional tables)
- ACTION: EXPLORE tablename (to examine table structure and data)  
- ACTION: EXECUTE (to run a new query)

MANDATORY: If additional work is needed, end your response with the appropriate ACTION commands. Do NOT hesitate or wait for permission - take action immediately to complete the user's request.";

                // Add the continuation prompt to conversation history
                _conversationHistory.Add(("System", continuePrompt));
                
                // Send to Gemini for continuation
                SetLoadingState(true);
                AddMessageToChat("System", "🔄 Analyzing results and continuing work on your request...", System.Windows.Media.Colors.Orange);
                
                var contextMessage = BuildContextMessage(continuePrompt, query);
                var geminiResponse = await Task.Run(async () => await _geminiService.SendMessage(contextMessage));
                
                // Add Gemini's response to conversation history and chat
                _conversationHistory.Add(("Gemini", geminiResponse));
                AddMessageToChat("Gemini", geminiResponse, System.Windows.Media.Colors.Green);
                
                // Process the response for actions and potential task completion
                var processedResponse = await ProcessGeminiResponseAsync(geminiResponse);
                
                // Check if Gemini indicates the task is complete
                if (processedResponse.Contains("task is complete", StringComparison.OrdinalIgnoreCase) ||
                    processedResponse.Contains("request is satisfied", StringComparison.OrdinalIgnoreCase) ||
                    processedResponse.Contains("fully answers", StringComparison.OrdinalIgnoreCase))
                {
                    _isWorkingOnTask = false;
                    _currentUserRequest = string.Empty;
                    AddMessageToChat("System", "✅ Task completed successfully!", System.Windows.Media.Colors.Green);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in HandlePartialResults: {ex.Message}");
                AddMessageToChat("System", $"Error continuing task: {ex.Message}", System.Windows.Media.Colors.Red);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async Task TriggerGeminiSyntaxFix(string failedQuery, string errorMessage, string investigationPrompt)
        {
            try
            {
                SetLoadingState(true);
                AddMessageToChat("Gemini", "🔧 Fixing SQL syntax error...", System.Windows.Media.Colors.LightGray);

                var currentQuery = GetSelectedQuerySilent();
                var contextMessage = BuildContextMessage(investigationPrompt, currentQuery);

                var geminiResponse = await Task.Run(async () => 
                    await _geminiService.SendMessage(contextMessage));

                RemoveLastMessage();
                
                _conversationHistory.Add(("Gemini", geminiResponse));
                AddMessageToChat("Gemini", geminiResponse, System.Windows.Media.Colors.DarkGreen);
                var processedResponse = await ProcessGeminiResponseAsync(geminiResponse);
            }
            catch (Exception ex)
            {
                RemoveLastMessage();
                AddMessageToChat("Error", $"Syntax fix error: {ex.Message}", System.Windows.Media.Colors.Red);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async Task TriggerGeminiInvestigation(string failedQuery, string explorationData)
        {
            try
            {
                SetLoadingState(true);
                AddMessageToChat("Gemini", "🔍 Investigating why the query returned no results...", System.Windows.Media.Colors.LightGray);

                var investigationPrompt = $"The query '{failedQuery}' returned 0 rows. " +
                    "Based on the table exploration data I can see, please:\n" +
                    "1. Identify why the query failed (likely incorrect column values or search criteria)\n" +
                    "2. Generate a corrected query using LIKE patterns or different search criteria\n" +
                    "3. Execute the corrected query immediately - DO NOT wait for permission\n\n" +
                    "MANDATORY: End your response with 'ACTION: EXECUTE' to run the corrected query.\n" +
                    "If you need to explore more tables first, use 'ACTION: EXPLORE tablename' then 'ACTION: EXECUTE'.\n\n" +
                    "Investigation context: The user was looking for data that should exist, so adjust the search strategy and take action immediately.";

                var currentQuery = GetSelectedQuerySilent();
                var contextMessage = BuildContextMessage(investigationPrompt, currentQuery);

                var geminiResponse = await Task.Run(async () => 
                    await _geminiService.SendMessage(contextMessage));

                RemoveLastMessage();
                
                _conversationHistory.Add(("Gemini", geminiResponse));
                AddMessageToChat("Gemini", geminiResponse, System.Windows.Media.Colors.DarkGreen);
                var processedResponse = await ProcessGeminiResponseAsync(geminiResponse);
            }
            catch (Exception ex)
            {
                RemoveLastMessage();
                AddMessageToChat("Error", $"Investigation error: {ex.Message}", System.Windows.Media.Colors.Red);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private string FormatQueryResults(string query, System.Data.DataTable result)
        {
            if (result.Rows.Count == 0) return "No rows returned";
            
            var summary = $"{result.Rows.Count} rows returned from query: {query}\n";
            
            // Show first few rows as sample
            if (result.Rows.Count > 0)
            {
                summary += "Sample results:\n";
                var columnNames = result.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName).Take(5).ToArray();
                summary += string.Join(" | ", columnNames) + "\n";
                
                for (int i = 0; i < Math.Min(3, result.Rows.Count); i++)
                {
                    var row = result.Rows[i];
                    var values = row.ItemArray.Take(5).Select(v => v?.ToString()?.Trim() ?? "NULL").ToArray();
                    summary += string.Join(" | ", values) + "\n";
                }
            }
            
            return summary;
        }

        private List<string> ExtractTableNamesFromQuery(string query)
        {
            var tableNames = new List<string>();
            
            // Simple pattern to extract table names from FROM and JOIN clauses
            var patterns = new[]
            {
                @"FROM\s+(\w+)",
                @"JOIN\s+(\w+)",
                @"INNER\s+JOIN\s+(\w+)",
                @"LEFT\s+JOIN\s+(\w+)",
                @"RIGHT\s+JOIN\s+(\w+)"
            };
            
            foreach (var pattern in patterns)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(query, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var tableName = match.Groups[1].Value.Trim('[', ']'); // Remove brackets if present
                    if (!tableNames.Contains(tableName))
                    {
                        tableNames.Add(tableName);
                    }
                }
            }
            
            return tableNames;
        }

        private void ClearChatButton_Click(object sender, RoutedEventArgs e)
        {
            ChatHistory.Document.Blocks.Clear();
            _conversationHistory.Clear();
            _isWorkingOnTask = false;
            _currentUserRequest = string.Empty;
            AddMessageToChat("System", "Chat history cleared. Ready for new task.", System.Windows.Media.Colors.Gray);
        }

        #region Menu Event Handlers

        private void NewQuery_Click(object sender, RoutedEventArgs e)
        {
            // Clear the current query editor
            if (QueryTabControl.SelectedContent is TextBox textBox)
            {
                textBox.Clear();
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "SQL Files (*.sql)|*.sql|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Open SQL File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var content = System.IO.File.ReadAllText(openFileDialog.FileName);
                    if (QueryTabControl.SelectedContent is TextBox textBox)
                    {
                        textBox.Text = content;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "SQL Files (*.sql)|*.sql|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Save SQL File"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var query = GetSelectedQuerySilent();
                    System.IO.File.WriteAllText(saveFileDialog.FileName, query);
                    MessageBox.Show("File saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ClearQuery_Click(object sender, RoutedEventArgs e)
        {
            if (QueryTabControl.SelectedContent is TextBox textBox)
            {
                textBox.Clear();
            }
        }

        private void ClearResults_Click(object sender, RoutedEventArgs e)
        {
            ResultsGrid.ItemsSource = null;
            ResultsLabel.Content = "Results";
        }

        private void EditSystemPrompt_Click(object sender, RoutedEventArgs e)
        {
            var defaultPrompt = GetDefaultSystemPrompt();
            var editor = new SystemPromptEditorWindow(_customSystemPrompt, defaultPrompt)
            {
                Owner = this
            };

            if (editor.ShowDialog() == true && editor.PromptChanged)
            {
                _customSystemPrompt = editor.SystemPrompt;
                MessageBox.Show("System prompt updated successfully. Changes will take effect for new conversations.",
                    "System Prompt Updated", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ResetSystemPrompt_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset the system prompt to default? This will lose all your customizations.",
                "Reset System Prompt",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _customSystemPrompt = GetDefaultSystemPrompt();
                SystemPromptEditorWindow.SaveSystemPrompt(_customSystemPrompt);
                MessageBox.Show("System prompt reset to default.", "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Gemini ProvideX Report Generator\n\n" +
                "An AI-powered tool for querying Sage 100 ProvideX databases with Google Gemini AI assistance.\n\n" +
                "Features:\n" +
                "• Automatic table mirroring from ProvideX to SQLite\n" +
                "• Intelligent query generation and error correction\n" +
                "• Natural language database interactions\n" +
                "• Real-time schema exploration\n\n" +
                "Built with WPF and Google Gemini AI",
                "About Gemini ProvideX Report Generator",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Documentation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "README.md",
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show(
                    "Documentation not found. Please check the README.md file in the application directory.",
                    "Documentation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        #endregion
    }
}
