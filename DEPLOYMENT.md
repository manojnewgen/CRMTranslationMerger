# Azure Static Web Apps Deployment Guide

## Prerequisites
- Azure account (free tier available): https://azure.microsoft.com/free/
- GitHub repository with the code

## Step-by-Step Deployment

### 1. Create Azure Static Web App

1. **Go to Azure Portal**: https://portal.azure.com
2. **Click "Create a resource"** → Search for **"Static Web App"**
3. **Click "Create"**

### 2. Configure Basic Settings

| Field | Value |
|-------|-------|
| **Subscription** | Your Azure subscription |
| **Resource Group** | Create new: `crm-translation-rg` |
| **Name** | `crm-translation-merger` |
| **Plan type** | **Free** (perfect for this app) |
| **Region** | Choose closest to you (e.g., East US, West Europe) |

### 3. Configure Deployment

| Field | Value |
|-------|-------|
| **Source** | GitHub |
| **GitHub Account** | Sign in and authorize |
| **Organization** | `manojnewgen` |
| **Repository** | `CRMTranslationMerger` |
| **Branch** | `main` |

### 4. Build Configuration

| Field | Value |
|-------|-------|
| **Build Presets** | Custom |
| **App location** | `/CRMTranslationMerger.Client` |
| **Api location** | `/CRMTranslationMerger.Server` |
| **Output location** | `wwwroot` |

### 5. Review + Create

1. Click **"Review + create"**
2. Click **"Create"**
3. Wait for deployment (2-3 minutes)

Azure will:
- ✅ Create the Static Web App
- ✅ Add a GitHub secret (`AZURE_STATIC_WEB_APPS_API_TOKEN`) to your repo
- ✅ Trigger the first deployment automatically
- ✅ Give you a URL like: `https://crm-translation-merger-xxxxx.azurestaticapps.net`

## 6. Configure Environment Variables

After deployment, add your environment variables:

1. Go to your Static Web App in Azure Portal
2. Click **"Configuration"** in the left menu
3. Click **"+ Add"** and add these:

| Name | Value | Note |
|------|-------|------|
| `OPENAI_API_KEY` | `sk-...` | Your OpenAI API key |
| `OPENAI_ENDPOINT` | `https://api.openai.com/v1/chat/completions` | Or Azure OpenAI endpoint |
| `OPENAI_MODEL` | `gpt-4o-mini` | Model name |

4. Click **"Save"**

**Note**: The app uses pattern matching (no AI calls), so these are optional. But keep them if you want to experiment with AI in the future.

## 7. Access Your App

Your app will be available at:
```
https://crm-translation-merger-xxxxx.azurestaticapps.net/crm-merge
```

(Replace `xxxxx` with your unique identifier)

## 8. Custom Domain (Optional)

Want a custom domain like `crm.yourdomain.com`?

1. Go to **"Custom domains"** in Azure Portal
2. Click **"+ Add"**
3. Follow the instructions to add your domain

## Auto-Deployment

Every time you push to GitHub `main` branch:
- ✅ GitHub Actions automatically builds your app
- ✅ Deploys to Azure Static Web Apps
- ✅ Updates live in ~2-3 minutes

Check deployment status:
- GitHub: Go to your repo → **Actions** tab
- Azure: Go to your Static Web App → **Deployment History**

## Costs

**FREE Tier includes:**
- ✅ 100 GB bandwidth per month
- ✅ Custom domains
- ✅ Free SSL certificates
- ✅ Global CDN
- ✅ Staging environments
- ✅ 2 custom domains

This is MORE than enough for a small internal app!

## Troubleshooting

### Build fails?
1. Check GitHub Actions logs
2. Verify paths in `.github/workflows/azure-static-web-apps.yml`
3. Ensure .NET 9.0 is specified

### API not working?
1. Check environment variables are set
2. Verify API location is `/CRMTranslationMerger.Server`
3. Check Azure Static Web Apps logs

### Need help?
- Azure Status: https://status.azure.com
- Azure Support: https://portal.azure.com → Support

## Local Testing

Test the production build locally:
```powershell
cd CRMTranslationMerger.Server
dotnet run -c Release
```

Then open: http://localhost:5159/crm-merge

## Monitoring

View logs and metrics in Azure Portal:
1. Go to your Static Web App
2. Click **"Log stream"** (real-time logs)
3. Click **"Metrics"** (usage statistics)

---

🎉 **That's it! Your app is now live on Azure!**
