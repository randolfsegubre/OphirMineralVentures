# Ophir Mineral Ventures — Website

This file is the project's source of truth for any AI coding assistant (Claude Code, or any other LLM) working in this repository. Read it before writing code. It supersedes generic defaults — where it's specific, follow it over a more "standard" pattern you might otherwise reach for.

## 1. What this project is

A custom corporate website for **Ophir Mineral Ventures, Inc.**, a Philippines-based nickel and chromite ore exporter owned by John David Montilla. Two purposes, in priority order:

1. **Compliance credibility** — a legitimate public presence for regulators, banks, and overseas buyers running due diligence on large ($100K–$40M range) ore shipments.
2. **Marketing / deal-closing** — professional collateral the owner can point buyers to.

A close third, load-bearing constraint: **the owner (non-technical) must be able to edit content himself**, and **the developer must be able to maintain this indefinitely using only skills he already has** (senior full-stack .NET/C#/Umbraco/ASP.NET Core — no PHP, no unfamiliar ecosystem). That second constraint is *why* this is built on Umbraco instead of WordPress — see `docs/proposals/01-Website-Plan-and-Architecture.pdf` §05 for the full reasoning if it's ever questioned.

This is not a high-traffic site and never needs to be engineered like one. It's a low-traffic, high-trust B2B brochure site. Don't over-architect it.

## Start here — first-session checklist

If this is the first coding session on this project, do these in order before writing any Umbraco-specific code:

1. Confirm the environment (all three verified present on this machine 2026-08-07):
   - `dotnet --list-sdks` → .NET 10.x (10.0.301 present) — Umbraco 18 requires .NET 10.0+
   - `node --version` → v24.11.1 or higher (v24.18.0 present) — required by Umbraco 18's backoffice tooling
   - `npm --version` (11.16.0 present)
2. Scaffold the solution per §4 into `src/`.
3. `dotnet run` and confirm the default Umbraco install wizard loads in the browser before customizing anything — working baseline first, customization second.
4. Complete the install wizard using SQLite (§3) — no separate database server to stand up.
5. Create the Document Types in §5, applying both compositions (`seoComposition`, `siteSettings`) rather than duplicating their properties per type.
6. Build templates/views page by page against the sitemap in §5, using placeholder content per the §10 policy. Don't wait on real client content to build structure. Build for both active cultures (§4a) from the start — retrofitting the language switcher onto English-only templates later is exactly the rework §4a's culture-variance decision exists to avoid.
7. Implement the contact form (§6) and the security middleware (§7) before considering any page done — they're small, and retrofitting security headers after the fact is exactly the kind of thing that gets skipped.
8. Only check off the §12 go-live list once every placeholder has been replaced with real client-provided content (§11).
9. **Before purchasing a paid Tier A hosting plan**, deploy to the host's free trial first and confirm Umbraco actually boots (§8's "Before committing money" gate) — this is unverified for the specific host until an actual trial deploy proves it.

If anything below conflicts with what you actually observe in the code, a newer Umbraco/.NET release, or something the client says — trust what you observe now, not this file. It was accurate as of 2026-08-07; the ecosystem moves.

## 2. Repository layout

```
OphirMineralVentures/
├── CLAUDE.md                  ← this file
├── README.md                  ← short human-facing pointer
├── docs/
│   └── proposals/             ← business documents (PDFs = what was sent/shown; source-html = editable originals)
│       ├── 01-Website-Plan-and-Architecture.pdf
│       ├── 02-Homepage-Design-Draft.pdf
│       ├── 03-Website-Cost-Proposal.pdf
│       ├── 04-Client-Proposal.pdf
│       └── source-html/       ← the .html source of each PDF above, edit these then re-export
└── src/                       ← the actual Umbraco solution — does not exist yet, see §3
```

`src/` is intentionally empty until the solution is scaffolded (§3). Don't create placeholder files in it.

## 3. Tech stack — decided, do not re-litigate without asking

