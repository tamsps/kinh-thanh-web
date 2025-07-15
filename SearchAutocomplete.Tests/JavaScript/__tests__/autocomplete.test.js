// Load the autocomplete functionality
const fs = require('fs');
const path = require('path');

// Read the autocomplete.js file
const autocompleteJs = fs.readFileSync(
  path.join(__dirname, '../../../SearchAutocomplete/wwwroot/js/autocomplete.js'),
  'utf8'
);

// Execute the JavaScript code in the test environment
eval(autocompleteJs);

describe('AutocompleteHandler', () => {
  let inputElement, dropdownElement, loadingElement, autocompleteHandler;

  beforeEach(() => {
    // Set up DOM elements
    document.body.innerHTML = `
      <div>
        <input type="text" id="searchInput" />
        <div id="autocompleteDropdown"></div>
        <div id="loadingIndicator" class="d-none"></div>
      </div>
    `;

    inputElement = document.getElementById('searchInput');
    dropdownElement = document.getElementById('autocompleteDropdown');
    loadingElement = document.getElementById('loadingIndicator');

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

    autocompleteHandler = new AutocompleteHandler(inputElement, dropdownElement, loadingElement);
  });

  afterEach(() => {
    document.body.innerHTML = '';
    jest.clearAllTimers();
  });

  describe('Initialization', () => {
    test('should initialize with correct elements', () => {
      expect(autocompleteHandler.input).toBe(inputElement);
      expect(autocompleteHandler.dropdown).toBe(dropdownElement);
      expect(autocompleteHandler.loading).toBe(loadingElement);
      expect(autocompleteHandler.selectedIndex).toBe(-1);
      expect(autocompleteHandler.suggestions).toEqual([]);
    });

    test('should attach event listeners', () => {
      const inputSpy = jest.spyOn(inputElement, 'addEventListener');
      const documentSpy = jest.spyOn(document, 'addEventListener');

      new AutocompleteHandler(inputElement, dropdownElement, loadingElement);

      expect(inputSpy).toHaveBeenCalledWith('input', expect.any(Function));
      expect(inputSpy).toHaveBeenCalledWith('keydown', expect.any(Function));
      expect(inputSpy).toHaveBeenCalledWith('blur', expect.any(Function));
      expect(inputSpy).toHaveBeenCalledWith('focus', expect.any(Function));
      expect(documentSpy).toHaveBeenCalledWith('click', expect.any(Function));
    });
  });

  describe('Input Handling', () => {
    test('should not trigger suggestions for input less than 2 characters', () => {
      const getSuggestionsSpy = jest.spyOn(autocompleteHandler, 'getSuggestions');

      inputElement.value = 'a';
      inputElement.dispatchEvent(new Event('input'));

      expect(getSuggestionsSpy).not.toHaveBeenCalled();
      expect(dropdownElement.style.display).toBe('none');
    });

    test('should debounce input and trigger suggestions after 300ms', (done) => {
      const getSuggestionsSpy = jest.spyOn(autocompleteHandler, 'getSuggestions').mockImplementation(() => {});

      inputElement.value = 'test';
      inputElement.dispatchEvent(new Event('input'));

      // Should not be called immediately
      expect(getSuggestionsSpy).not.toHaveBeenCalled();

      // Should be called after 300ms
      setTimeout(() => {
        expect(getSuggestionsSpy).toHaveBeenCalledWith('test');
        done();
      }, 350);
    });

    test('should cancel previous request when new input is received', () => {
      const mockAbort = jest.fn();
      autocompleteHandler.currentRequest = { abort: mockAbort };

      inputElement.value = 'new input';
      inputElement.dispatchEvent(new Event('input'));

      expect(mockAbort).toHaveBeenCalled();
    });
  });

  describe('Keyboard Navigation', () => {
    beforeEach(() => {
      autocompleteHandler.suggestions = ['suggestion 1', 'suggestion 2', 'suggestion 3'];
      autocompleteHandler.showDropdown();
    });

    test('should navigate down with ArrowDown key', () => {
      const event = new KeyboardEvent('keydown', { key: 'ArrowDown' });
      const preventDefaultSpy = jest.spyOn(event, 'preventDefault');

      inputElement.dispatchEvent(event);

      expect(preventDefaultSpy).toHaveBeenCalled();
      expect(autocompleteHandler.selectedIndex).toBe(0);
    });

    test('should navigate up with ArrowUp key', () => {
      autocompleteHandler.selectedIndex = 1;

      const event = new KeyboardEvent('keydown', { key: 'ArrowUp' });
      const preventDefaultSpy = jest.spyOn(event, 'preventDefault');

      inputElement.dispatchEvent(event);

      expect(preventDefaultSpy).toHaveBeenCalled();
      expect(autocompleteHandler.selectedIndex).toBe(0);
    });

    test('should select current suggestion with Enter key', () => {
      autocompleteHandler.selectedIndex = 1;
      const selectSuggestionSpy = jest.spyOn(autocompleteHandler, 'selectSuggestion');

      const event = new KeyboardEvent('keydown', { key: 'Enter' });
      const preventDefaultSpy = jest.spyOn(event, 'preventDefault');

      inputElement.dispatchEvent(event);

      expect(preventDefaultSpy).toHaveBeenCalled();
      expect(selectSuggestionSpy).toHaveBeenCalledWith('suggestion 2');
    });

    test('should hide dropdown with Escape key', () => {
      const event = new KeyboardEvent('keydown', { key: 'Escape' });
      const preventDefaultSpy = jest.spyOn(event, 'preventDefault');
      const blurSpy = jest.spyOn(inputElement, 'blur');

      inputElement.dispatchEvent(event);

      expect(preventDefaultSpy).toHaveBeenCalled();
      expect(blurSpy).toHaveBeenCalled();
      expect(dropdownElement.style.display).toBe('none');
    });
  });

  describe('Suggestion Display', () => {
    test('should display suggestions correctly', () => {
      const suggestions = ['test suggestion 1', 'test suggestion 2'];
      
      autocompleteHandler.displaySuggestions(suggestions);

      expect(dropdownElement.innerHTML).toContain('test suggestion 1');
      expect(dropdownElement.innerHTML).toContain('test suggestion 2');
      expect(dropdownElement.style.display).toBe('block');
    });

    test('should show no results message when no suggestions', () => {
      autocompleteHandler.displaySuggestions([]);

      expect(dropdownElement.innerHTML).toContain('No suggestions found');
      expect(dropdownElement.style.display).toBe('block');
    });

    test('should highlight search term in suggestions', () => {
      inputElement.value = 'test';
      const suggestions = ['this is a test suggestion'];
      
      autocompleteHandler.displaySuggestions(suggestions);

      expect(dropdownElement.innerHTML).toContain('<strong>test</strong>');
    });
  });

  describe('API Integration', () => {
    test('should make fetch request with correct parameters', async () => {
      const mockResponse = {
        ok: true,
        json: jest.fn().mockResolvedValue(['suggestion 1', 'suggestion 2'])
      };
      fetch.mockResolvedValue(mockResponse);

      await autocompleteHandler.getSuggestions('test');

      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/autocomplete/suggestions'),
        expect.objectContaining({
          headers: { 'Accept': 'application/json' }
        })
      );
    });

    test('should handle fetch errors gracefully', async () => {
      fetch.mockRejectedValue(new Error('Network error'));
      const showNoResultsSpy = jest.spyOn(autocompleteHandler, 'showNoResults');

      await autocompleteHandler.getSuggestions('test');

      expect(showNoResultsSpy).toHaveBeenCalled();
      expect(console.error).toHaveBeenCalledWith('Error fetching suggestions:', expect.any(Error));
    });

    test('should handle HTTP errors', async () => {
      const mockResponse = {
        ok: false,
        status: 500
      };
      fetch.mockResolvedValue(mockResponse);
      const showNoResultsSpy = jest.spyOn(autocompleteHandler, 'showNoResults');

      await autocompleteHandler.getSuggestions('test');

      expect(showNoResultsSpy).toHaveBeenCalled();
    });
  });

  describe('Loading States', () => {
    test('should show loading indicator during request', async () => {
      const mockResponse = {
        ok: true,
        json: jest.fn().mockResolvedValue([])
      };
      fetch.mockResolvedValue(mockResponse);

      const promise = autocompleteHandler.getSuggestions('test');

      // Loading should be shown immediately
      expect(loadingElement.classList.contains('d-none')).toBe(false);

      await promise;

      // Loading should be hidden after request
      expect(loadingElement.classList.contains('d-none')).toBe(true);
    });
  });

  describe('Selection', () => {
    test('should populate input with selected suggestion', () => {
      const suggestion = 'selected suggestion';
      
      autocompleteHandler.selectSuggestion(suggestion);

      expect(inputElement.value).toBe(suggestion);
      expect(dropdownElement.style.display).toBe('none');
    });

    test('should trigger input event after selection', () => {
      const inputEventSpy = jest.fn();
      inputElement.addEventListener('input', inputEventSpy);

      autocompleteHandler.selectSuggestion('test');

      expect(inputEventSpy).toHaveBeenCalled();
    });
  });

  describe('Utility Functions', () => {
    test('should escape HTML correctly', () => {
      const result = autocompleteHandler.escapeHtml('<script>alert("xss")</script>');
      expect(result).toBe('&lt;script&gt;alert("xss")&lt;/script&gt;');
    });

    test('should escape regex characters correctly', () => {
      const result = autocompleteHandler.escapeRegex('test.*+?^${}()|[]\\');
      expect(result).toBe('test\\.\\*\\+\\?\\^\\$\\{\\}\\(\\)\\|\\[\\]\\\\');
    });
  });
});