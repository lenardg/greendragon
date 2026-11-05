# GreenDragon agent instructions

## Solution profile

The Green Dragon uses GreenDragon namespaces and project names. It is a single ASP.NET Core Razor Pages web app with VanillaJS/CSS. ASP.NET Core controllers are the default API style (currently health only). Storage: none. Identity: anonymous, inferred for a trusted home network. Orchestration: Aspire (default). No CLI, database, scheduler or timed tasks. Deployment is on the home LAN, inferred from Sonos UPnP connectivity.

The portal provides request-driven speaker discovery, live now-playing cards, playback/mode controls and room volume. Enabled visible browser pages request status periodically; the server has no background polling. Shortcut activation remains future work.

## Sonos and tests

- Never test against live Sonos: no discovery scans, device queries, playback commands, event subscriptions, upstream tests, or smoke tests with Sonos enabled.
- All tests replace ISonosGateway. Keep tests light and focused on our worker, boundaries, page handling and security; assume OpenPhonos works.
- Sonos:Enabled is false by default. Startup, GET, health probes and idle time must never contact devices.
- Preserve the single bounded request queue for discovery, refresh and commands. Only enabled visible browser pages use completion-based refresh timers; no server polling, subscriptions or scheduled work. Cache player IDs/the discovered catalog with shared ASP.NET Core MemoryCache using 24-hour sliding expiration. Other stable metadata retains at least one-hour absolute expiration; live playback state stays fresh.
- Upstream discovery cannot be canceled in flight. Callers can cancel waiting; do not allow another operation to overlap the unfinished upstream operation.
- external/openphonos is an unmodified pinned MIT Git submodule. Application code-style rules do not apply to it. Do not format it or run its tests.
- src/OpenPhonos.* source-build wrappers use .NET 10 and the current central packages. The Sonos wrapper alone suppresses upstream unused-local warnings CS0168/CS0219; no application warnings may be suppressed.

## Composition and configuration

- WebProgram.cs / WebProgram calls ApplicationSetup for setup. Composition lives in Application/, one file per aspect. Setup aspects do not call one another; ApplicationSetup owns ordering.
- Middleware order: forwarded headers; production exception handler and HSTS; HTTPS redirect; security headers; routing; localization; applicable CORS/authentication/authorization; endpoints. Keep Razor Pages antiforgery enabled.
- Configure browser security in Application/WebSecurity.cs. Use CSP, nosniff, strict-origin-when-cross-origin referrer policy and secure production cookies. Razor must encode untrusted values; never use Html.Raw on device metadata.
- API controllers derive from ControllerBase, use ApiController, attribute routes, response metadata and server validation. Infrastructure health stays anonymous and must not query Sonos.
- Configuration precedence: appsettings.json, environment JSON, untracked appsettings.local.json, Development user secrets, environment variables, command line, optional Key Vault last.
- Strongly typed settings belong in Configuration/. Shared settings belong in Core. Feature flags remain boolean, integer or string values in the FeatureFlags section.
- User secrets ID: GreenDragon-web. Key Vault is optional at ConnectionStrings:GreenDragon-keyvault with DefaultAzureCredential.
- Serilog writes only to console and forwards logs to other providers. No bootstrap logger or file sink. Application Insights is conditional on its connection string.
- Register through area ServiceCollectionExtensions and AddArea methods, next to the code they compose. AppHost is exempt and keeps orchestration in AppHost.cs.
- Keep package versions in src/Directory.Packages.props, framework/nullable/usings in src/Directory.Build.props. Stable packages only. Use the .NET CLI for solution/project/package operations.

## UI and identity

- Pages remain thin and call Core business logic. Use server-side validation even when client validation exists.
- Use responsive semantic HTML, VanillaJS, CSS variables, accessible names and visible keyboard focus.
- Support System, Light and Dark themes; English JSON localization is shared by server and client.
- Native forms must work without JavaScript. Disable duplicate submissions while showing accessible loading feedback. Errors must allow retry.
- No authentication or Identity persistence is present. Keep anonymous deployment on the trusted LAN.
- If Identity UI is introduced later, use the dotnet-project authentication reference: DisableRegistration in FeatureFlags defaults false, blocks Register GET/POST with 404 when true, hides links, prevents external-login account creation and has mocked integration tests.

## Documentation and workflow

Use write-documentation for changes to docs/. See [the documentation index](docs/AGENTS.md). Use plan-change for larger changes and dotnet-codereview for reviews. Database and CLI skills are unnecessary for the current profile.

Scripts are not unit tested by default. Any new or modified PowerShell script must use SupportsShouldProcess and guard all side effects; agents may execute it only with -WhatIf. Shell scripts require equivalent --whatif support. Smoke-test hosts directly rather than running scripts live.

Run dotnet format and obtain a zero-warning build after every change. Verification must remain offline with respect to Sonos. Keep bin/, obj/, logs/, artifacts/, appsettings.local.json and runtime data out of Git.

## Commands

Run from the repository root:

- Restore dependency: git submodule update --init --recursive
- Restore tools: dotnet tool restore
- Format: dotnet format src/GreenDragon.slnx --exclude external
- Verify format: dotnet format src/GreenDragon.slnx --exclude external --verify-no-changes
- Build: dotnet build src/GreenDragon.slnx
- Tests: dotnet test --solution src/GreenDragon.slnx
- Audit updates: dotnet outdated src/GreenDragon.slnx --pre-release Never --fail-on-updates
- Audit vulnerabilities: dotnet list src/GreenDragon.slnx package --vulnerable --include-transitive
- Run for normal development: aspire run --apphost src/GreenDragon.AppHost/GreenDragon.AppHost.csproj (Podman preferred; no container resources currently)
- Safe smoke start: set Sonos__Enabled=false and run the Web host directly with dotnet run --project src/GreenDragon.Web --launch-profile https


