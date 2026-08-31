# Ophir Mineral Ventures Website — Content Editor's Guide

**Status: DRAFT — written progressively during the build (`docs/build/00_BUILD_PLAN.md` Phase 2 onward), finalized in Phase 10 against the real, live backoffice before handover to John.** Every `[SCREENSHOT: ...]` marker below is a placeholder — replace with a real, captured-from-the-live-site screenshot in Phase 10, never a mockup or a description standing in for the image. Every instruction should be walked through against the actual deployed backoffice before this is considered final, not just checked against what was planned.

This guide is written for John, not for a developer. If a future edit to this document ever gets more technical than "here's what to click," that's a sign it's drifted from its purpose — pull the technical detail back into `CLAUDE.md` or `docs/build/01_CONTENT_MODEL_SPEC.md` instead.

---

## Welcome

This is your website's control panel — Umbraco. You don't need any coding knowledge to use it. Everything in this guide is something you can do yourself: changing text, swapping photos, adding a news update, uploading a new certificate. If something ever looks broken or you're not sure what a button does, stop and check the "If something goes wrong" section near the end before making changes — most things here are safe to click around in, but a couple of actions (deleting a page, for instance) aren't easily undone.

## 1. Logging in

1. Go to `[your domain]/umbraco` in any web browser (Chrome, Edge, or Safari all work fine).
2. Enter the email and password set up for you when the site launched.
3. **Two-factor login is turned on for your account** — after your password, you'll be asked for a one-time code from your phone. This is a security requirement for the site, not optional, since it's the only door into editing your company's public content. [SCREENSHOT: login screen] [SCREENSHOT: 2FA prompt]

If you ever forget your password, use the "Forgot password" link on the login screen — it emails a reset link to your registered address. If your registered email itself is unreachable, that's a "contact your developer" situation (see the last section).

## 2. Finding your way around — the content tree

Once logged in, you'll see a panel on the left listing every page on your site, arranged like folders:

```
Home
 ├─ About
 ├─ Products
 │   ├─ [individual product entries]
 ├─ Sustainability
 ├─ Certifications
 │   ├─ [individual certificates]
 ├─ News
 │   ├─ [individual news articles]
 ├─ Contact
 ├─ Privacy Policy
 └─ Terms of Service
```

[SCREENSHOT: content tree, expanded]

Click any item to open it for editing. The right-hand side of the screen shows that page's content — headings, text boxes, image slots — exactly matching what visitors see on the live site, just editable.

**A note on structure:** you can edit the *content* of every page (text, images, adding new products/news/certificates), but the overall page layout and navigation structure is fixed by design — this keeps the site looking consistent and professional. If you ever want a structural change (a new section of the site, not just new content within an existing page), that's a "talk to your developer" request, not something to attempt from here.

## 3. Editing a page — English

1. Click the page in the content tree.
2. You'll see a row of tabs or a language indicator near the top — make sure you're on the **English (en-US)** version before editing. [SCREENSHOT: culture selector, English active]
3. Click into any text field and type your change directly — no special formatting needed for plain text fields. Fields with a formatting toolbar (bold, links, headings) are rich-text fields; use the toolbar buttons rather than pasting formatted text from Word, which can carry over messy hidden formatting.
4. To change a photo, click the image slot, then **Remove** the current one and **Add** the replacement from your computer or from the site's existing Media Library.
5. When you're happy with the change, see Section 5 below — **Save** and **Publish** are different things, and it's easy to make a change that doesn't actually go live if you only click Save.

[SCREENSHOT: a page mid-edit, showing a text field and an image field]

## 4. Editing the Chinese version — split-view

