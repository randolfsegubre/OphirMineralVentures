# Kickoff prompt — paste this into a fresh Claude Code session to start the build

**How to use this:** open a new Claude Code session rooted at `D:\_DEV\Personal_Projects\OphirMineralVentures` (or `D:\_DEV` — either works, since `CLAUDE.md` auto-loads by proximity either way), and paste the prompt below as your first message. It's self-contained — the new session doesn't have this conversation's history, so it needs to be told everything it needs, not just pointed at files and left to guess intent.

---

I'm ready to start building the actual Ophir Mineral Ventures website. This is a real freelance client engagement — not a prototype — so treat the plan and every documented constraint as load-bearing, not as suggestions to improve on.

Before writing any code:

1. Read `CLAUDE.md` (repo root) in full — it's the specification: stack, architecture decisions, content model, security requirements, hosting tiers, and what's explicitly out of scope. It auto-loads, but read it deliberately rather than skimming.
2. Read `docs/build/00_BUILD_PLAN.md` in full — it's the execution plan, broken into phases with entry/exit criteria. Work through the phases **in order**. Don't skip ahead because a later phase looks more interesting or a specific feature seems more urgent.
3. Pull in `docs/build/01_CONTENT_MODEL_SPEC.md`, `docs/build/02_TESTING_QA_PLAN.md`, and `docs/build/03_DEPLOYMENT_RUNBOOK.md` when their respective phases arrive, per `00_BUILD_PLAN.md`'s own guidance — you don't need all of them loaded up front.

Documentation discipline for this whole build, not optional polish:

- **End every phase with an entry in `docs/build/DEVLOG.md`** before moving to the next phase — what you did, anything you deviated from the plan and why (and update the source-of-truth doc in the same commit if it was a real decision, not a typo), what you're blocked on, what's next. Short and honest is fine — this is a working log, not a report written to impress someone.
- **Grow `docs/build/USER_GUIDE.md` as you build**, not after. The moment a feature is real and usable (content model exists, contact form works, language switcher works), write or correct that section of the guide while it's fresh — the guide currently has placeholder `[SCREENSHOT: ...]` markers and draft prose; replace/refine as the real UI takes shape rather than leaving it for a rushed pass at the very end.
- **Commit per phase or per meaningful sub-step**, not as one giant commit. Small, reviewable commits, clear messages.

Working rhythm — where to pause and check with me rather than proceeding unattended:

- **Phases 0–5** (environment, content model, templates, contact form/security, SEO plumbing, placeholder content) are structural build work with no money and no client dependency — work through these autonomously, checking in at natural phase boundaries with a short status update, but you don't need my sign-off to proceed from one to the next.
- **Before Phase 6** (hosting trial): stop and confirm with me before signing up for any hosting trial account, even a free one — account creation is something I want to do myself or explicitly approve, not have created on my behalf.
- **Before any paid hosting commitment**: hard stop, always. Phase 6 says "commit to a paid plan after the trial passes" — that decision and the actual payment are mine to make, not something to execute autonomously even if the trial clearly passed.
- **Phase 8** (real content swap-in) is blocked on client answers to `CLAUDE.md` §11 — don't fabricate plausible-sounding values to keep moving. If some of those answers exist by the time you reach this phase, use them; if not, flag exactly what's still missing and move on to whatever isn't blocked (there's usually still testing/QA or deployment-runbook work available even with content gaps open).
- **Phase 9** (go-live / DNS cutover): stop and confirm with me before actually cutting over DNS, even though the runbook is fully written — this is the one step that risks the client's real, live email if anything about the MX-record safeguard goes wrong, and I want eyes on it personally before it happens.
- **Phase 10** (user guide handover): once the guide is finalized against the real backoffice, let me review it before treating the engagement's documentation deliverable as "done" — this goes to a real client, John, and I want a final look.

A few things worth restating even though they're already in `CLAUDE.md`, because they're the easiest things for a fresh session to get subtly wrong under time pressure:

- **Bilingual (English + Simplified Chinese) is v1 scope, built from the start** — not retrofitted after English is done. Build every page in both cultures as you go (`CLAUDE.md` §4a explains why retrofitting culture variance has real, documented migration problems).
- **Never invent plausible-looking real business data** — registration numbers, permits, addresses, bios, product specs. Obvious bracketed placeholders only, until I provide the real values (`CLAUDE.md` §10).
- **Security headers, 2FA, rate limiting, and the honeypot are part of "done" for the contact form**, not a follow-up pass — `CLAUDE.md` §7 is explicit these aren't optional.
- **Don't reach for DDD/Clean/Hexagonal layering or a headless/decoupled CMS setup** — `CLAUDE.md` §4a explains exactly why this project deliberately isn't built that way. If a pattern from another project in this workspace feels like it "should" apply here, it probably doesn't — this one's scope and constraints are different on purpose.
- **Build TDD for the project's actual custom logic** — `CLAUDE.md` §4 and `docs/build/02_TESTING_QA_PLAN.md`'s "Development practice" section. Scaffold the xUnit test project in Phase 0, not deferred. From Phase 3 onward (contact form, security middleware, rate limiter, sitemap generation), write the failing test first, then implement — don't write the code and backfill tests after. This is scoped to real logic only: don't try to force a unit test onto an Umbraco document type or a Razor template, there's no testable contract there. This is an internal engineering practice — it's deliberately not in the client-facing proposal (Randolf's call), so don't surface it in anything meant for John.

Start with Phase 0. Confirm the environment, scaffold the solution, and get a booting, unmodified Umbraco install committed before touching any customization — same as the plan says.
