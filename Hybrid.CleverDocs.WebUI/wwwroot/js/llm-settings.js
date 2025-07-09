/**
 * LLM Settings Management
 * Handles user LLM provider configuration for per-user AI model selection
 */

class LLMSettingsManager {
    constructor() {
        this.initializeEventListeners();
        this.loadModelsForProvider();
        this.updateSliderValues();
    }

    initializeEventListeners() {
        // Provider change handler
        document.getElementById('provider').addEventListener('change', (e) => {
            this.onProviderChange(e.target.value);
        });

        // Form submission
        document.getElementById('llmConfigForm').addEventListener('submit', (e) => {
            e.preventDefault();
            this.saveConfiguration();
        });

        // Test configuration button
        document.getElementById('testConfigBtn').addEventListener('click', () => {
            this.testConfiguration();
        });

        // Delete configuration button
        const deleteBtn = document.getElementById('deleteConfigBtn');
        if (deleteBtn) {
            deleteBtn.addEventListener('click', () => {
                this.deleteConfiguration();
            });
        }

        // API key visibility toggle
        document.getElementById('toggleApiKey').addEventListener('click', () => {
            this.toggleApiKeyVisibility();
        });

        // Slider value updates
        document.getElementById('temperature').addEventListener('input', (e) => {
            document.getElementById('temperatureValue').textContent = e.target.value;
        });

        document.getElementById('topP').addEventListener('input', (e) => {
            document.getElementById('topPValue').textContent = e.target.value;
        });
    }

    async onProviderChange(provider) {
        // Update provider description
        const option = document.querySelector(`#provider option[value="${provider}"]`);
        const description = option ? option.dataset.description : '';
        document.getElementById('providerDescription').textContent = description;

        // Load models for the selected provider
        await this.loadModelsForProvider(provider);

        // Update API key placeholder based on provider
        this.updateApiKeyPlaceholder(provider);
    }

    async loadModelsForProvider(provider = null) {
        if (!provider) {
            provider = document.getElementById('provider').value;
        }

        try {
            const response = await fetch(`/Settings/GetSupportedModels?provider=${provider}`);
            const models = await response.json();

            const modelSelect = document.getElementById('model');
            const currentModel = modelSelect.value;

            // Clear existing options
            modelSelect.innerHTML = '';

            // Add model options
            models.forEach(model => {
                const option = document.createElement('option');
                option.value = model;
                option.textContent = model;
                if (model === currentModel) {
                    option.selected = true;
                }
                modelSelect.appendChild(option);
            });

            // If no current model is selected, select the first one
            if (!currentModel && models.length > 0) {
                modelSelect.value = models[0];
            }

        } catch (error) {
            console.error('Error loading models:', error);
            this.showAlert('Error loading models for the selected provider', 'danger');
        }
    }

    updateApiKeyPlaceholder(provider) {
        const apiKeyInput = document.getElementById('apiKey');
        const placeholders = {
            'openai': 'sk-proj-... (OpenAI API key)',
            'anthropic': 'sk-ant-... (Anthropic API key)',
            'azure': 'Azure OpenAI API key',
            'custom': 'Custom provider API key'
        };

        apiKeyInput.placeholder = placeholders[provider] || 'Enter your API key';
    }

    toggleApiKeyVisibility() {
        const apiKeyInput = document.getElementById('apiKey');
        const toggleBtn = document.getElementById('toggleApiKey');
        const icon = toggleBtn.querySelector('i');

        if (apiKeyInput.type === 'password') {
            apiKeyInput.type = 'text';
            icon.className = 'fas fa-eye-slash';
        } else {
            apiKeyInput.type = 'password';
            icon.className = 'fas fa-eye';
        }
    }

    updateSliderValues() {
        // Update temperature value display
        const tempSlider = document.getElementById('temperature');
        const tempValue = document.getElementById('temperatureValue');
        if (tempSlider && tempValue) {
            tempValue.textContent = tempSlider.value;
        }

        // Update top-p value display
        const topPSlider = document.getElementById('topP');
        const topPValue = document.getElementById('topPValue');
        if (topPSlider && topPValue) {
            topPValue.textContent = topPSlider.value;
        }
    }

