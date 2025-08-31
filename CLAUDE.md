# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Language Guidelines

- **User Communication**: Always communicate with users in Japanese (日本語)
- **Code Comments / Documents**: Write all code comments and documents in Japanese (日本語)
- **Test Naming**: Write test method names in Japanese to make tests more readable and identifiable
- **For AI Documentation**: This CLAUDE.md file should be in English for broader accessibility

## Project Overview

DmdataSharp is an unofficial .NET library for integrating with the dmdata.jp API service. It provides a comprehensive client for accessing Japan Meteorological Agency data (earthquake information, weather warnings, tsunami information, etc.) through both REST API and WebSocket connections.

## Build and Development Commands

This is a standard .NET solution that can be built using standard .NET commands:

- `dotnet build` - Build the solution
- `dotnet build --configuration Release` - Release build
- `dotnet test` - Run tests (if test files exist in the Tests project)
- `dotnet pack` - Create NuGet package (configured in DmdataSharp.csproj)

The main library project is located at `src/DmdataSharp/DmdataSharp.csproj`, and the test/sample project is at `Tests/Tests.csproj`.

## Architecture and Key Components

### Core Architecture
- **Builder Pattern**: `DmdataApiClientBuilder` serves as the primary entry point for creating API clients
- **Authentication Layer**: Supports API key authentication (`ApiKeyAuthenticator`) and OAuth authentication (`OAuthAuthenticator`)
- **Client Classes**: `DmdataV2ApiClient` for REST API and `DmdataV2Socket` for WebSocket connections

### Key Classes and Their Responsibilities

#### DmdataApiClientBuilder (`src/DmdataSharp/DmdataApiClientBuilder.cs`)
- Central factory for creating configured API clients
- Handles authentication method selection (API key, OAuth, custom)
- Configures HttpClient with timeout, user agent, etc.
- Key methods: `UseApiKey()`, `UseOAuth()`, `BuildV2ApiClient()`

#### DmdataV2ApiClient (`src/DmdataSharp/DmdataV2ApiClient.cs`)
- Main REST client for dmdata.jp V2 API
- Provides methods for contract information, telegram lists, earthquake data, and earthquake early warning data
- Handles stream-based telegram retrieval with proper resource management
- Key methods: `GetTelegramListAsync()`, `GetTelegramStreamAsync()`, `GetEarthquakeEventsAsync()`

#### DmdataV2Socket (`src/DmdataSharp/DmdataV2Socket.cs`)
- WebSocket client for real-time data streaming
- Event-driven architecture with Connected, DataReceived, Error, and Disconnected events
- Automatic ping/pong handling and connection management
- Supports custom endpoint connections for redundancy

### Authentication System
- **OAuth Support**: Full OAuth 2.0 implementation including DPoP support (experimental)
- **API Key**: Simple API key authentication
- **Credentials**: `OAuthRefreshTokenCredential`, `OAuthClientCredential` for token management

### Data Models
- **API Responses**: Strongly typed response classes located in `ApiResponses/V2/`
- **WebSocket Messages**: Located in `WebSocketMessages/V2/` for real-time data processing
- **Parameters**: Request parameter classes located in `ApiParameters/V2/`

### Exception Handling
Custom exception hierarchy in `Exceptions/`:
- `DmdataException` (base class)
- `DmdataAuthenticationException`, `DmdataForbiddenException`, `DmdataUnauthorizedException`
- `DmdataApiErrorException`, `DmdataRateLimitExceededException`, `DmdataApiTimeoutException`

## Target Frameworks
The library supports multiple .NET targets:
- .NET 7.0
- .NET 6.0
- .NET Standard 2.0
- .NET Framework 4.7.2

## Important Implementation Notes

### Stream Management
- `GetTelegramStreamAsync()` returns a disposable stream that must be properly disposed
- For convenience, `GetTelegramStringAsync()` automatically handles stream disposal

### Rate Limiting
- The library has an `AllowPararellRequest` property to control concurrent requests
- Default is false to prevent rate limit violations
- Manual rate limiting handling is required on the application side

