# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Gemini ProvideX Report Generator is a C# WPF application that bridges legacy ProvideX/Sage 100 ERP databases with modern AI-assisted data analysis. The application provides intelligent table mirroring from ProvideX to SQLite with Google Gemini AI integration for collaborative SQL query generation and debugging.

**Target Framework**: .NET 9.0 Windows  
**UI Framework**: WPF (Windows Presentation Foundation)  
**Primary Architecture**: MVVM with Service Layer Pattern

## Build and Development Commands

```bash
# Build the application
dotnet build

# Run the application
dotnet run

# Clean build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore

# Build specific configuration
dotnet build --configuration Release

# Run with specific arguments (if needed)
dotnet run -- [arguments]
```

## New Features (Latest Updates)

### Dynamic Gemini Model Selection
- **StartupConfigWindow** now includes model selection dropdown
- Fetches available models from Gemini API using `GoogleAI.ListModels()`
- Filters models to show only those supporting 'generateContent'
- User-friendly display names with model categorization
- Passes selected model to `GeminiService` constructor

### SQL Code Block Parsing Fix
- Updated regex pattern from `(?:sql)?` to `(?:\w+)?` in `ProcessGeminiResponseAsync`
- Now supports ```sqlite, ```sql, ```tsql, and any other language identifiers
- Fixes issue where queries in ```sqlite blocks weren't being detected

### Editable System Prompt
- **Settings → System Prompt...** opens a customizable prompt editor
- System prompts are saved to `SystemPrompt.txt` in the application directory
- Users can customize how Gemini behaves and responds
- Reset to default functionality available

### Enhanced Menu System
- **File menu**: New Query, Open/Save SQL files, Exit
- **Edit menu**: Clear Query, Clear Results
- **Settings menu**: System Prompt editor and reset
- **Help menu**: About dialog and documentation

### Improved AI Behavior
- Auto-EXPLORE tables immediately after mirroring to avoid column name guessing
- Prioritizes HISTORY tables (SO_SalesOrderHistoryHeader) for completed transactions
- More aggressive action-taking with reduced hesitation
- Automatic continuation after setup work until user's request is fully answered

### Auto-Continuation Logic
- Detects when Gemini executes actions but doesn't provide a query
- Automatically prompts Gemini to continue with the original request
- Eliminates most manual "continue" prompts
- Tracks task state with `_isWorkingOnTask` and `_currentUserRequest`

### Fixed Action Processing Order
- MIRROR actions now execute before EXPLORE actions (fixed in `ProcessGeminiResponseAsync`)
- Gemini's messages display before action results for natural conversation flow
- Prevents "Table does not exist" errors when exploring

### Async/Await Improvements
- Fixed `ExecuteCurrentQuery` from `async void` to `async Task`
- Properly awaits all async operations
- Ensures 0-row handling and error investigations complete properly

## Core Architecture

### Service Layer Pattern
- **OdbcService**: Handles ProvideX database connectivity via ODBC
  - Connection string format: `DSN={selected_dsn}`
  - Uses Windows ODBC credential management
  - Provides table enumeration and schema inspection
- **SqliteService**: Manages in-memory SQLite database with table mirroring
  - Creates in-memory SQLite database for fast querying
  - Handles type mapping from ProvideX to SQLite
  - Maintains mirrored table status and row counts
- **GeminiService**: Google Gemini AI integration with error handling
  - Configurable model selection (Gemini 1.5 Pro, 2.5 Flash, etc.)
  - DNS resolution checking and network error handling
  - Conversation context management

### Application Lifecycle
1. **StartupConfigWindow**: Captures Google Gemini API key and ODBC DSN selection
   - Model selection dropdown with dynamic API fetching
   - DSN enumeration from Windows registry
   - Connection validation before proceeding
2. **App.xaml.cs**: Critical shutdown mode management - uses `ShutdownMode.OnExplicitShutdown` during config to prevent premature exit
3. **MainWindow**: Split-panel UI with Object Explorer, Query Editor, Results Grid, and AI Chat
   - Async initialization with background table population
   - Cross-thread UI updates via `Dispatcher.InvokeAsync()`
   - Context building for AI with complete conversation history
4. **SystemPromptEditorWindow**: Editable system prompt interface with save/load functionality

