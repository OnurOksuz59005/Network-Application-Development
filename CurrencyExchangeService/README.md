# Currency Exchange WCF Service

This project implements a currency exchange service using Windows Communication Foundation (WCF) that fetches current exchange rates from the National Bank of Poland (NBP) API and stores them in a local database. It was created as part of the Network Application Development course.

## Project Structure

The solution consists of two projects:

1. **CurrencyExchangeService** - A WCF service that provides exchange rate information
   - Implements the `ICurrencyExchangeService` interface
   - Provides methods to get current and historical exchange rates
   - Uses Entity Framework for database operations
   - Caches exchange rates to reduce API calls
   - Fetches data from the NBP API when needed
   - Includes error handling and logging

2. **CurrencyExchangeClient** - A console application that consumes the WCF service
   - User-friendly console interface
   - Displays current exchange rates and conversion examples
   - Shows historical exchange rate data
   - Handles service communication errors gracefully
   - Falls back to direct API access if the service is unavailable

## Features

- Fetches real-time exchange rates from the NBP API
- Stores exchange rates in a local SQL Server database
- Caches exchange rates to reduce API calls and improve performance
- Provides historical exchange rate data
- Implements proper WCF service contract and data contract patterns
- Provides detailed error handling with appropriate fault contracts
- Includes logging of service operations
- Console client with colorful and intuitive user interface
- Support for multiple currency codes (EUR, USD, GBP, etc.)
- Automatic fallback to direct API access if service is unavailable

## Technical Implementation

### Service Contract

```csharp
[ServiceContract]
public interface ICurrencyExchangeService
{
    [OperationContract]
    double GetExchangeRate(string currencyCode);
}
```

### Service Implementation

The service implementation fetches data from the NBP API and returns the current exchange rate for the specified currency.

### Client Implementation

The client application creates a proxy to the WCF service and allows users to query exchange rates for different currencies. It also includes a fallback mechanism to directly access the NBP API if the service is unavailable.

## How to Run

1. First, build the solution using Visual Studio or MSBuild.

2. No additional packages need to be installed. The solution uses an in-memory repository to store exchange rates and history.

3. Start the WCF service host:
   - Navigate to the CurrencyExchangeService project directory: `CurrencyExchangeService\CurrencyExchangeService\bin\Debug`
   - Run the executable: `CurrencyExchangeService.exe`
   - You should see a console window indicating the service is running

4. In a separate window, start the client application:
   - Navigate to the CurrencyExchangeClient project directory: `CurrencyExchangeService\CurrencyExchangeClient\bin\Debug`
   - Run the executable: `CurrencyExchangeClient.exe`
   - The client will attempt to connect to the service

5. In the client application:
   - Enter a currency code (e.g., EUR, USD, GBP) to get the current exchange rate
   - Type 'list' to see available currencies (retrieved from the database)
   - Type 'history' to view historical exchange rates for a currency
   - Type 'exit' to quit the application

## API Reference

This project uses the National Bank of Poland API to fetch exchange rates. 

Endpoints used:
- Current rates: `http://api.nbp.pl/api/exchangerates/rates/a/{currency_code}/?format=json`
- Historical rates: `http://api.nbp.pl/api/exchangerates/rates/a/{currency_code}/last/{days}/?format=json`

The full documentation is available at: http://api.nbp.pl/en.html

## Data Storage

The application uses an in-memory repository to store exchange rates and history. The data structures include:

1. **Exchange Rates Cache** - Stores current exchange rates
   - CurrencyCode (e.g., "EUR")
   - Rate (exchange rate value)
   - FetchDate (when the rate was retrieved)
   - EffectiveDate (the date the rate is valid for)
   - TableNumber (NBP table reference)

2. **Currency Information** - Stores information about currencies
   - CurrencyCode (e.g., "EUR")
   - CurrencyName (e.g., "Euro")

3. **Exchange Rate History** - Stores historical exchange rates
   - CurrencyCode (e.g., "EUR")
   - Rate (exchange rate value)
   - Date (the date the rate is valid for)
   - TableNumber (NBP table reference)

The data is cached in memory with a 24-hour expiration period for current rates. Historical data is fetched from the NBP API when requested and stored in memory for future use.

## Requirements

- .NET Framework 4.8
- Windows operating system (for WCF hosting)
- Newtonsoft.Json (included in the project)