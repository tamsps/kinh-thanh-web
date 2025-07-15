// Load the filters functionality
const fs = require('fs');
const path = require('path');

// Read the filters.js file
const filtersJs = fs.readFileSync(
  path.join(__dirname, '../../../SearchAutocomplete/wwwroot/js/filters.js'),
  'utf8'
);

// Execute the JavaScript code in the test environment
eval(filtersJs);

describe('FilterManager', () => {
  let filterManager;

  beforeEach(() => {
    // Set up DOM elements
    document.body.innerHTML = `
      <div>
        <div id="typeFilters"></div>
        <div id="authorFilters"></div>
        <div id="sectionFilters">
          <div class="form-check">
            <input class="form-check-input filter-checkbox" type="checkbox" value="1" id="section_1" data-filter-type="section">
            <label class="form-check-label" for="section_1">Section 1</label>
          </div>
          <div class="form-check">
            <input class="form-check-input filter-checkbox" type="checkbox" value="2" id="section_2" data-filter-type="section">
            <label class="form-check-label" for="section_2">Section 2</label>
          </div>
        </div>
        <button id="clearFilters">Clear Filters</button>
        <div id="activeFilters"></div>
      </div>
    `;

    // Mock window objects
    global.window = {
      autocompleteHandler: {
        input: { value: '', dispatchEvent: jest.fn() }
      },
      searchHandler: {
        currentSearchTerm: '',
        refreshSearch: jest.fn()
      }
    };

    filterManager = new FilterManager();
  });

  afterEach(() => {
    document.body.innerHTML = '';
    fetch.mockClear();
  });

  describe('Initialization', () => {
    test('should initialize with empty filters', () => {
      expect(filterManager.activeFilters.types).toEqual([]);
      expect(filterManager.activeFilters.authors).toEqual([]);
      expect(filterManager.activeFilters.sectionIds).toEqual([]);
    });

    test('should attach event listeners to existing checkboxes', () => {
      const checkboxes = document.querySelectorAll('.filter-checkbox');
      expect(checkboxes.length).toBe(2);
      
      // Simulate checkbox change
      const checkbox = checkboxes[0];
      checkbox.checked = true;
      checkbox.dispatchEvent(new Event('change'));

      expect(filterManager.activeFilters.sectionIds).toContain(1);
    });

    test('should attach clear filters button listener', () => {
      const clearButton = document.getElementById('clearFilters');
      const clearSpy = jest.spyOn(filterManager, 'clearAllFilters');

      clearButton.click();

      expect(clearSpy).toHaveBeenCalled();
    });
  });

  describe('Filter Management', () => {
    test('should add section filter when checkbox is checked', () => {
      const checkbox = document.querySelector('[data-filter-type="section"][value="1"]');
      checkbox.checked = true;
      
      filterManager.handleFilterChange({ target: checkbox });

      expect(filterManager.activeFilters.sectionIds).toContain(1);
    });

    test('should remove section filter when checkbox is unchecked', () => {
      // First add the filter
      filterManager.activeFilters.sectionIds.push(1);
      
      const checkbox = document.querySelector('[data-filter-type="section"][value="1"]');
      checkbox.checked = false;
      
      filterManager.handleFilterChange({ target: checkbox });

      expect(filterManager.activeFilters.sectionIds).not.toContain(1);
    });

    test('should add type filter', () => {
      filterManager.addFilter('type', 'TestType');
      expect(filterManager.activeFilters.types).toContain('TestType');
    });

    test('should add author filter', () => {
      filterManager.addFilter('author', 'TestAuthor');
      expect(filterManager.activeFilters.authors).toContain('TestAuthor');
    });

    test('should not add duplicate filters', () => {
      filterManager.addFilter('type', 'TestType');
      filterManager.addFilter('type', 'TestType');
      
      expect(filterManager.activeFilters.types.filter(t => t === 'TestType')).toHaveLength(1);
    });

    test('should remove filters correctly', () => {
      filterManager.activeFilters.types.push('TestType');
      filterManager.activeFilters.authors.push('TestAuthor');
      filterManager.activeFilters.sectionIds.push(1);

      filterManager.removeFilter('type', 'TestType');
      filterManager.removeFilter('author', 'TestAuthor');
      filterManager.removeFilter('section', '1');

      expect(filterManager.activeFilters.types).not.toContain('TestType');
      expect(filterManager.activeFilters.authors).not.toContain('TestAuthor');
      expect(filterManager.activeFilters.sectionIds).not.toContain(1);
    });
  });

  describe('Active Filters Display', () => {
    test('should show "No active filters" when no filters are active', () => {
      filterManager.updateActiveFiltersDisplay();

      const activeFiltersContainer = document.getElementById('activeFilters');
      expect(activeFiltersContainer.innerHTML).toContain('No active filters');
    });

    test('should display active filters correctly', () => {
      filterManager.activeFilters.types.push('TestType');
      filterManager.activeFilters.authors.push('TestAuthor');
      
      filterManager.updateActiveFiltersDisplay();

      const activeFiltersContainer = document.getElementById('activeFilters');
      expect(activeFiltersContainer.innerHTML).toContain('Type: TestType');
      expect(activeFiltersContainer.innerHTML).toContain('Author: TestAuthor');
    });

    test('should create filter tags with remove buttons', () => {
      filterManager.activeFilters.types.push('TestType');
      filterManager.updateActiveFiltersDisplay();

      const activeFiltersContainer = document.getElementById('activeFilters');
      expect(activeFiltersContainer.innerHTML).toContain('remove-filter');
      expect(activeFiltersContainer.innerHTML).toContain('×');
    });
  });

  describe('Filter Removal by Tag', () => {
    test('should remove filter when tag remove button is clicked', () => {
      // Add a type filter
      filterManager.activeFilters.types.push('TestType');
      
      // Create a mock checkbox for the type
      document.body.innerHTML += `
        <input class="filter-checkbox" type="checkbox" value="TestType" data-filter-type="type" checked>
      `;

      filterManager.removeFilterByTag('type', 'TestType');

      expect(filterManager.activeFilters.types).not.toContain('TestType');
    });
  });

  describe('Clear All Filters', () => {
    test('should clear all filters and uncheck all checkboxes', () => {
      // Set up some active filters
      filterManager.activeFilters.types.push('TestType');
      filterManager.activeFilters.authors.push('TestAuthor');
      filterManager.activeFilters.sectionIds.push(1);

      // Check some checkboxes
      const checkboxes = document.querySelectorAll('.filter-checkbox');
      checkboxes.forEach(cb => cb.checked = true);

      filterManager.clearAllFilters();

      expect(filterManager.activeFilters.types).toEqual([]);
      expect(filterManager.activeFilters.authors).toEqual([]);
      expect(filterManager.activeFilters.sectionIds).toEqual([]);

      checkboxes.forEach(cb => {
        expect(cb.checked).toBe(false);
      });
    });
  });

  describe('Dynamic Filter Loading', () => {
    test('should load type filters from API', async () => {
      const mockTypes = ['Type1', 'Type2', 'Type3'];
      const mockResponse = {
        ok: true,
        json: jest.fn().mockResolvedValue(mockTypes)
      };
      fetch.mockResolvedValue(mockResponse);

      await filterManager.loadTypeFilters();

      expect(fetch).toHaveBeenCalledWith('/api/search/filters/types');
      
      const typeFiltersContainer = document.getElementById('typeFilters');
      expect(typeFiltersContainer.innerHTML).toContain('Type1');
      expect(typeFiltersContainer.innerHTML).toContain('Type2');
      expect(typeFiltersContainer.innerHTML).toContain('Type3');
    });

    test('should load author filters from API', async () => {
      const mockAuthors = ['Author1', 'Author2'];
      const mockResponse = {
        ok: true,
        json: jest.fn().mockResolvedValue(mockAuthors)
      };
      fetch.mockResolvedValue(mockResponse);

      await filterManager.loadAuthorFilters();

      expect(fetch).toHaveBeenCalledWith('/api/search/filters/authors');
      
      const authorFiltersContainer = document.getElementById('authorFilters');
      expect(authorFiltersContainer.innerHTML).toContain('Author1');
      expect(authorFiltersContainer.innerHTML).toContain('Author2');
    });

    test('should handle API errors gracefully', async () => {
      fetch.mockRejectedValue(new Error('API Error'));

      await filterManager.loadTypeFilters();
      await filterManager.loadAuthorFilters();

      expect(console.error).toHaveBeenCalledWith('Error loading type filters:', expect.any(Error));
      expect(console.error).toHaveBeenCalledWith('Error loading author filters:', expect.any(Error));
    });
  });

  describe('Filter Updates', () => {
    test('should trigger autocomplete update when search input has text', () => {
      global.window.autocompleteHandler.input.value = 'test search';
      
      filterManager.triggerFilterUpdate();

      expect(global.window.autocompleteHandler.input.dispatchEvent).toHaveBeenCalledWith(expect.any(Event));
    });

    test('should trigger search refresh when there is an active search', () => {
      global.window.searchHandler.currentSearchTerm = 'test';
      
      filterManager.triggerFilterUpdate();

      expect(global.window.searchHandler.refreshSearch).toHaveBeenCalled();
    });

    test('should not trigger updates when no active search or input', () => {
      global.window.autocompleteHandler.input.value = '';
      global.window.searchHandler.currentSearchTerm = '';
      
      filterManager.triggerFilterUpdate();

      expect(global.window.autocompleteHandler.input.dispatchEvent).not.toHaveBeenCalled();
      expect(global.window.searchHandler.refreshSearch).not.toHaveBeenCalled();
    });
  });

  describe('Utility Functions', () => {
    test('should get active filters correctly', () => {
      filterManager.activeFilters.types.push('TestType');
      filterManager.activeFilters.authors.push('TestAuthor');
      filterManager.activeFilters.sectionIds.push(1);

      const filters = filterManager.getActiveFilters();

      expect(filters.types).toContain('TestType');
      expect(filters.authors).toContain('TestAuthor');
      expect(filters.sectionIds).toContain(1);
      
      // Should return a copy, not the original
      expect(filters).not.toBe(filterManager.activeFilters);
    });

    test('should check if has active filters', () => {
      expect(filterManager.hasActiveFilters()).toBe(false);

      filterManager.activeFilters.types.push('TestType');
      expect(filterManager.hasActiveFilters()).toBe(true);
    });

    test('should escape HTML correctly', () => {
      const result = filterManager.escapeHtml('<script>alert("xss")</script>');
      expect(result).toBe('&lt;script&gt;alert("xss")&lt;/script&gt;');
    });

    test('should sanitize ID correctly', () => {
      const result = filterManager.sanitizeId('Test Type & Author!');
      expect(result).toBe('Test_Type___Author_');
    });
  });
});