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
    fontFamily: "system-ui, -apple-system, \"Segoe UI\", Roboto, \"Helvetica Neue\", Arial, sans-serif"
    fontSize: "1rem"
    fontWeight: 400
    lineHeight: 1.5
  heading:
    fontFamily: "system-ui, -apple-system, \"Segoe UI\", Roboto, \"Helvetica Neue\", Arial, sans-serif"
    fontWeight: 600
  code:
    fontFamily: "SFMono-Regular, Menlo, Monaco, Consolas, monospace"
    fontSize: "0.875em"
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
    background: "{colors.primary}"
    color: "{colors.on-primary}"
    use: "Save, Create, Submit — one per form"
  btn-danger:
    background: "{colors.danger}"
    color: "{colors.on-danger}"
    use: "Delete and other destructive actions only"
  btn-secondary:
    background: "{colors.secondary}"
    color: "{colors.on-secondary}"
    use: "Secondary actions where contrast with btn-primary is needed"
  btn-outline-secondary:
    use: "Cancel and back navigation links rendered as buttons"
  btn-outline-primary:
    use: "De-emphasized, non-destructive utility actions in dense panels (e.g. a session-list filter button) — never a substitute for the one true btn-primary in a form"
  btn-outline-danger:
    use: "De-emphasized destructive row actions inside dense tables (e.g. Revoke in a Grants or Sessions table). Prefer solid btn-danger everywhere else"
  btn-link:
    use: "Low-emphasis inline actions embedded in body text or beside a field (e.g. \"Send verification email\")"
  nav-link:
    use: "Top navigation and sidebar links"
  form-control:
    border-radius: "{rounded.md}"
    use: "All text, email, password, and select inputs"
  table:
    classes: "table table-bordered table-hover table-sm"
    header-class: "table-light"
  table-responsive:
    use: "Wrap every table in <div class=\"table-responsive\"> so wide tables scroll within their own container instead of forcing page-level horizontal scroll on mobile"
  card:
    border-radius: "{rounded.md}"
    surface: "{colors.surface}"
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

Identity does not load a web font. Body and heading text render in Bootstrap's default `system-ui` stack — the OS's native UI font (Segoe UI on Windows, San Francisco on macOS/iOS, Roboto on Android). This is a deliberate choice, not an oversight: no `<link>` or `@font-face` is present anywhere in `site.css` or `_Layout.cshtml`, and this file previously (incorrectly) documented an `Inter` token that was never actually wired up — corrected here to describe what's shipped.

| Role | Family | Size | Weight | Line height |
|---|---|---|---|---|
| Body | system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif | 14px (mobile) / 16px (≥768px) | 400 | 1.5 |
| Heading | same | Bootstrap scale (`h1`–`h6`) | 600 | Bootstrap default |
| Code | SFMono-Regular, Menlo, Monaco, Consolas, monospace | 0.875em | 400 | Bootstrap default |

Font size is controlled by a `site.css` media query (`html { font-size: 14px }`, raised to `16px` at `min-width: 768px`) — verified against the deployed stylesheet. Headings use `fw-semibold` or native `<h>` weight. Do not set custom `font-family` in component markup — use the body default, and do not introduce a web font without updating this section to match.

---

## Layout

Pages use Bootstrap's fluid container (`container-fluid` or `container`) with the shared `_Layout.cshtml`. No custom grid overrides. There are three distinct layout patterns, one per page family — do not mix them:

- **Anonymous account-flow pages** (`Login`, `Register`, `ForgotPassword`, `ResetPassword`, `LoginWith2fa`, `LoginWithRecoveryCode`, `ResendEmailConfirmation`, `ExternalLogin`): centered single column via `<div class="row justify-content-center"><div class="col-md-4">`. Pages that also offer an external-provider option (Login, Register) add a second `col-md-6 col-md-offset-2` beside it within the same centered row.
- **Manage sub-pages** (authenticated account settings): sidebar nav + content, two columns — a vertical `nav-pills`-style list of section links (Profile, Email, Password, …) on the left, the active section's content on the right. Not centered, not single-column — this is an intentionally different pattern from the anonymous flow above.
- **Admin pages**: full-width table layout. No sidebar nav — navigation is via the Admin Index card grid and in-page breadcrumb-style links.
- **Form layout**: `<div class="mb-3">` wrapper, `<label class="form-label">`, `<input class="form-control">`. Use `form-floating` for single-field focused pages (login, register already use it).
- **Collection edit tables**: `table table-bordered table-hover table-sm` with inputs inside cells. See Components section.

---

## Elevation & Depth

Identity uses exactly one elevation level: Bootstrap's `box-shadow` utility on the top navbar (`Pages/Shared/_Layout.cshtml`, `<nav class="... box-shadow ...">`), giving it a subtle lift over page content. No other surface (cards, modals, dropdowns) uses a shadow — cards are differentiated by the `outline` border color and `surface-variant` background only. Do not add `shadow-sm`/`shadow`/`shadow-lg` to any other component without updating this section.

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
| Destructive action (Delete, Remove, Revoke, Disable, Reset) | `btn btn-danger` |
| Secondary / neutral action (e.g. "Forget this browser") | `btn btn-secondary` or `btn btn-outline-secondary` |
| Cancel / back navigation | `<a class="btn btn-outline-secondary">` |
| De-emphasized non-destructive utility action (e.g. a table filter) | `btn btn-outline-primary` |
| De-emphasized destructive row action inside a dense table | `btn btn-outline-danger` |
| Low-emphasis inline action beside a field | `btn btn-link` |
| Small table row action | add `btn-sm` |