### Key Classes and Responsibilities
- **MainWindow.xaml.cs**: Primary UI orchestration, conversation management, action processing
- **Models/**: Data models for Tables, Columns, QueryResults, ChatMessages
- **ViewModels/**: MVVM pattern implementation (currently minimal usage)
- **Services/**: Business logic layer with database and AI operations

### Key Technical Patterns

#### Dual Database Architecture
- **ProvideX (ODBC)**: Source database using legacy ERP system
- **SQLite (In-Memory)**: Target database for fast querying and AI analysis
- Table mirroring preserves schema and data types between systems

#### AI-Powered Query Assistance
- Gemini receives full context: table schemas, query results, error messages, conversation history
- ACTION commands trigger automatic operations: `MIRROR`, `EXECUTE`, `EXPLORE`
- Automatic error correction for SQL syntax differences (TOP vs LIMIT)
- Iterative query improvement when results are empty or incorrect

#### Async UI Operations
All database and AI operations use async/await patterns with `Task.Run` to prevent UI blocking. Loading indicators show during Gemini API calls.

## Important Implementation Details

### Table Status Indicators
Visual status in Object Explorer TreeView:
- ⚪ Not mirrored
- 🔴 Empty mirror (0 rows)
- 🟡 Partially mirrored
- 🟢 Fully mirrored

### Context Building for AI
The `BuildContextMessage()` method in MainWindow provides Gemini with:
- Current query editor content
- Table mirror status and row counts  
- Recent query results or error messages
- Schema information for mirrored tables
- Complete conversation history
- Custom system prompt (loaded from SystemPrompt.txt)
- Current task state tracking (`_isWorkingOnTask`, `_currentUserRequest`)

### Error Handling Patterns
- **Empty Query Results**: Automatically triggers `HandleEmptyQueryResults()` to investigate with Gemini
- **SQL Syntax Errors**: `HandleQueryError()` provides error context to Gemini for automatic correction
- **Network Issues**: DNS resolution checking and proxy configuration in App.xaml.cs

## Database Connectivity

### ODBC Configuration
- Uses Windows registry to enumerate available DSNs
- Relies on Windows ODBC credential management (no custom credential dialogs)
- Connection string format: `DSN={selected_dsn}`

### SQLite Schema Mapping
Type conversion from ProvideX to SQLite:
- VARCHAR/CHAR → TEXT
- INT/INTEGER → INTEGER  
- DECIMAL/NUMERIC/FLOAT → REAL
- DATETIME/DATE → TEXT

## AI Integration Specifics

### Gemini API Configuration
- Uses `Mscc.GenerativeAI` package with `Model.Gemini15Pro`
- Includes DNS resolution checking and network error handling
- Proxy configuration for corporate environments

### Conversation Management
- Maintains conversation history as `List<(string sender, string message)>`
- Provides schema exploration via `ExploreTable()` method
- Real-time context updates with query results and errors

## Dependencies

### NuGet Packages (from .csproj)
- `Mscc.GenerativeAI` (2.6.3) - Google Gemini AI client with model selection
- `Microsoft.Data.Sqlite` (9.0.6) - SQLite database operations for in-memory mirroring
- `System.Data.Odbc` (9.0.6) - ProvideX/legacy ERP database connectivity
- `Google.Apis.Auth` (1.70.0) - Authentication support for Google APIs

### Framework Dependencies
- **.NET 9.0 Windows** - Target framework with Windows-specific features
- **WPF (UseWPF=true)** - Windows Presentation Foundation for desktop UI
- **Nullable reference types enabled** - Modern C# null safety
- **Implicit usings enabled** - Reduced boilerplate imports

## Development Notes

### WPF-Specific Considerations
- Uses `Dispatcher.InvokeAsync()` for cross-thread UI updates
- RichTextBox for formatted chat with auto-scrolling
- GridSplitters for resizable panels
- Context menus for table operations

### Debugging Features
- Extensive debug output via `System.Diagnostics.Debug.WriteLine()`
- MessageBox debug popups during development (should be removed in production)
- Comprehensive error logging throughout service layers

### Action Processing Pipeline
The application processes Gemini ACTION commands through `ProcessGeminiResponseAsync()`:
1. **MIRROR actions** execute first to create SQLite tables
2. **EXPLORE actions** follow to analyze table schemas  
3. **EXECUTE actions** run SQL queries and handle results
4. **Auto-continuation** triggers if actions complete but no query is provided

### Important Code Patterns
- **Async/Await Throughout**: All database and AI operations use proper async patterns
- **Cross-Thread UI Updates**: Uses `Dispatcher.InvokeAsync()` for thread-safe UI updates
- **Error Recovery**: Automatic SQL syntax correction and empty result investigation
- **State Management**: Tracks conversation state with `_isWorkingOnTask` and `_currentUserRequest`

### Key Configuration Files
- **SystemPrompt.txt**: Custom AI behavior prompt (created at runtime)
- **App.xaml**: Application-level resources and shutdown behavior
- **MainWindow.xaml**: Split-panel UI layout with GridSplitters

## Working with This Codebase

When modifying this application:
1. **Maintain dual-database architecture** - Always preserve the ProvideX → SQLite mirroring pattern
2. **Ensure complete AI context** - All AI interactions must include conversation history and current state
3. **Handle async operations properly** - Use `Task.Run()` for background work, `Dispatcher.InvokeAsync()` for UI updates
4. **Preserve ACTION command processing** - The MIRROR → EXPLORE → EXECUTE pipeline is critical for AI functionality
5. **Test with real ProvideX data** - The application is designed for legacy ERP systems with specific data patterns