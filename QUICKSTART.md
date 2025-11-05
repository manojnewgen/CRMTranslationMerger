# 🚀 Quick Start Guide

## Prerequisites
- .NET 9.0 SDK installed
- OpenAI API key (or Azure OpenAI credentials)

## Setup

### 1. Configure API Keys
Copy `.env.example` to `.env` and add your OpenAI API key:

```bash
cp .env.example .env
```

Edit `.env`:
```env
OPENAI_API_KEY=sk-proj-your-actual-key-here
OPENAI_ENDPOINT=https://api.openai.com/v1/chat/completions
OPENAI_MODEL=gpt-4o-mini
```

### 2. Run the Application

**Option A: Using PowerShell Script (Recommended)**
```powershell
.\run-dev.ps1
```

**Option B: Manual Start**

Terminal 1 (Server):
```powershell
cd CRMTranslationMerger.Server
dotnet watch run
```

Terminal 2 (Client):
```powershell
cd CRMTranslationMerger.Client
dotnet watch run
```

### 3. Access the Application
Open your browser to: **https://localhost:7046**

## How to Use

### Step 1: Prepare Your Files

**JSON File** - Your existing CRM email translations with this structure:
```json
{
  "Header": "Email Template Name",
  "Content": {
    "LocalizedContents": [
      {
        "CultureCode": "en-US",
        "Content": "Hello {{LoadedData.SenderProfile.Handle}}!"
      }
    ]
  }
}
```

**Excel File** - New translations to merge:
| CultureCode | Content |
|-------------|---------|
| en-US | Welcome [Sender name]! |
| es-ES | ¡Bienvenido [Sender name]! |

### Step 2: Upload Files
1. Upload your JSON file (existing translations)
2. Upload your Excel file (new translations)

### Step 3: Configure Options
- ✅ **Enable "Use AI to convert placeholders"** - AI will convert `[Sender name]` to `{{LoadedData.SenderProfile.Handle}}`
- ❌ **Disable** - Excel content will be used as-is

### Step 4: Merge & Download
Click **"Merge Translations"** and download the merged JSON file.

## Features

✅ **AI-Powered Conversion** - Automatically converts Excel placeholders to Handlebars expressions  
✅ **Client-Side Excel Parsing** - Your files never leave your browser  
✅ **Smart Merging** - Adds new translations and updates existing ones  
✅ **Multi-Language Support** - Handle any number of culture codes  
✅ **Secure API Proxy** - API keys stay server-side only

## Troubleshooting

### Build Errors
```powershell
dotnet clean
dotnet restore
dotnet build
```

### CORS Errors
Check that client and server URLs match in:
- `CRMTranslationMerger.Server/Program.cs` (CORS policy)
- `CRMTranslationMerger.Client/Program.cs` (HttpClient BaseAddress)

### AI Not Working
1. Verify `.env` file exists in solution root
2. Check `OPENAI_API_KEY` is set correctly
3. Restart the server

### Excel Parsing Fails
Ensure your Excel file has:
- A sheet with columns: `CultureCode`, `Content`
- At least one row of data

## Architecture

```
┌─────────────────────────────────────────────────────┐
│  Browser (Client - Blazor WASM)                     │
│  - Upload & parse Excel (SheetJS)                   │
│  - Render UI                                        │
│  - Call server API                                  │
└────────────────┬────────────────────────────────────┘
                 │ HTTPS
                 ▼
┌─────────────────────────────────────────────────────┐
│  Server (ASP.NET Core API)                          │
│  - Proxy AI requests                                │
│  - Protect API keys                                 │
│  - No CORS issues                                   │
└────────────────┬────────────────────────────────────┘
                 │ HTTPS
                 ▼
┌─────────────────────────────────────────────────────┐
│  OpenAI / Azure OpenAI                              │
│  - Convert placeholders                             │
│  - Return Handlebars expressions                    │
└─────────────────────────────────────────────────────┘
```

## Development

### Project Structure
```
CRMTranslationMerger/
├── CRMTranslationMerger.Client/      # Blazor WASM (net9.0)
│   ├── Pages/Merge.razor             # Main UI
│   └── wwwroot/excelInterop.js       # Excel parsing
├── CRMTranslationMerger.Server/      # ASP.NET Core (net9.0)
│   └── Services/OpenAiService.cs     # AI proxy
└── CRMTranslationMerger.Shared/      # DTOs (net9.0)
    └── Models/                        # Shared models
```

### Adding New Features
1. Update Shared models for new DTOs
2. Add server endpoints in `Program.cs`
3. Update client UI in `Merge.razor`

## Learn More
- [Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [OpenAI API](https://platform.openai.com/docs)
- [SheetJS](https://docs.sheetjs.com/)
