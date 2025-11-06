# CRM Translation Merger - Blazor WebAssembly

🚀 A complete Blazor WebAssembly application deployed on Azure Static Web Apps with AI-powered placeholder conversion.

## ✅ Production Deployment

**Live URL**: https://icy-hill-069f16b10.3.azurestaticapps.net

### Deployment Modes

| Mode | Best For | Setup |
|------|----------|-------|
| **Development** | Testing, personal use | Users provide their own OpenAI API keys through UI |
| **Production** | Shared organizational deployment | Azure Key Vault with Managed Identity |

## 🚀 Quick Start

### For End Users (No Setup Required)

1. Visit the app: https://icy-hill-069f16b10.3.azurestaticapps.net
2. Follow the 6-step on-screen guide
3. Choose conversion mode:
   - **Pattern Only**: Free, instant (for simple placeholders)
   - **AI-Powered**: Intelligent conversion (handles complex scenarios)
   - **Smart Hybrid**: Auto-detects complexity
4. If using AI mode, provide your OpenAI API key
5. Upload Excel file and download merged JSON

### For Administrators (Production Deployment)

Set up Azure Key Vault for centralized API key management:

```powershell
# Navigate to project root
cd C:\CRM-19119\CRMTranslationMerger

# Run automated setup (replace with your actual API key)
.\setup-keyvault.ps1 -OpenAiApiKey "sk-your-actual-key-here"
```

This creates:
- ✅ Azure Key Vault
- ✅ Managed Identity on Static Web App
- ✅ Secure secret storage
- ✅ Environment variables
- ✅ Access policies

**Cost**: ~$0.03 per 10,000 operations (essentially free for normal usage)

**Documentation**: See [SECURITY.md](SECURITY.md) for detailed setup and architecture

## ✅ What's Been Created

### Project Structure
```
CRMTranslationMerger/
├── CRMTranslationMerger.sln
├── CRMTranslationMerger.Client/          (Blazor WASM - net8.0)
│   ├── Pages/CrmMerge.razor             ✅ Complete UI with 6-step guide
│   ├── Services/PlaceholderConverter.cs ✅ Client-side pattern matching
│   └── wwwroot/
│       ├── staticwebapp.config.json     ✅ Azure SWA routing
│       └── excelInterop.js              ✅ Excel parsing via SheetJS
├── api/                                  (Azure Functions - Node.js 18)
│   ├── ConvertBatch/
│   │   └── index.js                     ✅ AI conversion with Key Vault support
│   ├── package.json                     ✅ Azure SDK dependencies
│   └── local.settings.json              ✅ Node.js runtime config
└── .github/workflows/
    └── azure-static-web-apps-*.yml      ✅ CI/CD deployment

### Shared Models (Reference - from original .NET architecture)
- ✅ `Models/AiRequest.cs`
- ✅ `Models/AiResponse.cs`
- ✅ `Models/LocalizedContent.cs`
- ✅ `Models/MergeResult.cs`
```

## 🔧 Key Features

### ✅ Enterprise-Grade Security
- **Development Mode**: User-provided API keys (never stored, TLS encrypted)
- **Production Mode**: Azure Key Vault with Managed Identity
- **Key Caching**: 1-hour TTL reduces Key Vault calls
- **Audit Logs**: Track API access through Azure Monitor
- **Zero Credentials**: Managed Identity eliminates credential management

### ✅ Intelligent Conversion
- **Pattern Matching**: Instant, free conversion for simple placeholders
- **AI-Powered**: Handles complex scenarios:
  - Gender conditionals: `him/her` → `{{#if (String.Equal gender "Female")}} her {{else}} him {{/if}}`
  - Helper functions: `String.Concat`, `String.Equal`, `Object.ToString`
  - Existing Handlebars preservation
  - Nested expressions
- **Smart Detection**: Auto-routes complex texts to AI

### ✅ AI Integration
- OpenAI GPT-4o Mini integration
- Azure OpenAI support
- Error handling and logging
- Fallback to manual detection

### ✅ Excel Processing
- SheetJS integration via JSInterop
- Client-side Excel parsing (no upload to server)
- Only text chunks sent to AI endpoint

## 📋 Implementation Status

| Component | Status | Notes |
|-----------|--------|-------|
| Shared Models | ✅ Complete | All DTOs created |
| Server AI Proxy | ✅ Complete | OpenAiService implemented |
| Server API Endpoint | ⏳ Template | Needs Program.cs update |
| Client UI | ⏳ Template | Needs Merge.razor creation |
| JSInterop | ⏳ Template | Needs excelInterop.js |
| Merge Logic | ⏳ Pending | Port from app.js |
| Configuration | ⏳ Template | Needs appsettings files |

## 🚀 Next Steps

1. **Fix Target Frameworks** (Client: net7.0 → net9.0)
2. **Update Program.cs** with AI endpoint
3. **Create Client UI files** (follow BLAZOR_SETUP_INSTRUCTIONS.md)
4. **Add configuration** (.env, appsettings.Development.json)
5. **Implement merge logic** in C#
6. **Build and test**

## 💡 Why Blazor?

✅ **No CORS Issues** - Server proxies AI calls
✅ **Type Safety** - C# end-to-end
✅ **Modern UI** - Component-based architecture  
✅ **Secure** - API keys never exposed to client
✅ **Deployable** - Azure, AWS, Docker ready

## 📚 Documentation

- **Setup Guide**: [BLAZOR_SETUP_INSTRUCTIONS.md](../BLAZOR_SETUP_INSTRUCTIONS.md)
- **Original App**: `../web-app/` (reference for logic)
- **AI Provider Guide**: See original docs for API key setup

## 🎓 Learning Resources

- [Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [ASP.NET Core Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [JSInterop in Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/)

## 🐛 Troubleshooting

### Framework Mismatch Error
**Problem**: `cannot be added due to incompatible targeted frameworks`

**Solution**: Update `.csproj` files to use `net9.0` (see step 4 in BLAZOR_SETUP_INSTRUCTIONS.md)

### CORS Errors
**Problem**: Shouldn't happen! But if they do...

**Solution**: Check CORS policy in Program.cs matches client URL

### AI Not Working
**Problem**: API key not found

**Solution**: 
1. Create `.env` file in solution root
2. Add: `OPENAI_API_KEY=sk-proj-your-key`
3. Restart server

## 📞 Support

For issues specific to:
- **Blazor**: Check Microsoft docs
- **AI Integration**: Review OpenAiService.cs comments
- **Excel Parsing**: See excelInterop.js in setup guide

---

**Created**: November 5, 2025
**Status**: Scaffolded - Ready for implementation
**Next**: Follow BLAZOR_SETUP_INSTRUCTIONS.md
