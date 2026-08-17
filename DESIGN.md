---
name: Identity
colors:
  primary: "#066fd1"
  on-primary: "#ffffff"
  secondary: "#6b7280"
  on-secondary: "#ffffff"
  surface: "#ffffff"
  on-surface: "#374151"
  surface-variant: "#f9fafb"
  outline: "#e5e7eb"
  danger: "#d63939"
  on-danger: "#ffffff"
  success: "#2fb344"
colors-dark:
  surface: "#111827"
  on-surface: "#e5e7eb"
  primary: "#066fd1"
  danger: "#d63939"
typography:
  body:
    fontFamily: "Inter, -apple-system, BlinkMacSystemFont, \"San Francisco\", \"Segoe UI\", Roboto, \"Helvetica Neue\", sans-serif"
    fontSize: "0.875rem"
    fontWeight: 400
    lineHeight: 1.4285714286
  heading:
    fontFamily: "Inter, -apple-system, BlinkMacSystemFont, \"San Francisco\", \"Segoe UI\", Roboto, \"Helvetica Neue\", sans-serif"
    fontWeight: 600
  code:
    fontFamily: "Monaco, Consolas, \"Liberation Mono\", \"Courier New\", monospace"
rounded:
  md: "6px"
  lg: "8px"
  pill: "50rem"
spacing:
  base: "4px"
  unit: "1rem"
  note: "Use framework utility classes (p-3, mb-4, gap-2). No custom spacing values."
components:
  btn-primary:
    use: "Save, Create, Submit — one per form"
  btn-danger:
    use: "Delete and other destructive actions only"
  btn-outline-secondary:
    use: "Cancel and back navigation rendered as buttons"
  btn-outline-danger:
    use: "De-emphasized destructive row action inside a dense table"
  btn-link:
    use: "Low-emphasis inline action beside a field"
  form-control:
    use: "All text, email, password, and select inputs"
  table:
    classes: "table table-bordered table-hover table-sm"
    header: "No class — the framework styles thead natively and theme-aware"
  table-responsive:
    use: "Wrap every table so it scrolls in its own container on narrow viewports"
  card:
    use: "Home page feature grid; Admin landing section cards; account manage panels"
  alert-danger:
    use: "Validation summaries and negative status messages"
  alert-success:
    use: "Positive status messages"
---

# Design Documentation

## Overview

Identity is a standalone **OpenID Connect Identity Provider** built on Duende IdentityServer and ASP.NET Core Identity. It issues tokens for every first-party client application and provides self-service account management, plus an admin section (`/Admin`) for role-holders.

Component library: **Tabler**, vendored at `Identity/wwwroot/lib/tabler/` — check that directory for the shipped version. Tabler is built on Bootstrap 5 and **bundles it**, so it replaces Bootstrap rather than layering on top. Every Bootstrap class name used in this app is provided by Tabler.

