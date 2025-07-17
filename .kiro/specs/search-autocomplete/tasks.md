# Implementation Plan

- [x] 1. Set up project structure and core interfaces
  - Create ASP.NET Core 9 project with Clean Architecture folder structure
  - Define repository interfaces for data abstraction (IKinhThanhRepository, ISectionRepository)
  - Create domain entities (KinhThanh, Section) with proper relationships
  - _Requirements: 4.1, 4.2, 4.3_

- [x] 2. Implement domain models and validation
- [x] 2.1 Create KinhThanh and Section domain entities
  - Write KinhThanh class with Id, Content, SectionId, From, To, Type, Author properties
  - Write Section class with Id, Name, Description, and KinhThanhs collection
  - Add data annotations for validation
  - _Requirements: 4.4_

- [x] 2.2 Create DTOs for data transfer
  - Implement SearchRequestDto with SearchTerm, Filters, Page, PageSize properties
  - Implement SearchResultDto with Results, TotalCount, CurrentPage, TotalPages
  - Implement AutocompleteRequestDto with SearchTerm, Filters, MaxResults
  - Create SearchFilters class with Types, Authors, SectionIds lists
  - _Requirements: 4.4_

- [x] 3. Implement data access layer
- [x] 3.1 Create Entity Framework DbContext
  - Set up DbContext with KinhThanh and Section DbSets
  - Configure entity relationships and constraints
  - Add database indexes for search performance
  - _Requirements: 4.3_

- [x] 3.2 Implement repository pattern
  - Create concrete KinhThanhRepository implementing IKinhThanhRepository
  - Implement SearchAsync method with filtering and pagination
  - Implement GetAutocompleteSuggestionsAsync with search term matching
  - Create SectionRepository implementing ISectionRepository
  - _Requirements: 4.3_

- [x] 4. Create application services
- [x] 4.1 Implement SearchService
  - Create SearchService implementing ISearchService interface
  - Implement SearchAsync method orchestrating repository calls
  - Add search result mapping from entities to DTOs
  - Implement GetSearchCountAsync for pagination
  - _Requirements: 4.2, 4.5_

- [x] 4.2 Implement AutocompleteService
  - Create AutocompleteService implementing IAutocompleteService
  - Implement GetSuggestionsAsync with filter application
  - Add logic to limit suggestions to maximum 10 results
  - _Requirements: 1.1, 1.2, 3.1, 3.2_

- [x] 5. Create API controllers
- [x] 5.1 Implement SearchController
  - Create SearchController with dependency injection for SearchService
  - Add POST endpoint for search with SearchRequestDto parameter
  - Implement proper HTTP status codes and error responses
  - Add validation for empty search terms
  - _Requirements: 2.1, 2.3, 4.5_

- [x] 5.2 Implement AutocompleteController
  - Create AutocompleteController with AutocompleteService dependency
  - Add GET endpoint for autocomplete suggestions
  - Implement minimum 2-character validation
  - Add proper error handling and status codes
  - _Requirements: 1.1, 1.5_

- [x] 6. Implement error handling and logging
- [x] 6.1 Create global exception middleware
  - Implement middleware to catch unhandled exceptions
  - Add structured logging with Serilog
  - Create custom exception types (SearchException, AutocompleteException)
  - Implement user-friendly error message mapping
  - _Requirements: 7.1, 7.2, 7.5_

- [x] 6.2 Add retry logic and resilience
  - Implement retry logic with exponential backoff for database operations
  - Add circuit breaker pattern for external dependencies
  - Create performance logging for search operations
  - _Requirements: 7.3, 7.4_

- [x] 7. Create responsive web interface
- [x] 7.1 Build search form with Razor views
  - Create main search page with textbox, search button, and filter checkboxes
  - Implement responsive layout with mobile-first approach
  - Add loading indicators and validation messages
  - Group filters by Type, Author, and Section
  - _Requirements: 2.1, 2.4, 3.3, 5.1_

- [x] 7.2 Implement JavaScript autocomplete functionality
  - Create autocomplete handler with 300ms debouncing
  - Implement dropdown display with keyboard navigation (arrow keys, Enter, Escape)
  - Add request cancellation for rapid typing
  - Display "No suggestions found" when no matches exist
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 5.4_

- [x] 7.3 Add search results display and pagination
  - Create results display component with pagination controls
  - Implement page navigation maintaining search criteria and filters
  - Add "No results found" message for empty results
  - Display current page and total pages information
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 8. Implement filter functionality
- [x] 8.1 Create filter management system
  - Implement real-time filter updates affecting autocomplete
  - Add active filter display showing currently applied filters
  - Create filter state management in JavaScript
  - Apply filters to both autocomplete and search results
  - _Requirements: 3.1, 3.2, 3.4, 3.5_

- [x] 8.2 Add responsive filter interface
  - Implement collapsible filter section for mobile devices
  - Ensure touch-friendly controls with minimum 44px targets
  - Add filter clear functionality
  - _Requirements: 5.1, 5.2_

- [x] 9. Create comprehensive test suite
- [x] 9.1 Write unit tests for domain and application layers
  - Create unit tests for KinhThanh and Section entities using NUnit
  - Test SearchService and AutocompleteService with mocked dependencies
  - Add tests for DTO validation and mapping
  - Achieve minimum 80% code coverage for search components
  - _Requirements: 8.1, 8.5_

- [x] 9.2 Write integration tests for API endpoints
  - Create integration tests for SearchController endpoints
  - Test AutocompleteController with various input scenarios
  - Add tests for error handling and validation scenarios
  - Test pagination and filtering functionality
  - _Requirements: 8.2_

- [x] 9.3 Write JavaScript tests for client-side functionality
  - Test autocomplete debouncing and cancellation logic
  - Test keyboard navigation and dropdown interactions
  - Add tests for filter management and state updates
  - Test responsive behavior and mobile interactions
  - _Requirements: 8.3_

- [x] 9.4 Create performance and error handling tests
  - Test search performance with large datasets
  - Add tests for exception handling and logging scenarios
  - Test retry logic and circuit breaker functionality
  - Verify sub-200ms autocomplete response times
  - _Requirements: 8.4, 5.3_

- [x] 10. Configure dependency injection and startup
  - Register all services and repositories in Program.cs
  - Configure Entity Framework with connection string
  - Set up Serilog logging configuration
  - Add CORS configuration for API endpoints
  - _Requirements: 4.2_

- [x] 11. Enhance filter endpoints with dynamic data
  - Replace mock data in SearchController filter endpoints with actual database queries
  - Implement GetDistinctTypesAsync method in KinhThanhRepository
  - Implement GetDistinctAuthorsAsync method in KinhThanhRepository
  - Update SearchController to use repository methods for dynamic filter data
  - _Requirements: 3.1, 3.2_

- [x] 12. Add database seeding and sample data
  - Create database seeding mechanism for development and testing
  - Add sample KinhThanh and Section data for demonstration
  - Implement data migration scripts if needed
  - Ensure proper database initialization in Program.cs
  - _Requirements: 4.3_

- [x] 13. Migrate to JSON data source with new filtering structure


  - Update data model to support book_name, book_type, and chapter number fields
  - Modify DatabaseSeeder to read from kinh_thanh_full.json instead of hardcoded data
  - Update filtering logic to work with book_name and book_type instead of current fields
  - Adapt search functionality to work with chapter-based structure
  - Update filter endpoints to return book_name and book_type options
  - _Requirements: 3.1, 3.2, 4.3_