One `btn-primary` per form. Never use `btn-primary` for a destructive action — always `btn-danger`, even when that action is reached via a link from a hub/menu page rather than the confirmation page itself. A link that leads to a page whose own submit button is `btn-danger` must itself be styled `btn-danger` (or, in a dense table row, `btn-outline-danger`) — don't let the entry point undersell what it does.

### Forms

```html
<div class="mb-3">
    <label asp-for="Field" class="form-label"></label>
    <input asp-for="Field" class="form-control" />
    <span asp-validation-for="Field" class="text-danger"></span>
</div>
```

A checkbox `asp-for` target must be a non-nullable `bool`. Several third-party entities (e.g. Duende's `Client.CoordinateLifetimeWithUserSession`, `SamlServiceProvider.RequireSignedAuthnRequests`/`RequireSignedLogoutResponses`) expose `bool?` properties — `asp-for` cannot bind a checkbox `<input>` directly to those; add a non-nullable proxy property on the page model instead (see `Pages/Admin/Clients/Edit/Index.cshtml.cs`'s `CoordinateLifetimeWithUserSession` for the pattern).

### Tables

```html
<div class="table-responsive">
    <table class="table table-bordered table-hover table-sm">
        <thead class="table-light">
            <tr>
                <th>…</th>
                <th class="visually-hidden">Actions</th>
            </tr>
        </thead>
        <tbody>…</tbody>
    </table>
</div>
```

Every table — Admin index pages and Manage sub-pages alike — is wrapped in `table-responsive` so it scrolls within its own container on narrow viewports instead of forcing the whole page to scroll horizontally. An action-only column (no visible header text, e.g. Details/Edit/Delete buttons) still needs a screen-reader-only header — an empty `<th></th>` fails automated accessibility checks (axe-core `empty-table-header`).

### Collection edit tables (Admin sub-pages)

Editable rows contain `<input class="form-control form-control-sm">`, rendered by a single server-side `@for` loop — there is no client-side JavaScript in this pattern. An "Add" button (`btn btn-sm btn-outline-secondary`, `asp-page-handler="AddRow"`) posts to a page handler that appends one blank item to the bound list and returns `Page()`, so the newly-added row appears via a normal page reload. A "Remove" button (`btn btn-sm btn-danger`, `asp-page-handler="RemoveRow" asp-route-index="@i"`) posts to a handler that removes the item at that index the same way. Both handlers carry an explicit `asp-route-id` — never rely on the browser reusing the current URL's query string, since after an Add/Remove round trip that URL still carries the previous `?handler=` value.

Every row field and its Remove button carries an index-based `id` (`{field}-{index}` / `{field}-remove-{index}`, e.g. `scope-0`, `claim-type-0`, `claim-remove-0`) using the loop's own `@i` — per project convention, E2E tests select elements by `id` only. Because the same loop renders both already-saved and freshly-added rows, indices are always contiguous starting at 0, matching ASP.NET Core's default `List<T>` model-binding requirement with no separate renumbering step needed. See `TESTING.md`'s "ID convention" table for the full pattern.

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
- Use `btn-danger` for every destructive action (Delete, Remove, Revoke, Disable, Reset) — including the entry-point link on a hub page, not just the final confirmation page's submit button.
- Use `btn-outline-secondary` styled as `<a>` for Cancel/Back.
- Use `table-light` on `<thead>` in every table, and wrap every `<table>` in `table-responsive`.
- Give every table header a non-empty accessible name — use `visually-hidden` text for action-only columns.
- Use `TempData["StatusMessage"]` + `_StatusMessage.cshtml` for post-redirect feedback.
- Use `Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : LocalRedirect("~/")` — never `LocalRedirect(returnUrl)` directly.
- Gate `/Admin/**` with the `"Admin"` role policy via `AuthorizeFolder` — no per-page `[Authorize]`.
- Use `[AllowAnonymous]` explicitly on any page that must be public inside the otherwise-protected Razor Pages tree.
- Add a non-nullable proxy property on the page model for any checkbox bound to a third-party `bool?` entity property.

**Don't:**
- Use `btn-primary` for delete, remove, revoke, disable, or reset actions — anywhere, including hub-page entry links.
- Use `btn-danger` for navigation or secondary actions.
- Add `<script src>` tags for grid libraries (Kendo, DataTables, etc.) or hand-rolled JavaScript on admin pages — collection editors use the server-side `OnPostAddRowAsync`/`OnPostRemoveRowAsync` pattern described above, with no client-side script.
- Hardcode `returnUrl` into `LocalRedirect` without `IsLocalUrl` check (open redirect risk).
- Set `border-radius`, `font-family`, or `color` inline or in component-scoped CSS — use Bootstrap tokens and utilities only.
- Use `form-floating` outside of single-field focused pages (login, register). Admin collection forms use standard label-above layout.
- Bind `asp-for` directly to a `bool?` property on a checkbox `<input>` — it throws at render time, not bind time, so it fails every request rather than only on unexpected data.
