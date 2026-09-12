# Ophir Mineral Ventures — Website

Corporate website for Ophir Mineral Ventures, Inc., a Philippines-based nickel/chromite ore exporter. Compliance-first, marketing-second, built so the owner can manage content himself.

- **Working on the code?** Read [`CLAUDE.md`](./CLAUDE.md) first — it's the full technical spec (stack, content model, security requirements, hosting, deployment).
- **Looking for the business proposals?** See [`docs/proposals/`](./docs/proposals/) — the plan/architecture, homepage design draft, cost proposal, and the client-facing proposal sent to the owner, as both PDF and editable HTML source.
- **Code lives in** `src/` — the Umbraco solution is scaffolded and Phases 0-5 are built (content model, templates for both EN/中文 cultures, the contact form, security middleware, SEO plumbing, placeholder content), plus a custom 404/500 error-page pass: the 404 page is a real, backoffice-editable content node (not hardcoded) so John can rewrite what it says without a code change, and the 500 page is a deliberate hardcoded fallback for when Umbraco itself might be what's broken. See `docs/build/DEVLOG.md` for the phase-by-phase history and current status; Phase 6 (hosting trial) is next.

Stack: Umbraco CMS on ASP.NET Core (self-hosted). No PHP, no page-builder subscription, no ongoing CMS license fee.

## Running it locally (clean checkout)

**Verified working end-to-end 2026-09-12** — build clean, 23/23 tests passing, front end renders in both cultures, backoffice login confirmed in a real browser, and both the 404 (real content node) and 500 (hardcoded fallback) error pages confirmed live against the real seeded database.

Prerequisites: .NET SDK 10.0.x, Node 24.11.1+, npm (see `CLAUDE.md` §1 checklist — versions drift, re-verify).

```bash
cd src
dotnet build OphirMineralVentures.slnx      # should be 0 warnings, 0 errors
dotnet test OphirMineralVentures.slnx       # 20 tests, all passing

cd OphirMineralVentures.Web

# One-time: configure the unattended-install admin account.
# MUST be these exact flat keys — a nested shape silently fails to bind
# (no error, no admin user created). See CLAUDE.md-adjacent project memory
# for the full story if this ever regresses.
dotnet user-secrets set "Umbraco:CMS:Unattended:InstallUnattended" "true"
dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserName" "Your Name"
dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserEmail" "you@example.com"
dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserPassword" "SomeStrongLocalPassword!1"

# One-time: the image-processing HMAC signing key. The committed appsettings.json
# deliberately ships this empty — Umbraco's ImageSharp middleware works fine
# without it locally, but a real value belongs in user-secrets (local) or
# host-level config (production), never committed. Generate one with:
#   openssl rand -base64 64
dotnet user-secrets set "Umbraco:CMS:Imaging:HMACSecretKey" "<a real random value>"

# Trust the HTTPS dev cert if you haven't already (needed for backoffice login):
dotnet dev-certs https --trust

# First run: boots Umbraco, creates the SQLite DB and the admin user via unattended install.
dotnet run --urls "https://localhost:44325;http://localhost:1153"
# Ctrl+C once it says "Application started."

# Seed the content model (uSync import) + placeholder content tree — only ever
# run this against a fresh/empty database, it always creates new nodes:
dotnet run -- --seed-phase2

# One-time, idempotent: creates the errorPage Document Type + a real 404
# content node (safe to re-run, skips if it already exists):
dotnet run -- --seed-error-page

# Normal run from here on:
dotnet run --urls "https://localhost:44325;http://localhost:1153"
```

Then open `https://localhost:44325/` for the public site (language switcher top-right) and `https://localhost:44325/umbraco/` for the backoffice, logging in with the e-mail/password you set above.

### What's left before this could go to a test/production deployment

- **Real hosting** — Phase 6 (free-trial deploy to confirm Umbraco actually boots on the chosen Tier A host) hasn't started yet; needs Randolf directly (account signup), per `CLAUDE.md` §8's gate.
- **Contact form email delivery is unverified** — `IEmailSender` (MailKit/SMTP) is wired and unit-tested, but no real SMTP credentials exist anywhere (local or prod). A real submission currently fails gracefully with a friendly error rather than 500ing, but no email has ever actually been sent. Needs the client's Google Workspace SMTP credentials, per `CLAUDE.md` §11.
- **2FA is implemented but not enrolled** — Umbraco's built-in Identity 2FA is available; enabling it needs an interactive login + authenticator app, not something to do headlessly.
- **All content is placeholder** — every business fact (SEC/DTI/DENR-MGB numbers, address, phone, bio, ore-spec figures) is a bracketed placeholder per `CLAUDE.md` §10, and every zh-Hans field is a translation placeholder pending the client's named bilingual reviewer. No real media (photos, logos, PDFs) has been uploaded.
- **Only one admin user exists** (whoever ran the unattended install). The client's own login and its Editor-role restriction (`CLAUDE.md` §7 — never Administrator) hasn't been created yet.
- **Cloudflare cache rules, DNS, and the real domain** don't exist yet — everything currently resolves against `localhost`.
- **uSync template GUIDs churn on every fresh re-seed** — a known Umbraco 18 API bug (`ITemplateService`/`IContentTypeService.CreateTemplateAsync` fails silently during uSync's own import; the seeder falls back to creating templates manually, which mints new GUIDs each time). Harmless for a single local database, but re-seeding a *second* fresh environment will produce a different set of Template `Key` values than what's committed in `uSync/v18/` — don't commit those regenerated GUIDs back, they're not a real schema change. See `docs/build/DEVLOG.md`'s Phase 2 entry for the full root cause.
