# Devlog — Ophir Mineral Ventures Website Build

Running log of the actual build, kept as the work happens — not reconstructed afterward. One entry per phase minimum (see `docs/build/00_BUILD_PLAN.md`), more if a session spans a phase boundary or something notable happens mid-phase.

**Entry format:**

```
## YYYY-MM-DD — Phase N: <phase name>

**Did:** what actually got built/decided this entry.
**Deviated from plan:** anything that differs from `CLAUDE.md` or `docs/build/00_BUILD_PLAN.md`/`01_CONTENT_MODEL_SPEC.md`, and why. If it's a real decision (not a typo fix), update the source-of-truth doc in the same commit — don't let this log become the only place the deviation is recorded.
**Blocked on:** anything waiting on the client (cross-reference `CLAUDE.md` §11) or another external factor.
**Next:** what the next session should pick up.
```

Keep entries honest and short — this is a working log, not a status report written for someone else to be impressed by. A one-line entry ("scaffolded solution, boots cleanly, nothing else changed") is completely fine when that's all that happened.

---

## 2026-08-31 — Phase 0: Environment & scaffold

**Did:** The environment on this machine doesn't match Section 1's 2026-08-07 snapshot — .NET wasn't installed at all, and Node was at v22.22.2. I installed .NET 10.0.111 via Ubuntu's own apt repository (`dotnet-sdk-10.0`), since Microsoft's own CDN is blocked by this session's network policy. Node 24.20.0 came from a direct binary download from nodejs.org (that host is reachable) into `/opt/node24`, symlinked in place of the old `/opt/node22` install so it's picked up without any shell configuration changes. `dotnet new install Umbraco.Templates` also failed, against nuget.org's search-metadata endpoint specifically (a different, blocked host from the registration/flat-container API that otherwise works fine) — downloading the `.nupkg` directly and installing from the local file avoided that call path entirely.

The solution is scaffolded under `src/`: `OphirMineralVentures.Web` on Umbraco 18.1.1, `OphirMineralVentures.Web.Tests` as an empty xUnit project referencing it, both in `OphirMineralVentures.slnx` (the SDK now defaults to the new `.slnx` format rather than `.sln`). `dotnet build` and `dotnet test` both pass clean with zero tests.

I ran the app with an unattended install configured via user-secrets (never committed) to verify it actually boots end-to-end rather than just compiles. It initially failed with `SQLite Error 14: unable to open database file` on every request. Tracing it — via a temporary `IRuntimeState` check added to and then removed from `Program.cs` — pointed to `RuntimeLevel.BootFailed` with `Reason.InstallMissingDatabase`. The actual cause was my own typo: I'd set `Umbraco:CMS:Unattended:InstallUnattendedUser` in user-secrets, which isn't a real config key (confirmed against the compiled `UnattendedSettings` model via reflection, since only the DLL is available here, not source) — the correct key is `InstallUnattended`. Since the wrong key silently failed to bind, unattended install never actually ran, and Umbraco was left with a connection string pointing at a database that didn't exist and no valid path to create it. Fixing the key name resolved it: the unattended install completed, the SQLite schema was created, and both the front end and the backoffice respond 200.

**Deviated from plan:** Environment needed real installation work beyond "confirm versions" — noted above; no source-of-truth doc change needed since Section 1's snapshot already flags itself as subject to drift. Used Umbraco.Templates 18.1.1 and Node 24.20.0 rather than the exact versions CLAUDE.md names, per Section 3a's standing "latest released version" policy — again no doc change needed, since that section already says to re-verify rather than trust its own numbers.

**Blocked on:** Nothing for Phase 0 itself. Section 6's hosting-tier verification (Section 8) and the open client questions (Section 11) remain outstanding for later phases, as expected at this point.

**Next:** Phase 1 — content model foundation, per `docs/build/01_CONTENT_MODEL_SPEC.md`: install uSync 18.0.3, build the `seoComposition`/`siteSettings` compositions first, then the remaining document types, with both en-US and zh-Hans active before marking anything culture-variant.

## 2026-08-31 — Phase 1: Content model foundation

**Did:** With no browser available in this environment to click through the backoffice, I built the entire content model — every document type, composition, data type, and both languages from `docs/build/01_CONTENT_MODEL_SPEC.md` — by calling Umbraco's own core services (`IContentTypeService`, `IDataTypeService`, `ILanguageService`) directly in C#, via a temporary seeding routine invoked with `dotnet run -- --seed-content-model`. Since Umbraco 18 ships only the compiled DLLs here (no source), I found the exact service/model signatures I needed by reflecting into `Umbraco.Core.dll` and reading its bundled XML doc comments rather than guessing.

What got built: `en-US`/`zh-Hans` languages; element types `labelValueItem`, `leadershipItem`, `socialLinkItem`, `initiativeItem` for the Block List blocks; custom-configured data types for the two Media Picker restrictions (image-only, PDF-only), four Block List configurations, and a Content Picker restricted to `article`; the `seoComposition` composition; the standalone `siteSettings` singleton; and all eleven page content types from Section 5's table, composed with `seoComposition` and wired with the right parent/child `AllowedContentTypes` restrictions. Every property follows the spec's blanket culture-variance rule.

I verified this properly rather than trusting the "created successfully" return values alone: a separate read-only check confirmed the composition's properties (`metaTitle` etc.) actually appear on `home` via `CompositionPropertyTypes`, that `Variations` is `Culture` where expected, that the Content Picker's `allowedContentTypes` config genuinely stores `article`'s key, and that the parent/child restrictions took.

