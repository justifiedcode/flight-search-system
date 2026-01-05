# Flight Search System

A working flight search system built with Hexagonal Architecture and asynchronous processing to aggregate results from multiple providers.

## 🎯 Problem

Users search for flights across multiple providers.
Each provider responds at different times and may fail independently.

The system:
- dispatches search requests to all providers
- collects responses asynchronously
- returns progressive results to the client

## 🏗️ High-Level Structure

This project follows a Hexagonal Architecture structure with clear separation of concerns.

<img width="2003" height="1918" alt="hexagonal_architecture" src="https://github.com/user-attachments/assets/e427f2f5-933f-4459-b646-671f5d896e1f" />

This repository focuses on the final solution and structure. Architectural reasoning is documented separately.

### Core Components

- **Domain Core**: Pure business logic with zero infrastructure dependencies
- **Ports**: Interfaces that define contracts (Incoming & Outgoing)
- **Adapters**: Infrastructure implementations that plug into ports
- **Host**: Composition root that wires everything together

## 📁 Project Structure

```
FlightSearch/
├── FlightSearch.Core/     # 🔵 Domain & Application Core
│   ├── Domain/          # Business entities and aggregates
│   └── Application/      # Use cases and ports
│       ├── DataSets/      # Data transfer objects
│       ├── Ports/
│       │   ├── Incoming/   # Primary ports (driving)
│       │   └── Outgoing/   # Secondary ports (driven)
│       └── UseCases/  # Application services
├── FlightSearch.Host/    # 🏠 Composition Root & Web API
│   ├── Controllers/       # HTTP driving adapters
│   ├── Services/   # Background driving adapters
│ └── wwwroot/flight-search.html      # HTML test interface
├── FlightSearch.Adapters.Database/        # 💾 Database driven adapter
├── FlightSearch.Adapters.SNS/       # 📡 Message dispatch adapter
├── FlightSearch.Adapters.SQS/  # 📥 Message response adapter
├── FlightSearch.Adapters.Amadeus/         # 🛩️ Amadeus provider adapter
├── FlightSearch.Adapters.Skyscanner/      # ✈️ Skyscanner provider adapter
└── FlightSearch.Adapters.Aviationstack/   # 🛫 Aviationstack provider adapter
```

## 🚀 Quick Start

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
   - **API**: https://localhost:5001 (Swagger UI)
   - **HTML Interface**: https://localhost:5001/flight-search.html or open the html file directly

### Testing with HTML Interface

1. **Open your browser** and navigate to `https://localhost:5001/flight-search.html`
2. **Use default values** (JFK → LAX, +7 days, 2 passengers) or customize
3. **Click "Search Flights"** and watch the real-time progress
4. **Observe the scatter-gather pattern**:
   - **8-12s**: Amadeus results appear (33% complete)
   - **15-20s**: Skyscanner results appear (66% complete) 
   - **25-30s**: Aviationstack results appear (100% complete)

## 🧪 Simulation Notes

All flight providers are simulated.
Response delays and failure rates can be configured in appsettings.json.

## Architectural Reasoning

This repository shows the final system structure and working implementation.

The architectural decisions, trade-offs, and reasoning behind this solution are documented separately in a companion PDF.

→ Architectural reasoning and trade-offs (PDF)

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

**Copyright (c) 2026 [Justifide Code, LLC]**

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
