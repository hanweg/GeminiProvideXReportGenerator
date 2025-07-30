# Gemini ProvideX Report Generator

A powerful C# WPF application that provides AI-human collaborative SQL query generation and data analysis for Sage 100 Advanced ProvideX ERP systems. Features real-time table mirroring from ProvideX to SQLite with Google Gemini AI integration for intelligent query assistance.

## 🎯 Overview

This application bridges the gap between legacy ProvideX databases and modern AI-assisted data analysis by:
- **Mirroring** ProvideX tables to SQLite for efficient querying
- **AI-powered** query generation and debugging with Google Gemini
- **Real-time** schema exploration and data discovery
- **Automatic** error correction and query optimization
- **Interactive** chat interface for natural language database queries

## ⚖️ Legal & Licensing Requirements

**IMPORTANT**: This software is released under the MIT License and is provided as-is. However, it requires additional licensed components that are NOT included:

### Required Licensed Software
- **Sage 100 Advanced ERP System**: You must have a valid, licensed installation of Sage 100 Advanced (formerly MAS 90/200)
- **ProvideX ODBC Driver**: Requires proper licensing from Sage or authorized dealers
- **Google Gemini API**: Requires valid API key and adherence to Google's terms of service

### Legal Disclaimers
- This software does NOT include any proprietary Sage 100 Advanced or ProvideX components
- Users are responsible for compliance with all applicable software licenses
- No warranty or support is provided for proprietary third-party components
- This tool is intended for authorized Sage 100 Advanced users with legitimate licenses only
- The software connects to databases through standard ODBC interfaces only

### Compliance Requirements
- Ensure your Sage 100 Advanced license permits ODBC access
- Verify ODBC driver licensing covers your intended use (read-only vs read-write)
- Maintain compliance with Google Gemini API terms of service
- This software does not circumvent or replace any licensing requirements

By using this software, you acknowledge that you have the necessary licenses for all required components and agree to use it in compliance with all applicable terms and conditions.

## ✨ Key Features

### 🔄 Database Integration
- **Smart Table Mirroring**: Mirror tables from ProvideX to SQLite with visual status indicators
- **Schema Preservation**: Maintains data types and structure during mirroring
- **Incremental Updates**: Re-mirror tables as needed

### 🤖 AI-Powered Query Assistant
- **Google Gemini Integration**: Full conversational AI for SQL assistance
- **Intelligent Context Awareness**: Sees table schemas, current queries, and results
- **Automatic Error Correction**: Fixes SQL syntax errors and data mismatches
- **Iterative Query Improvement**: Learns from failed queries and adjusts approach
- **Natural Language Interface**: Ask questions in plain English

## 🚀 Getting Started

### Prerequisites

**System Requirements:**
- Windows 10/11
- .NET 9.0 or later
- Visual Studio 2022 or later (for development)

**Required Licensed Software (NOT included):**
- **Sage 100 Advanced ERP System** with valid license
- **ProvideX ODBC Driver** with appropriate licensing:
  - Local ODBC driver requires serial number, user count, and activation key
  - Client/Server ODBC driver requires activated PxPlus File Server
- **Google Gemini API key** with valid Google Cloud account

**ODBC Configuration:**
- Properly configured ODBC DSN for your Sage 100 Advanced ProvideX database
- Valid database credentials managed through Windows ODBC
- Network connectivity to Sage 100 Advanced database server (if applicable)

### Installation
1. Clone the repository
2. `dotnet build`
3. `dotnet run`

### Configuration
1. **API Key**: Enter your Google Gemini API key in the startup dialog
2. **Model Selection**: Click "Fetch Available Models" to load compatible Gemini models
   - Choose from models like Gemini 2.5 Flash (fast), Gemini 2.5 Pro (advanced), etc.
   - Default is Gemini 1.5 Pro if no models are fetched
3. **DSN Selection**: Choose your ProvideX ODBC Data Source Name (Configured in Windows ODBC Data Sources)
4. **Authentication**: The app will prompt for database credentials when needed (Reccomended to save credentials in Windows ODBC Data Source Administrator -> Configure)

## 💡 How It Works