Then I exported the whole schema via uSync (`Umbraco:uSync:ExportAtStartup`, one run) and ran the strongest test available: wiped the SQLite database entirely, booted a genuinely fresh install, and drove uSync's import programmatically (`ISyncService.ImportAsync` — the `ImportAtStartup`/`ImportOnFirstBoot` config settings didn't actually trigger an import in testing despite logging a suspiciously-fast "Startup Complete", so I called the service directly instead and got a real result: 75 items imported, 0 failures). Re-running the same verification checks against that fresh, import-only database gave identical output — same content types, same composition wiring, same `article` key. That's the actual proof this project's "schema lives in git" architecture works, not just an assumption. The temporary seeder/verifier/importer classes are deleted; `uSync/v18/` is the committed source of truth from here on, per Section 4a.

**Deviated from plan:** `certification` is composed with `seoComposition` — the spec listed this as optional pending a Phase 2 decision on whether certifications get routable detail pages. I applied it now for consistency with every other content type rather than leaving it inconsistent; this is reversible in Phase 2 if certifications end up card-only.

**Blocked on:** Nothing for Phase 1. No actual content nodes exist yet (by design — that's Phase 5's placeholder-content pass), so the site currently has no home page to render.

**Next:** Phase 2 — templates & static structure: one Razor view per document type against the ModelsBuilder-generated models, the shared `_Layout`/`_Nav`/`_LanguageSwitcher` partials reading from `siteSettings`, and the `prefers-color-scheme` token theme copied from the proposal source HTML.

## 2026-09-01 — Phase 2: Templates & static structure

**Did:** This session ran on a fresh local Windows checkout (not the Phase 0-1 cloud container), so the environment was re-verified per `CLAUDE.md`'s first-session checklist rather than assumed: .NET SDK 10.0.301 and Node v24.18.0 were both already present on this machine — no installation work needed this time.

Built the full design system and template set: `wwwroot/css/site.css` carries the token structure from `docs/proposals/source-html/02-Homepage-Design-Draft.html` (stone/paper/ink/basalt/ore/seam palette, `prefers-color-scheme: dark` + `data-theme` override blocks for a future manual toggle) plus every component class the templates need (nav, hero, page-hero, stats bar, teaser grid, product cards, spec tables, certification seals, news cards, article prose, contact form, footer). `_Layout.cshtml` reads `siteSettings` for company name/footer, resolves `metaTitle`/`ogImage` fallbacks per `01_CONTENT_MODEL_SPEC.md`'s exact fallback rules. `_Nav.cshtml` builds the primary nav from Home's own children (About/Products/Compliance/Sustainability/News), and `_LanguageSwitcher.cshtml` follows Umbraco's documented `IPublishedContent.Cultures` loop. One Razor view per routable document type (10 total — `certification` stays a data-only child type with no template, rendering as inline cards on `certificationsListing` rather than getting its own routable page; noted as a Phase 2 decision, reversible later, matching the "optional pending a Phase 2 decision" note left in `01_CONTENT_MODEL_SPEC.md`).

Templates and content types don't exist without content, and content isn't uSync-tracked by design (`CLAUDE.md` §4a), so — same reasoning as Phase 1 — I wrote a seeder (`Seed/Phase2Seeder.cs`, run via `dotnet run -- --seed-phase2`) that imports the uSync schema into a fresh database, creates a Template for each routable content type, wires it as that type's default template, and creates placeholder content in both cultures for the entire tree (Home, About with one leadership entry, Products with 2 products, Sustainability with 3 initiatives, Compliance with 3 certifications, News with 2 articles, Contact, and both legal pages), then publishes every node in en-US and zh-Hans. Unlike Phase 1's seeder, **this one is being kept**, not deleted — it's the only way to get a demoable content tree on a fresh clone without hand-clicking through the backoffice, and it doesn't create a second source of truth the way a schema-seeder would (schema still lives only in uSync). It's documented as safe only against an empty database (it always creates new nodes, never checks for existing ones).

