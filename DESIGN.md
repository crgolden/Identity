---
name: Identity
colors:
  primary: "#0d6efd"
  on-primary: "#ffffff"
  secondary: "#6c757d"
  on-secondary: "#ffffff"
  surface: "#ffffff"
  on-surface: "#212529"
  surface-variant: "#f8f9fa"
  outline: "#dee2e6"
  danger: "#dc3545"
  on-danger: "#ffffff"
  focus-ring: "#258cfb"
typography:
  body:
    family: "Inter, system-ui, -apple-system, sans-serif"
    size-mobile: "14px"
    size-desktop: "16px"
    weight: 400
    line-height: 1.5
  heading:
    family: "Inter, system-ui, -apple-system, sans-serif"
    weight: 600
  code:
    family: "SFMono-Regular, Menlo, Monaco, Consolas, monospace"
    size: "0.875em"
rounded:
  sm: "0.25rem"
  md: "0.375rem"
  lg: "0.5rem"
  pill: "50rem"
spacing:
  base: "4px"
  unit: "1rem"
  note: "Use Bootstrap utility classes (p-3, mb-4, gap-2, etc.) — no custom spacing values."
components:
  btn-primary:
    background: primary
    color: on-primary
    use: "Save, Create, Submit — one per form"
  btn-danger:
    background: danger
    color: on-danger
    use: "Delete and other destructive actions only"
  btn-secondary:
    background: secondary
    color: on-secondary
    use: "Secondary actions where contrast with btn-primary is needed"
  btn-outline-secondary:
    use: "Cancel and back navigation links rendered as buttons"
  nav-link:
    use: "Top navigation and sidebar links"
  form-control:
    border-radius: md
    use: "All text, email, password, and select inputs"
  table:
    classes: "table table-bordered table-hover table-sm"
    header-class: "table-light"
  card:
    border-radius: md
    surface: surface
    use: "Admin landing page section cards; account manage panels"
  alert-danger:
    use: "Validation summaries and error status messages (TempData[\"StatusMessage\"] when negative)"
  alert-success:
    use: "Confirmation status messages (TempData[\"StatusMessage\"] when positive)"
---

# Design Documentation

## Overview

Identity is a standalone **OpenID Connect Identity Provider** (IdP) built on Duende IdentityServer 8 and ASP.NET Core Identity. It issues OIDC/OAuth2 tokens for all first-party client applications and provides a full self-service account management UI. An admin section (`/Admin`) gives role-holders direct CRUD access to every IdentityServer configuration entity and ASP.NET Identity user/role.

Component library: **Bootstrap 5.3.8**. All UI tokens above map directly to Bootstrap CSS custom properties and utility class names. No custom CSS framework or design system — only Bootstrap classes and the tokens declared in the front matter above.

