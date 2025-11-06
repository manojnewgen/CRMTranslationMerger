/**
 * Azure Function: ConvertBatch
 * Converts text placeholders to Handlebars expressions using AI or pattern matching
 * 
 * Security: Supports App Settings (Free tier) and user-provided keys
 * Note: Key Vault support requires @azure/identity and @azure/keyvault-secrets packages (Standard tier)
 */

const https = require('https');
const http = require('http');

/**
 * Retrieves OpenAI API key from multiple sources (in priority order):
 * 1. User-provided key (via API request)
 * 2. Azure App Settings (OPENAI_API_KEY) - Works on Free tier
 */
async function getOpenAiKey(userProvidedKey, context) {
    // Priority 1: User-provided key (for testing or personal use)
    if (userProvidedKey && userProvidedKey.startsWith('sk-')) {
        context.log('Using user-provided API key');
        return userProvidedKey;
    }

    // Priority 2: App Settings (works on Free tier)
    const appSettingKey = process.env.OPENAI_API_KEY;
    if (appSettingKey && appSettingKey.startsWith('sk-')) {
        context.log('✅ Using API key from App Settings');
        return appSettingKey;
    }

    // No key found from any source
    throw new Error('No API key available. Please either:\n' +
                    '1. Provide API key via UI, OR\n' +
                    '2. Configure OPENAI_API_KEY in App Settings');
}

module.exports = async function (context, req) {
    context.log('ConvertBatch function triggered');

    // CORS headers
    context.res = {
        headers: {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*',
            'Access-Control-Allow-Methods': 'POST, OPTIONS',
            'Access-Control-Allow-Headers': 'Content-Type'
        }
    };

    // Handle OPTIONS preflight
    if (req.method === 'OPTIONS') {
        context.res.status = 200;
        return;
    }

    try {
        const { Texts, Mode = 'hybrid', ApiKey, Endpoint, Model, JsonContext, ExcelContext } = req.body;

        if (!Texts || typeof Texts !== 'object') {
            context.res.status = 400;
            context.res.body = { error: 'Texts object is required' };
            return;
        }

        const mode = Mode.toLowerCase();
        context.log(`Processing ${Object.keys(Texts).length} texts in ${mode} mode with ${JsonContext ? 'JSON' : 'no'} context`);

        // Get API credentials
        // Priority: 1) User-provided key (for testing), 2) Azure Key Vault (production)
        let apiKey = null;
        const apiEndpoint = Endpoint;
        const apiModel = Model;
        
        // Only fetch API key if AI mode is needed
        if (mode === 'ai' || mode === 'hybrid') {
            try {
                apiKey = await getOpenAiKey(ApiKey, context);
                context.log('✅ API key obtained successfully');
            } catch (error) {
                context.res.status = 401;
                context.res.body = { error: error.message };
                return;
            }
        }

        // Pattern matching mappings (support both <> and [] formats)
        const placeholderMappings = {
            '[Sender name]': 'LoadedData.SenderProfile.Handle',
            '<sender name>': 'LoadedData.SenderProfile.Handle',
            '[Sender age]': 'LoadedData.SenderProfile.Age',
            '<sender age>': 'LoadedData.SenderProfile.Age',
            '[Sender height]': 'LoadedData.SenderProfile.Height',
            '<sender height>': 'LoadedData.SenderProfile.Height',
            '[Sender smoker status]': 'LoadedData.SenderProfile.SmokerStatus',
            '<sender smoker status>': 'LoadedData.SenderProfile.SmokerStatus',
            '[TimeAgo]': 'LoadedData.TimeAgo',
            '[Time]': 'LoadedData.Time',
            '[Recipient name]': 'LoadedData.RecipientProfile.Handle',
            '<recipient name>': 'LoadedData.RecipientProfile.Handle',
            '[Recipient age]': 'LoadedData.RecipientProfile.Age',
            '<recipient age>': 'LoadedData.RecipientProfile.Age'
        };

        const results = {};

        for (const [key, text] of Object.entries(Texts)) {
            try {
                let converted = text;

                // Determine if we should use AI
                const shouldUseAi = mode === 'ai' || 
                    (mode === 'hybrid' && isComplexText(text));

                if (shouldUseAi && apiKey) {
                    context.log(`🤖 Using AI for complex text: ${text.substring(0, 50)}...`);
                    converted = await convertWithAi(text, apiKey, apiEndpoint, apiModel, context, JsonContext, ExcelContext);
                } else {
                    context.log(`📐 Using pattern matching for: ${text.substring(0, 50)}...`);
                    converted = convertWithPatterns(text, placeholderMappings, context);
                }

                results[key] = converted;
            } catch (error) {
                context.log.error(`Error converting ${key}: ${error.message}`);
                results[key] = text; // Return original on error
            }
        }

        context.res.status = 200;
        context.res.body = results;

    } catch (error) {
        context.log.error('Function error:', error);
        context.res.status = 500;
        context.res.body = { error: error.message };
    }
};

