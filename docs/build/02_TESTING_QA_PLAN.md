# Testing & QA Plan

This site has no dedicated test project by default (`CLAUDE.md` §4 — xUnit only if genuine logic like sitemap generation or contact-form validation warrants it). QA here is mostly manual verification against real tools, not an automated suite. That's a deliberate scope call for a brochure site this size, not a corner cut.

## Functional checks (Build Phase 2–3, re-run in Phase 7 against staging)

- [ ] Every page type renders in both **en-US** and **zh-Hans**.
- [ ] Language switcher flips culture correctly from every page (not just Home), and lands on the *equivalent* page in the other culture, not the home page of that culture.
- [ ] Culture fallback behaves sanely if a zh-Hans placeholder is genuinely empty rather than filled with placeholder text (shouldn't happen post-Phase 2, but verify Umbraco doesn't 404 — it should fall back per the culture-fallback settings configured in Phase 1).
- [ ] Contact form: valid submission sends an email and redirects (Post-Redirect-Get) with a visible success state.
- [ ] Contact form: honeypot field silently drops the submission (no email sent, no error shown) — verify by scripting a submission with the honeypot field filled, not just leaving it visibly for a human to skip.
- [ ] Contact form: anti-forgery token rejection behaves correctly (submit without a valid token, e.g. via curl, confirm it's rejected).
- [ ] Contact form: rate limiter triggers after the configured threshold — script a burst of requests from one IP and confirm later ones are throttled.
- [ ] Dark mode: toggling the OS-level `prefers-color-scheme` (browser DevTools → Rendering → emulate CSS prefers-color-scheme) swaps the token palette correctly, no unstyled flash, no hardcoded light-only colors anywhere.
- [ ] All Media Picker file downloads (spec sheets, certificate PDFs) actually resolve and open.

## Security checks (Build Phase 7, against the real staging deployment — not `localhost`)

- [ ] Headers scan (e.g. securityheaders.com, or an equivalent CLI tool if the staging URL isn't yet publicly reachable) confirms every header from `CLAUDE.md` §7 is present with the exact values specified: `Strict-Transport-Security`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy`, `Content-Security-Policy`.
- [ ] HTTPS redirect works (`http://` → `https://` on first request).
- [ ] `/umbraco/*` requires authentication; unauthenticated requests to backoffice routes are rejected, not silently served.
- [ ] 2FA is actually enforced on the backoffice login, not just enabled and skippable.
- [ ] Confirm the deployed instance's rate limiter and honeypot behavior match what was verified locally in Phase 3 — reverse-proxy header handling (host, forwarded-for) can behave differently once deployed; don't assume localhost behavior transfers.
- [ ] Dependency count still matches `CLAUDE.md` §3's expectation (uSync plus whatever email-sending package was chosen) — if something else got added along the way, that's worth a deliberate note in the devlog, not a silent scope creep.

## Performance (Build Phase 7, against staging)

- [ ] Lighthouse (mobile emulation) — target: Performance 90+, Accessibility 90+, Best Practices 90+, SEO 90+. If Performance lands meaningfully below 90, check image sizing/compression before anything more elaborate — this is a low-traffic brochure site, the usual culprit is unoptimized media, not architecture.
- [ ] PageSpeed Insights (real Google tool, not just local Lighthouse) — same targets, confirms results aren't a local-only artifact.
- [ ] Confirm Cloudflare cache is actually serving cached HTML on repeat requests (check response headers for a cache-hit indicator), and that `/umbraco/*` and the contact-form POST are correctly bypassing cache — a cached POST or a cached backoffice response would be a real bug, not a performance nuance.

## Cross-browser / device spot check

Not exhaustive — this is a brochure site with server-rendered, dependency-light pages, so the realistic risk surface is small. Check:

- [ ] Latest Chrome, Firefox, Safari (desktop) — layout, dark mode, language switcher.
- [ ] Mobile Safari (iOS) and Chrome (Android) — nav collapses sensibly, contact form usable, tap targets aren't cramped.
- [ ] One older/smaller viewport (e.g. 360×640) — confirm nothing overflows horizontally.

## Content QA (Build Phase 5 exit, and again at Phase 8 exit)

- [ ] Full placeholder sweep per `CLAUDE.md` §10 — every business-data-shaped field either has real client-provided data (Phase 8) or an obvious bracketed placeholder (Phase 5), never a plausible-looking invented value.
- [ ] Bilingual parity: every published English page has a published, human-reviewed (not machine-translated) Chinese counterpart — this is `CLAUDE.md` §12's explicit go-live checklist item, don't let it slip through as "mostly done."
- [ ] Screenshot/demo safety check per §10's portfolio-use subsection: before capturing anything for external use, confirm what's on screen is placeholder, not real client data.

## Pre-launch checklist (Build Phase 9 gate)

This mirrors `CLAUDE.md` §12 — treat that section as authoritative; this is the working checklist to physically go through:

- [ ] DNS: A/CNAME correct, MX untouched, email verified working post-cutover.
- [ ] HTTPS/HSTS/headers verified on the *production* URL, not just staging.
- [ ] sitemap.xml submitted to Google Search Console and Bing Webmaster Tools.
- [ ] Google Business Profile created/claimed.
- [ ] Lighthouse/PageSpeed pass on the production URL.
- [ ] Zero placeholder data remains published, either culture.
- [ ] `docs/build/USER_GUIDE.md` is finalized and delivered (Build Phase 10) — go-live and handover should land together, not the site launching weeks before John has a guide to operate it.

If any box here can't be checked, that's a reason to hold go-live, not a reason to launch and fix it after — the whole point of a pre-launch gate is catching this before the client is looking at it.
