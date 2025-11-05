# CRM Translation Merger - Blazor WebAssembly

🚀 A complete Blazor WebAssembly application with server-side AI proxy for merging CRM email translations from Excel to JSON.

## ✅ What's Been Created

### Project Structure
```
CRMTranslationMerger/
├── CRMTranslationMerger.sln
├── CRMTranslationMerger.Client/          (Blazor WASM)
├── CRMTranslationMerger.Server/          (ASP.NET Core API)
└── CRMTranslationMerger.Shared/          (Shared Models)
    └── Models/
        ├── AiRequest.cs                  ✅ Created
        ├── AiResponse.cs                 ✅ Created
        ├── LocalizedContent.cs           ✅ Created
        └── MergeResult.cs                ✅ Created
```

### Server Files Created
- ✅ `Services/OpenAiService.cs` - Complete AI proxy service

## 🎯 Quick Start

### 1. Open in VS Code
```powershell
code c:\CRM-19119\CRMTranslationMerger\CRMTranslationMerger.sln
```

### 2. Follow the detailed setup in:
📄 **[BLAZOR_SETUP_INSTRUCTIONS.md](../BLAZOR_SETUP_INSTRUCTIONS.md)**

This file contains:
- Target framework fixes (net7.0 → net9.0)
- Complete Program.cs for Server
- All Client files (Merge.razor, index.html, app.css, excelInterop.js)
- Configuration files
- Build and run commands

## 🔧 Key Features

### ✅ Solved CORS Issue
- Server-side AI proxy eliminates browser CORS restrictions
- API keys stored securely on server, never exposed to client

### ✅ Complete Architecture
- **Client**: Blazor WebAssembly for UI
- **Server**: ASP.NET Core API with AI proxy endpoint
- **Shared**: DTOs for type-safe communication

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