| Layer | Choice | Notes |
|---|---|---|
| CMS | **Umbraco 18.1.0** (latest release) | Open source, MIT license, free regardless of scale. **STS, not LTS** — see §3a before assuming this is a set-and-forget choice |
| Framework | **ASP.NET Core on .NET 10** | Umbraco 18 requires ".NET 10.0 and higher". SDK 10.0.301 verified installed 2026-08-07. Pin in `global.json` once scaffolded |
| Node.js | **24.11.1+** (v24.18.0 verified) | Required by Umbraco 18's backoffice build tooling |
| Database | **SQLite** for dev and for Tier A production | Zero-ops, file-based, avoids a separate DB service cost. Move to Azure SQL only if Tier B is adopted |
| Hosting (default) | **Tier A — budget ASP.NET host** (e.g. SmarterASP.NET-class) | ~$75/yr all-in. .NET 10.x support confirmed; no renewal price hike. See §8 |
| CDN/WAF/DNS | **Cloudflare Free** | MX records stay pointed at Google Workspace — email is completely untouched by this project |
| Email delivery (contact form) | **SendGrid free tier** or MailKit+SMTP | 100 emails/day is far more than this site needs |
| Forms | **Hand-built** Surface Controller (§6) | Not the paid Umbraco Forms package — see §9 for why |
| Page building | **Razor views + Block List editors** | Not a paid page-builder package |
| Schema deployment | **uSync 18.0.3** | Free/open source. Serializes document types, data types and templates to disk so the content model is versioned in git (§4a). The one NuGet dependency that earns its place |
| Caching | **Cloudflare edge cache** + Umbraco's built-in content cache | No extra package. Cache rules must bypass `/umbraco/*` and the form POST (§4a) |

If you're about to add a NuGet package, stop and check: does solving this in plain C#/Razor take less effort than researching, licensing, and maintaining a package? For a site this size, the answer is usually yes. Every dependency added here is one more thing to patch for the life of the project (§7).

## 3a. Version policy — deliberate choice, with a known cost

**The project owner's standing instruction is to run the latest versions across the stack.** That was chosen knowingly over the safer LTS path, with the tradeoff spelled out below. Don't quietly "correct" this back to LTS in a future session — but equally, don't lose track of what it commits the project to.

Facts as of 2026-08-07:

| | Umbraco 18 (**chosen**) | Umbraco 17 LTS (rejected alternative) |
|---|---|---|
| Released | 2026-06-25 (18.1.0 patch 2026-08-05) | 2025-11-27 |
| Bug/security support ends | **2027-03-25** | 2027-11-27 |
| Security-only ends | **2027-06-25** | 2028-11-27 |
| LTS? | No — ~9mo support + 3mo security | Yes — 24mo + 12mo |

**What this commits the project to:** Umbraco ships a new major roughly every 6 months. On the STS track, this site needs a **major CMS upgrade roughly annually**, with the first one due before **June 2027**. Budget for that as recurring maintenance work — it is not covered by the "$0 maintenance / Scenario 1" assumption in the cost proposal, which was written against the LTS assumption. Flag this to the owner if the maintenance arrangement is ever formalized.

**On .NET 11:** .NET 11 does *not* apply yet and should not be installed for this project. It is in preview (Preview 7) until **GA on 2026-11-10**, and no Umbraco release targets it — Umbraco 18 is built against `net10.0`. Running Umbraco on a preview runtime would be unsupported by both Microsoft and Umbraco simultaneously. **Revisit in/after November 2026**, when .NET 11 is GA *and* an Umbraco version targeting it exists (likely Umbraco 19, expected Q4 2026) — both conditions, not just the first.

**Standing upgrade rule:** prefer the newest *released, non-preview* version of everything, and re-verify at the start of any significant work session rather than trusting the numbers written in this file. Never adopt a preview/RC release for this client's production site.

## 4. Solution scaffold

Not yet created. When this becomes the active task:

```bash
dotnet new install Umbraco.Templates
dotnet new umbraco -n OphirMineralVentures.Web --friendly-name "Admin" --friendly-email admin@ophirmineralventures.com
# move the generated project into src/
```

