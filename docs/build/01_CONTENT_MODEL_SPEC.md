# Content Model Spec — implementation detail

`CLAUDE.md` §5 gives the summary table. This document is the implementation-ready version: exact property editors, culture-variance flags, parent/child structure, and the container types §5 doesn't spell out. Build Phase 1 from this file, not from re-deriving it out of §5 alone.

**Culture variance default:** `CLAUDE.md` §4a states "all editable properties vary by culture" — this file follows that literally, including on Media Picker and Date Picker properties, even though real-world Umbraco practice often leaves those invariant (same hero image, same issue date, regardless of language). The blanket rule is deliberate — it means a hero image or a certificate scan *can* differ per culture if it's ever needed, at the cost of the editor having to fill in the property twice even when the value is identical both times. If this proves to be actual day-to-day friction once John is editing content, that's a decision to raise with Randolf, not something to quietly change mid-build.

**Build order:** compositions first (everything depends on them), then containers, then leaf types — this matches the dependency order in Umbraco's own Document Type editor (a composition must exist before it can be applied).

## Compositions

### `seoComposition`

Apply to every content-bearing type below except the listing containers, which is optional but recommended.

| Property alias | Editor | Culture-variant | Notes |
|---|---|---|---|
| `metaTitle` | Textstring | Yes | Falls back to page title if empty — implement in the view, not as an Umbraco default value |
| `metaDescription` | Textarea | Yes | ~155 char guidance in the property's editor description |
| `ogImage` | Media Picker (single, image types only) | Yes | Falls back to `siteSettings.defaultOgImage` if empty |

### `siteSettings`

