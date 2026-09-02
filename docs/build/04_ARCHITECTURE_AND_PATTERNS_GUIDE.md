# Architecture & Patterns Guide

Two jobs, both served by the same document:

1. **Explain the "why" behind every piece of custom logic** — which OOP pillars, which SOLID
   principle(s), which design pattern (if any) — so a new dev and a veteran dev arrive at the
   same understanding of intent, not just a reading of the code.
2. **Stand in for this AI-assisted workflow if it's ever unavailable.** If a developer needs to
   extend this codebase with no internet and no AI agent, the recipes in §3 are the manual
   playbook — copy the shape of the nearest existing example rather than reinventing one.

Read `CLAUDE.md` §4a first. This guide explains what's actually in the code; it does not license
adding structure that §4a already rejected. If you're about to name a pattern to justify adding a
layer, an interface, or an abstraction that CLAUDE.md doesn't already call for, that's a sign to
stop, not a gap in this guide.

## 1. The one-paragraph architecture (full version: `CLAUDE.md` §4a)

Coupled CMS, single-project monolith, server-rendered MVC, package-by-layer. Umbraco *is* the
application — there is no domain core to protect from it. Most of this codebase (Document Types,
Composers that only register schema, Razor views) is **configuration and markup, not logic**, and
has no meaningful OOP/SOLID/pattern shape to document — see §2 for why that's deliberate, not an
omission. This guide covers the actual custom logic: `Services/`, `Middleware/`, `RateLimiting/`,
`Seo/`, and the behavior (not the Umbraco base-class plumbing) inside `Controllers/`.

## 2. What's deliberately not covered here

Document Types, uSync schema, Razor `.cshtml` templates, and Composers whose `Compose()` method
only calls `builder.Services.Add...()`. These have no independently testable contract — the exact
same line `CLAUDE.md` §4 already draws for TDD scope applies here: "there's no meaningful failing
test to write for 'the Home page has a hero heading field,'" and there's no meaningful SOLID
principle either. Forcing one onto them would be over-engineering — the thing §3/§4a spend real
words rejecting. If a future session catches itself explaining "the Strategy pattern behind the
`aboutPage` Document Type," that's the line being crossed.

## 3. Custom logic, component by component

