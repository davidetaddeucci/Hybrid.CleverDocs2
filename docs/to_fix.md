# Pending Fixes and To-Do Items

This document captures the current issues and remaining tasks to address in the WebServices backend for SciPhi AI R2R.

## HealthChecks
- Fix the CS1503 overload error in `AddRabbitMQ` (string vs. `Func<IServiceProvider, IConnection>`).

## Code Cleanup
- Remove duplicate `using` directives in `Program.cs` and all Controllers (warnings CS0105).
- Eliminate stray and orphaned code blocks left from previous health-check implementations.

## Nullability
- Address null-reference warnings (CS8603) in client classes (e.g., return value warnings in service wrappers).

## Module Stubbing
- Continue stubbing and aligning other modules’ DTOs and consumers:
  - DocumentClient
  - GraphClient
  - SearchClient
  - MaintenanceClient
  - OrchestrationClient
  - LocalLLMClient
  - ValidationClient
  - McpTuningClient
  - Any newly added endpoints

## Testing
- Plan and implement unit tests for:
  - Health-check endpoints
  - Consumer logic (`IngestionChunkConsumer`)
  - DTO mappings and request validations

## Next Steps
1. Clean up `Program.cs` to consolidate a single `AddHealthChecks()` chain.
2. Rebuild and verify clean compile.
3. Continue incremental module implementation.