A single top-level node, not part of the public navigation — create it under the content root, alias `siteSettings`, and mark it `hideFromNavigation` at the document-type level (Umbraco's own "not in navigation" flag) rather than relying on template logic to skip it. Templates read from this node (via a helper that fetches the single settings node) instead of hardcoding company details anywhere in Razor — this is the mechanism that makes §4a's "no hardcoded company details in Razor" rule actually enforceable.

| Property alias | Editor | Culture-variant | Notes |
|---|---|---|---|
| `companyName` | Textstring | Yes | Legal/trading name, may render differently per culture |
| `registeredAddress` | Textarea | Yes | |
| `phone` | Textstring | Yes | |
| `socialLinks` | Block List (block: `platform` Textstring + `url` Textstring) | No | Same links both cultures |
| `defaultOgImage` | Media Picker (single, image) | No | Fallback target for every `seoComposition.ogImage` |

## Content types

### `home` — root

Allowed at content root only, single instance.

| Property alias | Editor | Culture-variant | Notes |
|---|---|---|---|
| `heroHeading` | Textstring | Yes | |
| `heroSubtext` | Textarea | Yes | |
| `heroImage` | Media Picker (single, image) | Yes | Per the blanket-variance note above |
| `stats` | Block List (block: `label` Textstring + `value` Textstring) | Yes | e.g. "Years operating" / "12" |
| `featuredNews` | Content Picker (multiple, restricted to `article`) | No | Picks nodes; each node's own display text is already culture-variant |

Compositions: `seoComposition`.

### `aboutPage` — single instance, child of `home`

| Property alias | Editor | Culture-variant | Notes |
|---|---|---|---|
| `body` | Rich Text Editor | Yes | Main narrative |
| `leadership` | Block List (block: `name` Textstring, `role` Textstring, `photo` Media Picker, `bio` Textarea) | Yes | Whole block is culture-variant per the blanket rule — a person's name will read identically both languages in practice, that's fine |

Compositions: `seoComposition`.

### `productsListing` — single instance, child of `home`

Container page; renders as the "Products" nav entry and lists its `product` children.

| Property alias | Editor | Culture-variant | Notes |
|---|---|---|---|
| `intro` | Rich Text Editor | Yes | Optional text above the product grid |

Compositions: `seoComposition`. Allowed child content types: `product` only.

### `product` — multiple, child of `productsListing`

| Property alias | Editor | Culture-variant | Notes |
|---|---|---|---|
| `useCase` | Textarea | Yes | |
| `specs` | Block List (block: `label` Textstring + `value` Textstring) | Yes | Renders as an assay-style spec table — e.g. "Ni%" / "1.8–2.2" |
| `specSheet` | Media Picker (single, PDF) | Yes | |

Compositions: `seoComposition` (its `metaTitle` doubles as the product name shown in nav/cards — don't add a separate `name` property, avoid duplicating the same string in two places).

### `sustainabilityPage` — single instance, child of `home`

| Property alias | Editor | Culture-variant | Notes |
|---|---|---|---|
| `body` | Rich Text Editor | Yes | |
| `initiatives` | Block List (block: `title` Textstring + `description` Textarea) | Yes | |

Compositions: `seoComposition`.

### `certificationsListing` — single instance, child of `home`

| Property alias | Editor | Culture-variant | Notes |
|---|---|---|---|
| `intro` | Rich Text Editor | Yes | |

Compositions: `seoComposition`. Allowed child content types: `certification` only.

### `certification` — multiple, child of `certificationsListing`

| Property alias | Editor | Culture-variant | Notes |
|---|---|---|---|
| `name` | Textstring | Yes | e.g. "DENR-MGB Mineral Ore Export Permit" |
| `referenceNumber` | Textstring | Yes | Per blanket rule; a reference number is the same string both cultures in practice — fine |
| `issuingBody` | Textstring | Yes | |
| `certificateFile` | Media Picker (single, PDF) | Yes | Usually the same scanned document both cultures unless the client provides a certified translation |
| `issueDate` | Date Picker | Yes | |
| `expiryDate` | Date Picker | Yes | |

Compositions: `seoComposition` — applied. Decided during Phase 1 build (2026-08-31): applying it to every content type uniformly was simpler than tracking one inconsistent exception, and it's a no-cost decision to reverse in Phase 2 if certifications end up rendering only as inline cards on `certificationsListing` with no routable detail page of their own.

### `newsListing` — single instance, child of `home`

Renders as the "News" nav entry. This is also the pool `home.featuredNews` picks from.

| Property alias | Editor | Culture-variant | Notes |
|---|---|---|---|
| `intro` | Rich Text Editor | Yes | |

Compositions: `seoComposition`. Allowed child content types: `article` only.

### `article` — multiple, child of `newsListing`

| Property alias | Editor | Culture-variant | Notes |
|---|---|---|---|
| `title` | Textstring | Yes | |
| `publishDate` | Date Picker | Yes | |
| `body` | Rich Text Editor | Yes | |
| `featuredImage` | Media Picker (single, image) | Yes | |

Compositions: `seoComposition`.

### `contactPage` — single instance, child of `home`

The form itself is code (`CLAUDE.md` §6), not content — this type only holds the static display content around it.

| Property alias | Editor | Culture-variant | Notes |
|---|---|---|---|
| `address` | Textarea | Yes | |
| `phone` | Textstring | Yes | |
| `hours` | Textstring | Yes | e.g. "Mon–Fri, 9am–6pm PHT" |

Compositions: `seoComposition`.

### `legalPage` — multiple instances (Privacy Policy, Terms of Service), child of `home`

| Property alias | Editor | Culture-variant | Notes |
|---|---|---|---|
| `body` | Rich Text Editor | Yes | Rarely edited but still owner-editable per §1 |

Compositions: `seoComposition`.

## Views / templates

One Razor view per document type, named to match Umbraco's convention against the ModelsBuilder-generated model (`Home.cshtml`, `AboutPage.cshtml`, `ProductsListing.cshtml`, `Product.cshtml`, `SustainabilityPage.cshtml`, `CertificationsListing.cshtml`, `Certification.cshtml` — only if it gets a routable detail page, see note above, `NewsListing.cshtml`, `Article.cshtml`, `ContactPage.cshtml`, `LegalPage.cshtml`). Shared partials: `_Layout.cshtml` (header/nav/language switcher/footer, reading from `siteSettings`), `_Nav.cshtml`, `_LanguageSwitcher.cshtml` (the `IPublishedContent.Cultures` loop per `CLAUDE.md` §4a).

## Sanity check before starting Phase 1

Cross-check this document against `CLAUDE.md` §5's summary table before building — if a future edit changes one, update the other in the same commit. They are two views of the same model; letting them drift is the exact kind of gap `docs/build/00_BUILD_PLAN.md`'s Phase 1 devlog entry is meant to catch.
