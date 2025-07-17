class AutocompleteHandler {
    constructor(inputElement, dropdownElement, loadingElement) {
        this.input = inputElement;
        this.dropdown = dropdownElement;
        this.loading = loadingElement;
        this.debounceTimer = null;
        this.currentRequest = null;
        this.selectedIndex = -1;
        this.suggestions = [];
        
        this.init();
    }
    
    init() {
        this.input.addEventListener('input', (e) => this.handleInput(e));
        this.input.addEventListener('keydown', (e) => this.handleKeydown(e));
        this.input.addEventListener('blur', (e) => this.handleBlur(e));
        this.input.addEventListener('focus', (e) => this.handleFocus(e));
        
        // Close dropdown when clicking outside
        document.addEventListener('click', (e) => {
            if (!this.input.contains(e.target) && !this.dropdown.contains(e.target)) {
                this.hideDropdown();
            }
        });
    }
    
    handleInput(event) {
        const searchTerm = event.target.value.trim();
        
        // Clear previous timer
        if (this.debounceTimer) {
            clearTimeout(this.debounceTimer);
        }
        
        // Cancel previous request
        if (this.currentRequest) {
            this.currentRequest.abort();
        }
        
        if (searchTerm.length < 2) {
            this.hideDropdown();
            return;
        }
        
        // Debounce the request by 300ms
        this.debounceTimer = setTimeout(() => {
            this.getSuggestions(searchTerm);
        }, 300);
    }
    
    handleKeydown(event) {
        if (!this.isDropdownVisible()) {
            return;
        }
        
        switch (event.key) {
            case 'ArrowDown':
                event.preventDefault();
                this.navigateDown();
                break;
            case 'ArrowUp':
                event.preventDefault();
                this.navigateUp();
                break;
            case 'Enter':
                event.preventDefault();
                this.selectCurrent();
                break;
            case 'Escape':
                event.preventDefault();
                this.hideDropdown();
                this.input.blur();
                break;
        }
    }
    
    handleBlur(event) {
        // Delay hiding to allow for click events on dropdown items
        setTimeout(() => {
            if (!this.dropdown.matches(':hover')) {
                this.hideDropdown();
            }
        }, 150);
    }
    
    handleFocus(event) {
        const searchTerm = event.target.value.trim();
        if (searchTerm.length >= 2 && this.suggestions.length > 0) {
            this.showDropdown();
        }
    }
    
    async getSuggestions(searchTerm) {
        try {
            this.showLoading();
            
            // Get current filters
            const filters = window.filterManager ? window.filterManager.getActiveFilters() : {};
            
            // Build query parameters
            const params = new URLSearchParams({
                searchTerm: searchTerm,
                maxResults: 10
            });
            
            // Add filters to params
            if (filters.bookNames && filters.bookNames.length > 0) {
                filters.bookNames.forEach(bookName => params.append('bookNames', bookName));
            }
            if (filters.bookTypes && filters.bookTypes.length > 0) {
                filters.bookTypes.forEach(bookType => params.append('bookTypes', bookType));
            }
            if (filters.chapterNumbers && filters.chapterNumbers.length > 0) {
                filters.chapterNumbers.forEach(chapter => params.append('chapterNumbers', chapter));
            }
            
            // Create AbortController for request cancellation
            const controller = new AbortController();
            this.currentRequest = controller;
            
            const response = await fetch(`/api/autocomplete/suggestions?${params}`, {
                signal: controller.signal,
                headers: {
                    'Accept': 'application/json'
                }
            });
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const suggestions = await response.json();
            this.displaySuggestions(suggestions);
            
        } catch (error) {
            if (error.name !== 'AbortError') {
                console.error('Error fetching suggestions:', error);
                this.showNoResults();
            }
        } finally {
            this.hideLoading();
            this.currentRequest = null;
        }
    }
    
    displaySuggestions(suggestions) {
        this.suggestions = suggestions;
        this.selectedIndex = -1;
        
        if (suggestions.length === 0) {
            this.showNoResults();
            return;
        }
        
        const html = suggestions.map((suggestion, index) => 
            `<div class="autocomplete-item" data-index="${index}" onclick="window.autocompleteHandler.selectSuggestion('${this.escapeHtml(suggestion)}')">${this.highlightMatch(suggestion)}</div>`
        ).join('');
        
        this.dropdown.innerHTML = html;
        this.showDropdown();
    }
    
    showNoResults() {
        this.dropdown.innerHTML = '<div class="autocomplete-no-results">No suggestions found</div>';
        this.showDropdown();
    }
    
    highlightMatch(text) {
        const searchTerm = this.input.value.trim();
        if (!searchTerm) return this.escapeHtml(text);
        
        const regex = new RegExp(`(${this.escapeRegex(searchTerm)})`, 'gi');
        return this.escapeHtml(text).replace(regex, '<strong>$1</strong>');
    }
    
    navigateDown() {
        if (this.selectedIndex < this.suggestions.length - 1) {
            this.selectedIndex++;
            this.updateSelection();
        }
    }
    
    navigateUp() {
        if (this.selectedIndex > 0) {
            this.selectedIndex--;
            this.updateSelection();
        }
    }
    
    updateSelection() {
        const items = this.dropdown.querySelectorAll('.autocomplete-item');
        items.forEach((item, index) => {
            item.classList.toggle('active', index === this.selectedIndex);
        });
    }
    
    selectCurrent() {
        if (this.selectedIndex >= 0 && this.selectedIndex < this.suggestions.length) {
            this.selectSuggestion(this.suggestions[this.selectedIndex]);
        }
    }
    
    selectSuggestion(suggestion) {
        this.input.value = suggestion;
        this.hideDropdown();
        this.input.focus();
        
        // Trigger input event to update any listeners
        this.input.dispatchEvent(new Event('input', { bubbles: true }));
    }
    
    showDropdown() {
        this.dropdown.style.display = 'block';
        this.dropdown.classList.add('fade-in');
    }
    
    hideDropdown() {
        this.dropdown.style.display = 'none';
        this.dropdown.classList.remove('fade-in');
        this.selectedIndex = -1;
    }
    
    isDropdownVisible() {
        return this.dropdown.style.display === 'block';
    }
    
    showLoading() {
        this.loading.classList.remove('d-none');
    }
    
    hideLoading() {
        this.loading.classList.add('d-none');
    }
    
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
    
    escapeRegex(text) {
        return text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    }
}

// Initialize autocomplete when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    const searchInput = document.getElementById('searchInput');
    const autocompleteDropdown = document.getElementById('autocompleteDropdown');
    const loadingIndicator = document.getElementById('loadingIndicator');
    
    if (searchInput && autocompleteDropdown && loadingIndicator) {
        window.autocompleteHandler = new AutocompleteHandler(searchInput, autocompleteDropdown, loadingIndicator);
    }
});