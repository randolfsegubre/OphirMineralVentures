# Build Plan — Ophir Mineral Ventures Website

**Read this before writing code.** `CLAUDE.md` (repo root) is the specification — what to build, and why each decision was made. This document is the execution plan — what order to build it in, what "done" means at each step, and what gets documented along the way. If the two ever conflict, `CLAUDE.md` wins on *what*, this file wins on *sequencing*.

## How to use this document

- Work through the phases **in order**. Don't jump to Phase 3 because it looks more interesting — each phase's exit criteria exist because skipping ahead has a specific, named cost (usually: rework, or a security/content gap that's easy to forget once later phases bury it).
- **Every phase ends with a `docs/build/DEVLOG.md` entry** before moving to the next one. One entry per phase minimum — more if a session spans a phase boundary or something notable happened mid-phase (a blocked decision, a changed assumption, a bug that cost real time). See `docs/build/DEVLOG.md`'s own header for the entry format.
- **`docs/build/USER_GUIDE.md` grows alongside the build, not after it.** The moment a feature is usable (the content model exists, the contact form works, the language switcher works), write the corresponding user-guide section while the UI is fresh in context — not from memory during a rushed pre-launch scramble. Phase exit criteria below call this out explicitly wherever it applies.
- Commit per phase (or per meaningful sub-step within a long phase), not as one giant commit at the end. Small, reviewable commits are what makes `git log` useful later, and match the git discipline already established elsewhere in this workspace.
- If you're a fresh Claude Code session starting cold: `CLAUDE.md` loads automatically at session start (it's at repo root). This file does not auto-load — you're reading it because either a human pointed you here, or `CLAUDE.md`'s "Start here" checklist told you to. Read `docs/build/01_CONTENT_MODEL_SPEC.md` before Phase 1, `docs/build/02_TESTING_QA_PLAN.md` before Phase 7, and `docs/build/03_DEPLOYMENT_RUNBOOK.md` before Phase 6 and Phase 9. Don't read all four cover-to-cover before starting Phase 0 — pull each in when its phase arrives.

## Phase overview

| # | Phase | Depends on | Produces |
|---|---|---|---|
| 0 | Environment & scaffold | — | Booting Umbraco install, empty `src/` filled in |
| 1 | Content model foundation | 0 | Document types, compositions, culture variance on |
| 2 | Templates & static structure | 1 | Every page type renders, EN+ZH nav, language switcher |
| 3 | Contact form & security middleware | 2 | Working form, all §7 headers, rate limiting |
| 4 | SEO & compliance plumbing | 2, 3 | sitemap.xml, robots.txt, meta composition wired, schema.org markup |
| 5 | Placeholder content pass | 2 | Every page has real *structure*, placeholder *data* (§10) |
| 6 | Hosting trial & verification | 0 | Tier A free-trial deploy proven to boot, or fallback to Tier B |
| 7 | Security & QA pass | 3, 4 | Headers scan clean, Lighthouse pass, cross-browser check |
| 8 | Real content swap-in | 5, and client answers to §11 | Placeholders replaced with real, client-provided data |
| 9 | Go-live | 6, 7, 8 | DNS cutover, search console submission, live site |
| 10 | User guide finalization & handover | all of the above | `docs/build/USER_GUIDE.md` complete, delivered to John |

## Phase 0 — Environment & scaffold

**Goal:** a booting, unmodified Umbraco install sitting in `src/`, proving the baseline works before any customization begins.

- Confirm `.NET 10.x`, `Node 24.11.1+`, `npm` per `CLAUDE.md`'s first-session checklist step 1. Don't assume the machine still matches the 2026-08-07 snapshot in that file — re-check.
- Run the scaffold command in `CLAUDE.md` §4. Move the generated project into `src/`.
- `dotnet run`, complete the install wizard with SQLite (`CLAUDE.md` §3). No customization yet.
- Confirm the default Umbraco starter kit or blank install loads cleanly in the browser, backoffice included.
- `dotnet new gitignore` inside `src/` per §4's note. Commit the untouched scaffold as its own commit before changing anything — this gives a clean revert point if a later phase goes sideways.