/**
 * Determines if text is complex enough to require AI
 */
function isComplexText(text) {
    // Already contains Handlebars - needs AI to preserve/enhance
    const hasHandlebars = text.includes('{{') && text.includes('}}');
    
    const placeholderCount = (text.match(/(\[([^\]]+)\]|<([^>]+)>)/g) || []).length;
    const hasConditionals = /\b(if|else|unless)\b/i.test(text);
    const hasLoops = /\b(each|for|loop)\b/i.test(text);
    const hasMultipleSentences = text.split(/[.!?]/).length > 2;
    const hasNestedBrackets = text.includes('[[') || /\[([^\]]*\[)/.test(text);
    const hasHelperFunctions = /String\.(Concat|Equal|Append)|Object\.ToString/.test(text);
    const hasGenderConditions = /(him|her|his|hers)\/(him|her|his|hers)/.test(text);
    
    return hasHandlebars ||
           hasHelperFunctions ||
           hasGenderConditions ||
           (placeholderCount > 2 && text.length > 50) || 
           hasConditionals || 
           hasLoops || 
           hasMultipleSentences ||
           hasNestedBrackets;
}

/**
 * Pattern-based conversion (fast, free, deterministic)
 */
function convertWithPatterns(text, mappings, context) {
    if (!text) return text;

    // If text already contains Handlebars expressions, return as-is
    if (text.includes('{{') && text.includes('}}')) {
        context.log('Text already contains Handlebars, preserving as-is');
        return text;
    }

    // Auto-repair malformed placeholders
    text = repairMalformedPlaceholders(text, mappings, context);

    // Match both [Placeholder] and <placeholder> formats
    const placeholderPattern = /(\[([^\]]+)\]|<([^>]+)>)/g;
    const matches = [...text.matchAll(placeholderPattern)];

    if (matches.length === 0) return text;

    // Single placeholder alone
    if (matches.length === 1 && matches[0][0] === text) {
        const placeholder = matches[0][0];
        const handlebarsVar = mappings[placeholder];
        if (handlebarsVar) {
            return `{{${handlebarsVar}}}`;
        }
        context.log.warn(`Unknown placeholder: ${placeholder}`);
        return text;
    }

    // Multiple parts - use String.Concat
    const parts = [];
    let lastIndex = 0;

    for (const match of matches) {
        const placeholder = match[0];
        const placeholderIndex = match.index;

        // Add text before placeholder
        if (placeholderIndex > lastIndex) {
            const beforeText = text.substring(lastIndex, placeholderIndex);
            parts.push(`"${escapeQuotes(beforeText)}"`);
        }

        // Add placeholder variable
        const handlebarsVar = mappings[placeholder];
        if (handlebarsVar) {
            parts.push(handlebarsVar);
        } else {
            parts.push(`"${escapeQuotes(placeholder)}"`);
        }

        lastIndex = placeholderIndex + placeholder.length;
    }

    // Add remaining text
    if (lastIndex < text.length) {
        const afterText = text.substring(lastIndex);
        parts.push(`"${escapeQuotes(afterText)}"`);
    }

    if (parts.length === 0) return text;
    if (parts.length === 1) {
        return parts[0].startsWith('"') ? text : `{{${parts[0]}}}`;
    }

    const concatenated = parts.join(' ');
    return `{{String.Concat ${concatenated}}}`;
}

/**
 * Auto-repair malformed placeholders
 */
function repairMalformedPlaceholders(text, mappings, context) {
    let original = text;

    // Fix missing closing brackets
    for (const placeholder of Object.keys(mappings)) {
        const placeholderName = placeholder.replace(/[\[\]]/g, '');
        const pattern = new RegExp(`\\[${placeholderName}(\\s+[a-zà-ÿ])`, 'gi');
        
        if (pattern.test(text)) {
            text = text.replace(pattern, `[${placeholderName}]$1`);
            context.log(`🔧 Repaired: Added ']' after [${placeholderName}]`);
        }
    }

    // Fix double brackets
    if (text.includes('[[')) {
        text = text.replace(/\[\[/g, '[').replace(/\]\]/g, ']');
        context.log('🔧 Repaired: Fixed double brackets');
    }

    if (text !== original) {
        context.log(`📝 Repaired: ${original} → ${text}`);
    }

    return text;
}

