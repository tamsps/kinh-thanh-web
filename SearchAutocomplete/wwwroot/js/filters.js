class FilterManager {
    constructor() {
        this.activeFilters = {
            bookNames: [],
            bookTypes: [],
            chapterNumbers: []
        };
        
        this.init();
    }
    
    init() {
        // Initialize filter checkboxes
        this.initializeFilterCheckboxes();
        
        // Initialize clear filters button
        const clearFiltersBtn = document.getElementById('clearFilters');
        if (clearFiltersBtn) {
            clearFiltersBtn.addEventListener('click', () => this.clearAllFilters());
        }
        
        // Load dynamic filters
        this.loadDynamicFilters();
    }
    
    initializeFilterCheckboxes() {
        const filterCheckboxes = document.querySelectorAll('.filter-checkbox');
        filterCheckboxes.forEach(checkbox => {
            checkbox.addEventListener('change', (e) => this.handleFilterChange(e));
        });
    }
    
    async loadDynamicFilters() {
        try {
            // Load book names and chapter numbers from the API
            await this.loadBookNameFilters();
            await this.loadChapterFilters();
        } catch (error) {
            console.error('Error loading dynamic filters:', error);
        }
    }
    
    async loadBookNameFilters() {
        try {
            const response = await fetch('/api/search/filters/book-names');
            if (response.ok) {
                const bookNames = await response.json();
                this.renderFilterGroup('bookNameFilters', bookNames, 'bookName', 'Book Name');
            }
        } catch (error) {
            console.error('Error loading book name filters:', error);
        }
    }
    
    async loadChapterFilters() {
        try {
            // For now, we'll generate chapter numbers 1-50 as common chapters
            // In a real implementation, this could be dynamic based on selected books
            const chapters = Array.from({length: 50}, (_, i) => i + 1);
            this.renderFilterGroup('chapterFilters', chapters, 'chapter', 'Chapter');
        } catch (error) {
            console.error('Error loading chapter filters:', error);
        }
    }
    
    renderFilterGroup(containerId, items, filterType, labelPrefix) {
        const container = document.getElementById(containerId);
        if (!container || !items.length) return;
        
        const html = items.map(item => `
            <div class="form-check">
                <input class="form-check-input filter-checkbox" type="checkbox" 
                       value="${this.escapeHtml(item)}" 
                       id="${filterType}_${this.sanitizeId(item)}" 
                       data-filter-type="${filterType}">
                <label class="form-check-label" for="${filterType}_${this.sanitizeId(item)}">
                    ${this.escapeHtml(item)}
                </label>
            </div>
        `).join('');
        
        container.innerHTML = html;
        
        // Add event listeners to new checkboxes
        container.querySelectorAll('.filter-checkbox').forEach(checkbox => {
            checkbox.addEventListener('change', (e) => this.handleFilterChange(e));
        });
    }
    
    handleFilterChange(event) {
        const checkbox = event.target;
        const filterType = checkbox.dataset.filterType;
        const value = checkbox.value;
        
        if (checkbox.checked) {
            this.addFilter(filterType, value);
        } else {
            this.removeFilter(filterType, value);
        }
        
        this.updateActiveFiltersDisplay();
        this.triggerFilterUpdate();
    }
    
    addFilter(type, value) {
        switch (type) {
            case 'bookName':
                if (!this.activeFilters.bookNames.includes(value)) {
                    this.activeFilters.bookNames.push(value);
                }
                break;
            case 'bookType':
                if (!this.activeFilters.bookTypes.includes(value)) {
                    this.activeFilters.bookTypes.push(value);
                }
                break;
            case 'chapter':
                const chapterNum = parseInt(value);
                if (!this.activeFilters.chapterNumbers.includes(chapterNum)) {
                    this.activeFilters.chapterNumbers.push(chapterNum);
                }
                break;
        }
    }
    
    removeFilter(type, value) {
        switch (type) {
            case 'bookName':
                this.activeFilters.bookNames = this.activeFilters.bookNames.filter(b => b !== value);
                break;
            case 'bookType':
                this.activeFilters.bookTypes = this.activeFilters.bookTypes.filter(t => t !== value);
                break;
            case 'chapter':
                const chapterNum = parseInt(value);
                this.activeFilters.chapterNumbers = this.activeFilters.chapterNumbers.filter(c => c !== chapterNum);
                break;
        }
    }
    
    updateActiveFiltersDisplay() {
        const activeFiltersContainer = document.getElementById('activeFilters');
        if (!activeFiltersContainer) return;
        
        const filterTags = [];
        
        // Add book name filters
        this.activeFilters.bookNames.forEach(bookName => {
            filterTags.push(this.createFilterTag('Book Name', bookName, 'bookName'));
        });
        
        // Add book type filters
        this.activeFilters.bookTypes.forEach(bookType => {
            const displayName = bookType === 'C' ? 'Cựu Ước (C)' : bookType === 'T' ? 'Tân Ước (T)' : bookType;
            filterTags.push(this.createFilterTag('Book Type', displayName, 'bookType', bookType));
        });
        
        // Add chapter filters
        this.activeFilters.chapterNumbers.forEach(chapterNum => {
            filterTags.push(this.createFilterTag('Chapter', chapterNum, 'chapter'));
        });
        
        if (filterTags.length === 0) {
            activeFiltersContainer.innerHTML = '<small class="text-muted">No active filters</small>';
        } else {
            activeFiltersContainer.innerHTML = `
                <div class="mb-2">
                    <small class="text-muted">Active filters:</small>
                </div>
                ${filterTags.join('')}
            `;
        }
    }
    
    createFilterTag(category, value, type, originalValue = null) {
        const valueToRemove = originalValue || value;
        return `
            <span class="active-filter">
                <small><strong>${category}:</strong> ${this.escapeHtml(value)}</small>
                <span class="remove-filter" onclick="window.filterManager.removeFilterByTag('${type}', '${this.escapeHtml(valueToRemove)}')" title="Remove filter">
                    ×
                </span>
            </span>
        `;
    }
    
    removeFilterByTag(type, value) {
        // Uncheck the corresponding checkbox
        let checkbox;
        if (type === 'section') {
            checkbox = document.querySelector(`input[data-filter-type="${type}"][value="${value}"]`);
        } else {
            checkbox = document.querySelector(`input[data-filter-type="${type}"][value="${value}"]`);
        }
        
        if (checkbox) {
            checkbox.checked = false;
            this.removeFilter(type, value);
            this.updateActiveFiltersDisplay();
            this.triggerFilterUpdate();
        }
    }
    
    clearAllFilters() {
        // Uncheck all filter checkboxes
        const filterCheckboxes = document.querySelectorAll('.filter-checkbox');
        filterCheckboxes.forEach(checkbox => {
            checkbox.checked = false;
        });
        
        // Clear active filters
        this.activeFilters = {
            bookNames: [],
            bookTypes: [],
            chapterNumbers: []
        };
        
        this.updateActiveFiltersDisplay();
        this.triggerFilterUpdate();
    }
    
    triggerFilterUpdate() {
        // Trigger autocomplete update if there's text in the search input
        const searchInput = document.getElementById('searchInput');
        if (searchInput && searchInput.value.trim().length >= 2) {
            // Trigger autocomplete refresh
            if (window.autocompleteHandler) {
                searchInput.dispatchEvent(new Event('input'));
            }
        }
        
        // Trigger search refresh if there's an active search
        if (window.searchHandler && window.searchHandler.currentSearchTerm) {
            window.searchHandler.refreshSearch();
        }
    }
    
    getActiveFilters() {
        return { ...this.activeFilters };
    }
    
    hasActiveFilters() {
        return this.activeFilters.bookNames.length > 0 || 
               this.activeFilters.bookTypes.length > 0 || 
               this.activeFilters.chapterNumbers.length > 0;
    }
    
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
    
    sanitizeId(text) {
        return text.replace(/[^a-zA-Z0-9]/g, '_');
    }
}

// Initialize filter manager when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    window.filterManager = new FilterManager();
});