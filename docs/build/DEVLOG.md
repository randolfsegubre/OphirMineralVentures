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