**Exit criteria:** `dotnet run` boots Umbraco, backoffice login works, nothing customized yet. Committed.

**Devlog entry:** environment versions actually found (they may differ from `CLAUDE.md`'s snapshot — note the delta), any scaffold surprises.

## Phase 1 — Content model foundation

**Goal:** every document type and composition from `docs/build/01_CONTENT_MODEL_SPEC.md` exists in Umbraco, culture variance is on, and the schema is synced to disk via uSync.

- Install uSync 18.0.3 (`CLAUDE.md` §3 — the one dependency that earns its place).
- Build `seoComposition` and `siteSettings` first — every content type depends on them.
- Build each document type from `docs/build/01_CONTENT_MODEL_SPEC.md` in the order listed there (compositions → containers → leaf content types). Apply both compositions where the spec says to.
- Turn on "Allow vary by culture" at build time, not retroactively — `CLAUDE.md` §4a explains why retrofitting this is genuinely broken (umbraco-cms#22159), not just extra work.
- Add both languages (en-US default, zh-Hans) in Settings → Languages before marking any property culture-variant — Umbraco needs the language to exist first.
- Run a uSync export, commit the synced schema files. This is what makes the content model reviewable in `git diff` from here on.

**Exit criteria:** every document type in `docs/build/01_CONTENT_MODEL_SPEC.md` exists, both compositions applied correctly, both languages active, uSync export committed.

**Devlog entry:** any content-model deviation from the spec doc and why (update the spec doc itself too, don't let it drift out of sync with reality).

## Phase 2 — Templates & static structure

**Goal:** every page type has a working Razor view, the site has real navigation and a working language switcher, both cultures render.

- Build one view per document type, matching the ModelsBuilder-generated model names (check the model exists before hand-rolling `IPublishedContent` access — `CLAUDE.md` §5's closing note).
- Shared layout: header (logo slot, nav, language switcher), footer (siteSettings-driven company details), both following Umbraco's own documented multilanguage pattern (`CLAUDE.md` §4a) — don't hand-roll the culture loop.
- Implement the `prefers-color-scheme: dark` token-based theme per `CLAUDE.md` §4a, copying the token structure from `docs/proposals/source-html/*.html` rather than inventing new tokens.
- Populate every page in **both** cultures with placeholder text (§10 policy) as you build it — building English-only "to be translated later" is exactly the rework §4a's culture-variance decision exists to avoid. Placeholder Chinese text is fine at this phase (e.g. `[zh-Hans placeholder — pending client translation]`); the point is proving the switcher and the culture-fallback behavior work end to end, not producing real copy yet.
- Verify the language switcher actually flips culture on every page type, not just Home.

**Exit criteria:** every page type renders in both cultures, nav/footer/switcher work site-wide, dark-mode tokens applied.

**Devlog entry:** which pages are done, any template/design deviations from the homepage mockup (`docs/proposals/02-Homepage-Design-Draft.pdf`) and why.

**User guide checkpoint:** write the `USER_GUIDE.md` sections on "the content tree" and "how the language switcher works" now, while you're looking at the actual structure.

## Phase 3 — Contact form & security middleware

**Goal:** the site's only public write path works and is hardened; every §7 requirement is implemented, not deferred.

- Implement `ContactSurfaceController` per `CLAUDE.md` §6 exactly — anti-forgery token, honeypot, Post-Redirect-Get.
- Wire `IEmailSender` (SendGrid free tier or MailKit+SMTP per §3) behind an interface — ordinary DI, not a ports-and-adapters pattern (§4a is explicit that this distinction matters, don't over-describe it).
- Add ASP.NET Core's built-in `RateLimiter` middleware on the POST endpoint.
- Add the full security header middleware block from `CLAUDE.md` §7 verbatim, then verify with a headers-scanning tool locally (see `docs/build/02_TESTING_QA_PLAN.md`).
- Enable 2FA on the Umbraco backoffice via built-in Identity 2FA.
- Set the client's eventual login to Editor role, never Administrator — do this now so it's not forgotten under later pressure.

**Exit criteria:** contact form submits and emails successfully, honeypot silently drops bot posts, rate limit triggers under rapid repeat submission, every header in §7 present on every response, 2FA enabled.

**Devlog entry:** which email provider was actually wired up, and the exact recipient address confirmed against `CLAUDE.md` §11's open question (the `to:` address in §6's code sample is a placeholder pending client confirmation — don't ship it unverified).

## Phase 4 — SEO & compliance plumbing

**Goal:** the technical SEO baseline from the proposal's SEO section is actually implemented, not just promised.

- `seoComposition` fields (metaTitle, metaDescription, ogImage) actually rendered into `<head>` on every page.
- `sitemap.xml` generated (dynamically from published content, both cultures, correct `hreflang` alternates per page).
- `robots.txt` present, not blocking anything it shouldn't.
- Structured data (schema.org `Organization`/`LocalBusiness` markup, driven by `siteSettings`) on at least Home and Contact.
- Confirm Cloudflare cache rules bypass `/umbraco/*` and the contact-form POST per `CLAUDE.md` §4a — verify this once Cloudflare is actually in front of something in Phase 6, but write the rule now if the account exists.

**Exit criteria:** sitemap reachable and valid, meta tags populated from real Umbraco properties (not hardcoded), structured data validates in Google's Rich Results Test.

**Devlog entry:** sitemap/robots URLs once a real domain is pointed at the site (may not be until Phase 6/9 — note as pending if so).

## Phase 5 — Placeholder content pass

**Goal:** every page has its *final structure* — every Block List section, every repeatable item type — populated with placeholder data, so the site is demo-able and reviewable before any real client data exists.

- Sweep every page type, fill every property with placeholder content following `CLAUDE.md` §10's rules exactly (obvious bracketed placeholders for anything that looks like real business data — registration numbers, permits, address, bio, ore-spec figures).
- This phase is what makes the site something you can screenshot or demo to the client mid-build without any portfolio-safety risk (`CLAUDE.md` §10's portfolio-use subsection) — verify nothing here could be mistaken for real data before treating this phase as done.

**Exit criteria:** every page fully populated with structurally-final, data-placeholder content, in both cultures.

**Devlog entry:** note this phase's completion explicitly — it's the natural point to send the client a first working preview link, if hosting (Phase 6) has landed by then.

## Phase 6 — Hosting trial & verification

**Goal:** resolve `CLAUDE.md` §8's open verification gate — confirm Tier A (SmarterASP.NET-class) actually runs this Umbraco install before any paid commitment.

Follow `docs/build/03_DEPLOYMENT_RUNBOOK.md` in full for this phase — it has the actual step-by-step actions. Summary:

- Deploy to the host's free trial (60 days, no card required for SmarterASP.NET).
- Confirm: site boots, backoffice loads, SQLite reads/writes correctly, both cultures render.
- **If it passes:** proceed to a paid Tier A term.
- **If it fails:** fall back to Tier B (Azure) without re-litigating the plan — `CLAUDE.md` §8 already prices both and says moving tiers costs nothing but time.
- Point Cloudflare (free tier) at whichever origin was chosen. Region = Singapore/Hong Kong/SE Asia on whichever host, per §8 — never let it default to US/Europe.

**Exit criteria:** a real, reachable staging URL (not `localhost`) serving the actual site, on the confirmed hosting tier, behind Cloudflare.

**Devlog entry:** which tier passed the trial and why (or why it failed and the fallback decision), actual staging URL.

## Phase 7 — Security & QA pass

**Goal:** verify, don't assume, that Phases 3 and 4's work actually holds up under real scanning tools, using `docs/build/02_TESTING_QA_PLAN.md` as the checklist.

- Headers-scanning tool against the live staging URL — confirm every §7 header present with correct values.
- Lighthouse/PageSpeed on mobile — target scores in `docs/build/02_TESTING_QA_PLAN.md`.
- Cross-browser/device spot check (see the QA plan's matrix).
- Confirm rate limiting and the honeypot actually behave correctly against the *deployed* instance, not just localhost — hosting environments sometimes handle middleware ordering or reverse-proxy headers differently than `dotnet run`.

**Exit criteria:** every item in `docs/build/02_TESTING_QA_PLAN.md`'s pre-launch section checked off against the real staging deployment.

**Devlog entry:** scan results, any fixes made in response.

## Phase 8 — Real content swap-in

**Goal:** replace every placeholder from Phase 5 with real client-provided data, once `CLAUDE.md` §11's open questions are answered.

- This phase cannot start in earnest until the client has answered §11 — treat that list as a hard dependency, not a nice-to-have. If some answers arrive before others, swap in what's available rather than waiting for the full set — but track what's still outstanding.
- Real Simplified Chinese translation goes in here too, from the named bilingual person confirmed per `CLAUDE.md` §4a/§11 — not machine-translated, not written by the developer unless explicitly asked and qualified.
- Re-run the §10 placeholder sweep at the end of this phase — the exit criteria for Phase 5 was "no *real-looking* placeholder," the exit criteria here is "no placeholder at all."

**Exit criteria:** zero bracketed placeholders remain published, in either culture. This directly satisfies `CLAUDE.md` §12's go-live checklist item on placeholder data.

**Devlog entry:** what was still outstanding from §11 at go-live time, if anything was launched with a known gap (and the plan to close it).

## Phase 9 — Go-live

Follow `docs/build/03_DEPLOYMENT_RUNBOOK.md`'s go-live section exactly. Summary:

- DNS cutover: A/CNAME only. **Never touch MX records** — this is a hard invariant, not a suggestion (`CLAUDE.md` §8).
- Verify client email still works immediately after cutover, before considering this phase done.
- Submit sitemap.xml to Google Search Console and Bing Webmaster Tools.
- Create/claim Google Business Profile.
- Final Lighthouse pass on the production URL (not staging).

**Exit criteria:** every box in `CLAUDE.md` §12's Definition of Done checked, live production URL, email confirmed unaffected.

**Devlog entry:** go-live date/time, final verification results.

## Phase 10 — User guide finalization & handover

**Goal:** `docs/build/USER_GUIDE.md` is complete, accurate against the *actual* shipped backoffice (not the plan), and handed to John before or at go-live — this is the explicit deliverable the engagement promised (client proposal Part II.2; `CLAUDE.md` §1's non-technical-owner constraint).

- Walk through `docs/build/USER_GUIDE.md` section by section against the real, live backoffice. Every screenshot, every instruction — confirm it matches what John will actually see, not what was planned in Phase 2.
- Add real screenshots (currently the guide has placeholder callouts marking where they go — see that file's header).
- Confirm the guide covers, at minimum: logging in, understanding the content tree, editing an English page, editing the matching Chinese page (split-view), publishing vs. saving as draft, adding a news article, replacing the logo/photos, managing certification uploads, and who to contact if something breaks.
- Hand the finished document to John — as a PDF (matching the existing `docs/proposals/` PDF-export workflow) alongside the live site, not as a markdown file he has no way to open comfortably.

**Exit criteria:** `docs/build/USER_GUIDE.md` complete and PDF-exported, delivered to the client. This closes the loop the user (Randolf) explicitly required before considering this project done.

**Devlog entry:** handover date, delivery format, any follow-up training/call scheduled.

---

**If you're picking this project back up after a long gap:** check `docs/build/DEVLOG.md`'s most recent entry for exactly where things stand, then re-verify — don't trust — every version number and open question against `CLAUDE.md`'s own "re-verify versions" rule (§ near the end) and §11's open-questions list, since both can go stale between sessions.
