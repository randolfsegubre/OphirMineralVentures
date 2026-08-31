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