Building this surfaced three real Umbraco 18 API changes worth recording since they're not obvious from the docs:
1. **`IPublishedContent.Children()`/`.Root()` now require `INavigationQueryService` + `IPublishedStatusFilteringService`** (a v18 rewrite of tree navigation for performance). `INavigationQueryService` resolves fine via the *derived* `IDocumentNavigationQueryService`, but `IPublishedStatusFilteringService` has no registered implementation anywhere in this scaffold's dependency graph (confirmed by direct assembly search across every referenced Umbraco DLL) — a genuine gap, not a Razor `@inject` scoping quirk. Worked around it with `Extensions/PublishedContentNavigationExtensions.cs`, which uses `IDocumentNavigationQueryService.TryGetChildrenKeys`/`Path`-parsing plus `IPublishedContentQuery.Content(Guid)` instead — the latter already only returns currently-published content, which is what the missing filtering service exists to guarantee anyway. If a later Umbraco 18.x patch registers the missing service, this workaround can be deleted in favor of the documented `Children(nav, filter)` extension directly.
2. **`Model.Value<T>("alias")` inline in markup (not inside a `@{ }` code block) needs explicit parens** — `@Model.Value<string>("x")` intermittently mis-parses as a `<` comparison in Razor's markup-expression mode; `@(Model.Value<string>("x"))` is unambiguous. Applied across every view.
3. **`IContentTypeService.CreateTemplateAsync`** (the one-call "create a template and wire it as this content type's default" convenience method) fails silently with `ContentTypeOperationStatus.Unknown` and no logged cause for every content type in this project — root cause not identified. Used `ITemplateService.CreateForContentTypeAsync` (3-arg overload; the 4-arg replacement fails with `LayoutTemplateNotFound` because it validates the view's `Layout = "_Layout.cshtml"` reference against a registered Template entity, which a plain layout file will never be) plus manual `IContentType.SetDefaultTemplate` + `UpdateAsync` instead.

Also discovered and fixed a routing ambiguity: with `siteSettings` and `home` both published as root-level content nodes and no domains configured, Umbraco had no reliable way to decide which one owns `/` (intermittently resolved "/" to `siteSettings`, which has no template, producing 404s). Fixed by creating `home` before `siteSettings` (lower content ID) and, more robustly, assigning explicit domains via `IDomainService.UpdateDomainsAsync` — `localhost:5205/` → en-US, `localhost:5205/zh-hans` → zh-Hans (path-prefixed single-domain bilingual routing, Umbraco's standard pattern absent separate hostnames per culture). Production domain assignment is a Phase 6/9 deployment-runbook concern, not committed via uSync (added `ContentHandler`/`DomainHandler` to `uSync:Sets:Default:DisabledHandlers` in `appsettings.json` specifically so environment-specific domains and content never get committed as schema, matching the "content is not synced" architecture invariant — this also fixed an unrelated problem where repeated local re-seeds were auto-exporting placeholder content into `uSync/v18/Content/`, which had to be deleted before this fix).

Verified in the actual browser (Chrome via the harness's Browser pane, not just curl) rather than assuming template-renders-without-error means correct: every one of the 10 routable pages loads at 200 in both en-US and zh-Hans (zh-Hans URLs are `/zh-hans/zh-<slug>/`, generated automatically from each node's zh-Hans name — not hand-guessable, confirmed by reading the rendered nav rather than assuming a URL pattern). Confirmed working end-to-end: the language switcher (`EN`/`中文` pills, correct `hreflang`, correct target URLs, active-state highlighting), the primary nav's current-page highlighting, the footer's Privacy Policy/Terms of Service links, Block List rendering for all four block types in use (Home's stats bar, About's leadership card, Product's spec table ×2, Sustainability's initiatives list), Rich Text rendering, the certification cards, the news listing sorted by date descending plus its article detail pages, the product listing plus its 2 detail pages, and `prefers-color-scheme: dark` actually swapping the token palette (checked via the browser's color-scheme emulation, both light and dark, plus a mobile 375px viewport — nav links collapse below 820px with no hamburger replacement, inherited as-is from the homepage design draft's own mobile behavior, not a Phase 2 regression, but worth flagging as a real UX gap if the client ever reviews the site on mobile before that's addressed).

**Deviated from plan:** `certification` confirmed as card-only with no routable template (see above) — updates the "pending Phase 2 decision" note in `01_CONTENT_MODEL_SPEC.md`'s Views section, no further doc change needed since that note already flagged it as an open decision to be made here. Phase 2's placeholder content goes further than "prove the switcher works" — it's a genuinely complete placeholder pass (every property on every content type filled, in both cultures) — because getting the seeder right cost the same either way once the Block List JSON format was solved, and it means Phase 5 can be closer to a verification/polish pass than starting from a blank tree. All placeholder content follows `CLAUDE.md` §10's bracketed-placeholder convention for anything resembling a real business fact (SEC/DTI/DENR-MGB numbers, address, phone, John David Montilla's bio, ore-spec figures marked "(indicative)"); zh-Hans fields use the literal `[zh-Hans placeholder — pending client translation]` marker per the Build Plan's explicit Phase 2 allowance, not machine translation or developer-authored Chinese, per `CLAUDE.md` §4a's translation policy.

**Known gaps carried forward:** Hardcoded UI chrome strings ("View Products", "Learn more →", section kickers like "Explore") are English-only in the templates themselves — only CMS-driven content is culture-variant. Whether these need their own translation is worth raising with the client; not addressed here since `CLAUDE.md` §4a scopes bilingual support to content, not UI chrome, and this wasn't flagged as in-scope for Phase 2. Mobile nav has no menu/hamburger (inherited from the design draft, noted above). `home.featuredNews` (Content Picker) was left unpopulated — Home's "Latest news" section is written and tested via the empty-state branch only; wiring an actual picker value is cosmetic and deferred rather than risking a malformed property value.

**Blocked on:** Nothing for Phase 2. Contact form is intentionally non-functional placeholder markup (`ContactPage.cshtml` has a comment marking where `ContactSurfaceController` attaches) — that's Phase 3's job per `CLAUDE.md` §6, not a gap.

**Next:** Phase 3 — contact form & security middleware, built TDD per `CLAUDE.md` §4's testing decision: `ContactSurfaceController` (anti-forgery, honeypot, Post-Redirect-Get), `IEmailSender`, the full §7 security header middleware, and the `RateLimiter` on the POST endpoint.

## 2026-09-01 — Phase 3: Contact form & security middleware

**Did:** Built TDD throughout, per `CLAUDE.md` §4 — test written and confirmed failing before each piece existed, then implemented to green. 15 tests, all passing, `dotnet test` clean.

`ContactFormProcessor` (`Services/ContactFormProcessor.cs`) holds the actual validation/honeypot/send decision logic as a plain, DI-testable class — kept independent of `SurfaceController` so it doesn't need Umbraco's full context/database/cache constructor chain to unit test. `ContactSurfaceController` (`Controllers/ContactSurfaceController.cs`) is a thin wrapper per `CLAUDE.md` §6: anti-forgery token, Post-Redirect-Get, calls into the processor. `IEmailSender` is implemented via MailKit + SMTP (`Services/MailKitEmailSender.cs`) rather than SendGrid — chosen so the form can send through the client's existing Google Workspace SMTP relay instead of requiring a new third-party account before it's actually needed; real credentials aren't configured anywhere (`Services/EmailSettings.cs` binds an empty "Email" config section, meant for user-secrets locally / host env vars in production, per §11's still-open recipient-inbox question). `SecurityHeadersMiddleware` (`Middleware/`) implements §7's header block verbatim, as an actual middleware class rather than an inline lambda, specifically so it's unit-testable via a bare `TestServer` without booting Umbraco. `ContactFormRateLimitMiddleware` (`RateLimiting/`) rate-limits to 5 requests/minute/IP.

Three things only surfaced by actually exercising the running app (not just green tests) that would have shipped broken otherwise:

1. **The public CSP breaks Umbraco's own backoffice.** `script-src 'self'` blocks the backoffice SPA's inline bootstrap script and ES module imports — confirmed by loading `/umbraco/` in a real browser and reading the console (a blank page, not an error page — the kind of failure that's invisible unless you actually look). Fixed by scoping `SecurityHeadersMiddleware` to skip `/umbraco/*` entirely: the backoffice is an authenticated admin SPA, not the anonymous-visitor surface the CSP exists to protect, and this is exactly the "specific, understood reason" §7 allows for loosening. Added a regression test (`BackofficeRequests_DoNotGetThePublicSiteCsp`) so this can't silently regress.
2. **`Html.BeginUmbracoForm` doesn't post to a fixed URL.** It posts back to whatever page the form is rendered on, with an encrypted `ufprt` field carrying the real controller/action route data — and that page URL varies per culture (`/contact/` vs `/zh-hans/zh-contact/`). The rate limiter's first design keyed off a fixed `/umbraco/surface/ContactSurface/Submit` path, which would never have matched a real submission — rewritten to key off the presence of the `HoneypotField` form field instead, verified live via curl (fetched a real antiforgery token + `ufprt`, submitted, confirmed a 6-request burst throttled correctly, confirmed the throttle is shared across culture pages for the same visitor).
3. **An unhandled `IEmailSender` failure would have 500'd on every real visitor** until real SMTP credentials exist (which they don't yet — confirmed live: submitting a real form via curl with valid antiforgery/ufprt correctly reaches the send call and throws on the empty `FromAddress`, exactly as expected with no credentials configured). Added a `Failed` result to `ContactSubmissionResult`, caught and logged in `ContactFormProcessor`, surfaced to the visitor as a friendly "something went wrong" notice instead of a raw crash — on a site whose whole job is compliance credibility (`CLAUDE.md` §1), a crash on the one write path was worth the extra ~15 lines.

Verified live end-to-end via curl (fetching real antiforgery tokens/cookies/`ufprt` values rather than trusting the code): anti-forgery correctly rejects a POST missing the token (400); a fully valid submission reaches the send call, fails gracefully, and shows the error notice on redirect (Post-Redirect-Get confirmed working); a 6-request burst from one IP throttles at request 5 (matches `PermitLimit = 5`, correctly counting across the whole rolling window, not per-request-batch). Did not verify the actual "email arrives" happy path — that's blocked on real SMTP credentials, tracked below.

Also fixed, while verifying the backoffice: `dotnet dev-certs https --trust` wasn't run on this machine (a fresh local checkout, unlike the Phase 0-1 cloud container) — needed to test anything backoffice-related, since Umbraco's OpenIddict-based backoffice login requires HTTPS. Added `Umbraco:CMS:WebRouting:ApplicationUrlDetection: FirstRequest` to `appsettings.Development.json` after noticing Post-Redirect-Get was generating redirect URLs pointing at a stale port from an earlier http-only run in the same session — a real but dev-only quirk, not present once a site has one fixed production hostname.

**Deviated from plan:** None from `CLAUDE.md` §6/§7's actual requirements — the CSP backoffice scoping, the rate-limiter redesign, and the graceful-failure handling are all implementation details discovered while making the spec's requirements actually work, not deviations from what was asked for.

**Blocked on:**
- `CLAUDE.md` §11's open question — exact recipient inbox and real Google Workspace SMTP credentials. `ContactFormProcessor`'s recipient defaults to `john@ophirminerals.com` (the placeholder from §6's own sample) and `EmailSettings` binds to nothing locally. The actual "does an email land in an inbox" path is genuinely unverified and can't be until these exist.
- **2FA is available (built into Umbraco core, confirmed no extra package was added) but not interactively enrolled for the real admin account** — enabling it requires logging in and scanning a QR code with an authenticator app, which isn't something to do without Randolf's own device. Separately, the local admin login (from Phase 0's unattended-install user-secrets) failed with "check your credentials" on this machine when tested live — untriaged, not investigated further since it's orthogonal to Phase 3's actual scope and 2FA enrollment needs an interactive session with Randolf either way.
- Editor-vs-Administrator role assignment for John's eventual login — no real client user exists yet to assign a role to; deferred to Phase 8/10 onboarding, per `CLAUDE.md` §7's standing reminder (already written down there, not just here).

**Next:** Phase 4 — SEO & compliance plumbing: render `seoComposition` into `<head>` on every page, sitemap.xml/hreflang generation (TDD, same pattern as this phase), robots.txt, schema.org structured data on Home and Contact.

## 2026-09-01 — Phase 4: SEO & compliance plumbing

**Did:** `metaTitle`/`metaDescription`/`ogImage` were already rendering into `<head>` from Phase 2 — added canonical and per-culture `hreflang` `<link>` tags alongside them, since a bilingual site needs those for the reasons `CLAUDE.md` §4a already spells out for the language switcher.

Built the sitemap generator TDD, same pattern as Phase 3: `SitemapGenerator.Generate(IEnumerable<SitemapPage>)` is pure XML-generation logic — takes plain `SitemapPage` records (a page's URL per culture), no `IPublishedContent` dependency at all — so it's directly unit-testable per `02_TESTING_QA_PLAN.md`'s TDD scope note. 6 tests: single/multi-culture entries, self-referencing hreflang (required per Google's own guidance, not optional), no cross-contamination of alternates between different pages, empty input, and namespace declarations. `UmbracoSitemapPageSource` is the thin Umbraco-integration adapter that walks the tree from Home and builds those records (glue, not TDD-scoped, verified functionally instead) — excludes `certification` (no routable template, per Phase 2's decision) and `siteSettings` (outside the Home subtree, never public). `robots.txt` disallows `/umbraco/` and references the sitemap. Structured data: `OrganizationSchema`/`PostalAddressSchema` (typed classes with `[JsonPropertyName("@type")]` etc., not an anonymous object + string-replacing the serialized JSON — `@type`/`@context` aren't valid C# identifiers, and hacking them in via `.Replace()` on serialized output is exactly the kind of fragile thing worth avoiding when a typed class costs nothing more) — `Organization` on Home, `LocalBusiness` on Contact (using the contact page's own address/phone fields, which are more specific than `siteSettings`'), both driven by real content, not hardcoded.

Two real problems only found by actually curling the running app:

1. **`/sitemap.xml` and `/robots.txt` can't reach `UmbracoContext` the way a normal page can.** First attempt was a custom middleware (matching `SecurityHeadersMiddleware`'s pattern) registered ahead of Umbraco's own pipeline setup — threw "Wasn't able to get an UmbracoContext" immediately. Moving it later in the pipeline (inside `WithMiddleware`, after `UseWebsite()`) didn't fix it either, nor did switching to a plain MVC controller reached through real endpoint routing (the same routing stage `ContactSurfaceController` reaches without issue). Root cause: Umbraco only establishes `UmbracoContext` for a request when the URL actually resolves to a content node — `/sitemap.xml` and `/robots.txt` never do, so it's simply never created for them, regardless of pipeline position. Fixed with `IUmbracoContextFactory.EnsureUmbracoContext()` inside `SeoController.Sitemap()` — the documented pattern for exactly this scenario (a controller/service outside Umbraco's normal content-routed pipeline that still needs to query the published cache).
2. **`UrlMode.Absolute` resolves against Umbraco's own cached "application URL", which goes stale across local dev profile switches.** Caught because canonical/hreflang tags and the sitemap were all pointing at `localhost:5205` while the app was actually serving `localhost:44325`. Fixed everywhere it mattered (sitemap, canonical, hreflang, both schema.org `url` fields) by building absolute URLs from the current request's own `Scheme`/`Host` instead — `robots.txt` was already doing this by construction (needed the request context anyway for its `Sitemap:` line) and never had the bug. Left as a known, understood local-dev-only artifact: ordinary in-page navigation links (nav bar, footer, teaser cards) still resolve through Umbraco's own domain-based URL generation, which reflects whatever port was live the last time the dev-seeder registered a domain — never a problem in production, where there's exactly one real hostname that's set once, but worth remembering if a future session sees a stale-port link locally and wonders whether something regressed.

Verified live end-to-end on the port matching the seeded domain (`:5205` — testing on `:44325`/`:1153` intermittently 404'd `/zh-hans/*` for the same domain-registration-staleness reason as problem 2 above, not a real bug): full sweep of all 10 page types in both cultures plus `/sitemap.xml` and `/robots.txt` all 200; sitemap contains all 13 routable pages × 2 cultures = 26 URLs with correct `hreflang` alternates and no `certification`/`siteSettings` entries; `robots.txt` correctly disallows `/umbraco/` and references the sitemap at the current host; both schema.org blocks present with correct company name, current-host URL, and §10-compliant bracketed placeholders for phone/address.

**Deviated from plan:** None from `CLAUDE.md`/`00_BUILD_PLAN.md`'s actual Phase 4 requirements — the `IUmbracoContextFactory` usage and the request-host URL building are implementation details needed to make the spec's requirements work, not scope changes.

**Blocked on:**
- Real domain — `CLAUDE.md` §11's open question. Every URL in this phase's output (sitemap `<loc>`, canonical, hreflang, robots.txt's `Sitemap:` line, schema.org `url`) is correctly *mechanism*-complete (built from the actual serving host, not hardcoded) but obviously reflects `localhost` until a real domain exists and Phase 6/9 point it at the deployed site — noted here per `00_BUILD_PLAN.md`'s own instruction to record this as pending if a real domain isn't live yet.
- Cloudflare cache rules (bypass `/umbraco/*` and the contact-form POST) — explicitly deferred to Phase 6 per `00_BUILD_PLAN.md`, no Cloudflare account/hosting exists yet to write the rule against.
- Google's Rich Results Test / actual sitemap submission — needs a real, publicly reachable domain; can't be done against `localhost`.

**Next:** Phase 5 — placeholder content pass: sweep every page type for any remaining gaps against `CLAUDE.md` §10 (most of this is already done from Phase 2's seeder, so this should be closer to a verification/polish pass than starting fresh), confirm nothing could be mistaken for real client data before treating the site as demoable.

## 2026-09-01 — Phase 5: Placeholder content pass

**Did:** As expected from Phase 2's note, this was a verification pass, not a from-scratch content build — Phase 2's seeder already populated the full tree in both cultures. Started the app on the seeded domain's port and pulled the plain-text content of all 10 page types in en-US, plus the 7 zh-Hans routes, and read every line against `CLAUDE.md` §10 line by line rather than spot-checking.

**Result: clean, no gaps found.** Every field that could look like a real business fact is bracketed exactly per §10's convention: SEC/DTI-BIR/DENR-MGB reference numbers, business address, phone, John David Montilla's bio, and every ore-spec figure (labeled "(indicative)" per §10's specific instruction for assay data). Certification *names* and *issuing bodies* ("Securities and Exchange Commission (Philippines)", "Mines and Geosciences Bureau (DENR)") are real, public institutional facts — not fabricated data about Ophir itself — so they're correctly left unbracketed, matching the design draft's own precedent. `companyName` is the real legal name, which is a given fact from the client brief, not something §10 asks to placeholder. Every zh-Hans page consistently shows the `[zh-Hans placeholder — pending client translation]` / `[ZH] ...` markers from Phase 2's seeder — no accidental English leakage, no developer-authored Chinese, matching `CLAUDE.md` §4a's translation policy exactly.

Two things noted, neither a §10 violation nor blocking:
- Article dates render with the culture-correct month name (`八月 24, 2026` on the zh-Hans pages) via `.ToString("MMMM d, yyyy")` picking up the request's `CultureInfo` automatically — correct behavior, just not full Chinese date formatting (`2026年8月24日`). A cosmetic localization nicety for a later pass, not a data-accuracy issue Phase 5 is scoped to fix.
- Media Picker fields (hero image, leadership photos, spec sheets, certificate PDFs, featured images) remain empty — there's no real media to upload yet, and unlike text there's no bracketed-placeholder convention for a binary file. Left empty by design; every template already hides the corresponding UI section gracefully when the field is null (verified in Phase 2), and the CSS's gradient-swatch fallback treatments (`.swatch`, `.portrait`, `.leadcard .photo`, `.thumb`, `.article-hero`) keep every page looking visually finished without a real image. This is expected to stay this way until Phase 8's real assets arrive — not a gap to close now.

**Deviated from plan:** None — this phase's own plan language already anticipated it would be closer to verification than a build, and that's exactly how it went.

**Blocked on:** Nothing for Phase 5 itself. The site is demoable/screenshot-safe right now per `CLAUDE.md` §10's portfolio-use subsection (confirmed via this audit) — but per `00_BUILD_PLAN.md`'s own note, sending an actual preview link to the client is still blocked on Phase 6 (hosting), since there's no publicly reachable URL yet, only `localhost`.

**Next:** Phase 6 — hosting trial & verification: deploy to the Tier A host's free trial (`CLAUDE.md` §8's "before committing money" gate) and confirm Umbraco actually boots there before any paid commitment. **This phase needs Randolf directly (hosting account signup) — stopping here per the standing instruction to check in before Phase 6.**

## 2026-09-07 — Pre-Phase-6 verification: build/run confirmed clean, local admin login gap closed

**Did:** Re-verified the whole local build/run path from scratch on this machine (not a new phase — a "does this actually still work" pass before Phase 6, per Randolf's ask to confirm the project is genuinely dev-complete). `git status` was clean on `develop`, up to date with `origin/develop`, worktree correctly detached per the standing dev-flow convention — nothing to reconcile before starting.

`dotnet build OphirMineralVentures.slnx`: clean, 0 warnings, 0 errors. `dotnet test`: 20/20 passing. Both matched the state left at the end of Phase 5.

Ran the app against the sqlite database already sitting in `umbraco/Data/` (dated 2026-09-03, from an earlier session) — the front end rendered correctly (Home, Products, both en-US and zh-Hans routes, dark theme, hero illustration, language switcher all confirmed live in a real browser), but `/umbraco/` login failed with invalid-credentials for every guess, matching the untriaged gap Phase 3 already flagged ("local admin login... failed with 'check your credentials'... untriaged"). Root cause, now actually diagnosed: `dotnet user-secrets list` for this project's `UserSecretsId` came back empty — whatever credentials produced that database were never persisted to this machine's user-secrets store (likely set in a prior session/container that didn't survive), so the real password was simply unknowable, not a bug in Umbraco's login itself.

Fixed by redoing the unattended install correctly, from scratch: backed up the existing `Umbraco.sqlite.db*` files out of the repo (kept outside `src/`, they're gitignored dev artifacts with only placeholder data — not a loss of anything real), then set the **flat** `Umbraco:CMS:Unattended:InstallUnattended` / `UnattendedUserName` / `UnattendedUserEmail` / `UnattendedUserPassword` user-secrets keys exactly per the gotcha already documented in project memory (a nested `InstallUnattendedUser:...` shape silently no-ops). `dotnet run` on the now-empty database completed a real unattended install ("Unattended install completed" logged, admin row created — confirmed by successful backoffice login afterward, not just the log line). Then ran `dotnet run -- --seed-phase2` to re-import the uSync schema and rebuild the placeholder content tree; it reported the same 10 "Failed to create template" errors from uSync's own import that Phase 2 already root-caused (the `CreateTemplateAsync` bug), immediately followed by the seeder's manual-creation fallback succeeding for all 10 — expected, not a regression. All 12 content nodes published in both cultures without error.

Verified live in the browser end to end: front end (Home, Products, zh-Hans routes) still 200 and correctly showing placeholder/`[ZH]` content; backoffice login now succeeds with the newly-set credentials, lands on a fully-populated Content section (Home node's `Hero Heading`/`Hero Subtext` show the real seeded copy, `Hero Image` correctly shows an empty picker since no media exists yet), full nav (Content/Media/Library/Settings/Packages/Users/Members/Translation) all present.

**One side effect caught and reverted, not committed:** re-seeding a fresh database mints new random Template `Key` GUIDs (the manual-creation fallback doesn't reuse the GUIDs already committed in `uSync/v18/`), and something in the boot process re-exported those new keys back over the tracked `uSync/v18/ContentTypes/*.config` and `Templates/*.config` files, plus rewrote `Views/ContactPage.cshtml` with a BOM + no-trailing-newline (also a template-recreation side effect). Confirmed via `git diff` these were pure GUID/whitespace churn with no actual schema change, and reverted all of it (`git checkout --`) rather than committing — committing regenerated GUIDs on every re-seed would make `git diff` on `uSync/v18/` noisy and meaningless. This is a real, mildly annoying consequence of the still-unfixed Umbraco 18 `CreateTemplateAsync` bug from Phase 2 — worth knowing about if a future session re-seeds and sees the same files show up as "modified," but not worth fixing properly right now (would mean making the seeder look up and reuse the committed uSync template keys instead of letting `ITemplateService` mint new ones — a small, well-scoped fix but out of proportion for what was meant to be a verification pass, not a rebuild).

**Deviated from plan:** None from `CLAUDE.md`/the build plan's actual requirements — closing the login gap used the exact fix the gotcha already prescribed, not a new decision.

**Blocked on:** Same items already tracked — SMTP credentials (contact form email delivery unverified), 2FA enrollment (needs an interactive session with Randolf's own device), client Editor-role login (no real client user yet), and Phase 6 hosting (needs Randolf for account signup). None of these are new; this session closed one previously-open item (the local login gap) without opening any new ones.

**Also did:** Updated `README.md` — it had gone stale (`Code lives in src/ once the Umbraco solution is scaffolded (not yet...)`) despite Phases 0-5 having landed weeks earlier. Added a verified "Running it locally" section with the exact commands (including the flat-key user-secrets gotcha, spelled out so it can't silently recur) and an honest "what's left before test/production" list.

**Next:** Phase 6 — hosting trial & verification, unchanged from the prior entry — still needs Randolf directly for the hosting account signup.

## 2026-09-11 — Portfolio-finalization pass: custom 404/500 error pages

**Did:** Added the site's first real error-handling — a 500 page (`Middleware/ErrorPageMiddleware.cs`,
`app.Map("/error/500", ...)`, used as the re-execute target for `UseExceptionHandler("/error/500")`,
Production/staging-only per the existing Development-detailed-exception-page convention) and a 404
page backed by a real, backoffice-editable Umbraco content node (see below — this went through a
correction mid-session, recorded in full since the reasoning matters for any future error-handling
work on this or another Umbraco project). Both pages show a centered message and a "Back to Home"
link that does a real page reload, not a soft re-render.

**First attempt, abandoned — worth recording so it isn't retried:** the initial implementation was
a plain MVC `ErrorController` + Razor views. It compiled and unit-tested fine in isolation, but
turned out to be completely unreachable in the real running app. Root cause, confirmed live: this
app's `WithEndpoints(u => { u.UseBackOfficeEndpoints(); u.UseWebsiteEndpoints(); })` never calls
`MapControllers()`, and even after adding that, the controller *still* wasn't reached — because
Umbraco's own content-resolution middleware (registered via `u.UseWebsite()`) runs ahead of
ordinary ASP.NET Core endpoint routing/dispatch and short-circuits any path it doesn't recognise as
real content with its own backoffice SPA shell, before endpoint dispatch is ever reached. Forcing
early `app.UseRouting()`/`app.UseEndpoints(...)` ahead of `UseUmbraco()` to compensate made things
worse, not better: it broke the real homepage (confirmed by response body, not just status code —
it started returning the backoffice shell for `/` too), because Umbraco's own content pages aren't
pre-registered endpoints either; they're resolved dynamically by that same middleware, so forcing
routing to resolve earlier just let an unrelated Umbraco backoffice fallback controller
(`BackOfficeDefaultController.Index`) win the route match for literally everything. Reverted all of
that and replaced the controller/views with plain `app.Map()` branches instead — the same primitive
already proven reliable for this app's own `/qa-test-throw` diagnostic — with the HTML inlined as
C# string constants (no Razor, no Umbraco dependency at all). This is genuinely simpler, not just a
workaround: an error page has more reason than most code to not depend on Umbraco's own view/content
resolution being healthy.

**Verified live**, in Production mode against the real seeded SQLite database (`--no-launch-profile`
strips `appsettings.Development.json`'s connection string, so it was supplied directly via
`ConnectionStrings__umbracoDbDSN` env vars for this test only — there is no
`appsettings.Production.json` yet, expected at this pre-Phase-6 stage since real deployment secrets
are meant to come from host-level config per `CLAUDE.md` §8, not a committed file):
- Homepage still renders real content correctly (`<title>Ophir Mineral Ventures — ...</title>`,
  status 200) — confirming the earlier homepage breakage really was the routing experiment, not
  this feature.
- A deliberate unhandled exception (`/qa-test-throw`, removed before commit) correctly re-executes
  to `/error/500`: status 500, the custom "Something Went Wrong" page renders, and the real
  exception message never appears in the response body.
- `/error/404` and `/error/500` hit directly both render correctly with the right status codes.

**Initial limitation (since fixed, see below):** the first cut of this feature made 404 a second
hardcoded page too, for the same "must not depend on Umbraco" reasoning as 500 — and separately, the
automatic re-execute-on-unmatched-route path didn't reach it anyway (Umbraco's own built-in "Page Not
Found" handler writes a response body before `UseStatusCodePagesWithReExecute`'s re-execute condition
is ever checked). Randolf corrected the underlying premise directly: **in any Umbraco app, error
pages should be designed and editable through the CMS itself, the same way Hotelplan's ECMS handles
error pages** — a hardcoded 404 page defeats the entire point of building on a CMS for a
non-technical owner, and `CLAUDE.md`'s own "the owner must be able to edit content himself"
constraint applies to this page as much as any other.

**Fixed the same day**, properly rather than as a patch: `Seed/ErrorPageSeeder.cs` (invoked once via
`dotnet run -- --seed-error-page`, idempotent) creates a real `errorPage` Document Type — `heading`
(Textstring) + `message` (Richtext editor), reusing the exact data types already backing
`legalPage`/`siteSettings` rather than guessing default GUIDs — uSync-exports the schema (committed,
`uSync/v18/ContentTypes|Templates/errorpage.config`), and creates one real content node under Home
with placeholder copy (content, deliberately **not** uSync-tracked, same as every other page's
content — the owner's own data). `Services/ErrorPageContentFinder.cs` implements Umbraco's own
`IContentLastChanceFinder` extension point, querying `IPublishedContentQuery` for the `errorPage`
node by document-type alias and serving it with a 404 status — this runs *inside* Umbraco's normal
content-resolution pipeline, so it doesn't fight the routing-precedence problem the hardcoded
approach hit; it's the content resolution for this case, not something trying to run around it. No
`Umbraco:CMS:Content:Error404Collection` config needed (that approach requires an environment-
specific content-node ID/GUID, which doesn't fit this project's git-versioned schema — querying by
alias at request time sidesteps that entirely). `Views/ErrorPage.cshtml` renders through the site's
real `_Layout.cshtml`, so the 404 page now looks like the rest of the site rather than a bare page.
500 stays exactly as the hardcoded `ErrorPageMiddleware` page described above — a deliberate,
documented exception: an unhandled exception can mean Umbraco's own content/view resolution is what's
actually broken, so that page still must not depend on it being healthy.

**Two real bugs hit and fixed while building this:**
1. `IContentLastChanceFinder` is registered as a singleton, but `IPublishedContentQuery` is scoped
   per request — constructor-injecting it directly failed DI validation at startup ("cannot consume
   scoped service from singleton"). Fixed by injecting `IServiceScopeFactory` and resolving the query
   service through a fresh scope inside `TryFindContent` itself.
2. The Razor view initially failed to compile (`UmbracoCompilationException`, generic wrapper with
   no inner detail in the response body) — root cause turned out to be `@Model.Value<string>("heading")`
   without parentheses around the generic method call; Razor's parser needs `@(Model.Value<string>(...))`
   the same way `LegalPage.cshtml` already does it. Found by deleting the view down to a one-line
   `<p>test</p>` body (confirmed the content-finder wiring itself was correct) and adding lines back
   one at a time until it broke again.

**Verified live** end to end, in both Development mode (real content, real DB) and Production mode
(exception-handler path, real DB supplied via env vars since there's still no
`appsettings.Production.json` at this pre-Phase-6 stage): homepage and other real pages unaffected;
an actually-unmatched URL now returns a genuine 404 status with the real, backoffice-editable content
(heading "We couldn't find that page", the message paragraph, a working "Back to Home" link),
rendered through the site's real layout and CSS; the exception-handler path still returns 500 with
the hardcoded page, and the real exception message still never leaks into the response.

**uSync export side effect, reverted, not committed:** running the seeder's `StartupExportAsync`
re-exported all 12 handlers (92 changes), which — matching the already-documented 2026-09-07 Umbraco
18 `CreateTemplateAsync` template-GUID-churn bug — rewrote every *other* content type's/template's
`AllowedTemplates`/`Template Key` GUID with a freshly-minted one, pure noise with no schema change.
Confirmed via `git diff` and reverted everything except the two genuinely new `errorpage.config`
files, same as the established practice from that earlier entry.

**Deviated from plan:** None from `CLAUDE.md` itself — error handling wasn't previously specified in
detail. The 404 approach (a real Document Type + content node) is a deliberate, small addition to the
content model (CLAUDE.md §5 doesn't list it), justified directly by §1's non-technical-owner
constraint rather than by the general "would plain C# take less effort" test, which pointed the wrong
way here and was corrected.

**Blocked on:** Nothing new. Same outstanding items as the prior entry (SMTP credentials, 2FA
enrollment, client Editor-role login, Phase 6 hosting).

**Next:** Phase 6 — hosting trial & verification, unchanged. If Lakbay.Cms (the other Umbraco-based
project) ever gets error-handling work, apply the same CMS-editable-404/hardcoded-500 split rather
than re-deriving it from scratch.
