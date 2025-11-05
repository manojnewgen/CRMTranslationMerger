# Sample Data for Testing CRM Translation Merger

## Files in This Directory

### ✅ crm-translations.xlsx (READY TO USE)
Excel file with the correct format for the **CRM Merge** page. Contains translations in English, Spanish, and French.

**Format:**
| LanguageCode | TranslationKey | Value |
|--------------|----------------|-------|
| en | messageReceived | [Sender name] has sent you a message |
| es | subject | ¡[Sender name] te acaba de escribir 💌! |

### crm-translations.csv
Source CSV file. If you need to recreate the Excel file, run:
```powershell
.\create-excel.ps1
```

### CRM_Email_Content_message.received__0__0__1
Real CRM JSON structure showing the nested dictionary format:
```json
{
  "contentDoc": {
    "content": {
      "localizedContents": {
        "de": {
          "messageReceived": "...",
          "subject": "..."
        }
      }
    }
  }
}
```

### sample-translations.json & sample-translations.xlsx
**DEPRECATED** - These use the old simple structure (List<LocalizedContent>).  
Use `crm-translations.xlsx` instead for the real CRM structure.

## Quick Start

1. **Start the server:**
   ```powershell
   cd CRMTranslationMerger.Server
   dotnet run
   ```

2. **Open in browser:**
   ```
   http://localhost:5159/crm-merge
   ```

3. **Upload files:**
   - **JSON file:** `CRM_Email_Content_message.received__0__0__1` (or your own CRM JSON)
   - **Excel file:** `crm-translations.xlsx`

4. **Click "Merge Translations"**

5. **Download merged JSON**

## Excel File Requirements

### ✅ Required Format
- **File Type:** Excel (.xlsx) - NOT CSV
- **Column Names (exact):** LanguageCode, TranslationKey, Value
- **LanguageCode:** Simple codes like `en`, `es`, `de`, `fr` (not `en-US`)
- **TranslationKey:** Key name in the JSON like `messageReceived`, `subject`, `replyNow`
- **Value:** Translation text with placeholders like `[Sender name]`

### Example Rows
```
LanguageCode    TranslationKey      Value
en              messageReceived     [Sender name] has sent you a message
en              subject             [Sender name] just wrote to you 💌
es              messageReceived     [Sender name] te ha enviado un mensaje
de              subject             [Sender name] hat Ihnen geschrieben
```

## AI Placeholder Conversion

The AI will automatically convert placeholders:
- `[Sender name]` → `{{String.Append LoadedData.SenderProfile.Handle ...}}`
- `[TimeAgo]` → `{{String.Append LoadedData.TimeAgo ...}}`

Make sure your OpenAI API key is configured in the `.env` file:
```
OPENAI_API_KEY=sk-...
OPENAI_MODEL=gpt-4o-mini
```
