/**
 * Collection Create Form JavaScript
 * Enterprise-grade form handling with real-time preview and validation
 */

class CollectionCreateForm {
    constructor() {
        this.tagSuggestionsTimeout = null;
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.updatePreview();
    }

    setupEventListeners() {
        // Tag input with suggestions
        const tagsInput = document.getElementById('TagsInput');
        if (tagsInput) {
            tagsInput.addEventListener('input', (e) => this.handleTagInput(e.target.value));
            tagsInput.addEventListener('keydown', (e) => this.handleTagKeydown(e));
            tagsInput.addEventListener('blur', () => {
                // Delay hiding suggestions to allow clicking
                setTimeout(() => this.hideTagSuggestions(), 200);
            });
        }

        // Form validation
        const form = document.getElementById('createCollectionForm');
        if (form) {
            form.addEventListener('submit', (e) => this.handleFormSubmit(e));
        }

        // Real-time character counting
        this.setupCharacterCounters();
    }

    setupCharacterCounters() {
        const nameInput = document.getElementById('Name');
        const descriptionInput = document.getElementById('Description');

        if (nameInput) {
            this.addCharacterCounter(nameInput, 100);
        }

        if (descriptionInput) {
            this.addCharacterCounter(descriptionInput, 500);
        }
    }

    addCharacterCounter(input, maxLength) {
        const counter = document.createElement('div');
        counter.className = 'character-counter';
        counter.innerHTML = `<span class="current">0</span>/<span class="max">${maxLength}</span>`;
        
        input.parentNode.appendChild(counter);

        input.addEventListener('input', () => {
            const current = input.value.length;
            const currentSpan = counter.querySelector('.current');
            currentSpan.textContent = current;
            
            if (current > maxLength * 0.9) {
                counter.classList.add('warning');
            } else {
                counter.classList.remove('warning');
            }
            
            if (current >= maxLength) {
                counter.classList.add('error');
            } else {
                counter.classList.remove('error');
            }
        });

        // Initial update
        input.dispatchEvent(new Event('input'));
    }

    updatePreview() {
        const name = document.getElementById('Name')?.value || 'Collection Name';
        const description = document.getElementById('Description')?.value || '';
        const selectedColor = document.querySelector('input[name="Color"]:checked')?.value || '#3B82F6';
        const selectedIcon = document.querySelector('input[name="Icon"]:checked')?.value || 'folder';
        const isFavorite = document.getElementById('SetAsFavorite')?.checked || false;
        const tagsInput = document.getElementById('TagsInput')?.value || '';

        // Update preview elements
        const previewName = document.getElementById('previewName');
        const previewDescription = document.getElementById('previewDescription');
        const previewIcon = document.getElementById('previewIcon');
        const previewFavorite = document.getElementById('previewFavorite');
        const previewTags = document.getElementById('previewTags');
        const previewCard = document.querySelector('.preview-card');

        if (previewName) {
            previewName.textContent = name;
        }

        if (previewDescription) {
            if (description) {
                previewDescription.textContent = description;
                previewDescription.style.display = 'block';
            } else {
                previewDescription.style.display = 'none';
            }
        }

        if (previewIcon) {
            previewIcon.innerHTML = `<i class="fas fa-${selectedIcon}"></i>`;
            previewIcon.style.color = selectedColor;
        }

        if (previewFavorite) {
            previewFavorite.style.display = isFavorite ? 'block' : 'none';
        }

        if (previewCard) {
            previewCard.style.borderLeftColor = selectedColor;
        }

        // Update tags
        if (previewTags) {
            const tags = this.parseTags(tagsInput);
            if (tags.length > 0) {
                previewTags.innerHTML = tags.map(tag => 
                    `<span class="preview-tag">${tag}</span>`
                ).join('');
                previewTags.style.display = 'block';
            } else {
                previewTags.style.display = 'none';
            }
        }
    }