/**
 * AI-powered conversion using OpenAI or Azure OpenAI
 */
async function convertWithAi(text, apiKey, endpoint, model, context, jsonContext, excelContext) {
    const apiEndpoint = endpoint || 'https://api.openai.com/v1/chat/completions';
    const modelName = model || 'gpt-4o-mini';

    const systemPrompt = buildSystemPrompt(jsonContext, excelContext);
    const userPrompt = buildUserPrompt(text);

    const requestBody = JSON.stringify({
        model: modelName,
        messages: [
            { role: 'system', content: systemPrompt },
            { role: 'user', content: userPrompt }
        ],
        temperature: 0.1,
        max_tokens: 1000
    });

    return new Promise((resolve, reject) => {
        const url = new URL(apiEndpoint);
        const protocol = url.protocol === 'https:' ? https : http;

        const options = {
            hostname: url.hostname,
            port: url.port || (url.protocol === 'https:' ? 443 : 80),
            path: url.pathname + url.search,
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${apiKey}`,
                'Content-Length': Buffer.byteLength(requestBody)
            }
        };

        const req = protocol.request(options, (res) => {
            let data = '';

            res.on('data', (chunk) => {
                data += chunk;
            });

            res.on('end', () => {
                try {
                    if (res.statusCode !== 200) {
                        context.log.error(`AI API Error: ${res.statusCode} - ${data}`);
                        reject(new Error(`AI API Error: ${res.statusCode}`));
                        return;
                    }

                    const response = JSON.parse(data);
                    const convertedText = response.choices[0]?.message?.content?.trim();

                    if (!convertedText) {
                        reject(new Error('Empty response from AI'));
                        return;
                    }

                    if (isValidHandlebarsOutput(convertedText)) {
                        context.log(`✅ AI conversion successful`);
                        resolve(convertedText);
                    } else {
                        context.log.warn('⚠️ AI produced invalid Handlebars');
                        reject(new Error('Invalid Handlebars syntax from AI'));
                    }
                } catch (error) {
                    reject(error);
                }
            });
        });

        req.on('error', (error) => {
            context.log.error('AI request error:', error);
            reject(error);
        });

        req.write(requestBody);
        req.end();
    });
}

/**
 * Builds comprehensive system prompt for AI with JSON context
 */
function buildSystemPrompt(jsonContext, excelContext) {
    let contextAnalysis = '';
    
    if (jsonContext) {
        try {
            const json = JSON.parse(jsonContext);
            const sample = JSON.stringify(json, null, 2).substring(0, 1000); // First 1000 chars for context
            contextAnalysis = `\n\nEXISTING JSON STRUCTURE (learn from these patterns):
${sample}
...

INSTRUCTIONS:
- Study the existing JSON to understand naming patterns
- See how placeholders are already converted (e.g., {{LoadedData.SenderProfile.Handle}})
- Match the style and structure of existing conversions
- Ensure consistency with existing keys and values
`;
        } catch (e) {
            // Ignore parse errors
        }
    }
    
    return `You are an expert Handlebars template converter for a CRM email system. Your task is to convert text with placeholders AND pseudocode conditionals into valid Handlebars syntax.

AVAILABLE VARIABLES:
- LoadedData.SenderProfile.Handle (sender's name/username)
- LoadedData.SenderProfile.Age (sender's age)
- LoadedData.SenderProfile.SiteCode (sender's site code, e.g., "43")
- LoadedData.SenderProfile.PayingStatus (sender's payment status, e.g., "NOPAY", "PAID")
- LoadedData.SenderProfile.Height (sender's height)
- LoadedData.SenderProfile.SmokerStatus (sender's smoking status)
- LoadedData.RecipientProfile.Handle (recipient's name/username)
- LoadedData.RecipientProfile.Age (recipient's age)
- LoadedData.TimeAgo (relative time, e.g., '2 hours ago')
- LoadedData.Time (absolute time)
- localVars.gender (gender variable for conditionals)

AVAILABLE HELPER FUNCTIONS:
- String.Concat - Concatenate strings and variables
- String.Equal - Compare two strings
- Object.ToString - Convert object to string

CONVERSION RULES:

1. PSEUDOCODE CONDITIONALS - Convert to Handlebars:
   Input: IF sender site code == 43 ... ELSE ...
   Output: {{#if (String.Equal (Object.ToString LoadedData.SenderProfile.SiteCode) "43")}} ... {{else}} ... {{/if}}
   
   Input: AND paying Status == 'NOPAY' ...
   Output: {{#if (String.Equal LoadedData.SenderProfile.PayingStatus "NOPAY")}} ... {{/if}}
   
   Example full conversion:
   Input:
   ```
   IF sender site code == 43
   {
   AND paying Status == 'NOPAY' Someone likes you
   ELSE <sender name> likes you
   }
   ELSE 💗 <sender name> is interested. Is it mutual?
   ```
   
   Output:
   ```
   {{#if (String.Equal (Object.ToString LoadedData.SenderProfile.SiteCode) "43")}}
   {{#if (String.Equal LoadedData.SenderProfile.PayingStatus "NOPAY")}}Someone likes you{{else}}{{LoadedData.SenderProfile.Handle}} likes you{{/if}}
   {{else}}💗 {{LoadedData.SenderProfile.Handle}} is interested. Is it mutual?{{/if}}
   ```

2. Single placeholder alone:
   {{variable}}
   Example: <sender name> → {{LoadedData.SenderProfile.Handle}}

3. Multiple parts (text + placeholders):
   {{String.Concat "text" variable "text"}}
   Example: Hi <sender name>, welcome! → {{String.Concat "Hi " LoadedData.SenderProfile.Handle ", welcome!"}}

4. Preserve existing Handlebars:
   If input already contains valid Handlebars expressions, keep them exactly as-is

CRITICAL RULES:
- ✅ Convert IF/ELSE/AND pseudocode to proper Handlebars {{#if}} blocks
- ✅ Convert placeholders: <sender name> → {{LoadedData.SenderProfile.Handle}}
- ✅ Convert placeholders: <sender age> → {{LoadedData.SenderProfile.Age}}
- ✅ Convert placeholders: <sender height> → {{LoadedData.SenderProfile.Height}}
- ✅ Convert placeholders: <sender smoker status> → {{LoadedData.SenderProfile.SmokerStatus}}
- ✅ Keep the exact same text, just swap [Placeholder] → {{Variable}}
- Preserve exact spacing and punctuation from original text
- Escape quotes inside strings with \\"
- Keep exact capitalization of helper functions (String.Concat, String.Equal, Object.ToString)
- Maintain proper nesting depth for conditionals
- If text already has valid Handlebars syntax, DO NOT change it

${contextAnalysis}

OUTPUT REQUIREMENTS:
- Return ONLY the converted Handlebars expression
- No explanations, no markdown, no extra text
- Must be valid Handlebars syntax
- DO NOT add logic that wasn't in the input text
- Preserve any existing Handlebars expressions exactly`;
}

/**
 * Builds user prompt
 */
function buildUserPrompt(text) {
    return `Convert this text to Handlebars syntax by ONLY replacing placeholders.

Input text: "${text}"

Known placeholders to convert:
- <sender name> OR [Sender name] → LoadedData.SenderProfile.Handle
- <sender age> OR [Sender age] → LoadedData.SenderProfile.Age  
- <sender height> OR [Sender height] → LoadedData.SenderProfile.Height
- <sender smoker status> OR [Sender smoker status] → LoadedData.SenderProfile.SmokerStatus
- [Recipient name] → LoadedData.RecipientProfile.Handle
- [Recipient age] → LoadedData.RecipientProfile.Age
- [TimeAgo] → LoadedData.TimeAgo
- [Time] → LoadedData.Time

CRITICAL: 
- DO NOT add any if/else logic or conditionals
- DO NOT change the text content
- ONLY replace the placeholders with their Handlebars variables
- Keep everything else exactly as-is

Return ONLY the converted Handlebars expression.`;
}

/**
 * Validates Handlebars output
 */
function isValidHandlebarsOutput(output) {
    if (!output || typeof output !== 'string') return false;
    if (!output.startsWith('{{') || !output.endsWith('}}')) return false;
    
    const openCount = (output.match(/\{\{/g) || []).length;
    const closeCount = (output.match(/\}\}/g) || []).length;
    if (openCount !== closeCount) return false;
    
    if (output.includes('{{{{{') || output.includes('}}}}}')) return false;
    
    if (output.includes('String.Concat') && !/String\.Concat\s+/.test(output)) {
        return false;
    }
    
    return true;
}

/**
 * Escapes quotes in text
 */
function escapeQuotes(text) {
    return text.replace(/"/g, '\\"');
}

/**
 * Validates API key format for security
 */
function isValidApiKeyFormat(apiKey) {
    if (!apiKey || typeof apiKey !== 'string') return false;
    
    // OpenAI API keys start with 'sk-'
    // Azure OpenAI keys are alphanumeric, 32+ chars
    const isOpenAI = apiKey.startsWith('sk-') && apiKey.length > 20;
    const isAzure = /^[a-zA-Z0-9]{32,}$/.test(apiKey);
    
    return isOpenAI || isAzure;
}