    async saveConfiguration() {
        const form = document.getElementById('llmConfigForm');
        const formData = new FormData(form);

        const config = {
            provider: formData.get('provider'),
            model: formData.get('model'),
            apiKey: formData.get('apiKey') || null,
            apiEndpoint: formData.get('apiEndpoint') || null,
            temperature: parseFloat(formData.get('temperature')),
            maxTokens: parseInt(formData.get('maxTokens')),
            topP: parseFloat(formData.get('topP')),
            enableStreaming: formData.has('enableStreaming'),
            isActive: true
        };

        try {
            this.showLoading('Saving configuration...');

            const response = await fetch('/Settings/SaveLLMSettings', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                },
                body: JSON.stringify(config)
            });

            const result = await response.json();

            if (result.success) {
                this.showAlert(result.message, 'success');
                
                // Update the page to reflect the new configuration
                setTimeout(() => {
                    window.location.reload();
                }, 1500);
            } else {
                this.showAlert(result.message, 'danger');
            }

        } catch (error) {
            console.error('Error saving configuration:', error);
            this.showAlert('Error saving configuration. Please try again.', 'danger');
        } finally {
            this.hideLoading();
        }
    }

    async testConfiguration() {
        const form = document.getElementById('llmConfigForm');
        const formData = new FormData(form);

        const config = {
            provider: formData.get('provider'),
            model: formData.get('model'),
            apiKey: formData.get('apiKey') || null,
            apiEndpoint: formData.get('apiEndpoint') || null,
            temperature: parseFloat(formData.get('temperature')),
            maxTokens: parseInt(formData.get('maxTokens')),
            topP: parseFloat(formData.get('topP')),
            enableStreaming: formData.has('enableStreaming'),
            isActive: true
        };

        try {
            this.showLoading('Testing configuration...');

            const response = await fetch('/Settings/TestLLMConfiguration', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                },
                body: JSON.stringify(config)
            });

            const result = await response.json();

            if (result.success) {
                this.showAlert(`✅ ${result.message} (Response time: ${result.responseTime.toFixed(0)}ms)`, 'success');
            } else {
                this.showAlert(`❌ ${result.message}`, 'warning');
            }

        } catch (error) {
            console.error('Error testing configuration:', error);
            this.showAlert('Error testing configuration. Please try again.', 'danger');
        } finally {
            this.hideLoading();
        }
    }

    async deleteConfiguration() {
        if (!confirm('Are you sure you want to delete your custom LLM configuration? This will revert to the system default.')) {
            return;
        }

        try {
            this.showLoading('Deleting configuration...');

            const response = await fetch('/Settings/DeleteLLMConfiguration', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                }
            });

            const result = await response.json();

            if (result.success) {
                this.showAlert(result.message, 'success');
                
                // Reload the page to show system default
                setTimeout(() => {
                    window.location.reload();
                }, 1500);
            } else {
                this.showAlert(result.message, 'danger');
            }

        } catch (error) {
            console.error('Error deleting configuration:', error);
            this.showAlert('Error deleting configuration. Please try again.', 'danger');
        } finally {
            this.hideLoading();
        }
    }

    showAlert(message, type = 'info') {
        const alertContainer = document.getElementById('alertContainer');
        const alertId = 'alert-' + Date.now();

        const alertHtml = `
            <div id="${alertId}" class="alert alert-${type} alert-dismissible fade show" role="alert">
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;

        alertContainer.innerHTML = alertHtml;

        // Auto-dismiss success alerts after 5 seconds
        if (type === 'success') {
            setTimeout(() => {
                const alert = document.getElementById(alertId);
                if (alert) {
                    const bsAlert = new bootstrap.Alert(alert);
                    bsAlert.close();
                }
            }, 5000);
        }
    }

    showLoading(message) {
        const alertContainer = document.getElementById('alertContainer');
        alertContainer.innerHTML = `
            <div class="alert alert-info" role="alert">
                <div class="d-flex align-items-center">
                    <div class="spinner-border spinner-border-sm me-2" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                    ${message}
                </div>
            </div>
        `;
    }

    hideLoading() {
        // Loading will be replaced by success/error message
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new LLMSettingsManager();
});