- Single ASP.NET Core project is enough. Do not split into multiple class libraries (Domain/Application/Infrastructure layering) for a site this size — that's the right call for the E-Commerce.AI.API-style project elsewhere in this workspace, not for a content-driven brochure site with no complex business logic.
- Nullable reference types: enabled.
- File-scoped namespaces.
- `dotnet user-secrets` for local connection strings/API keys. Never committed. Production secrets live in host-level environment variables/app settings.
- **Testing**: no dedicated test project needed at this site's scope. If one becomes warranted later (sitemap.xml generation logic, contact-form validation rules), xUnit is the .NET-ecosystem default — don't reach for anything more elaborate for a content-driven brochure site.
- Running `dotnet new gitignore` inside `src/` after scaffolding is expected and fine — it'll sit alongside the root `.gitignore` in this repo, not replace it.

## 4a. Architecture decisions

Full reasoning in `docs/proposals/05-Architecture-Decisions.pdf`. The load-bearing points:

### The architecture, named

**Coupled CMS architecture — server-rendered MVC monolith with edge caching.** By dimension:

| Dimension | Pattern |
|---|---|
| CMS delivery model | **Coupled (traditional)** — CMS handles management *and* presentation. Not headless, not decoupled |
| Deployment topology | **Monolith** — one deployable unit |
| Application pattern | **MVC** — `RenderController`/`SurfaceController`, Razor views, ModelsBuilder models |
| Rendering | **SSR** — no client-side hydration layer |
| Code organization | **Package-by-layer** — folders by technical concern |
| Caching | **Reverse-proxy / edge caching** — CDN in front of origin |

Headless and static-SSG alternatives were evaluated and rejected — they double the operational surface and violate the "maintainable alone, in my own stack" constraint. Don't reintroduce them.

### Explicitly NOT DDD, Clean, Onion, or Hexagonal

A deliberate rejection, not an oversight — do not "improve" the structure by introducing these.

- **Not DDD.** There is no business domain. The entities are pages and documents; the behavior is rendering them. No invariants, aggregates, or ubiquitous language to model. DDD would be structure without complexity to justify it.
- **Not Clean/Onion/Hexagonal.** These keep a domain core independent of frameworks so infrastructure can be swapped. Here **Umbraco *is* the application** — content model, routing, rendering, editor UI. Abstracting away from it means indirection over the thing supplying all the value, to enable a swap that will never happen.
- `IEmailSender` (interface + SendGrid implementation) is ordinary DI for testability, **not** ports-and-adapters. Don't describe or extend it as such.

**The one trigger that would change this:** if the deferred buyer RFQ portal (§9) is ever built — quotes, statuses, buyer accounts, approval flow — that is genuine domain logic with real invariants. The correct response then is a properly layered module with its own bounded context *inside* this monolith. Not a rewrite, and not retrofitting layering onto brochure pages that will never need it. The trigger is real domain complexity arriving, not the page count growing.

**Single project, organized by concern.** `Views/`, `Services/`, `Controllers/`, `Composers/`, `Middleware/`, `uSync/`. No Domain/Application/Infrastructure assembly split.

**Edge-first caching.** Cloudflare caches rendered HTML with a long TTL; purge on publish. Cache rules must **bypass** `/umbraco/*` and the contact-form POST. This is what makes budget hosting viable — the origin should see almost no public traffic. If a page renders stale or the backoffice behaves oddly, suspect cache rules first.

**Constrained content model.** Fixed document types with an approved set of Block List blocks per page type. The owner edits text/images/news and reorders approved blocks; he must not be able to restructure layout. On a compliance site, rigidity is a feature — don't "helpfully" add a free-form page builder.

**Single write path is an invariant.** The contact form POST is the only public write endpoint. Any feature that adds another (RFQ portal, buyer login) is an architectural change needing a security rethink, not just a new page.

**uSync for the content model.** uSync 18.0.3 supports Umbraco 18. Document types, data types and templates serialize to disk and are committed to git, so schema is versioned alongside the Razor views that depend on it. Content itself is **not** synced — production content belongs to the owner.

**Theme follows the visitor's system preference — no manual light/dark toggle.** Implement purely via CSS `prefers-color-scheme: dark`, swapping a token palette (background/ink/accent custom properties), the same pattern already used in the homepage design draft and every proposal document (`docs/proposals/source-html/*.html` — copy the token structure from there rather than inventing a new one). No JS-based toggle, no `localStorage` preference, no manual switcher UI for v1 — some visitors are uncomfortable in one mode or the other, and respecting whatever they've already configured at the OS level is both less work and more correct than adding a control for it. If a manual override is ever requested later, layer it on top of the same tokens (a `data-theme` attribute) rather than replacing the system-preference base case.

