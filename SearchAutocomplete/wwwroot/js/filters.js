class FilterManager {
    constructor() {
        this.activeFilters = {
            types: [],
            authors: [],
            sectionIds: []
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
            // Load unique types and authors from the API
            await this.loadTypeFilters();
            await this.loadAuthorFilters();
        } catch (error) {
            console.error('Error loading dynamic filters:', error);
        }
    }
    
    async loadTypeFilters() {
        try {
            const response = await fetch('/api/search/filters/types');
            if (response.ok) {
                const types = await response.json();
                this.renderFilterGroup('typeFilters', types, 'type', 'Type');
            }
        } catch (error) {
            console.error('Error loading type filters:', error);
        }
    }
    
    async loadAuthorFilters() {
        try {
            const response = await fetch('/api/search/filters/authors');
            if (response.ok) {
                const authors = await response.json();
                this.renderFilterGroup('authorFilters', authors, 'author', 'Author');
            }
        } catch (error) {
            console.error('Error loading author filters:', error);
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
            case 'type':
                if (!this.activeFilters.types.includes(value)) {
                    this.activeFilters.types.push(value);
                }
                break;
            case 'author':
                if (!this.activeFilters.authors.includes(value)) {
                    this.activeFilters.authors.push(value);
                }
                break;
            case 'section':
                const sectionId = parseInt(value);
                if (!this.activeFilters.sectionIds.includes(sectionId)) {
                    this.activeFilters.sectionIds.push(sectionId);
                }
                break;
        }
    }
    
    removeFilter(type, value) {
        switch (type) {
            case 'type':
                this.activeFilters.types = this.activeFilters.types.filter(t => t !== value);
                break;
            case 'author':
                this.activeFilters.authors = this.activeFilters.authors.filter(a => a !== value);
                break;
            case 'section':
                const sectionId = parseInt(value);
                this.activeFilters.sectionIds = this.activeFilters.sectionIds.filter(id => id !== sectionId);
                break;
        }
    }
    
    updateActiveFiltersDisplay() {
        const activeFiltersContainer = document.getElementById('activeFilters');
        if (!activeFiltersContainer) return;
        
        const filterTags = [];
        
        // Add type filters
        this.activeFilters.types.forEach(type => {
            filterTags.push(this.createFilterTag('Type', type, 'type'));
        });
        
        // Add author filters
        this.activeFilters.authors.forEach(author => {
            filterTags.push(this.createFilterTag('Author', author, 'author'));
        });
        
        // Add section filters
        this.activeFilters.sectionIds.forEach(sectionId => {
            const sectionCheckbox = document.querySelector(`input[data-filter-type="section"][value="${sectionId}"]`);
            if (sectionCheckbox) {
                const sectionLabel = sectionCheckbox.nextElementSibling.textContent.trim();
                filterTags.push(this.createFilterTag('Section', sectionLabel, 'section', sectionId));
            }
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
            types: [],
            authors: [],
            sectionIds: []
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
        return this.activeFilters.types.length > 0 || 
               this.activeFilters.authors.length > 0 || 
               this.activeFilters.sectionIds.length > 0;
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