Each card: what it is → OOP pillars in play → SOLID principle(s) → pattern (named honestly, "none
beyond X" where that's the truth) → why.

### `IEmailSender` / `MailKitEmailSender` (`Services/`)

- **What**: one-method interface (`SendAsync`), one concrete SMTP implementation.
- **OOP**: abstraction + polymorphism — callers depend on the interface type, not the concrete class.
- **SOLID**: **Dependency Inversion** (the high-level `ContactFormProcessor` depends on `IEmailSender`,
  not on MailKit) and, in miniature, **Interface Segregation** — the interface exposes exactly the
  one operation a caller needs, nothing SMTP-specific leaks through it.
- **Pattern**: a DI-swappable dependency, not a full Strategy pattern (there's one implementation
  selected at startup via a Composer, not several selected at runtime) and explicitly **not**
  Ports-and-Adapters/Hexagonal — `CLAUDE.md` §4a says this outright, don't re-describe it that way
  in a future session.
- **Why**: testability without hitting a real SMTP server (see `ContactFormProcessorTests.cs`,
  which mocks this interface with Moq) and the ability to swap providers (SendGrid ↔ MailKit)
  without touching the code that decides *whether* to send.

### `ContactFormProcessor` (`Services/`)

- **What**: pure decision logic — validate → honeypot check → send → classify the outcome.
- **OOP**: encapsulation — `ContactSubmissionResult` is a small closed enum (`Sent`/`Dropped`/
  `Invalid`/`Failed`), not a string or a bare bool, so every caller handles every outcome explicitly.
- **SOLID**: **Single Responsibility** — owns the *decision*, not the HTTP concerns (that's the
  controller) or the transport concerns (that's `IEmailSender`).
- **Pattern**: an outcome-enum idiom, not a full `Result<T>`/Either monad. Deliberately simple —
  the `CLAUDE.md` §3 test ("does plain C# take less effort than a package") applies to structure
  choices too, not just dependencies.
- **Why**: kept independent of `SurfaceController`/Umbraco specifically so it's unit-testable
  without constructing Umbraco's full DI chain — see its own doc comment.

### `ContactSurfaceController` (`Controllers/`)

- **What**: the site's only public write path (`CLAUDE.md` §6). Anti-forgery + Post-Redirect-Get,
  delegates the actual decision to `ContactFormProcessor`.
- **SOLID**: **Single Responsibility** (HTTP/Umbraco plumbing only) via **Dependency Inversion**
  (constructor-injects `ContactFormProcessor` rather than newing it up).
- **Pattern**: extends Umbraco's own `SurfaceController` base class, which already implements a
  Template-Method shape (`CurrentUmbracoPage()`/`RedirectToCurrentUmbracoPage()`) — this class
  fills in one step (`Submit`), it doesn't invent the pattern.
- **Why**: keeps business logic out of the controller entirely, so the controller stays a thin,
  Umbraco-specific adapter and the decision logic stays portable/testable.

### `SecurityHeadersMiddleware` / `ContactFormRateLimitMiddleware` (`Middleware/`, `RateLimiting/`)

- **What**: two cross-cutting request-pipeline concerns — headers, rate limiting.
- **Pattern**: genuinely **Chain of Responsibility** — this is what ASP.NET Core's middleware
  pipeline itself is built on, worth naming accurately rather than hedging.
- **SOLID**: **Single Responsibility**, one concern per middleware — resist the urge to merge them
  into one "site policy" middleware even though both currently gate on request path/content.
- **Not a pattern**: the `/umbraco/*` path exclusion in both is a guard clause, not a Strategy —
  don't over-describe a simple `if` as a pattern just because a pattern name is available.
- **Why**: `SecurityHeadersMiddleware` skips `/umbraco/*` because the backoffice is an authenticated
  admin SPA, not the anonymous surface the CSP protects, and the strict CSP breaks its own inline
  bootstrap script (confirmed live). `ContactFormRateLimitMiddleware` detects a submission by the
  `HoneypotField` form key rather than a fixed path, because `Html.BeginUmbracoForm` posts back to
  whatever page rendered it — see the middleware's own doc comment.

### `PublishedContentNavigationExtensions` (`Extensions/`)

- **What**: extension methods (`ChildrenOf`, `RootOf`) wrapping `IDocumentNavigationQueryService`.
- **Pattern**: adapter-*shaped*, not a formal Adapter (there's no interface making it swappable —
  it's encapsulating one awkward API behind a clean one, not enabling substitution). Don't upgrade
  this description to "the Adapter pattern" in a future session; it would overstate what's there.
- **Why**: works around `IPublishedStatusFilteringService` having no DI registration on this
  Umbraco 18 scaffold (confirmed via reflection — a genuine gap, not a usage mistake). See its own
  doc comment for the full story.

### `SitemapGenerator` / `UmbracoSitemapPageSource` / `SeoController` (`Seo/`, `Controllers/`)

- **What**: `SitemapGenerator` is pure, static, Umbraco-free XML-building logic (TDD-covered).
  `UmbracoSitemapPageSource` is the thin Umbraco-integration glue that feeds it — deliberately
  *not* unit tested, per its own doc comment, and verified functionally instead.
- **SOLID**: the same **Dependency Inversion** shape as `IEmailSender`/`ContactFormProcessor` — the
  testable core (`SitemapGenerator`) has zero Umbraco dependency, so it doesn't know or care that
  its caller is Umbraco-backed. Again: this split exists for testability, not as a ports-and-
  adapters boundary — same distinction `CLAUDE.md` §4a draws for `IEmailSender`, applied here too.
- **Why**: `SeoController` needs `IUmbracoContextFactory.EnsureUmbracoContext()` because Umbraco
  only establishes an `UmbracoContext` for URLs that resolve to a real content node, and
  `/sitemap.xml` never does — confirmed live, see the controller's own doc comment.

### `OrganizationSchema` / `PostalAddressSchema` (`Seo/`)

- **What**: plain typed classes for schema.org JSON-LD, with `[JsonPropertyName("@type")]` etc.
- **OOP**: encapsulation only — no behavior, a Data Transfer Object, not a pattern beyond that.
- **Why**: typed rather than an anonymous object + string-replace hack, because `@type`/`@context`
  aren't valid C# identifiers — `JsonPropertyName` is the direct, no-hack way to handle that.

### Composers (`Composers/ContactFormComposer.cs`, `SeoComposer.cs`)

- **What**: implement Umbraco's own `IComposer` extension point to register services.
- **Pattern**: this *is* Umbraco's own Composition Root hook — using it correctly, not inventing
  anything. Don't document a Document-Type-only Composer here (§2) — only ones wiring real services.

## 4. Manual/offline continuation guide

No AI agent, no internet: copy the shape of the nearest existing example rather than starting from
a blank file. Each recipe below names the file to copy.

**Adding a new swappable service dependency** (follow `IEmailSender`/`MailKitEmailSender`):
1. Define a small interface in `Services/` — one or two methods, only what callers actually need.
2. Write the concrete implementation.
3. Register it in a Composer: `builder.Services.AddScoped<IYourInterface, YourImpl>();`
4. Inject the interface via constructor wherever it's needed — never `new` it up directly.
5. Unit test against the interface with Moq (see `ContactFormProcessorTests.cs`) — no real network
   call, no real Umbraco context needed.

**Adding a new middleware** (follow `SecurityHeadersMiddleware`):
1. Constructor takes `RequestDelegate next`; expose `Task InvokeAsync(HttpContext context)`.
2. Do your one thing, then `await _next(context);` — don't skip calling `next` unless deliberately
   short-circuiting (like the rate limiter's 429).
3. Register with `app.UseMiddleware<YourMiddleware>();` in `Program.cs`, in the right order relative
   to `UseUmbraco()` — see `Program.cs`'s existing ordering and comments before moving anything.
4. If it's public-facing behavior, decide deliberately whether `/umbraco/*` should be exempt — the
   backoffice is a different trust boundary than the anonymous public site (see
   `SecurityHeadersMiddleware`'s own reasoning).

**Adding a new Surface Controller** (follow `ContactSurfaceController` — the site's only other
public write path, per `CLAUDE.md` §4a's "single write path is an invariant"):
1. Inherit `SurfaceController`; `[HttpPost]` + `[ValidateAntiForgeryToken]` on the action.
2. Don't put decision logic in the controller — delegate to a plain injectable class the way this
   one delegates to `ContactFormProcessor`, so the logic stays unit-testable.
3. Post-Redirect-Get: never return a view directly from a successful POST.
4. If it needs rate limiting, follow `ContactFormRateLimitMiddleware`'s pattern (detect the
   submission by a field the form itself carries, not a fixed route) — `Html.BeginUmbracoForm`
   posts back to the current page URL, not a fixed one.

**Writing a TDD test for new logic** (`CLAUDE.md` §4 — required for actual custom logic, not for
Document Types/views):
1. xUnit + Moq, already referenced in `OphirMineralVentures.Web.Tests`.
2. One behavior per `[Fact]`, named `Method_When_Condition_ThenOutcome`.
3. Arrange (build the input + mocks) → Act (call the one method under test) → Assert (the return
   value, and `mock.Verify(...)` for anything that should or shouldn't have been called).
4. Write the test first, watch it fail, then implement — `ContactFormProcessorTests.cs` is the
   canonical shape to copy.

**Adding a new Document Type/template**: not this guide — see `docs/build/01_CONTENT_MODEL_SPEC.md`.

**Before adding any new abstraction, layer, or interface**, ask: would a 10-line method on an
existing class do this? If yes, that's the answer (`CLAUDE.md` §3's "would plain C# take less
effort" test, applied to structure, not just dependencies). Never introduce `Domain`/`Application`/
`Infrastructure` folders, never describe DI here as "ports and adapters," never reach for DDD/
Clean/Onion/Hexagonal — `CLAUDE.md` §4a rejects all four by name, deliberately, not by oversight.

## 5. Keeping this current

Whenever a phase adds genuinely new custom logic (not a Document Type, not a Razor view), it earns
a card in §3 of this guide — the same discipline `DEVLOG.md` already has for "what happened,"
applied to "why it's built this way." `DEVLOG.md` and this guide are companions, not duplicates:
check both when picking up a phase cold.
