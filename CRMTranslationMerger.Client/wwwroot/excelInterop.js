// excelInterop.js - Client-side Excel parsing using SheetJS
window.excelInterop = {
    /**
     * Parse Excel file from base64 string
     * @param {string} base64Data - Base64 encoded Excel file
     * @returns {Promise<string>} JSON string of parsed data
     */
    parseExcel: function (base64Data) {
        return new Promise((resolve, reject) => {
            try {
                // Check if XLSX library is loaded
                if (typeof XLSX === 'undefined') {
                    reject(new Error('SheetJS library not loaded. Please refresh the page.'));
                    return;
                }

                // Convert base64 to binary
                const binaryString = atob(base64Data);
                const bytes = new Uint8Array(binaryString.length);
                for (let i = 0; i < binaryString.length; i++) {
                    bytes[i] = binaryString.charCodeAt(i);
                }

                // Parse Excel file
                const workbook = XLSX.read(bytes, { type: 'array' });

                // Get first sheet
                const firstSheetName = workbook.SheetNames[0];
                const worksheet = workbook.Sheets[firstSheetName];

                // Convert to JSON
                const jsonData = XLSX.utils.sheet_to_json(worksheet, {
                    raw: false, // Keep strings as strings
                    defval: ''  // Default value for empty cells
                });

                // Return as JSON string
                resolve(JSON.stringify(jsonData));
            } catch (error) {
                reject(new Error('Failed to parse Excel file: ' + error.message));
            }
        });
    },

    /**
     * Download file from base64 data
     * @param {string} filename - Name of the file to download
     * @param {string} base64Data - Base64 encoded file content
     */
    downloadFile: function (filename, base64Data) {
        try {
            // Convert base64 to blob
            const binaryString = atob(base64Data);
            const bytes = new Uint8Array(binaryString.length);
            for (let i = 0; i < binaryString.length; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }
            const blob = new Blob([bytes], { type: 'application/json' });

            // Create download link
            const url = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = filename;
            
            // Trigger download
            document.body.appendChild(link);
            link.click();
            
            // Cleanup
            document.body.removeChild(link);
            window.URL.revokeObjectURL(url);
        } catch (error) {
            console.error('Failed to download file:', error);
            alert('Failed to download file: ' + error.message);
        }
    }
};

// Log when loaded
console.log('✓ excelInterop.js loaded');