### WebSocket Connection Management
- WebSocket connections require proper disposal
- Auto-reconnection is not implemented - must be handled on the application side
- Supports custom endpoints for geographic redundancy

### OAuth Flow
- Supports authorization code flow with PKCE
- DPoP support is available but experimental
- Includes refresh token management
- Local HTTP server for OAuth callback handling

## Testing Guidelines

### Test Focus
- Focus tests on the core functionality and business logic of classes
- Avoid testing infrastructure code such as event handlers, ResetEvent, or other implementation details
- Test the public API behavior and expected outcomes rather than internal mechanisms
- Prioritize testing actual API interactions, data processing, and error handling scenarios

### Test Organization and Best Practices
- **Consolidate Related Tests**: Group similar test scenarios into comprehensive test methods rather than creating multiple small tests
- **Avoid Redundant Testing**: Do not create separate tests for simple property setters/getters or method chaining that returns the same instance
- **Focus on Integration**: Create tests that verify complete workflows (e.g., builder pattern with full configuration) rather than individual method calls
- **Meaningful Test Names**: Use descriptive Japanese test method names that clearly indicate the scenario being tested
- **Efficient Test Structure**: Use test data arrays or loops to test multiple similar scenarios in a single test method when appropriate

### What NOT to Test
- Simple property assignments that only set and return values
- Method chaining that returns `this` (fluent interface patterns)
- Initial state verification of simple properties
- Infrastructure implementation details (ManualResetEventSlim, internal timers, etc.)
- Constant value definitions
- Enum existence checks

### What TO Test
- Complete business workflows and use cases
- Error handling and exception scenarios
- API response parsing and data transformation
- Authentication and authorization flows
- Resource disposal and cleanup
- Message validation and processing logic
- Integration between multiple components

### Test Data and URL Guidelines
**CRITICAL**: When creating test data, NEVER use real production URLs or endpoints to prevent accidental external requests:

#### Forbidden Test Data
- **NEVER** use actual dmdata.jp URLs: `api.dmdata.jp`, `data.api.dmdata.jp`, `ws.api.dmdata.jp`, etc.
- **NEVER** use real endpoint hostnames: `ws-tokyo.api.dmdata.jp`, `ws-osaka.api.dmdata.jp`, etc.

#### Required Test Data Patterns
- **Use modified/shortened URLs**: `wsdmdatajp`, `customapidmdatajp`, `customdataapidmdatajp`
- **Use modified endpoints**: `tokyodmdatajp`, `osakadmdatajp` instead of real hostnames
- **Use examplecom**: For general URL testing where domain doesn't matter
- **Use invalid/test schemes**: `invalid-endpoint`, `test-endpoint` for error testing

#### Examples
```csharp
// ✅ GOOD - Modified URLs that won't trigger real requests
var mockResponse = new SocketStartResponse
{
    Websocket = new SocketStartResponse.Info
    {
        Url = "wss://wsdmdatajp/v2/socket"  // Modified, safe
    }
};

// ❌ BAD - Real production URL that could trigger requests
var badResponse = new SocketStartResponse 
{
    Websocket = new SocketStartResponse.Info 
    {
        Url = "wss://ws.api.dmdata.jp/v2/socket"  // Real URL - FORBIDDEN
    }
};
```

This prevents accidental external HTTP/WebSocket requests during testing and protects against unintended API calls to production services.

### Example of Good Test Structure
```csharp
[Fact(DisplayName = "ビルダーパターンのメソッドチェーニングが正常に動作する")]
public void BuilderPattern_MethodChaining_WorksCorrectly()
{
    // Tests complete builder configuration workflow
    // Verifies multiple settings in a single integrated test
}

[Fact(DisplayName = "APIクライアントのメソッド呼び出しが例外をスローしない")]
public void ApiClientMethods_DoNotThrowExceptions()
{
    // Tests multiple API methods using arrays/loops
    // Consolidates parameter validation testing
}
```