---
name: fe
description: 'Frontend Blazor components, Tailwind CSS layout patterns, and mobile-responsive design for SourceBase. Use when writing or reviewing Blazor components, UI layout, or responsive behavior — especially for mobile S (320px) and mobile M (375px) viewports.'
trigger: /fe
---

# /fe

Frontend conventions and mobile-responsive layout for SourceBase (`SourceBase.Web`).

## Stack

- **Blazor WebAssembly** — components in `SourceBase.Web/`
- **Tailwind CSS v4** — utility-first; source in `wwwroot/app.css`, output compiled to `wwwroot/app.min.css`
- **CSS build:** `npm run build:css` (one-shot) · `npm run watch:css` (dev watch)

## Blazor Event Handler Rules

Never use inline lambdas for event handlers or component parameters — use named delegates:

```csharp
// void with loop-captured param → Action
private Action OpenEdit(T item) => () => { _editing = item; _showForm = true; };
// @onclick="OpenEdit(item)"

// async Task with loop-captured param → Func<Task>
private Func<Task> SelectItem(Guid id) => async () => { _selectedId = id; await LoadAsync(); };
// @onclick="SelectItem(item.Id)"

// multi-statement inline → named method
private void CancelDelete() { _showDelete = false; _deleting = null; }
// OnCancel="CancelDelete"
```

## Tailwind Breakpoint Reference

Tailwind v4 default breakpoints (min-width, mobile-first):

| Prefix   | Width   | Targets                                         |
| -------- | ------- | ----------------------------------------------- |
| _(none)_ | 0px+    | Mobile S (320px), Mobile M (375px), all screens |
| `sm:`    | 640px+  | Tablet and up                                   |
| `md:`    | 768px+  | Desktop and up                                  |
| `lg:`    | 1024px+ | Large desktop                                   |

Mobile S = 320px, Mobile M = 375px — **both fall below `sm:` (640px)**, so they share the unprefixed base styles. Design the base (no-prefix) layer for 320px first, then progressively enhance with `sm:` and `md:`.

## Mobile-First Layout Rules

### Viewport targets

- **Mobile S (320px):** minimum supported width. Everything must fit without horizontal scroll.
- **Mobile M (375px):** most common mobile size. Primary mobile design target.
- **Tablet/desktop:** enhance with `sm:` / `md:` prefixes.

### Core principles

1. **Base styles are 320px.** Write unprefixed classes assuming a 320px viewport. Layer `sm:` / `md:` enhancements on top.
2. **No fixed widths on containers.** Use `w-full`, `max-w-*` + `mx-auto`, or `flex-1` — never `w-[400px]` or similar that would overflow on mobile S.
3. **Stack vertically on mobile, row on larger screens.**
   ```html
   <div class="flex flex-col sm:flex-row gap-3">...</div>
   ```
4. **Compact spacing on mobile.** Use `p-3 md:p-6`, `px-4 md:px-6`, `gap-2 md:gap-4` — avoid large padding that wastes screen real estate on 320px.
5. **Text sizing.** Prefer `text-sm` as the mobile base; reserve `text-base` / `text-lg` for headings. Avoid `text-2xl`+ without a mobile override.
6. **Truncate long strings.** Use `truncate` or `line-clamp-*` on names, titles, and labels that can overflow on narrow screens.

### Grid layouts

Use responsive column counts:

```html
<!-- 1 col on mobile S/M, 2 on sm, 3+ on md -->
<div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-3 md:gap-4">...</div>

<!-- Side-by-side on sm, stacked on mobile -->
<div class="grid grid-cols-1 sm:grid-cols-[1fr_auto] gap-2">...</div>
```

### Cards and panels

```html
<div class="bg-white rounded-lg shadow-sm border border-gray-200 p-3 md:p-6">...</div>
```

Use `p-3` (12px) on mobile, `p-6` (24px) on desktop. Don't apply the `.card` utility class when you need mobile-specific padding — build the classes inline so you can add responsive variants.

### Forms

- Inputs are always `w-full` — never fixed width.
- Stack label + input vertically (default block layout is fine).
- On mobile, form action buttons should be `w-full sm:w-auto` so they fill the narrow viewport.

```html
<button class="btn-primary w-full sm:w-auto">Save</button>
```

### Tables vs. stacked cards

Tables overflow on narrow screens. On Mobile S/M, prefer a stacked card list over a `<table>`:

```html
<!-- mobile: stacked cards; md+: table -->
<div class="block md:hidden space-y-2">
  @foreach (var item in items) {
  <div class="card p-3 text-sm">...</div>
  }
</div>
<table class="hidden md:table w-full text-sm">
  ...
</table>
```

### Navigation / sidebar

The project uses a slide-in mobile drawer (`MobileOpen` state) + `md:flex` desktop sidebar (see `NavMenu.razor` / `MainLayout.razor`). Follow the same pattern for any secondary nav panels:

- Mobile: `fixed inset-y-0 left-0 z-50` drawer triggered by a hamburger button (`md:hidden`).
- Desktop: persistent sidebar with `hidden md:flex`.
- Always add a backdrop overlay (`fixed inset-0 bg-black/50 md:hidden`) when the drawer is open.

### Top bar / header

```html
<div class="flex items-center h-14 md:h-16 px-3 md:px-6 bg-white border-b border-gray-200 gap-2 md:gap-3"></div>
```

Use `h-14` (56px) on mobile, `h-16` (64px) on desktop. Reduce horizontal padding to `px-3` on mobile.

### Overflow-safe patterns

```html
<!-- prevent text overflow -->
<span class="truncate max-w-[120px] sm:max-w-none">@longText</span>

<!-- horizontal scroll only where intentional -->
<div class="overflow-x-auto">
  <table class="min-w-[480px]">
    ...
  </table>
</div>
```

### Utility classes defined in app.css

| Class            | Use                                                                                         |
| ---------------- | ------------------------------------------------------------------------------------------- |
| `.btn-primary`   | Primary action button                                                                       |
| `.btn-secondary` | Secondary / cancel button                                                                   |
| `.btn-danger`    | Destructive action button                                                                   |
| `.btn-sm`        | Smaller button variant (add alongside btn-\*)                                               |
| `.form-input`    | Text inputs                                                                                 |
| `.form-select`   | Select dropdowns                                                                            |
| `.form-label`    | Field labels                                                                                |
| `.form-error`    | Inline validation error text                                                                |
| `.card`          | White rounded panel with border (`p-6` built-in — override with `!p-3` on mobile if needed) |
| `.badge`         | Inline status badge                                                                         |

## Component Directory

```
SourceBase.Web/
  Layout/
    MainLayout.razor    # Root layout: sidebar + top bar + main content
    NavMenu.razor       # Sidebar nav with mobile drawer support
    AuthLayout.razor    # Unauthenticated layout
  Components/
    ConfirmDialog.razor
    CurrencyInput.razor
    IconPicker.razor
    LoadingSpinner.razor
    NotificationBell.razor
    Pagination.razor
    SelectInput.razor
    UserModal.razor
  Pages/               # Route-level components
```
