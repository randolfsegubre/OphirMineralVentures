# What to send Ophir Mineral Ventures

**Send exactly two files. Nothing else in this folder is for the client.**

## ✅ Send these two

| File | What it is |
|---|---|
| `00-Ophir-Website-Complete-Proposal.pdf` | The main proposal — everything John needs to decide. Business case, his Google Workspace question answered, architecture in plain terms, hosting options, security, SEO, full costs in USD + PHP, deployment plan, post-launch plan, FAQ, and a sign-off page. |
| `02-Homepage-Design-Draft.pdf` | The visual mockup, so he can see the look and feel. Clearly marked as a draft with placeholder content. |

That's it. The proposal references the design draft, so they're meant to go together.

## ❌ Do NOT send these — internal working documents

| File | Why it stays internal |
|---|---|
| `01-Website-Plan-and-Architecture.pdf` | Written to Randolf, not John. Contains research on his trade data, stack comparisons, and internal reasoning. |
| `03-Website-Cost-Proposal.pdf` | Internal cost analysis. Discusses maintenance scenarios including "if the developer becomes unavailable" — honest planning, but not client-facing framing. |
| `05-Architecture-Decisions.pdf` | Deep technical content (why not DDD/Clean Architecture, caching internals). Would confuse rather than reassure. |
| `04-Client-Proposal.pdf` | **Superseded.** An earlier, shorter client proposal, replaced by `00-`. Kept only for history — sending it would contradict the current one. |
| `source-html/` | The editable source files the PDFs are generated from. |

## Before sending — check these

- [ ] Prices are current (last verified **2026-08-28**, USD→PHP **₱61.87**). If it's been more than a few weeks, re-check the rate — it moved ₱60.70 → ₱61.87 in three weeks.
- [ ] The proposal's cover date reflects when you're actually sending it.
- [ ] You're comfortable with the support arrangement offered in Part XI (it currently presents both "included" and "light retainer" as equally fine — steer it if you'd rather offer only one).
- [ ] Part IV.3 discloses that Umbraco 18 needs a platform upgrade before June 2027. That's deliberate honesty about a recurring cost. Remove it only if you've decided to use Umbraco 17 LTS instead.

## How to update a document

Never edit the PDFs directly. Edit the matching file in `source-html/`, then regenerate:

```bash
bash rebuild-pdfs.sh
```

Close any open PDFs in Acrobat first — a PDF open in a viewer is file-locked and cannot be overwritten.