### Startup Flow
1. **Configuration Window**: Enter API key and select DSN
2. **Connection Validation**: Tests both Gemini API and database connections
3. **Main Interface**: Loads with Object Explorer showing available tables for mirroring

### AI Collaboration Workflow
```
User: "Find sales orders for customer XYZ"
  ↓
Gemini: Mirrors relevant tables (SO_SalesOrderHeader, AR_Customer)
  ↓
Gemini: Generates initial query
  ↓
System: Executes query and provides results/errors to Gemini
  ↓
Gemini: Automatically fixes syntax errors or data matching issues
  ↓
System: Shows final results in grid
```

<img width="1010" height="761" alt="gempvx001" src="https://github.com/user-attachments/assets/88b4f977-9145-438a-bf3f-155c193bc4b7" />

<img width="1010" height="761" alt="gempvx002" src="https://github.com/user-attachments/assets/f7f35902-b094-4d82-afc4-2e3bff0dcb4a" />

### Intelligent Error Handling
- **Syntax Errors**: Auto-detects SQL Server vs SQLite syntax issues
- **Empty Results**: Explores table data to find correct search criteria
- **Connection Issues**: Graceful fallback with helpful error messages
- **Data Mismatches**: Tries to use fuzzy matching and LIKE patterns when needed

## 🔧 Technical Architecture

### Core Components
- **MainWindow**: Primary UI with split-panel layout
- **GeminiService**: AI integration with context-aware messaging
- **OdbcService**: ProvideX database connectivity and operations
- **SqliteService**: In-memory SQLite database with mirroring logic

### Key Technologies
- **WPF**: Modern desktop UI framework
- **SQLite**: Fast in-memory database for queries
- **ODBC**: Legacy database connectivity
- **Google Gemini AI**: Advanced language model for SQL assistance
- **Async/Await**: Non-blocking operations throughout

### Database Schema Handling
- **Dynamic Schema Detection**: Reads table structures from ODBC metadata
- **Type Mapping**: Converts ProvideX types to SQLite equivalents
- **Constraint Preservation**: Maintains primary keys and data constraints
- **Sample Data Caching**: Stores representative data for AI context

## 🎮 Usage Examples

### Basic Table Mirroring
1. Right-click table in Object Explorer
2. Select "Mirror to SQLite"
3. Watch status indicator change to show mirrored

### Automatic Gemini table guessing and mirroring, followed by query generation and results printing / analysis
<img width="1010" height="761" alt="image" src="https://github.com/user-attachments/assets/632fedbb-b6e8-42de-9c08-beb326c2595d" />
<img width="1010" height="761" alt="image" src="https://github.com/user-attachments/assets/ecc90c55-4e0e-4286-82fe-28caa2378e66" />

### AI-Assisted Queries (Enhanced Flow)
```
You: "Find the latest sales order to customer XYZ"
Gemini: I'll help you find that. Let me check the sales history tables.
        ACTION: MIRROR SO_SalesOrderHistoryHeader
        ACTION: MIRROR AR_Customer
System: [Mirrors tables automatically]
Gemini: Now let me explore the table structures.
        ACTION: EXPLORE SO_SalesOrderHistoryHeader
        ACTION: EXPLORE AR_Customer
System: [Shows table schemas]
System: 📋 Tables ready. Asking Gemini to continue with the query...
Gemini: Perfect! I can see the table structure. Let me find that order.
        SELECT * FROM SO_SalesOrderHistoryHeader WHERE CustomerNo = 'XYZ'...
        ACTION: EXECUTE
System: [Query executes and shows results - no manual intervention needed!]
```

### Automatic Error Recovery
```
Gemini: SELECT TOP 10 * FROM Customers
System: "SQL syntax error - SQLite uses LIMIT not TOP"
Gemini: 🔧 Fixing SQL syntax error...
        SELECT * FROM Customers LIMIT 10
        ACTION: EXECUTE
System: [Executes successfully]
```

### Zero Results Investigation
```
System: Query executed successfully. 0 rows returned.
System: Query returned 0 rows. Exploring tables and asking Gemini to investigate...
Gemini: 🔍 Investigating why the query returned no results...
        Let me try with a LIKE pattern instead...
        SELECT * FROM Customers WHERE CustomerName LIKE '%XYZ%'
        ACTION: EXECUTE
```