### Bilingual EN/中文 — now in v1 scope, not deferred

**Status changed 2026-08-30: this was originally "prepare the schema now, launch English-only, add Chinese later" — the client decided to launch bilingual from day one, since Chinese buyers are current clients, not a speculative future market.** Update any assumption elsewhere in this file or in your own reasoning that treats this as a v2/future item — it isn't anymore.

**Turn on "Allow vary by culture" for document types and their editable properties**, with **both English and Simplified Chinese (zh-Hans) active from launch**, not just English with Chinese prepared-for. Enabling variance retrospectively has documented migration edge cases — culture-varying properties get skipped when the parent content type is invariant ([umbraco-cms#22159](https://github.com/umbraco/umbraco-cms/issues/22159)), which lands exactly on the composition-based model in §5 (`seoComposition` applied to otherwise-invariant types) — this is exactly why the earlier "enable at build time" decision was made, and it's now paying off rather than just being insurance.

**Build a visible language switcher** in the site header — loop over `IPublishedContent.Cultures` per Umbraco's own documented pattern (see `docs.umbraco.com/umbraco-cms/tutorials/multilanguage-setup`), not a custom implementation.

**Translation is content, not code — comes from the client, same as everything else in §10's placeholder-data policy.** Do not write Chinese copy yourself unless you're actually fluent and the client has explicitly asked you to source it; do not machine-translate and publish without human review — on a site whose entire purpose is credibility with Chinese buyers, a bad translation actively undermines the goal rather than being a neutral placeholder.

**Decided 2026-08-30: client-provided translation (a named bilingual person at Ophir) is the actual v1 plan, not one of two equal options.** A professional translation service (~$200–400 for initial content) is documented in the client proposal (Part IX/XIV) as available on request, not built into the default plan or timeline. Don't treat this as still-open when scoping Phase 5 (§8's deployment phases) — confirm the specific person's name (client proposal Part XIV/XV sign-off), not which sourcing model.

**Do not substitute browser-based auto-translate (Chrome's built-in translate, etc.) for real culture-variant content — checked 2026-08-30, this isn't viable, not just inferior.** Chrome's translate feature calls Google's Translate API; that API has been blocked in mainland China since 2022, so for a buyer browsing without a VPN it doesn't fire at all, not just badly. It's also invisible to search engines — they index what's actually served, not text a browser swaps in client-side afterwards — so relying on it would mean the site never appears in Chinese-language search results either. If this ever comes up again (a future session, a cost-cutting request), the answer is still no: real server-rendered zh-Hans content via Umbraco's culture variance is the only approach that reaches this audience at all, not merely the higher-quality one.

**Scope boundary, confirmed with the client 2026-08-30: bilingual applies to the public site only, never the Umbraco backoffice.** Umbraco has two genuinely separate language systems — don't conflate them:

| System | What it controls | Scope here |
|---|---|---|
| **Content culture variance** (§4a above) | What a *visitor* reads on the public pages | **Both en-US and zh-Hans, active** — this is the whole feature |
| **Backoffice UI language** (an Umbraco user-account setting, unrelated to culture variance) | What an *editor* sees inside `/umbraco` — menu labels, buttons, "Save and Publish" | **English only, always.** All CMS users are internal Ophir staff; no one editing content needs a Chinese admin UI |

Concretely: never install/enable an Umbraco backoffice Chinese language pack, never treat "the site needs to be bilingual" as implying anything about the editing experience. This isn't a cost-saving shortcut — it was never in scope, and no proposal figure (Part IX cost, Part X timeline) ever assumed it was. Worth stating explicitly anyway, since the two systems share the word "language" and are an easy thing for a fresh session to genuinely confuse.

**This is a recurring content-maintenance cost, not a one-time launch task.** Every future edit (news post, updated cert note, changed spec) needs a Chinese counterpart or the zh-Hans version silently goes stale. If you ever notice an English-only page live for more than a few days without its Chinese counterpart, flag it to the client — don't let it drift quietly.

## 5. Content model — Umbraco Document Types

Build one Document Type per row. Aliases are camelCase per Umbraco convention.

| Document Type (alias) | Key properties |
|---|---|
| `home` | heroHeading, heroSubtext, heroImage (Media Picker), stats (Block List: label + value), featuredNews (Content Picker, multiple) |
| `aboutPage` | body (Block List/Rich Text), leadership (Block List: name, role, photo, bio) |
| `product` | name, useCase, specs (Block List: label + value — renders as an assay-style spec table), specSheet (Media Picker, PDF) |
| `certification` | name, referenceNumber, issuingBody, certificateFile (Media Picker, PDF), issueDate, expiryDate |
| `sustainabilityPage` | body, initiatives (Block List: title, description) |
| `article` | title, publishDate, body, featuredImage — for the News section |
| `contactPage` | address, phone, hours — static content; the form itself is code, not content (§6) |
| `legalPage` | body — Privacy Policy / Terms, rarely edited but still owner-editable |

All editable properties above should be set to vary by culture (§4a) even though only English is active at launch.

**Two reusable compositions — apply these, don't duplicate the properties per type:**

- `seoComposition` — metaTitle, metaDescription, ogImage. Apply to every content-bearing type above.
- `siteSettings` — a single top-level node: companyName, registeredAddress, phone, socialLinks, defaultOgImage. Templates read from this instead of hardcoding company details anywhere in Razor.

Check for a Models Builder-generated strongly-typed model before hand-rolling `IPublishedContent` property access.

## 6. Contact form — Surface Controller pattern

This replaces the paid Umbraco Forms package (§9). Implement once as plain code:

```csharp
public class ContactSurfaceController : SurfaceController
{
    private readonly IEmailSender _emailSender; // wraps SendGrid or MailKit

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(ContactFormModel model)
    {
        if (!ModelState.IsValid) return CurrentUmbracoPage();
        if (!string.IsNullOrEmpty(model.HoneypotField)) return RedirectToCurrentUmbracoPage(); // silently drop bots

        await _emailSender.SendAsync(to: "john@ophirmineralventures.com", model);
        TempData["FormSubmitted"] = true;
        return RedirectToCurrentUmbracoPage(); // Post-Redirect-Get
    }
}
```

Required, not optional: anti-forgery token, a honeypot field, and rate limiting on this endpoint (§7). This is the site's only write path and its only real attack surface — treat it accordingly.

## 7. Security requirements — non-negotiable

These are acceptance criteria, not suggestions. Any PR/change touching `Program.cs`, the contact form, or auth should be checked against this list.

- `app.UseHsts()`, `app.UseHttpsRedirection()` — always on, all environments except local dev.
- Security headers set explicitly in middleware, not left to defaults:
  ```csharp
  app.Use(async (context, next) =>
  {
      context.Response.Headers.Append("X-Frame-Options", "DENY");
      context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
      context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
      context.Response.Headers.Append("Content-Security-Policy",
          "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; " +
          "script-src 'self'; frame-ancestors 'none'");
      await next();
  });
  ```
  Start this strict. Loosen only for a specific, understood reason (e.g. adding a third-party script) — never loosen speculatively.
- Umbraco backoffice: 2FA enabled via Umbraco's built-in Identity 2FA, not a third-party package.
- Contact form: `[ValidateAntiForgeryToken]` + honeypot (§6) + ASP.NET Core's built-in `RateLimiter` middleware on the POST endpoint (a few requests/minute/IP is enough).
- Least-privilege backoffice roles: the client gets an Editor role (content only), never Administrator.
- Dependency count stays small (§3) — every package is something you're committing to patch.
- If ads are ever revisited (§10, deferred), any third-party ad script gets a CSP relaxation scoped to *only* the pages carrying it, never a site-wide loosening.

## 8. Hosting & deployment tiers

| Tier | Stack | Est. cost | When to use |
|---|---|---|---|
| **A — Minimum Secure** (default) | Budget ASP.NET host (SmarterASP.NET-class), **Singapore region** + SQLite | ~$75/yr | Start here. Nothing about the other tiers is "more secure" — just more convenient at scale |
| B — Managed (Azure) | Azure App Service (B1 Linux, $13.14/mo), **Southeast Asia (Singapore) or East Asia (Hong Kong) region** + Azure SQL Basic ($4.90/mo) | ~$232/yr | If staging slots / less hands-on ops become worth paying for |
| C — Google Cloud Run | Containerized, serverless | ~$0–60/yr | Cheapest at this traffic level if comfortable with Docker. **Separate Google Cloud billing — not part of the client's Workspace subscription**, don't conflate the two when discussing this with the client |

*Prices re-verified 2026-08-28; USD→PHP ₱61.87. Re-check before quoting — see `docs/proposals/03-Website-Cost-Proposal.pdf`.*

**Region matters here and costs nothing extra.** Both SmarterASP.NET and Azure have real Asia-Pacific data centers (SmarterASP.NET: Singapore, Japan, India; Azure: Southeast Asia/Singapore, East Asia/Hong Kong) — pick one of these at signup, don't let it default to US/Europe. This isn't optional polish: it's meaningfully lower latency for the client's own day-to-day backoffice use from the Philippines (which always hits the origin directly, never the Cloudflare cache — see §4a), and it shortens the origin leg for every cache-miss request generally. A second Tier A candidate worth trialing alongside SmarterASP.NET: **[ASPHostPortal.com](https://asphostportal.com)** — has both Singapore and Hong Kong data centers and explicitly markets "Full Trust" hosting (the exact permission model Umbraco needs), which is more directly relevant marketing than SmarterASP.NET's generic ASP.NET Core page. Same "verify via free trial before paying" gate above applies to it too.

### Mainland China reachability — a real, honest limitation of the free-tier architecture

Given ~100% of Ophir's recorded buyers are China-based (§1), this is worth being precise about rather than letting Part V.2's "delivery network" framing overclaim. Researched 2026-08-30:

- **The free Cloudflare tier this architecture is built on does not include China-optimized routing.** Cloudflare sells that separately as "Cloudflare China Network" — Enterprise-only, ~$5,000/month, and it requires the *site owner* to hold an ICP filing and a China business entity to provision it. Not viable for this project at any budget tier, since Ophir isn't a China-registered entity.
- **This is a performance issue, not an access/legal issue.** Mainland China routes international traffic (including plain Cloudflare, and any foreign-hosted site generally) through congested gateways — expect a real but moderate latency/reliability hit for mainland visitors, not a block. **Confirmed: no ICP license or China entity is required for Ophir just because Chinese visitors view the site** — ICP is a hosting-*location* requirement (mainland-hosted servers) and Cloudflare's own prerequisite for their specific China product, not a rule that gates foreign sites being viewed from China. Don't let this get miscommunicated to the client as "we need a China business entity" — that's wrong; it's not needed at all under the baseline architecture.
- **A real middle-ground option exists** if the client ever wants to invest in this specifically: [Chinafy](https://www.chinafy.com) — a managed China-acceleration layer, from ~$280/month (~$3,360/yr), used as a partner product by AWS/Azure/Alibaba Cloud themselves. Notably it also fixes a *different*, non-obvious problem the CDN choice alone can't: any Google-hosted resource embedded in the page (Google Fonts, Google Analytics/GA4's tracking script, embedded Google Maps) has its own independent China-reachability problem, separate from wherever the site itself is hosted. **If GA4 is used for analytics (§8's SEO tooling), expect it to under-report mainland Chinese visitors specifically** — the tracking script itself is a Google property with the same gateway congestion, likely worse. Worth flagging to the client if traffic data ever needs to inform a real decision about this — the analytics will be quietly incomplete for the one audience that matters most, not just slow to load.
- **Not recommended by default; documented as a real, priced option, not built into the baseline.** This is now written into the client proposal itself (Part V.2 + Part XII) as an explicit, priced future option — not a silent gap, and not something to push the client toward before real traffic data justifies the cost.

### Before committing money to Tier A: verify it actually runs Umbraco

Tier A's host choice (SmarterASP.NET-class shared hosting) has an evidence gap worth closing before any paid commitment, not after. Checked 2026-08-30:

- **Confirmed:** the host supports the right underlying prerequisites — .NET 10 / ASP.NET Core hosting, and SQLite with proper file read/write access (their own KB confirms this).
- **Not confirmed:** no independent report found of current Umbraco (13+) actually running there. The only concrete Umbraco data point for that host is a decade-old forum report — for Umbraco 7.2, a pre-.NET-Core architecture with a completely different hosting model (classic ASP.NET Full/Medium Trust, which doesn't apply to modern .NET at all). Not evidence of a problem, just not evidence of anything current.
- **Azure (Tier B), by contrast, needs no such check** — Umbraco publishes [its own official Azure App Service docs](https://docs.umbraco.com/umbraco-cms/fundamentals/setup/server-setup/azure-web-apps), first-party and current.

**Gate:** once the solution is scaffolded (§4), before purchasing a paid Tier A term, deploy it to the host's free trial (SmarterASP.NET offers 60 days, no card required) and confirm it actually boots, the backoffice loads, and SQLite writes correctly. Only commit to a paid plan after that passes. If it fails, fall back to Tier B (Azure) without re-litigating the plan — the cost proposal already prices both, and moving between tiers costs nothing but time.

DNS: only the A/CNAME record changes to point at whichever host is chosen. **MX records are never touched** — the client's Google Workspace email must keep working through every deployment. If you ever find yourself editing MX records for this project, stop — that's out of scope and risks breaking the client's email.

CI/CD skeleton (Tier B example, adapt per tier):

```yaml
on:
  push:
    branches: [main]
jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v6
      - uses: actions/setup-dotnet@v6
        with: { dotnet-version: '10.0.x' }
      - run: dotnet publish -c Release -o ./publish
      - uses: azure/webapps-deploy@v3
        with: { app-name: ophirmineralventures, package: ./publish }
```

Action versions above (`@v6`, `@v3`) were current as of 2026-08-07 — check the Marketplace for newer major versions before reusing this verbatim; GitHub Action tags move faster than this file gets updated.

Secrets (deploy credentials, SendGrid API key) live in GitHub Actions secrets. Never in `appsettings.json`, never committed.

## General rule: re-verify versions, don't trust this file's numbers

Every version pinned in this file (.NET 10, Umbraco 18.1.0, Node 24, the Action tags above) was the newest released option as of 2026-08-07. Per §3a, the owner wants the latest across the stack — so treat every version number here as "verify this is still current, bump if not," not as a fixed target. Stable releases only; never preview/RC on this client's production site.

## 9. Explicitly out of scope — do not build unless asked

These were evaluated and deliberately deferred. Don't scope-creep into them while working on something adjacent:

- **Umbraco Forms** (paid add-on) — hand-built form in §6 replaces it.
- **Umbraco Cloud** (managed hosting, $660–$10,800/yr) — self-hosting is cheaper and this project has an in-house .NET developer, which is what Cloud's price is really paying to avoid needing.
- **Umbraco commercial support contract** — same reasoning.
- **Advertising** (AdSense or sponsored placements) — evaluated, deliberately parked. If revisited: News section only, never on Home/About/Compliance/Products/Contact, and see the CSP note in §7.
- **China delivery acceleration** (Chinafy or similar, ~$280/mo) — real option, deliberately parked pending actual traffic data. See §8's China-reachability section. **Not the same thing as the bilingual site below — that one IS in scope.**
- **Buyer RFQ portal, careers page, investor-relations section, CRM integration** — all v2+, listed in `docs/proposals/01-Website-Plan-and-Architecture.pdf` §11.

## 10. Placeholder-data policy — read before writing any content

Several real facts about the business are **not yet confirmed** (see §11). Never invent specifics that look like real business data:

- Do not fabricate SEC registration numbers, DTI/BIR numbers, DENR-MGB permit numbers, or certificate details. Use an obvious placeholder (`[SEC Reg. No. — confirm with client]`) until the client supplies the real value.
- Do not fabricate a business address, phone number, or founding date.
- Do not write John David Montilla a biography beyond what he's actually provided — a generic-but-plausible-sounding bio is worse than an honest placeholder, because it could get published as fact.
- Real ore-spec numbers (Ni%, Cr₂O₃%, etc.) shown anywhere are indicative/placeholder until real assay data is provided — label them as such.
- This isn't a formatting nitpick: this site's whole job is compliance credibility. Publishing an invented-looking number that turns out wrong is worse than publishing nothing.

### Portfolio use — pending client sign-off (client proposal Part XI.4)

Randolf has asked the client for permission to reference this project (design + code) in his own portfolio and to future clients, with real business data excluded. Client sign-off is pending — check `docs/proposals/00-Ophir-Website-Complete-Proposal.pdf` Part XV.2's signed portfolio line before treating this as agreed.

**The architecture already does most of the work here, which is worth preserving deliberately, not by accident:** uSync (§4a) syncs schema/templates to git, **not content** — the client's actual page text, certificate numbers, and figures live in the runtime database, never in the repository. That means the codebase itself is portfolio-safe by construction, as long as this separation is never violated. Concretely:

- **Never commit real client content into git** — not as seed/migration data, not as a "temporary" hardcoded default, not in a code comment or commit message used as a worked example. If a real figure or document needs referencing while building something, use the same bracketed-placeholder convention as the rest of this file, even in your own scratch notes within the repo.
- **Before any actual portfolio sharing happens** (once/if the client confirms), do a real sweep — `git log -p` for anything that ever touched a real value, not just a check of the current working tree — since a value removed later still lives in history. Squash or exclude history if anything real was ever committed, rather than assuming a clean current state means a clean repo.
- **Screenshots/demos need their own check**: verify the running site is showing placeholder content (§10 above), not live client data, before capturing anything for portfolio use.
## 11. Open questions pending the client

Needed before these areas can move from placeholder to real content — don't guess at these, ask:

- Preferred domain name, and current registrar (may be Squarespace if it was ever a Google Domains registration — see the main plan doc §02).
- SEC/DTI/BIR registration details and which DENR-MGB permits he wants public, with current copies.
- Logo/brand assets, or sign-off to design a wordmark from scratch.
- Real operations/product/leadership photos.
- How he wants his own role/bio presented.
- **Who specifically translates** — the sourcing model is decided (client-provided, §4a) and a professional service stays available on request, so this is down to naming the actual bilingual person at Ophir doing it. Needed before Phase 5 (Translate) can start.
- Confirmation his Google Workspace email should be left exactly as-is (default assumption: yes).
- Which maintenance arrangement he and the developer land on — affects nothing technical, but affects the support-response expectations any future work should assume.

## 12. Definition of done — go-live checklist

- [ ] DNS: A/CNAME points at the chosen host; MX untouched; email verified still working post-cutover
- [ ] HTTPS, HSTS, and all §7 headers verified (spot-check with a headers-scanning tool)
- [ ] sitemap.xml submitted to Google Search Console and Bing Webmaster Tools (Bing's index covers Yahoo too)
- [ ] Google Business Profile created/claimed
- [ ] Lighthouse/PageSpeed pass on mobile
- [ ] No placeholder data (§10) remains published — full sweep before calling it live
- [ ] Every page has a reviewed (not just machine-translated) Simplified Chinese version live, and the language switcher works both directions — §4a

## 13. Reference documents

The full reasoning behind every decision above lives in `docs/proposals/`. Read the relevant one before deviating from this file:

- `01-Website-Plan-and-Architecture.pdf` — research findings, stack comparison, sitemap rationale, security/SEO plan
- `02-Homepage-Design-Draft.pdf` — the visual design direction (placeholder content, real layout)
- `03-Website-Cost-Proposal.pdf` — full cost breakdown including the Umbraco platform's own licensing costs
- `00-Ophir-Website-Complete-Proposal.pdf` — **the master client document.** Consolidates everything below into one comprehensive, client-facing proposal (business case, Google Workspace explainer, architecture in plain terms, hosting, security, SEO, cost in USD+PHP, deployment plan, post-launch plan and its assumptions, FAQ, sign-off). This is what goes to John. The others are working documents.
- `04-Client-Proposal.pdf` — the earlier, shorter client-facing version. Superseded by `00-` but kept for history
- `05-Architecture-Decisions.pdf` — why the alternatives were rejected, the edge-caching request-path diagram, and the culture-variance decision (§4a)

`source-html/` under the same folder has the editable HTML source for each PDF — edit there and re-export, don't edit the PDFs directly.
