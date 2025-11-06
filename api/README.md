# Azure Functions API for CRM Translation Merger

This folder contains Azure Functions that power the backend API for the CRM Translation Merger tool.

## 📁 Structure

```
api/
├── ConvertBatch/          # Batch conversion function
│   ├── index.js          # Function logic
│   └── function.json     # Function configuration
├── host.json             # Functions runtime configuration
├── package.json          # Node.js dependencies
└── .funcignore           # Files to exclude from deployment
```

## 🚀 Deployment

This API is automatically deployed with the Static Web App via GitHub Actions. No separate deployment needed!

## 🔌 Endpoints

### POST `/api/convert-batch`

Converts text placeholders to Handlebars expressions using AI or pattern matching.

**Request Body:**
```json
{
  "Texts": {
    "en|subject": "Hi [Sender name], welcome!",
    "en|body": "You are [Sender age] years old"
  },
  "Mode": "hybrid",
  "ApiKey": "sk-...",
  "Endpoint": "https://api.openai.com/v1/chat/completions",
  "Model": "gpt-4o-mini"
}
```

**Modes:**
- `pattern` - Fast, free pattern matching (simple placeholders only)
- `ai` - AI-powered conversion (handles complex scenarios)
- `hybrid` - Auto-detect complexity and use best approach (recommended)

**Response:**
```json
{
  "en|subject": "{{String.Concat \"Hi \" LoadedData.SenderProfile.Handle \", welcome!\"}}",
  "en|body": "{{String.Concat \"You are \" LoadedData.SenderProfile.Age \" years old\"}}"
}
```

## 🧪 Local Testing

To test locally, install Azure Functions Core Tools:

```bash
npm install -g azure-functions-core-tools@4

# Start the function
cd api
func start
```

The API will be available at `http://localhost:7071/api/convert-batch`

## 🔒 Security

- API keys are never stored, only used in-memory for the request
- All requests are logged (without sensitive data)
- CORS is configured to allow requests from the static app

## 📝 Features

### Pattern Matching (Free & Fast)
- Simple placeholder replacement
- Auto-repair malformed placeholders
- Deterministic output

### AI-Powered (Intelligent)
- Handles conditionals (if/else)
- Supports loops (each/for)
- Complex nested expressions
- Context-aware conversion

### Smart Hybrid (Recommended)
- Auto-detects text complexity
- Uses pattern matching for simple cases
- Falls back to AI for complex scenarios
- Best balance of speed, cost, and accuracy