This file is a [design.md](https://github.com/google-labs-code/design.md)-format visual design system spec — UI tokens and component conventions only. For application architecture (routing tiers, authentication flows, data layer, observability, security, CI/CD), see [ARCHITECTURE.md](ARCHITECTURE.md).

---

## Colors

| Token | Hex | Bootstrap mapping |
|---|---|---|
| `primary` | `#0d6efd` | `--bs-primary`, `btn-primary`, `text-primary` |
| `on-primary` | `#ffffff` | White text on primary backgrounds |
| `secondary` | `#6c757d` | `--bs-secondary`, `btn-secondary` |
| `on-secondary` | `#ffffff` | White text on secondary backgrounds |
| `surface` | `#ffffff` | Page and card backgrounds |
| `on-surface` | `#212529` | `--bs-body-color`, default body text |
| `surface-variant` | `#f8f9fa` | `--bs-light`, table headers (`table-light`), footer, sidebar |
| `outline` | `#dee2e6` | `--bs-border-color`, form control borders, table borders |
| `danger` | `#dc3545` | `--bs-danger`, `btn-danger`, `alert-danger`, `text-danger` |
| `on-danger` | `#ffffff` | White text on danger backgrounds |
| `focus-ring` | `#258cfb` | Custom focus ring color defined in `site.css` |

---

## Typography

| Role | Family | Size | Weight | Line height |
|---|---|---|---|---|
| Body | Inter, system-ui, -apple-system, sans-serif | 14px (mobile) / 16px (≥768px) | 400 | 1.5 |
| Heading | same | Bootstrap scale (`h1`–`h6`) | 600 | Bootstrap default |
| Code | SFMono-Regular, Menlo, Monaco, Consolas, monospace | 0.875em | 400 | Bootstrap default |

Font size is controlled by `site.css` media query. Headings use `fw-semibold` or native `<h>` weight. Do not set custom `font-family` in component markup — use the body default.

---

## Layout

Pages use Bootstrap's fluid container (`container-fluid` or `container`) with the shared `_Layout.cshtml`. No custom grid overrides.

- **Account pages** (Login, Register, Manage sub-pages): centered single column, max-width constrained via `col-md-6 offset-md-3` or equivalent.
- **Admin pages**: full-width table layout. No sidebar nav — navigation is via the Admin Index card grid and in-page breadcrumb-style links.
- **Form layout**: `<div class="mb-3">` wrapper, `<label class="form-label">`, `<input class="form-control">`. Use `form-floating` for single-field focused pages (login, register already use it).
- **Collection edit tables**: `table table-bordered table-hover table-sm` with inputs inside cells. See Components section.

---

## Shapes

| Token | Value | Applied to |
|---|---|---|
| `sm` | `0.25rem` | Small badges, tight UI elements |
| `md` | `0.375rem` | Inputs (`form-control`), cards (Bootstrap default) |
| `lg` | `0.5rem` | Larger cards, modals |
| `pill` | `50rem` | `rounded-pill` badges |

Use Bootstrap `rounded-*` utilities. Do not set `border-radius` inline or in custom CSS.

---

## Components

### Buttons

| Use case | Class |
|---|---|
| Primary action (Save, Create, Submit) | `btn btn-primary` |
| Destructive action (Delete, Remove) | `btn btn-danger` |
| Secondary action | `btn btn-secondary` |
| Cancel / back navigation | `<a class="btn btn-outline-secondary">` |
| Small table row action | add `btn-sm` |

One `btn-primary` per form. Never use `btn-primary` for delete — always `btn-danger`.

### Forms

```html
<div class="mb-3">
    <label asp-for="Field" class="form-label"></label>
    <input asp-for="Field" class="form-control" />
    <span asp-validation-for="Field" class="text-danger"></span>
</div>
```

### Tables

```html
<table class="table table-bordered table-hover table-sm">
    <thead class="table-light">
        <tr>…</tr>
    </thead>
    <tbody>…</tbody>
</table>
```

### Collection edit tables (Admin sub-pages)

Editable rows contain `<input class="form-control form-control-sm">`. An "Add" button (`btn btn-sm btn-outline-secondary`) appends a row from a `<template>` element. A "Remove" button (`btn btn-sm btn-danger`) removes the row. On submit, `admin-collection.js` renumbers surviving inputs into `CollectionProperty[0].Field`, `[1].Field`, … format.

`admin-collection.js` is loaded only in the `@section Scripts` of collection Edit pages — not globally.

### Status messages

```csharp
TempData["StatusMessage"] = "Your profile has been updated.";
```

Rendered in `_StatusMessage.cshtml` as `alert-success` (positive) or `alert-danger` (negative, prefix message with "Error: ").

### Cards (Admin Index)

```html
<div class="card h-100">
    <div class="card-body">
        <h5 class="card-title">Section</h5>
        <a asp-page="…" class="btn btn-sm btn-primary">Manage</a>
    </div>
</div>
```

---

## Do's and Don'ts

**Do:**
- Use `btn-danger` for every destructive action (Delete, Remove, Revoke).
- Use `btn-outline-secondary` styled as `<a>` for Cancel/Back.
- Use `table-light` on `<thead>` in every admin table.
- Use `TempData["StatusMessage"]` + `_StatusMessage.cshtml` for post-redirect feedback.
- Use `Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : LocalRedirect("~/")` — never `LocalRedirect(returnUrl)` directly.
- Gate `/Admin/**` with the `"Admin"` role policy via `AuthorizeFolder` — no per-page `[Authorize]`.
- Use `[AllowAnonymous]` explicitly on any page that must be public inside the otherwise-protected Razor Pages tree.

**Don't:**
- Use `btn-primary` for delete or remove actions.
- Use `btn-danger` for navigation or secondary actions.
- Add `<script src>` tags for external grid libraries (Kendo, DataTables, etc.) on admin pages — use the vanilla `admin-collection.js` pattern.
- Hardcode `returnUrl` into `LocalRedirect` without `IsLocalUrl` check (open redirect risk).
- Set `border-radius`, `font-family`, or `color` inline or in component-scoped CSS — use Bootstrap tokens and utilities only.
- Use `form-floating` outside of single-field focused pages (login, register). Admin collection forms use standard label-above layout.
