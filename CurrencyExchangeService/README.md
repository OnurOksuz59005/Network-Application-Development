# Currency Exchange WCF Service

This project implements a currency exchange service using Windows Communication Foundation (WCF) that fetches current exchange rates from the National Bank of Poland (NBP) API. It was created as part of the Network Application Development course.

## Project Structure

The solution consists of two projects:

1. **CurrencyExchangeService** - A WCF service that provides exchange rate information
   - Implements the `ICurrencyExchangeService` interface
   - Provides a method to get the current exchange rate for a specified currency
   - Fetches data from the NBP API
   - Includes error handling and logging

2. **CurrencyExchangeClient** - A console application that consumes the WCF service
   - User-friendly console interface
   - Displays exchange rates and conversion examples
   - Handles service communication errors gracefully
   - Falls back to direct API access if the service is unavailable

## Features

- Fetches real-time exchange rates from the NBP API
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

2. Start the WCF service host:
   - Navigate to the CurrencyExchangeService project directory
   - Run the executable: `CurrencyExchangeService.exe`
   - You should see a console window indicating the service is running

3. In a separate window, start the client application:
   - Navigate to the CurrencyExchangeClient project directory
   - Run the executable: `CurrencyExchangeClient.exe`
   - The client will attempt to connect to the service

4. In the client application:
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