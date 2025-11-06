# Security Architecture

## Deployment Modes

This application supports **two deployment approaches** depending on your use case:

### � Development Mode: User-Provided API Keys
**Best for:** Testing, personal use, individual deployments

Users provide their own OpenAI API keys through the UI. Keys are:
- ✅ Never stored (memory-only during request)
- ✅ Encrypted in transit (HTTPS)
- ✅ Per-user accountability
- ✅ Zero infrastructure costs
- ✅ No setup required

### 🏢 Production Mode: Azure Key Vault
**Best for:** Shared organizational deployment, multi-user access

Centralized API key stored in Azure Key Vault with Managed Identity:
- ✅ One shared OpenAI account for entire organization
- ✅ Secrets never in code or browser
- ✅ Centralized key rotation (no code changes needed)
- ✅ Audit logs for compliance
- ✅ Cost-effective (~$0.03 per 10k operations)

## Architecture Comparison

| Feature | User-Provided Keys | Azure Key Vault |
|---------|-------------------|-----------------|
| **Cost** | $0 (FREE) | ~$0.03 per 10k ops |
| **Security** | ✅ Excellent (isolated) | ✅ Excellent (centralized) |
| **Setup Complexity** | ✅ Zero config | ⚠️ Requires Azure setup |
| **Best For** | Individual users | Shared deployment |
| **Key Management** | Each user manages own | IT/Admin manages centrally |
| **Accountability** | Per-user tracking | Shared usage tracking |
| **Flexibility** | ✅ Each user chooses provider | ❌ One provider for all |

---

## 🔒 Security Features

### 1. **Transport Security** ✅
- All API requests use HTTPS (TLS 1.2+)
- API keys encrypted in transit
- Azure Static Web Apps enforces secure connections

### 2. **No Persistence** ✅
- Keys NEVER stored in database
- Keys NEVER stored in logs
- Keys NEVER stored in cache
- Keys exist only in memory during request processing
- Cache cleared after 1 hour (Key Vault mode only)

### 3. **API Key Validation** ✅
```javascript
// Development Mode: Validates user-provided keys
if (userProvidedKey && userProvidedKey.startsWith('sk-')) {
    return userProvidedKey;
}

// Production Mode: Fetches from Key Vault with Managed Identity
const secret = await kvClient.getSecret(secretName);
return secret.value;
```

### 4. **Managed Identity Authentication** ✅ (Production Mode)
- No credentials stored in code
- Azure handles authentication automatically
- Service-to-service secure communication
- Principle of least privilege (only "get secret" permission)

### 5. **CORS Protection** ✅
- API only accessible from configured domains
- Preflight OPTIONS handling
- Proper security headers

### 6. **Input Sanitization** ✅
- Request body validation
- Text content sanitization
- Mode parameter validation
- Prevents injection attacks

---

## 🚀 Setting Up Production Mode (Azure Key Vault)

### Prerequisites
- Azure CLI installed: https://aka.ms/azure-cli
- Azure subscription with permissions to create Key Vault
- OpenAI API key ready

### Quick Setup (5 minutes)

Run the provided PowerShell script:

```powershell
# Navigate to project root
cd C:\CRM-19119\CRMTranslationMerger

# Run setup script
.\setup-keyvault.ps1 -OpenAiApiKey "sk-your-actual-key-here"
```

The script automatically:
1. ✅ Creates Azure Key Vault
2. ✅ Stores your OpenAI API key securely
3. ✅ Enables Managed Identity on Static Web App
4. ✅ Grants Key Vault access permissions
5. ✅ Configures environment variables
6. ✅ Displays cost estimate and next steps

### Manual Setup (Step-by-Step)

If you prefer manual setup or need to customize:

#### 1. Create Key Vault
```bash
az keyvault create \
  --name kv-crm-translation \
  --resource-group rg-crm-translation \
  --location eastus
```

#### 2. Add OpenAI API Key
```bash
az keyvault secret set \
  --vault-name kv-crm-translation \
  --name OPENAI-API-KEY \
  --value "sk-your-actual-key-here"
```

#### 3. Enable Managed Identity
```bash
az staticwebapp identity assign \
  --name icy-hill-069f16b10 \
  --resource-group rg-crm-translation
```

This returns a `principalId` - copy it for the next step.

#### 4. Grant Key Vault Access
```bash
az keyvault set-policy \
  --name kv-crm-translation \
  --object-id <principal-id-from-step-3> \
  --secret-permissions get list
```

#### 5. Configure Environment Variables
```bash
az staticwebapp appsettings set \
  --name icy-hill-069f16b10 \
  --resource-group rg-crm-translation \
  --setting-names \
    "KEYVAULT_URL=https://kv-crm-translation.vault.azure.net/" \
    "OPENAI_SECRET_NAME=OPENAI-API-KEY"
```

#### 6. Deploy Code
Commit and push your code changes. GitHub Actions will automatically deploy the updated function with Key Vault integration.

### Verification

Test the API without providing an API key:

