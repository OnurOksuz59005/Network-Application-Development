# Currency Exchange Application

This project provides a simple currency exchange application that fetches current exchange rates from the National Bank of Poland (NBP) API.

## Project Structure

The solution now consists of a single simplified project:

**SimpleCurrencyExchangeApp** - A standalone console application that directly accesses the NBP API
   - User-friendly console interface
   - Displays exchange rates and conversion examples
   - Handles API communication errors gracefully

## Features

- Fetches real-time exchange rates from the NBP API
- Provides detailed error handling
- Console application with colorful and intuitive user interface
- Support for multiple currency codes (EUR, USD, GBP, etc.)
- Shows currency conversion examples

## Technical Implementation

The application directly connects to the NBP API to fetch current exchange rates. It uses:

- HttpClient for API communication
- Newtonsoft.Json for parsing JSON responses
- Async/await pattern for non-blocking API calls

## How to Run

1. Build the solution using Visual Studio or MSBuild.

2. Run the application:
   - Navigate to the SimpleCurrencyExchangeApp project directory
   - Run the executable: `SimpleCurrencyExchangeApp.exe`

3. In the application:
   - Enter a currency code (e.g., EUR, USD, GBP) to get the current exchange rate
   - Type 'list' to see available currency codes
   - Type 'exit' to quit the application

## API Reference

This project uses the National Bank of Poland API to fetch exchange rates. 

Endpoint used: `http://api.nbp.pl/api/exchangerates/rates/a/{currency_code}/?format=json`

The full documentation is available at: http://api.nbp.pl/en.html

## Requirements

- .NET Framework 4.8
- Windows operating system (for WCF hosting)

## Future Enhancements

- Add support for historical exchange rates
- Implement caching to reduce API calls
- Create a graphical user interface client
- Add support for currency conversion calculations
- Implement a RESTful API alongside the WCF service