This file is a [design.md](https://github.com/google-labs-code/design.md)-format spec — UI tokens and component conventions only. For application architecture see [ARCHITECTURE.md](ARCHITECTURE.md).

---

## No custom CSS

**The application ships zero hand-written CSS.** `wwwroot/` contains only vendored third-party stylesheets:

| File | Origin |
|---|---|
| `lib/tabler/tabler.min.css` | Tabler distribution |
| `lib/inter/latin.css` | Inter webfont package, with its own `files/*.woff2` |

There is no `site.css` and no `*.cshtml.css` CSS-isolation file. Both previously existed and were deleted: `site.css` only re-stated framework defaults, and `Pages/Shared/_Layout.cshtml.css` was compiled and served but **never linked**, so every rule in it was dead — including a navbar shadow the old spec claimed as the app's one elevation level.

When something needs to change visually, override a framework CSS custom property — never add a rule, and never add a stylesheet. If you believe a custom rule is unavoidable, that is a signal to re-check whether a framework utility already does it.

---

## Colors

Tabler defines these; do not restate them in markup with hardcoded utilities.

| Token | Light | Dark |
|---|---|---|
| `primary` | `#066fd1` | unchanged |
| `danger` | `#d63939` | unchanged |
| `success` | `#2fb344` | unchanged |
| `secondary` | `#6b7280` | lighter grey |
| `surface` (body background) | `#f9fafb` | `#111827` |
| `on-surface` (body text) | `#374151` | `#e5e7eb` |
| `outline` (borders) | `#e5e7eb` | dark grey |

**Never use a hardcoded light or dark utility** — `text-dark`, `text-white`, `bg-white`, `bg-light`, `bg-dark`, `navbar-light`, `navbar-dark`, `table-light`. Each one pins a palette and breaks the opposite colour mode. All of them were removed from this codebase for exactly that reason; re-introducing one is a regression.

---

## Dark mode

Identity follows the **operating system preference**, with no in-app toggle.

`wwwroot/js/theme.js` sets `data-bs-theme` on `<html>` from `matchMedia("(prefers-color-scheme: dark)")`. It is loaded **synchronously in `<head>`**, before the body renders, so there is no flash of light theme. It is served from `'self'`, so it needs no Content-Security-Policy allowance.

Tabler supplies the dark palette through `[data-bs-theme=dark]` blocks. Components inherit it automatically — provided markup uses semantic classes and not the hardcoded utilities listed above.

---

## Typography

Inter is **self-hosted** at `wwwroot/lib/inter/`, loaded via the font package's own stylesheet. It is not fetched from a CDN and does not use a hand-written `@font-face`. Tabler's stack names Inter first but ships no font files of its own, so without this the app would silently fall back to the system UI font.

| Role | Family | Size | Weight |
|---|---|---|---|
| Body | Inter, then the system UI stack | `0.875rem` | 400 |
| Heading | same | framework scale | 600 |
| Code | Monaco, Consolas, Liberation Mono, Courier New, monospace | framework scale | 400 |

The body size is the framework default and is deliberately denser than the browser default. Do not override it, and do not set `font-family` in component markup.

---

## Elevation

Elevation comes entirely from Tabler's component defaults. **No `shadow-*` utility appears anywhere in the markup, and none should be added.**

Measured on the rendered page: the header carries a 1px inset bottom rule rather than a drop shadow, cards and buttons carry Tabler's own subtle shadows, and the footer carries none. If a surface needs lift, it already has whatever Tabler gives it — reaching for `shadow-sm`/`shadow`/`shadow-lg` means overriding a deliberate framework decision.

---

## Shapes

There are two radii, not one: form controls render at `6px`, cards at `8px`. Both are framework defaults — measured on the deployed site, not assumed. Use `rounded-*` utilities; never set `border-radius` directly. `rounded-pill` remains available for badges.

---

## Layout

Three page-family patterns — do not mix them:

- **Anonymous account flow** (`Login`, `Register`, `ForgotPassword`, `ResetPassword`, `LoginWith2fa`, `LoginWithRecoveryCode`, `ResendEmailConfirmation`, `ExternalLogin`): centred single column, `<div class="row justify-content-center"><div class="col-md-4">`. Pages offering an external provider add a second column beside it.
- **Manage sub-pages**: sidebar nav plus content, two columns, driven by `_ManageNav`.
- **Admin pages**: full-width table layout, navigated via the Admin index card grid.

The shell (`Pages/Shared/_Layout.cshtml`) is Tabler's own page structure: a `page` flex column containing a `header.navbar` and a `page-wrapper`, which holds `page-body` and a `footer footer-transparent`. Content sits inside `container-xl`. The footer is placed by that structure rather than by utility classes.

**Page titles stay on the page, not in a `page-header` block.** Tabler offers one, but the per-page `<h1>` carries context the page title does not — `Users in {role}` against a title of `Role Users`, for instance — and several Manage pages deliberately have no heading at all. Rendering headings from `ViewData["Title"]` would flatten both.

**Forms:** `<div class="mb-3">` wrapper, `<label class="form-label">`, `<input class="form-control">`. Account-flow and account-management forms wrap fields in `form-floating`; admin forms use label-above instead.

A checkbox `asp-for` target must be a non-nullable `bool`. Several third-party entities expose `bool?`; add a non-nullable proxy property on the page model (see `Pages/Admin/Clients/Edit/Index.cshtml.cs`).

---

## Components

### Buttons

| Use case | Class |
|---|---|
| Primary action | `btn btn-primary` |
| Destructive action | `btn btn-danger` |
| Cancel / back | `<a class="btn btn-outline-secondary">` |
| De-emphasized destructive row action in a dense table | `btn btn-outline-danger` |
| Low-emphasis inline action | `btn btn-link` |
| Table row action | add `btn-sm` |
| External identity provider (Google) | `btn-outline-secondary d-inline-flex align-items-center justify-content-center gap-2` |

One `btn-primary` per form. Never `btn-primary` for a destructive action — including the entry-point link on a hub page. A link leading to a page whose submit button is `btn-danger` must itself read as destructive.

The Google button in `_ExternalProviders.cshtml` carries the official multicolor "G" mark as an inline `<svg>`, sized with `width`/`height` attributes (never inline `style`), plus the CTA text "Continue with Google" — following Google's Sign In branding guidelines, which require the logo be reproduced unaltered and paired with an approved CTA string. It uses `btn-outline-secondary` rather than `btn-primary`: the mark's own colors carry the brand recognition regardless of theme, so no hardcoded light/dark utility is needed, and it stays visually secondary to the page's real primary action (`Log in`/`Register`). Any other provider (there is none currently — Google is the only one configured, in `Program.cs`) falls back to the plain `btn-primary` + display-name rendering.

### Tables

```html
<div class="table-responsive">
    <table class="table table-bordered table-hover table-sm">
        <thead>
            <tr>
                <th>…</th>
                <th class="visually-hidden">Actions</th>
            </tr>
        </thead>
        <tbody>…</tbody>
    </table>
</div>
```

`<thead>` carries **no class**. Tabler styles table headers natively — uppercase, letter-spaced, on `--tblr-bg-surface-tertiary`, which flips with the colour mode. The old `table-light` was both a hardcoded light value *and* an override of that better default.

Every table is wrapped in `table-responsive`. An action-only column still needs a screen-reader-only header — an empty `<th></th>` fails the axe-core `empty-table-header` rule.

Alignment utilities are orthogonal to the class set and may be added where needed.

### Collection edit tables (Admin)

Editable rows use `<input class="form-control form-control-sm">` rendered by a server-side `@for` loop — no client-side JavaScript. Add/Remove buttons post to `OnPostAddRowAsync`/`OnPostRemoveRowAsync` handlers that mutate the bound list and return `Page()`. Both carry an explicit `asp-route-id`.

Every row field and its Remove button carries an index-based `id` (`{field}-{index}`), because **E2E tests select by `id`**. Keep every `id` when restyling — a class change is free, an `id` change breaks tests. Detail lists rendered for assertion also carry ids (for example `user-role-{index}`); a class-based selector is not an acceptable substitute.

### Status messages

`TempData["StatusMessage"]` renders through `_StatusMessage.cshtml` as `alert-success`, or `alert-danger` when prefixed with "Error:".

---

## Do's and Don'ts

**Do:**
- Use `btn-danger` for every destructive action, including hub-page entry links.
- Wrap every table in `table-responsive`, and give every header an accessible name.
- Keep every `id` attribute — tests depend on them.
- Prefer an HTML attribute over an inline style when sizing media (`height="32"`), since `style-src 'self'` blocks inline styles.
- Use `[AllowAnonymous]` explicitly on any page that must be public.

**Don't:**
- Add any `.css` file, or any hand-written CSS rule.
- Use hardcoded palette utilities (`text-dark`, `bg-white`, `table-light`, `navbar-light`, …).
- Use inline `style="…"` — it is blocked by the Content-Security-Policy on every page.
- Add `<script>` blocks inline — also blocked. Put JavaScript in `wwwroot/js/` and pass data via `data-` attributes.
- Use `btn-primary` for delete/remove/revoke/disable/reset.
- Use `form-floating` on admin pages.
- Add `<script src>` grid libraries or hand-rolled JavaScript to admin pages.