### Customizing AI Behavior
#### It is highly recommended to add commonly used Table and Column names to the system prompt, sample schema and data as well as natural language description of important relationships - also a good spot to put common pitfalls and what to avoid!
1. Go to Settings → System Prompt...
2. Edit the prompt to customize Gemini's behavior
3. Save changes (stored in SystemPrompt.txt)
4. New conversations will use your custom prompt

## 🛠️ Advanced Features

### Action Commands
Gemini can trigger automatic actions:
- `ACTION: MIRROR tablename` - Mirror a table from ProvideX
- `ACTION: EXECUTE` - Execute the generated query
- `ACTION: EXPLORE tablename` - Show schema and sample data

### Context Awareness
Gemini has full visibility into:
- Current query in editor
- Table mirror status and row counts
- Recent query results and errors
- Complete conversation history
- Real-time schema information

### Performance Optimizations
- **In-Memory SQLite**: Fast query execution
- **Background Threading**: Non-blocking AI operations
- **Lazy Loading**: Tables mirrored only when needed
- **Conversation History Management**: Automatic context pruning

## 🔒 Security & Compliance

### Security Considerations
- **API Key Storage**: Google Gemini API keys are entered per session and not persisted to disk
- **Database Credentials**: Uses Windows ODBC credential management for secure authentication
- **Network Security**: All AI communications use HTTPS connections to Google APIs
- **Data Privacy**: Only database schemas and sample data are sent to AI initially, ### Results of Queries are sent as well - be cautious with sensitive data!
- **Local Processing**: Table mirroring occurs locally using in-memory SQLite

### Compliance Guidelines
- **Data Governance**: Ensure compliance with your organization's data governance policies
- **License Compliance**: Maintain compliance with all required software licenses

## 📄 License

MIT License

### Third-Party Dependencies
This software relies on several third-party components with their own licenses:
- **Mscc.GenerativeAI**: For Google Gemini integration
- **Microsoft.Data.Sqlite**: SQLite database operations
- **System.Data.Odbc**: ODBC connectivity
- **Google.Apis.Auth**: Google API authentication

### Important License Notices
- This MIT license applies ONLY to the source code in this repository
- Required proprietary software (Sage 100, ProvideX ODBC drivers) have separate licensing terms
- Users must independently obtain and comply with all required third-party licenses
- The MIT license does not grant rights to any proprietary software or trademarks

## ™️ Trademark Acknowledgments

This software provides compatibility with third-party systems and uses trademark names solely for the purpose of identifying compatibility and system requirements under the principle of **Nominative Fair Use** as recognized in U.S. trademark law.

**Nominative Fair Use Justification:**
1. **Necessity**: The proprietary software products cannot be readily identified without using their trademarked names
2. **Minimal Use**: Only the minimum necessary trademark text is used (no logos, stylized fonts, or promotional materials)
3. **No Implied Endorsement**: Clear disclaimers prevent any suggestion of sponsorship, affiliation, or approval by trademark owners

**Trademark Notices:**
- **Sage**, **Sage 100**, **MAS 90**, **MAS 200**, and related marks are registered trademarks or trademarks of The Sage Group plc and its affiliated entities
- **ProvideX** is a trademark of Sage Software, Inc. and/or its affiliated entities
- **Google Gemini** and **Google AI** are trademarks of Google LLC
- **Microsoft**, **Windows**, **.NET**, and **WPF** are trademarks of Microsoft Corporation
- **SQLite** is a trademark of the SQLite Development Team

**Disclaimer:**
- This software is not affiliated with, endorsed by, sponsored by, or approved by Sage, Google, Microsoft, or any other trademark owner
- All trademarks are the property of their respective owners
- Trademark usage is limited to factual identification of compatibility and system requirements only
- No partnership, endorsement, or commercial relationship is implied or suggested

## 🙏 Acknowledgments

- **Google Gemini AI**: For providing the intelligent query assistance
- **SQLite Team**: For the excellent embedded database
- **Sage 100/ProvideX**: For the robust ERP foundation
- **Microsoft WPF Team**: For the modern desktop UI framework

---

*Built with ❤️ for the ERP community by leveraging the power of AI and modern development practices.*
