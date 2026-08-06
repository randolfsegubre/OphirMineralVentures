# Ophir Mineral Ventures — Website

This file is the project's source of truth for any AI coding assistant (Claude Code, or any other LLM) working in this repository. Read it before writing code. It supersedes generic defaults — where it's specific, follow it over a more "standard" pattern you might otherwise reach for.

## 1. What this project is

A custom corporate website for **Ophir Mineral Ventures, Inc.**, a Philippines-based nickel and chromite ore exporter owned by John David Montilla. Two purposes, in priority order:

1. **Compliance credibility** — a legitimate public presence for regulators, banks, and overseas buyers running due diligence on large ($100K–$40M range) ore shipments.
2. **Marketing / deal-closing** — professional collateral the owner can point buyers to.

A close third, load-bearing constraint: **the owner (non-technical) must be able to edit content himself**, and **the developer must be able to maintain this indefinitely using only skills he already has** (senior full-stack .NET/C#/Umbraco/ASP.NET Core — no PHP, no unfamiliar ecosystem). That second constraint is *why* this is built on Umbraco instead of WordPress — see `docs/proposals/01-Website-Plan-and-Architecture.pdf` §05 for the full reasoning if it's ever questioned.

This is not a high-traffic site and never needs to be engineered like one. It's a low-traffic, high-trust B2B brochure site. Don't over-architect it.

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
| CMS | **Umbraco CMS**, latest LTS at scaffold time (13.x or newer) | Open source, MIT license, free regardless of scale |
| Framework | **ASP.NET Core**, current .NET LTS | Pin the exact version in `global.json` once scaffolded |
| Database | **SQLite** for dev and for Tier A production | Zero-ops, file-based, avoids a separate DB service cost. Move to Azure SQL only if Tier B is adopted |
| Hosting (default) | **Tier A — budget ASP.NET host** (e.g. SmarterASP.NET-class) | ~$104/yr all-in. See §8 for the full tier comparison |
| CDN/WAF/DNS | **Cloudflare Free** | MX records stay pointed at Google Workspace — email is completely untouched by this project |
| Email delivery (contact form) | **SendGrid free tier** or MailKit+SMTP | 100 emails/day is far more than this site needs |
| Forms | **Hand-built** Surface Controller (§6) | Not the paid Umbraco Forms package — see §9 for why |
| Page building | **Razor views + Block List editors** | Not a paid page-builder package |

If you're about to add a NuGet package, stop and check: does solving this in plain C#/Razor take less effort than researching, licensing, and maintaining a package? For a site this size, the answer is usually yes. Every dependency added here is one more thing to patch for the life of the project (§7).

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
| **A — Minimum Secure** (default) | Budget ASP.NET host + SQLite | ~$104/yr | Start here. Nothing about the other tiers is "more secure" — just more convenient at scale |
| B — Managed (Azure) | Azure App Service (B1) + Azure SQL Basic | ~$236/yr | If staging slots / less hands-on ops become worth paying for |
| C — Google Cloud Run | Containerized, serverless | ~$0–60/yr | Cheapest at this traffic level if comfortable with Docker. **Separate Google Cloud billing — not part of the client's Workspace subscription**, don't conflate the two when discussing this with the client |

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
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - run: dotnet publish -c Release -o ./publish
      - uses: azure/webapps-deploy@v3
        with: { app-name: ophirmineralventures, package: ./publish }
```

Secrets (deploy credentials, SendGrid API key) live in GitHub Actions secrets. Never in `appsettings.json`, never committed.

## 9. Explicitly out of scope — do not build unless asked

These were evaluated and deliberately deferred. Don't scope-creep into them while working on something adjacent:

- **Umbraco Forms** (paid add-on) — hand-built form in §6 replaces it.
- **Umbraco Cloud** (managed hosting, $660–$10,800/yr) — self-hosting is cheaper and this project has an in-house .NET developer, which is what Cloud's price is really paying to avoid needing.
- **Umbraco commercial support contract** — same reasoning.
- **Advertising** (AdSense or sponsored placements) — evaluated, deliberately parked. If revisited: News section only, never on Home/About/Compliance/Products/Contact, and see the CSP note in §7.
- **Bilingual (English/Chinese) site** — real strategic value given buyers are ~100% China-based (per the research in the main plan doc), but a v2 item, not v1.
- **Buyer RFQ portal, careers page, investor-relations section, CRM integration** — all v2+, listed in `docs/proposals/01-Website-Plan-and-Architecture.pdf` §11.

## 10. Placeholder-data policy — read before writing any content

Several real facts about the business are **not yet confirmed** (see §11). Never invent specifics that look like real business data:

- Do not fabricate SEC registration numbers, DTI/BIR numbers, DENR-MGB permit numbers, or certificate details. Use an obvious placeholder (`[SEC Reg. No. — confirm with client]`) until the client supplies the real value.
- Do not fabricate a business address, phone number, or founding date.
- Do not write John David Montilla a biography beyond what he's actually provided — a generic-but-plausible-sounding bio is worse than an honest placeholder, because it could get published as fact.
- Real ore-spec numbers (Ni%, Cr₂O₃%, etc.) shown anywhere are indicative/placeholder until real assay data is provided — label them as such.
- This isn't a formatting nitpick: this site's whole job is compliance credibility. Publishing an invented-looking number that turns out wrong is worse than publishing nothing.

## 11. Open questions pending the client

Needed before these areas can move from placeholder to real content — don't guess at these, ask:

- Preferred domain name, and current registrar (may be Squarespace if it was ever a Google Domains registration — see the main plan doc §02).
- SEC/DTI/BIR registration details and which DENR-MGB permits he wants public, with current copies.
- Logo/brand assets, or sign-off to design a wordmark from scratch.
- Real operations/product/leadership photos.
- How he wants his own role/bio presented.
- Interest in a future bilingual (English/Chinese) version.
- Confirmation his Google Workspace email should be left exactly as-is (default assumption: yes).
- Which maintenance arrangement he and the developer land on — affects nothing technical, but affects the support-response expectations any future work should assume.

## 12. Definition of done — go-live checklist

- [ ] DNS: A/CNAME points at the chosen host; MX untouched; email verified still working post-cutover
- [ ] HTTPS, HSTS, and all §7 headers verified (spot-check with a headers-scanning tool)
- [ ] sitemap.xml submitted to Google Search Console and Bing Webmaster Tools (Bing's index covers Yahoo too)
- [ ] Google Business Profile created/claimed
- [ ] Lighthouse/PageSpeed pass on mobile
- [ ] No placeholder data (§10) remains published — full sweep before calling it live

## 13. Reference documents

The full reasoning behind every decision above lives in `docs/proposals/`. Read the relevant one before deviating from this file:

- `01-Website-Plan-and-Architecture.pdf` — research findings, stack comparison, sitemap rationale, security/SEO plan
- `02-Homepage-Design-Draft.pdf` — the visual design direction (placeholder content, real layout)
- `03-Website-Cost-Proposal.pdf` — full cost breakdown including the Umbraco platform's own licensing costs
- `04-Client-Proposal.pdf` — the client-facing version sent to John David Montilla

`source-html/` under the same folder has the editable HTML source for each PDF — edit there and re-export, don't edit the PDFs directly.