## Additional rules

These rules may not apply during scaffolding, but they must be copied into AGENTS.md.

### Business logic

* Business logic should live in the Core library.
* Business logic classes use the `BL` suffix.
* Every single BL class should get an interface for testability.
* Example:
  * `FiscalYearBL` / `IFiscalYearBL`
* Put business logic classes in the `BusinessLogic/` folder.
  * If the logic becomes complex, or if there are several areas of the solution, we can group these using subfolders.
* Save an AGENTS.md file in the root of `{productName}.Core` that tells how these files should be named, stored and
  handled.

### Business rules

* Business rules, like time limits, retry counts, etc., should live in a separate `BusinessRules` class in the Core
  project.
* Rules can be grouped by subclasses.
  * For now, create a single embedded class called `Example`.
* For example, my code could write `BusinessRules.Bookkeeping.TaxDeadlineReminderDays` to express how many days before
  the tax deadline to remind.
* For some values, where it makes sense, we want to have helper methods to calculate the thing, like deadlines.
  * For example: `BusinessRules.Bookkeeping.GetTaxDeadlineReminder()`, which would calculate a reminder starting today
    (if it makes sense).
  * For example: `BusinessRules.Bookkeeping.GetTaxReturnFilingReminder(taxReturnDeadline)`, if we need a deadline and
    then calculate the reminder from that.
  * All date calculation methods need to accept a `DateTime` or `DateOnly`, so that values can be injected in unit
    tests.

### API interfaces

Applies whenever a web host exposes API endpoints.

* Use ASP.NET Core API controllers unless the user requests another API style. Do not ask the user to choose.
* Controllers live under `/Controllers`, derive from `ControllerBase`, and use `[ApiController]` and attribute routes.
* FastEndpoints and minimal APIs use vertical slices in the web host:
  `/Api/{vertical}/{endpoint}/{endpoint}Endpoint.cs`. Keep endpoint setup and handling in that file, with adjacent
  `{endpoint}Request.cs` and `{endpoint}Response.cs` classes only as required. FastEndpoints validation can live in
  `{endpoint}Validation.cs` beside the endpoint. See [references/backend.md](references/backend.md) for mapping rules.
* API requests should be named `XXXRequest` classes.
* API responses should be named `XXXResponse` classes.
* For controllers, API models live in the web host project under `/Models`, with subfolders as needed. For
  FastEndpoints and minimal APIs, endpoint-specific contracts live beside the endpoint; shared models may use `/Models`.
* Supporting API models use the `AM` (API Model) postfix and supporting input models use `AIM` (API Input Model).
  Request and response classes keep their `Request` / `Response` suffixes without an extra AM/AIM postfix.
* If a model is used both as part of a complex model (Response) and sometimes returned by itself, then the AM model can
  be returned directly.
* Describe API return values in Swagger metadata using the selected style's attributes or endpoint metadata.

### Page models

Applies whenever a web host serves Razor Pages or MVC views.

* Pages stay thin: page models / controllers bind input, call business logic from Core and choose the result. No
  business logic or data access in pages.
* Form input is validated on the server (data annotations / model validation), even when the client validates too.

### Coding rules

* Do not hard-code anything; use constants and readonly values.
* Do not use magic strings and magic numbers in the code.
  * If it's a business rule, put it into `BusinessRules`.
  * If we use strings to identify things (configuration names, state names, etc.), create a container class for them
    (for example, `FeatureFlagNames`).
  * Even when using constants, try not to spread them out; instead, concentrate them around a topic whenever possible.
    For example, a `HostingSettings` class to store all hosting-related constants.

### Code style

* The repository-root `.editorconfig` is the source of truth for formatting and code style. Do not override it with
  other `.editorconfig` files, except for generated code (see below).
* Use file-scoped namespaces (`namespace {productName}.Core.BusinessLogic;`), never block namespaces
  (`namespace X { … }`). Namespaces follow the folder structure.
* Do not use top-level statements. Every C# file contains classes (or records, interfaces, enums). Entry points have
  a `Main` method and use role-specific file and class names: `CLIProgram.cs` / `CLIProgram` for CLI,
  `BackendProgram.cs` / `BackendProgram` for Backend, `WebProgram.cs` / `WebProgram` for Web, and
  `AppHost.cs` / `AppHost` for Aspire AppHost. Other executable projects use `Program.cs` / `Program`.
  Keep all AppHost orchestration and builder registrations in `AppHost.cs`.
* One public type per file; the file name matches the type name.
* Every public and protected type, constructor, method, property, constant and enum value gets an XML documentation
  comment (`/// <summary>`, plus `<param>`, `<returns>`, `<exception>` where they apply). Write comments that explain
  purpose and behavior; do not just restate the name. Use `<inheritdoc />` for interface implementations.
  * Test projects do not need XML comments.
  * Generated code (for example EF Core migrations) is exempt. If it produces CS1591 warnings, suppress them with a
    `[**/Migrations/*.cs]` section in the root `.editorconfig` that sets `dotnet_diagnostic.CS1591.severity = none`.
* Run `dotnet format` on the solution after making changes. `dotnet format --verify-no-changes` and `dotnet build`
  must both pass with no warnings.
* Keep `using` directives sorted (`System` first) and remove unused ones. Do not leave commented-out code.

### Async

Use async APIs (`async`/`await`, `CancellationToken` passed through) whenever an async overload exists. Never block on
async code with `.Result` or `.Wait()`.
