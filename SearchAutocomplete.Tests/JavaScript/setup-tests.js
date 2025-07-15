// Setup file for Jest tests
require('@testing-library/jest-dom');

// Mock fetch globally
global.fetch = jest.fn();

// Mock console methods to avoid noise in tests
global.console = {
  ...console,
  log: jest.fn(),
  debug: jest.fn(),
  info: jest.fn(),
  warn: jest.fn(),
  error: jest.fn(),
};

// Mock window.bootstrap for Bootstrap components
global.bootstrap = {
  Tooltip: jest.fn(),
  Popover: jest.fn(),
};

// Reset mocks before each test
beforeEach(() => {
  fetch.mockClear();
  console.log.mockClear();
  console.debug.mockClear();
  console.info.mockClear();
  console.warn.mockClear();
  console.error.mockClear();
});