# Flight Search System

A working flight search system implementing a combination of design patterns including Scatter-Gather, Correlation ID, and Aggregator patterns within the hexagonal architecture pattern boundary.

## The Problem

Users search for flights across multiple providers.
Each provider responds at different times and may fail independently.

The system:
- dispatches search requests to all providers
- collects responses asynchronously
- returns progressive results to the client

## User Interface

Users see results as soon as the first provider responds. Waiting for all sources is unacceptable.

<img width="1516" height="780" alt="flight_search_0" src="https://github.com/user-attachments/assets/332de906-ea88-40c5-a668-34aded958ea0" />  
<br><br>
<img width="1462" height="813" alt="flight_search_1" src="https://github.com/user-attachments/assets/5dbedec8-3a11-412c-9d44-30706aacdd0e" />

## Project Structure

```
FlightSearch/
├── FlightSearch.Core/    
│   ├── Domain/          # Business entities and aggregates
│   └── Application/      
│       ├── DataSets/      # Data transfer objects
│       ├── Ports/
│       │   ├── Incoming/   # Primary ports (driving)
│       │   └── Outgoing/   # Secondary ports (driven)
│       └── UseCases/  # Application services
├── FlightSearch.Host/ 
│   ├── Controllers/       # HTTP endpoints
│   ├── Services/   # Worker services
│   └── wwwroot/flight-search.html      # HTML test interface
├── FlightSearch.Adapters.Database/        # Database adapter
├── FlightSearch.Adapters.SNS/       # Message dispatch adapter
├── FlightSearch.Adapters.SQS/  # Message response adapter
├── FlightSearch.Adapters.Amadeus/         # Amadeus provider adapter
├── FlightSearch.Adapters.Skyscanner/      # Skyscanner provider adapter
└── FlightSearch.Adapters.Aviationstack/   # Aviationstack provider adapter
```

## Quick Start

### Prerequisites

- **.NET 8 SDK** or later
- **Visual Studio 2022** or **VS Code** (optional)

### Running the Application

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd FlightSearch
   ```

2. **Build the solution**
   ```bash
   dotnet build
   ```

3. **Run the Host project**
   
```bash
   cd FlightSearch.Host
   dotnet run
   ```

4. **Access the application**
   - **API**: http://localhost:49603 (Swagger UI)
   - **HTML Interface**: http://localhost:49603/flight-search.html or open the html file directly. Click the "Seach Flights" and watch the real-time progress.

## Simulation Notes

All flight providers are simulated.
Response delays and failure rates can be configured in appsettings.json.

## Architectural Reasoning

This repository shows the code structure and working implementation. The architectural reasoning behind this solution is documented in a separate PDF.

→ [Architectural Reasoning Behind Building A Flight Search System](https://www.justifiedcode.com/flight-search-system/)

## Use This Code

Feel free to download, study, and use this code as a reference for implementing a flight search system.

## Learning Resources

- [AWS Scatter-Gather Pattern](https://docs.aws.amazon.com/prescriptive-guidance/latest/cloud-design-patterns/scatter-gather.html)

