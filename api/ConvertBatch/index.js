/**
 * Azure Function: ConvertBatch
 * Converts text placeholders to Handlebars expressions using AI or pattern matching
 */

const https = require('https');
const http = require('http');

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
        const { Texts, Mode = 'hybrid', ApiKey, Endpoint, Model } = req.body;

        if (!Texts || typeof Texts !== 'object') {
            context.res.status = 400;
            context.res.body = { error: 'Texts object is required' };
            return;
        }

        const mode = Mode.toLowerCase();
        context.log(`Processing ${Object.keys(Texts).length} texts in ${mode} mode`);

        // Pattern matching mappings
        const placeholderMappings = {
            '[Sender name]': 'LoadedData.SenderProfile.Handle',
            '[Sender age]': 'LoadedData.SenderProfile.Age',
            '[TimeAgo]': 'LoadedData.TimeAgo',
            '[Time]': 'LoadedData.Time',
            '[Recipient name]': 'LoadedData.RecipientProfile.Handle',
            '[Recipient age]': 'LoadedData.RecipientProfile.Age'
        };

        const results = {};

        for (const [key, text] of Object.entries(Texts)) {
            try {
                let converted = text;

                // Determine if we should use AI
                const shouldUseAi = mode === 'ai' || 
                    (mode === 'hybrid' && isComplexText(text));

                if (shouldUseAi && ApiKey) {
                    context.log(`🤖 Using AI for complex text: ${text.substring(0, 50)}...`);
                    converted = await convertWithAi(text, ApiKey, Endpoint, Model, context);
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
    
    const placeholderCount = (text.match(/\[([^\]]+)\]/g) || []).length;
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

    const placeholderPattern = /\[([^\]]+)\]/g;
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
async function convertWithAi(text, apiKey, endpoint, model, context) {
    const apiEndpoint = endpoint || 'https://api.openai.com/v1/chat/completions';
    const modelName = model || 'gpt-4o-mini';

    const systemPrompt = buildSystemPrompt();
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
 * Builds comprehensive system prompt for AI
 */
function buildSystemPrompt() {
    return `You are an expert Handlebars template converter for a CRM email system. Your task is to convert text with placeholders into valid Handlebars syntax.

AVAILABLE VARIABLES:
- LoadedData.SenderProfile.Handle (sender's name/username)
- LoadedData.SenderProfile.Age (sender's age)
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

1. Single placeholder alone:
   {{variable}}
   Example: [Sender name] → {{LoadedData.SenderProfile.Handle}}

2. Multiple parts (text + placeholders):
   {{String.Concat "text" variable "text"}}
   Example: Hi [Sender name], welcome! → {{String.Concat "Hi " LoadedData.SenderProfile.Handle ", welcome!"}}

3. Conditionals with gender or other conditions:
   {{#if (String.Equal (Object.ToString localVars.gender) "Female")}} her {{else}} him {{/if}}
   Example: Message [Sender name]. Talk to [Gender:her/him] → 
   {{String.Concat "Message " LoadedData.SenderProfile.Handle ". Talk to " (if (String.Equal (Object.ToString localVars.gender) "Female") "her" "him")}}

4. Complex conditionals in text:
   When text has gender-specific words like "him/her", "his/hers", convert to:
   {{#if (String.Equal (Object.ToString localVars.gender) "Female")}} her {{else}} him {{/if}}

5. Nested conditionals:
   Preserve exact Handlebars conditional structure with proper spacing

6. Email addresses with variables:
   [Sender name]@domain.com → {{LoadedData.SenderProfile.Handle}}@domain.com

7. Preserve existing Handlebars:
   If input already contains valid Handlebars expressions, keep them exactly as-is

IMPORTANT RULES:
- Preserve exact spacing and punctuation from original text
- Escape quotes inside strings with \\"
- Keep exact capitalization of helper functions (String.Concat, String.Equal, Object.ToString)
- Maintain proper nesting depth for conditionals
- If text already has valid Handlebars syntax, DO NOT change it

OUTPUT REQUIREMENTS:
- Return ONLY the converted Handlebars expression
- No explanations, no markdown, no extra text
- Must be valid Handlebars syntax
- Preserve any existing Handlebars expressions exactly`;
}

/**
 * Builds user prompt
 */
function buildUserPrompt(text) {
    return `Convert this text to Handlebars syntax:

Input text: "${text}"

Known placeholders: [Sender name], [Sender age], [Recipient name], [Recipient age], [TimeAgo], [Time]

Convert and return ONLY the Handlebars expression, nothing else.`;
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
