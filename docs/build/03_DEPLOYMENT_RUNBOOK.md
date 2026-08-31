# Deployment Runbook

Concrete step-by-step actions behind `CLAUDE.md` §8 (hosting tiers) and §12 (go-live checklist). Used in Build Phase 6 (trial verification) and Phase 9 (go-live). Read `CLAUDE.md` §8 in full before starting — this document is the "do this" version of what that section already reasons through.

## Phase 6 — Tier A free-trial verification (do this before spending any money)

**Why this phase exists:** `CLAUDE.md` §8 documents a real, unclosed evidence gap — SmarterASP.NET-class hosting has no current independent confirmation of running Umbraco 13+. Only a decade-old Umbraco 7.2 report exists, for an architecturally unrelated hosting model. This phase closes that gap with a real trial deploy instead of assuming it'll work.

1. Sign up for the host's free trial (SmarterASP.NET: 60 days, no card required). Region: **Singapore** — do not let it default to US/Europe (§8's region note — this isn't cosmetic, it's real latency for the client's own daily backoffice use from the Philippines).
2. Publish the Phase 0–5 build (`dotnet publish -c Release`) and upload/deploy per the host's ASP.NET Core deployment method (Web Deploy, FTP, or their control panel's app manager — whichever the trial account offers).
3. Verify, in order:
   - The site boots at the trial subdomain (no 500 error, no missing-runtime error).
   - The Umbraco backoffice loads and login works.
   - SQLite reads and writes correctly — publish a test edit from the backoffice and confirm it persists across a request (i.e. isn't silently failing to write to the file system, which is the realistic failure mode on locked-down shared hosting).
   - Both cultures (en-US, zh-Hans) render correctly.
4. **If all of the above pass:** proceed to purchase a paid Tier A term on this host, same Singapore region.
5. **If anything fails:** stop. Do not debug shared-hosting file-permission issues indefinitely — `CLAUDE.md` §8 is explicit that Tier B (Azure) is priced and ready as a no-argument fallback. Move to it, note why in `docs/build/DEVLOG.md`, and don't re-litigate the plan.
6. Try the second Tier A candidate — [ASPHostPortal.com](https://asphostportal.com) — as an alternative trial if the first host fails and Azure feels premature; it explicitly markets "Full Trust" hosting, which is the more directly relevant claim for Umbraco. Same trial-before-paying discipline applies.

**Tier B (Azure) needs no such trial** — Umbraco publishes official Azure App Service docs, so the risk this phase exists to de-risk doesn't apply there. If Tier B is chosen (either as the fallback or as the deliberate first choice), skip straight to provisioning:

- Azure App Service, **B1 Linux**, region **Southeast Asia (Singapore)** or **East Asia (Hong Kong)**.
- Azure SQL Basic (replaces SQLite for this tier — `CLAUDE.md` §3).
- Use the CI/CD skeleton in `CLAUDE.md` §8 as the starting point for the GitHub Actions workflow — check the Marketplace for newer `@v6`/`@v3` Action tags before reusing verbatim, per that section's own note.

## Cloudflare setup (either tier)

1. Add the domain to Cloudflare (free tier).
2. DNS: only add/point the **A or CNAME record** for the web host. **Do not touch MX records** — the client's Google Workspace email routes through them and must be untouched by this project, full stop.
3. Cache rules: bypass `/umbraco/*` entirely (never cache backoffice routes) and bypass the contact-form POST endpoint. Cache everything else with a long TTL; purge on publish (either manually per release, or via Umbraco's own cache-purge webhook to the Cloudflare API if that's worth automating later — not required for launch).
4. SSL/TLS mode: Full (Strict) once the origin has a valid certificate — never "Flexible," which would break `CLAUDE.md` §7's HSTS requirement end-to-end.
5. WAF: Cloudflare's default free-tier ruleset is enough at this traffic level — nothing custom needed for launch.

## Phase 9 — Go-live

1. **Confirm Phase 6, 7, and 8 are all actually complete** (`docs/build/00_BUILD_PLAN.md`'s exit criteria for each) — go-live is not the phase to discover Phase 8's client-content swap-in is half-done.
2. DNS cutover: update the A/CNAME record (already pointed at the trial in Phase 6 — this step is really "confirm it's still correct" if Cloudflare/hosting were already live during Phases 6–8, or the actual first cutover if a separate staging subdomain was used until now).
3. **Immediately verify email still works** — send a real test email to the client's address and confirm receipt. This is the single highest-consequence thing to get wrong in this whole runbook; if MX records were somehow touched, this is where it would surface.
4. Submit `sitemap.xml` to Google Search Console (add/verify the property first if not already done) and Bing Webmaster Tools (covers Yahoo's index too, per `CLAUDE.md` §12).
5. Create or claim the Google Business Profile listing for Ophir Mineral Ventures, Inc.
6. Run a final Lighthouse/PageSpeed pass against the **production** URL (not staging — caching, CDN, and TLS behave differently once live, so a stale staging result isn't sufficient evidence).
7. Re-run the headers scan from `docs/build/02_TESTING_QA_PLAN.md` against production.
8. Walk `CLAUDE.md` §12's full checklist and confirm every box, including the bilingual-completeness item.
9. Only after all of the above: hand over `docs/build/USER_GUIDE.md` (Build Phase 10) and consider the engagement's go-live milestone complete.

## Rollback plan

If go-live surfaces a serious problem (site not booting on production, email broken, a security header missing in the production environment specifically):

1. **Email first, always** — if there's any doubt MX records were touched, revert DNS immediately and verify email before doing anything else. This is the one irreversible-feeling risk in the whole plan; treat it as such.
2. For a broken site: revert the A/CNAME record back to wherever it pointed before cutover (parking page, or nothing) while the problem is fixed on staging, then re-attempt cutover. Don't debug live production DNS in place if reverting is available.
3. Because deployment is git-driven (Phase 6's CI/CD), a broken release can also be rolled back by redeploying the last known-good commit rather than only via DNS — use whichever is faster to restore correct behavior.

## Secrets

Deploy credentials and the email-provider API key (SendGrid or SMTP credentials) live in GitHub Actions secrets (Tier B) or the host's own environment-variable/app-settings panel (Tier A). Never in `appsettings.json`, never committed — this is already stated in `CLAUDE.md` §8, repeated here because it's the kind of thing worth re-stating right where the actual deploy action happens.
