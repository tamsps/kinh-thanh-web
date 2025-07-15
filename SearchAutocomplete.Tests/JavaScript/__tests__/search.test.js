// Load the search functionality
const fs = require('fs');
const path = require('path');

// Read the search.js file
const searchJs = fs.readFileSync(
  path.join(__dirname, '../../../SearchAutocomplete/wwwroot/js/search.js'),
  'utf8'
);

// Execute the JavaScript code in the test environment
eval(searchJs);

describe('SearchHandler', () => {
  let searchHandler;

  beforeEach(() => {
    // Set up DOM elements
    document.body.innerHTML = `
      <div>
        <form id="searchForm">
          <input type="text" id="searchInput" value="test search" />
          <button type="submit" id="searchButton">
            <span id="searchButtonText">Search</span>
            <span id="searchButtonSpinner" class="d-none"></span>
          </button>
        </form>
        <div id="searchResults" class="d-none">
          <div id="resultsCount"></div>
          <div id="resultsContainer"></div>
          <div id="paginationContainer"></div>
        </div>
        <div id="noResults" class="d-none"></div>
      </div>
    `;

    // Mock window.filterManager
    global.window = {
      filterManager: {
        getActiveFilters: jest.fn(() => ({
          types: [],
          authors: [],
          sectionIds: []
        }))
      }
    };

    searchHandler = new SearchHandler();
  });

  afterEach(() => {
    document.body.innerHTML = '';
    fetch.mockClear();
  });

  describe('Initialization', () => {
    test('should initialize with default values', () => {
      expect(searchHandler.currentPage).toBe(1);
      expect(searchHandler.pageSize).toBe(10);
      expect(searchHandler.totalPages).toBe(0);
      expect(searchHandler.currentSearchTerm).toBe('');
    });

    test('should attach event listeners', () => {
      const form = document.getElementById('searchForm');
      const button = document.getElementById('searchButton');
      
      const formSpy = jest.spyOn(form, 'addEventListener');
      const buttonSpy = jest.spyOn(button, 'addEventListener');

      new SearchHandler();

      expect(formSpy).toHaveBeenCalledWith('submit', expect.any(Function));
      expect(buttonSpy).toHaveBeenCalledWith('click', expect.any(Function));
    });
  });

  describe('Search Handling', () => {
    test('should prevent form submission and perform search', async () => {
      const mockResponse = {
        ok: true,
        json: jest.fn().mockResolvedValue({
          results: [],
          totalCount: 0,
          currentPage: 1,
          totalPages: 0
        })
      };
      fetch.mockResolvedValue(mockResponse);

      const event = new Event('submit');
      const preventDefaultSpy = jest.spyOn(event, 'preventDefault');

      await searchHandler.handleSearch(event);

      expect(preventDefaultSpy).toHaveBeenCalled();
      expect(fetch).toHaveBeenCalledWith('/api/search', expect.objectContaining({
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        }
      }));
    });

    test('should show error for empty search term', async () => {
      document.getElementById('searchInput').value = '';
      const showErrorSpy = jest.spyOn(searchHandler, 'showError');

      const event = new Event('submit');
      await searchHandler.handleSearch(event);

      expect(showErrorSpy).toHaveBeenCalledWith('Please enter a search term');
      expect(fetch).not.toHaveBeenCalled();
    });

    test('should handle search API errors', async () => {
      const mockResponse = {
        ok: false,
        status: 500,
        json: jest.fn().mockResolvedValue({ message: 'Server error' })
      };
      fetch.mockResolvedValue(mockResponse);

      const showErrorSpy = jest.spyOn(searchHandler, 'showError');
      const event = new Event('submit');

      await searchHandler.handleSearch(event);

      expect(showErrorSpy).toHaveBeenCalledWith('Server error');
    });
  });

  describe('Results Display', () => {
    test('should display search results correctly', () => {
      const mockResult = {
        results: [
          {
            id: 1,
            content: 'Test content 1',
            sectionName: 'Section 1',
            type: 'Type1',
            author: 'Author1',
            from: 'Page 1',
            to: 'Page 2'
          },
          {
            id: 2,
            content: 'Test content 2',
            sectionName: 'Section 2',
            type: 'Type2',
            author: 'Author2'
          }
        ],
        totalCount: 2,
        currentPage: 1,
        totalPages: 1
      };

      searchHandler.displayResults(mockResult);

      const resultsContainer = document.getElementById('resultsContainer');
      const resultsCount = document.getElementById('resultsCount');
      const searchResults = document.getElementById('searchResults');

      expect(resultsCount.textContent).toBe('2 results');
      expect(resultsContainer.innerHTML).toContain('Test content 1');
      expect(resultsContainer.innerHTML).toContain('Test content 2');
      expect(searchResults.classList.contains('d-none')).toBe(false);
    });

    test('should show no results message when no results found', () => {
      const mockResult = {
        results: [],
        totalCount: 0,
        currentPage: 1,
        totalPages: 0
      };

      searchHandler.displayResults(mockResult);

      const searchResults = document.getElementById('searchResults');
      const noResults = document.getElementById('noResults');

      expect(searchResults.classList.contains('d-none')).toBe(true);
      expect(noResults.classList.contains('d-none')).toBe(false);
    });

    test('should create result item HTML correctly', () => {
      const item = {
        content: 'Test content',
        sectionName: 'Test Section',
        type: 'Test Type',
        author: 'Test Author',
        from: 'Page 1',
        to: 'Page 2'
      };

      const html = searchHandler.createResultItem(item);

      expect(html).toContain('Test content');
      expect(html).toContain('Test Section');
      expect(html).toContain('Test Type');
      expect(html).toContain('Test Author');
      expect(html).toContain('Page 1 - Page 2');
    });
  });

  describe('Pagination', () => {
    test('should create pagination for multiple pages', () => {
      const mockResult = {
        results: [],
        totalCount: 25,
        currentPage: 2,
        totalPages: 3
      };

      searchHandler.displayResults(mockResult);

      const paginationContainer = document.getElementById('paginationContainer');
      expect(paginationContainer.innerHTML).toContain('pagination');
      expect(paginationContainer.innerHTML).toContain('Page 2 of 3');
    });

    test('should not create pagination for single page', () => {
      const mockResult = {
        results: [],
        totalCount: 5,
        currentPage: 1,
        totalPages: 1
      };

      searchHandler.displayResults(mockResult);

      const paginationContainer = document.getElementById('paginationContainer');
      expect(paginationContainer.innerHTML).toBe('');
    });

    test('should handle page navigation', async () => {
      searchHandler.currentSearchTerm = 'test';
      searchHandler.totalPages = 3;
      
      const mockResponse = {
        ok: true,
        json: jest.fn().mockResolvedValue({
          results: [],
          totalCount: 25,
          currentPage: 2,
          totalPages: 3
        })
      };
      fetch.mockResolvedValue(mockResponse);

      await searchHandler.goToPage(2);

      expect(fetch).toHaveBeenCalledWith('/api/search', expect.objectContaining({
        body: expect.stringContaining('"page":2')
      }));
    });

    test('should not navigate to invalid pages', async () => {
      searchHandler.totalPages = 3;
      searchHandler.currentPage = 2;

      await searchHandler.goToPage(0); // Invalid page
      await searchHandler.goToPage(4); // Invalid page
      await searchHandler.goToPage(2); // Current page

      expect(fetch).not.toHaveBeenCalled();
    });
  });

  describe('Loading States', () => {
    test('should show loading state during search', async () => {
      const mockResponse = {
        ok: true,
        json: jest.fn().mockResolvedValue({
          results: [],
          totalCount: 0,
          currentPage: 1,
          totalPages: 0
        })
      };
      fetch.mockResolvedValue(mockResponse);

      const searchButton = document.getElementById('searchButton');
      const searchButtonText = document.getElementById('searchButtonText');
      const searchButtonSpinner = document.getElementById('searchButtonSpinner');

      const event = new Event('submit');
      const searchPromise = searchHandler.handleSearch(event);

      // Should show loading state immediately
      expect(searchButton.disabled).toBe(true);
      expect(searchButtonText.classList.contains('d-none')).toBe(true);
      expect(searchButtonSpinner.classList.contains('d-none')).toBe(false);

      await searchPromise;

      // Should hide loading state after completion
      expect(searchButton.disabled).toBe(false);
      expect(searchButtonText.classList.contains('d-none')).toBe(false);
      expect(searchButtonSpinner.classList.contains('d-none')).toBe(true);
    });
  });

  describe('Error Handling', () => {
    test('should show error alert', () => {
      searchHandler.showError('Test error message');

      const errorAlert = document.getElementById('searchError');
      expect(errorAlert).toBeTruthy();
      expect(errorAlert.innerHTML).toContain('Test error message');
      expect(errorAlert.classList.contains('alert-danger')).toBe(true);
    });

    test('should auto-remove error alert after 5 seconds', (done) => {
      searchHandler.showError('Test error message');

      const errorAlert = document.getElementById('searchError');
      expect(errorAlert).toBeTruthy();

      setTimeout(() => {
        expect(document.getElementById('searchError')).toBeFalsy();
        done();
      }, 5100);
    });
  });

  describe('Utility Functions', () => {
    test('should escape HTML correctly', () => {
      const result = searchHandler.escapeHtml('<script>alert("xss")</script>');
      expect(result).toBe('&lt;script&gt;alert("xss")&lt;/script&gt;');
    });

    test('should refresh search when called', async () => {
      searchHandler.currentSearchTerm = 'test';
      
      const mockResponse = {
        ok: true,
        json: jest.fn().mockResolvedValue({
          results: [],
          totalCount: 0,
          currentPage: 1,
          totalPages: 0
        })
      };
      fetch.mockResolvedValue(mockResponse);

      await searchHandler.refreshSearch();

      expect(fetch).toHaveBeenCalled();
    });
  });
});