Every page on this site exists in two languages: English and Simplified Chinese (中文). This isn't automatic translation — it's real content you (or whoever you've assigned to this) write directly into the Chinese fields.

1. From the same page, switch the language selector to **Chinese (zh-Hans)**. [SCREENSHOT: culture selector, Chinese active]
2. Umbraco offers a **split-view** mode — English and Chinese side by side, so you can see exactly what needs a matching update. [SCREENSHOT: split-view editor showing EN | ZH side by side]
3. Fill in each field with its Chinese equivalent, the same way you'd edit the English version.
4. **Every English change needs a matching Chinese update.** If you update an English page and don't have time to translate it immediately, that's fine short-term — but flag it to whoever is handling translation so it doesn't sit half-updated for long. A page that's current in one language and stale in the other looks worse to a Chinese-speaking buyer than if it had never been translated at all.

If you don't have someone in-house available for a particular translation and need a professional service, that's a conversation with your developer — it was scoped as an available option from the start (see your original proposal), just not the default plan.

## 5. Saving vs. Publishing

This is the single most important distinction in this guide:

- **Save** keeps your changes as a private draft. Nobody visiting the website sees them yet.
- **Publish** makes your changes live, immediately, for anyone visiting the site.

[SCREENSHOT: Save / Save and Publish buttons]

If you're partway through a change and want to come back to it later without visitors seeing an unfinished page, use **Save**. When you're ready for the public to see it, use **Save and Publish**. A common mistake is assuming "Save" already made something live — it hasn't.

## 6. Adding a news article

1. In the content tree, click **News**, then choose **Create** (usually a "+" icon) and select the **Article** page type.
2. Fill in the title, date, and body text — in both languages per Section 4.
3. Add a featured image if you have one.
4. **Save and Publish** when ready.

[SCREENSHOT: creating a new article]

New articles automatically appear at the top of your News section — you don't need to manually reorder anything.

## 7. Replacing your logo and photos

Your logo and site photos live in the **Media Library** (a separate section in the left-hand navigation, usually below the content tree). [SCREENSHOT: Media Library]

- To replace the logo: upload the new file to the Media Library, then open the page/setting where the current logo is used (your site settings, not an individual page) and swap it in the same way you'd replace any other image.
- For best results, upload images close to the size they'll actually display at — a very large original photo will still work, but it slows down page loading for your visitors. If you're not sure what size to use, ask your developer once, and note the answer here for next time.

## 8. Managing certifications

1. In the content tree, click **Certifications**, then **Create** and select the **Certification** type.
2. Fill in the certificate name, issuing body, reference number, and issue/expiry dates.
3. Upload the certificate file (PDF) in the file slot.
4. **Save and Publish.**

[SCREENSHOT: creating a certification entry]

This section exists specifically because buyers and banks checking your company's legitimacy look here first — keep it current. If a certificate is renewed or a new permit is issued, updating it here takes a few minutes and matters more than almost anything else on the site.

## 9. Managing product information

Each product entry (under **Products**) has a description, typical use case, a specification table (percentages/grades), and an optional downloadable spec sheet. Edit these the same way as any other page (Section 3), remembering the Chinese counterpart (Section 4). The specification table is a repeatable list — use the **Add** button within that field to add a new row (e.g. a new grade/percentage line) rather than cramming multiple values into one field.

[SCREENSHOT: product spec table editor]

## 10. If something goes wrong

- **A change you published looks wrong on the live site:** open the page again, fix it, and Save and Publish again — corrections are just as easy as the original edit.
- **You accidentally deleted something important:** stop before doing anything else and contact your developer — some deletions are recoverable from Umbraco's recycle bin, but don't assume that without checking first.
- **You can't log in / lost your 2FA device:** contact your developer directly rather than repeatedly retrying — account lockouts are a security feature, not a bug.
- **You're not sure whether an action is safe:** it almost always is if you're only editing text/images within an existing page. Be more careful with anything involving *deleting* a page or *creating* a new top-level section — those are the two categories worth pausing on.

**Developer contact:** Randolf Segubre — rsegubre@gmail.com — [phone number]

## Glossary

- **Backoffice** — the admin/editing area of the site (`/umbraco`), as opposed to the public-facing site your customers see.
- **Culture / language variant** — Umbraco's term for the English vs. Chinese version of the same page.
- **Content tree** — the left-hand list of every page on the site.
- **Media Library** — where uploaded images and PDFs are stored, separate from the pages that use them.
- **Draft vs. Published** — see Section 5. A draft is only visible to you; published is visible to everyone.
- **Document type** — the "kind" of page (e.g. Product, Article, Certification) — determines which fields are available to fill in.

---

*This guide will be delivered to you as a PDF at project handover, alongside the live site, so you have a copy you can keep and print if useful.*