    parseTags(tagsInput) {
        if (!tagsInput) return [];
        return tagsInput.split(',')
            .map(tag => tag.trim())
            .filter(tag => tag.length > 0)
            .slice(0, 10); // Limit to 10 tags
    }

    handleTagInput(value) {
        clearTimeout(this.tagSuggestionsTimeout);
        
        // Update preview immediately
        this.updatePreview();
        
        // Fetch suggestions with debounce
        if (value.length >= 1) {
            this.tagSuggestionsTimeout = setTimeout(() => {
                this.fetchTagSuggestions(value);
            }, 300);
        } else {
            this.hideTagSuggestions();
        }
    }

    handleTagKeydown(e) {
        const suggestions = document.getElementById('tagSuggestions');
        if (!suggestions || suggestions.style.display === 'none') return;

        const items = suggestions.querySelectorAll('.suggestion-item');
        let activeIndex = Array.from(items).findIndex(item => item.classList.contains('active'));

        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                activeIndex = (activeIndex + 1) % items.length;
                this.setActiveSuggestion(items, activeIndex);
                break;
            case 'ArrowUp':
                e.preventDefault();
                activeIndex = activeIndex <= 0 ? items.length - 1 : activeIndex - 1;
                this.setActiveSuggestion(items, activeIndex);
                break;
            case 'Enter':
                e.preventDefault();
                if (activeIndex >= 0 && items[activeIndex]) {
                    this.selectTagSuggestion(items[activeIndex].textContent);
                }
                break;
            case 'Escape':
                this.hideTagSuggestions();
                break;
        }
    }

    setActiveSuggestion(items, activeIndex) {
        items.forEach((item, index) => {
            if (index === activeIndex) {
                item.classList.add('active');
            } else {
                item.classList.remove('active');
            }
        });
    }

    async fetchTagSuggestions(term) {
        try {
            // Get the last tag being typed
            const tags = term.split(',');
            const lastTag = tags[tags.length - 1].trim();
            
            if (lastTag.length < 1) {
                this.hideTagSuggestions();
                return;
            }

            const response = await fetch(`/Collections/tag-suggestions?term=${encodeURIComponent(lastTag)}`);
            if (response.ok) {
                const suggestions = await response.json();
                this.displayTagSuggestions(suggestions, tags);
            }
        } catch (error) {
            console.error('Error fetching tag suggestions:', error);
        }
    }

    displayTagSuggestions(suggestions, currentTags) {
        const suggestionsContainer = document.getElementById('tagSuggestions');
        if (!suggestionsContainer) return;

        // Filter out already selected tags
        const existingTags = currentTags.slice(0, -1).map(tag => tag.trim().toLowerCase());
        const filteredSuggestions = suggestions.filter(suggestion => 
            !existingTags.includes(suggestion.toLowerCase())
        );

        if (filteredSuggestions.length === 0) {
            this.hideTagSuggestions();
            return;
        }

        suggestionsContainer.innerHTML = filteredSuggestions
            .slice(0, 5) // Limit to 5 suggestions
            .map(suggestion => `
                <div class="suggestion-item" onclick="collectionCreateForm.selectTagSuggestion('${suggestion}')">
                    <i class="fas fa-tag"></i>
                    <span>${suggestion}</span>
                </div>
            `).join('');

        suggestionsContainer.style.display = 'block';
    }

    selectTagSuggestion(suggestion) {
        const tagsInput = document.getElementById('TagsInput');
        if (!tagsInput) return;

        const tags = tagsInput.value.split(',');
        tags[tags.length - 1] = suggestion;
        
        tagsInput.value = tags.join(', ') + ', ';
        tagsInput.focus();
        
        this.hideTagSuggestions();
        this.updatePreview();
    }

    hideTagSuggestions() {
        const suggestionsContainer = document.getElementById('tagSuggestions');
        if (suggestionsContainer) {
            suggestionsContainer.style.display = 'none';
        }
    }

    handleFormSubmit(e) {
        const form = e.target;
        
        // Basic validation
        if (!this.validateForm(form)) {
            e.preventDefault();
            e.stopPropagation();
            return false;
        }

        // Show loading state
        this.setFormLoading(true);
        
        // Process tags
        this.processTagsBeforeSubmit();
        
        return true;
    }

    validateForm(form) {
        let isValid = true;
        const errors = [];

        // Name validation
        const nameInput = form.querySelector('#Name');
        if (!nameInput.value.trim()) {
            errors.push('Collection name is required');
            this.showFieldError(nameInput, 'Collection name is required');
            isValid = false;
        } else if (nameInput.value.length > 100) {
            errors.push('Collection name must be 100 characters or less');
            this.showFieldError(nameInput, 'Collection name must be 100 characters or less');
            isValid = false;
        }

        // Description validation
        const descriptionInput = form.querySelector('#Description');
        if (descriptionInput.value.length > 500) {
            errors.push('Description must be 500 characters or less');
            this.showFieldError(descriptionInput, 'Description must be 500 characters or less');
            isValid = false;
        }

        // Color validation
        const colorInput = form.querySelector('input[name="Color"]:checked');
        if (!colorInput) {
            errors.push('Please select a color');
            isValid = false;
        }

        // Icon validation
        const iconInput = form.querySelector('input[name="Icon"]:checked');
        if (!iconInput) {
            errors.push('Please select an icon');
            isValid = false;
        }

        if (!isValid) {
            this.showFormErrors(errors);
        }

        return isValid;
    }

    showFieldError(field, message) {
        // Remove existing error
        const existingError = field.parentNode.querySelector('.field-error');
        if (existingError) {
            existingError.remove();
        }

        // Add new error
        const errorDiv = document.createElement('div');
        errorDiv.className = 'field-error text-danger';
        errorDiv.textContent = message;
        field.parentNode.appendChild(errorDiv);

        // Add error class to field
        field.classList.add('is-invalid');

        // Remove error on input
        field.addEventListener('input', function removeError() {
            field.classList.remove('is-invalid');
            if (errorDiv.parentNode) {
                errorDiv.remove();
            }
            field.removeEventListener('input', removeError);
        });
    }

    showFormErrors(errors) {
        // Create or update error summary
        let errorSummary = document.getElementById('errorSummary');
        if (!errorSummary) {
            errorSummary = document.createElement('div');
            errorSummary.id = 'errorSummary';
            errorSummary.className = 'alert alert-danger';
            
            const form = document.getElementById('createCollectionForm');
            form.insertBefore(errorSummary, form.firstChild);
        }

        errorSummary.innerHTML = `
            <h6><i class="fas fa-exclamation-triangle"></i> Please correct the following errors:</h6>
            <ul class="mb-0">
                ${errors.map(error => `<li>${error}</li>`).join('')}
            </ul>
        `;

        // Scroll to top
        errorSummary.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    processTagsBeforeSubmit() {
        const tagsInput = document.getElementById('TagsInput');
        if (tagsInput) {
            const tags = this.parseTags(tagsInput.value);
            tagsInput.value = tags.join(', ');
        }
    }

    setFormLoading(loading) {
        const submitButton = document.getElementById('createButton');
        const form = document.getElementById('createCollectionForm');

        if (loading) {
            submitButton.disabled = true;
            submitButton.innerHTML = `
                <span class="spinner-border spinner-border-sm me-2" role="status"></span>
                Creating Collection...
            `;
            form.classList.add('loading');
        } else {
            submitButton.disabled = false;
            submitButton.innerHTML = `
                <i class="fas fa-plus"></i>
                <span>Create Collection</span>
            `;
            form.classList.remove('loading');
        }
    }
}

// Global instance
let collectionCreateForm;

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    collectionCreateForm = new CollectionCreateForm();
});
