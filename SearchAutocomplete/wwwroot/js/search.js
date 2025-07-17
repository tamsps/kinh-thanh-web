class SearchHandler {
    constructor() {
        this.currentPage = 1;
        this.pageSize = 10;
        this.totalPages = 0;
        this.currentSearchTerm = '';
        this.currentFilters = {};
        
        this.init();
    }
    
    init() {
        const searchForm = document.getElementById('searchForm');
        const searchButton = document.getElementById('searchButton');
        
        if (searchForm) {
            searchForm.addEventListener('submit', (e) => this.handleSearch(e));
        }
        
        if (searchButton) {
            searchButton.addEventListener('click', (e) => this.handleSearch(e));
        }
    }
    
    async handleSearch(event) {
        event.preventDefault();
        
        const searchInput = document.getElementById('searchInput');
        const searchTerm = searchInput.value.trim();
        
        if (!searchTerm) {
            this.showError('Please enter a search term');
            return;
        }
        
        this.currentSearchTerm = searchTerm;
        this.currentPage = 1;
        this.currentFilters = window.filterManager ? window.filterManager.getActiveFilters() : {};
        
        await this.performSearch();
    }
    
    async performSearch(page = 1) {
        try {
            this.showSearchLoading(true);
            this.hideResults();
            
            const requestData = {
                searchTerm: this.currentSearchTerm,
                filters: this.currentFilters,
                page: page,
                pageSize: this.pageSize
            };
            
            const response = await fetch('/api/search', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify(requestData)
            });
            
            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
            }
            
            const result = await response.json();
            this.displayResults(result);
            this.currentPage = page;
            
        } catch (error) {
            console.error('Search error:', error);
            this.showError(error.message || 'An error occurred while searching');
        } finally {
            this.showSearchLoading(false);
        }
    }
    
    displayResults(result) {
        const resultsContainer = document.getElementById('resultsContainer');
        const searchResults = document.getElementById('searchResults');
        const noResults = document.getElementById('noResults');
        const resultsCount = document.getElementById('resultsCount');
        
        if (result.results.length === 0) {
            searchResults.classList.add('d-none');
            noResults.classList.remove('d-none');
            return;
        }
        
        // Update results count
        resultsCount.textContent = `${result.totalCount} results`;
        
        // Generate results HTML
        const resultsHtml = result.results.map(item => this.createResultItem(item)).join('');
        resultsContainer.innerHTML = resultsHtml;
        
        // Generate pagination
        this.createPagination(result);
        
        // Show results
        noResults.classList.add('d-none');
        searchResults.classList.remove('d-none');
        searchResults.classList.add('fade-in');
        
        // Scroll to results
        searchResults.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
    
    createResultItem(item) {
        return `
            <div class="search-result-item">
                <div class="search-result-content">
                    ${this.escapeHtml(item.content)}
                </div>
                <div class="search-result-meta">
                    ${item.bookName ? `<span class="badge bg-primary">${this.escapeHtml(item.bookName)}</span>` : ''}
                    ${item.bookType ? `<span class="badge bg-secondary">${item.bookType === 'C' ? 'Cựu Ước' : item.bookType === 'T' ? 'Tân Ước' : this.escapeHtml(item.bookType)}</span>` : ''}
                    ${item.chapterNumber ? `<span class="badge bg-info">Chương ${item.chapterNumber}</span>` : ''}
                    ${item.statementNumber ? `<span class="badge bg-success">Câu ${item.statementNumber}</span>` : ''}
                    ${item.sectionName ? `<small class="text-muted">${this.escapeHtml(item.sectionName)}</small>` : ''}
                </div>
            </div>
        `;
    }
    
    createPagination(result) {
        const paginationContainer = document.getElementById('paginationContainer');
        
        if (result.totalPages <= 1) {
            paginationContainer.innerHTML = '';
            return;
        }
        
        this.totalPages = result.totalPages;
        const currentPage = result.currentPage;
        
        let paginationHtml = '<nav aria-label="Search results pagination"><ul class="pagination justify-content-center">';
        
        // Previous button
        paginationHtml += `
            <li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                <a class="page-link" href="#" onclick="window.searchHandler.goToPage(${currentPage - 1})" aria-label="Previous">
                    <span aria-hidden="true">&laquo;</span>
                </a>
            </li>
        `;
        
        // Page numbers
        const startPage = Math.max(1, currentPage - 2);
        const endPage = Math.min(result.totalPages, currentPage + 2);
        
        if (startPage > 1) {
            paginationHtml += `<li class="page-item"><a class="page-link" href="#" onclick="window.searchHandler.goToPage(1)">1</a></li>`;
            if (startPage > 2) {
                paginationHtml += '<li class="page-item disabled"><span class="page-link">...</span></li>';
            }
        }
        
        for (let i = startPage; i <= endPage; i++) {
            paginationHtml += `
                <li class="page-item ${i === currentPage ? 'active' : ''}">
                    <a class="page-link" href="#" onclick="window.searchHandler.goToPage(${i})">${i}</a>
                </li>
            `;
        }
        
        if (endPage < result.totalPages) {
            if (endPage < result.totalPages - 1) {
                paginationHtml += '<li class="page-item disabled"><span class="page-link">...</span></li>';
            }
            paginationHtml += `<li class="page-item"><a class="page-link" href="#" onclick="window.searchHandler.goToPage(${result.totalPages})">${result.totalPages}</a></li>`;
        }
        
        // Next button
        paginationHtml += `
            <li class="page-item ${currentPage === result.totalPages ? 'disabled' : ''}">
                <a class="page-link" href="#" onclick="window.searchHandler.goToPage(${currentPage + 1})" aria-label="Next">
                    <span aria-hidden="true">&raquo;</span>
                </a>
            </li>
        `;
        
        paginationHtml += '</ul></nav>';
        
        // Add page info
        paginationHtml += `
            <div class="text-center mt-2">
                <small class="text-muted">
                    Page ${currentPage} of ${result.totalPages} 
                    (${result.totalCount} total results)
                </small>
            </div>
        `;
        
        paginationContainer.innerHTML = paginationHtml;
    }
    
    async goToPage(page) {
        if (page < 1 || page > this.totalPages || page === this.currentPage) {
            return;
        }
        
        await this.performSearch(page);
    }
    
    showSearchLoading(show) {
        const searchButton = document.getElementById('searchButton');
        const searchButtonText = document.getElementById('searchButtonText');
        const searchButtonSpinner = document.getElementById('searchButtonSpinner');
        
        if (show) {
            searchButton.disabled = true;
            searchButtonText.classList.add('d-none');
            searchButtonSpinner.classList.remove('d-none');
        } else {
            searchButton.disabled = false;
            searchButtonText.classList.remove('d-none');
            searchButtonSpinner.classList.add('d-none');
        }
    }
    
    hideResults() {
        const searchResults = document.getElementById('searchResults');
        const noResults = document.getElementById('noResults');
        
        searchResults.classList.add('d-none');
        noResults.classList.add('d-none');
    }
    
    showError(message) {
        // Create or update error alert
        let errorAlert = document.getElementById('searchError');
        if (!errorAlert) {
            errorAlert = document.createElement('div');
            errorAlert.id = 'searchError';
            errorAlert.className = 'alert alert-danger alert-dismissible fade show mt-3';
            
            const searchForm = document.getElementById('searchForm');
            searchForm.parentNode.insertBefore(errorAlert, searchForm.nextSibling);
        }
        
        errorAlert.innerHTML = `
            <i class="bi bi-exclamation-triangle"></i> ${this.escapeHtml(message)}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        `;
        
        // Auto-hide after 5 seconds
        setTimeout(() => {
            if (errorAlert) {
                errorAlert.remove();
            }
        }, 5000);
    }
    
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
    
    // Public method to trigger search from filters
    async refreshSearch() {
        if (this.currentSearchTerm) {
            this.currentFilters = window.filterManager ? window.filterManager.getActiveFilters() : {};
            await this.performSearch(1);
        }
    }
}

// Initialize search handler when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    window.searchHandler = new SearchHandler();
});