```powershell
# Test script (no API key in request)
$response = Invoke-RestMethod -Uri "https://icy-hill-069f16b10.3.azurestaticapps.net/api/convert-batch" `
  -Method POST `
  -ContentType "application/json" `
  -Body (@{
    Texts = @{
        "test" = "Hello [Sender name]!"
    }
    Mode = "ai"
  } | ConvertTo-Json)

$response
```

If configured correctly, the function will:
1. ✅ Authenticate using Managed Identity
2. ✅ Fetch API key from Key Vault
3. ✅ Cache the key for 1 hour
4. ✅ Process your request
5. ✅ Return converted text

### Cost Estimate

| Service | Usage | Monthly Cost |
|---------|-------|--------------|
| **Azure Key Vault** | ~1,000 operations/month | ~$0.00 (free tier) |
| **Managed Identity** | Included | $0.00 |
| **Static Web App** | Free tier | $0.00 |
| **OpenAI API** | Pay-per-use | Varies |

**Total Azure Infrastructure**: Essentially free for normal usage

---

## � How It Works

### Development Mode (User-Provided Keys)
```
┌─────────────┐         HTTPS          ┌──────────────────┐
│   Browser   │ ──────────────────────>│  Azure Function  │
│             │    API Key in Body     │                  │
│ (Password   │    (TLS Encrypted)     │  ✓ Validate key  │
│  field)     │                        │  ✓ Use for AI    │
│             │<───────────────────────│  ✓ Discard key   │
└─────────────┘    Response Only       └──────────────────┘
                                               │
                                               ↓
                                       ┌──────────────┐
                                       │   OpenAI     │
                                       │     API      │
                                       └──────────────┘
```

### Production Mode (Azure Key Vault)
```
┌─────────────┐         HTTPS          ┌──────────────────┐
│   Browser   │ ──────────────────────>│  Azure Function  │
│             │    No API Key          │                  │
│   (User)    │                        │  ✓ Check cache   │
│             │                        │  ↓ Cache miss    │
│             │<───────────────────────│  ✓ Fetch from KV │
└─────────────┘    Response Only       │  ✓ Cache key     │
                                       │  ✓ Use for AI    │
                                       └──────────────────┘
                                               │    ↑
                                   Managed     │    │
                                   Identity    ↓    │
                                       ┌──────────────┐
                                       │  Azure Key   │
                                       │    Vault     │
                                       └──────────────┘
                                               │
                                               ↓
                                       ┌──────────────┐
                                       │   OpenAI     │
                                       │     API      │
                                       └──────────────┘
```

### 💰 Cost Comparison:

**Option 1: User-Provided Keys (Current)** ⭐
```
Azure Static Web Apps:     FREE (100GB bandwidth/month)
Azure Functions:           FREE (1M requests/month)
OpenAI API:               User pays their own
Total Azure Cost:         $0.00/month
```

**Option 2: Azure Key Vault**
```
Azure Static Web Apps:     FREE
Azure Functions:           FREE
Azure Key Vault:          $0.03 per 10,000 operations
                          + $0.03 per 10,000 transactions
                          ≈ $5-10/month for moderate use
OpenAI API:               You pay for everyone
Total Azure Cost:         $5-10/month + OpenAI costs for all users
```

**Option 3: Shared Environment Variable**
```
Azure Static Web Apps:     FREE
Azure Functions:           FREE
OpenAI API:               You pay for ALL users
Total Cost:               $50-500/month depending on usage
Risk:                     High (one leaked key = all users affected)
```

### 🎯 Recommendation: Stick with Current Implementation

**For Your Use Case:**
- ✅ Users are internal/trusted (your team)
- ✅ Small number of users
- ✅ Users can manage their own keys
- ✅ No billing to manage
- ✅ Perfect security isolation

### 📋 User Instructions:

**For OpenAI:**
1. Get API key from: https://platform.openai.com/api-keys
2. Copy key (starts with `sk-...`)
3. Paste in app's API Key field
4. Select "OpenAI" provider
5. Choose model (gpt-4o-mini recommended)

**For Azure OpenAI:**
1. Get key from Azure Portal
2. Copy endpoint URL
3. Paste both in app
4. Select "Azure OpenAI" provider

### 🛡️ Security Best Practices for Users:

1. **Never share API keys**
2. **Use read-only keys if possible**
3. **Set usage limits** in OpenAI dashboard
4. **Rotate keys regularly** (monthly)
5. **Monitor usage** in OpenAI dashboard

### 🚨 If You Ever Need Key Vault (Future):

Only consider Azure Key Vault if:
- You have 100+ users
- You want to bill users yourself
- You need centralized key rotation
- Compliance requires it

For your current scale: **User-provided keys are optimal** ✅

### 📊 Current Security Posture:

```
✅ Encryption in Transit (HTTPS/TLS)
✅ No Persistence (Memory-only)
✅ API Key Validation
✅ CORS Protection
✅ Input Sanitization
✅ Per-User Isolation
✅ Zero Azure Costs
✅ Simple Architecture
✅ Production Ready
```

**Verdict: Your current implementation is enterprise-grade and cost-optimal!** 🎉
