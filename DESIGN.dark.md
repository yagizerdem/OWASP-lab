---
name: Cybersecurity Workbench
colors:
  surface: "#101419"
  surface-dim: "#101419"
  surface-bright: "#36393f"
  surface-container-lowest: "#0a0e13"
  surface-container-low: "#181c21"
  surface-container: "#1c2025"
  surface-container-high: "#262a30"
  surface-container-highest: "#31353b"
  on-surface: "#e0e2ea"
  on-surface-variant: "#bbcabf"
  inverse-surface: "#e0e2ea"
  inverse-on-surface: "#2d3136"
  outline: "#86948a"
  outline-variant: "#3c4a42"
  surface-tint: "#4edea3"
  primary: "#4edea3"
  on-primary: "#003824"
  primary-container: "#10b981"
  on-primary-container: "#00422b"
  inverse-primary: "#006c49"
  secondary: "#ffb3ad"
  on-secondary: "#68000a"
  secondary-container: "#a40217"
  on-secondary-container: "#ffaea8"
  tertiary: "#7bd0ff"
  on-tertiary: "#00354a"
  tertiary-container: "#19aee8"
  on-tertiary-container: "#003e55"
  error: "#ffb4ab"
  on-error: "#690005"
  error-container: "#93000a"
  on-error-container: "#ffdad6"
  primary-fixed: "#6ffbbe"
  primary-fixed-dim: "#4edea3"
  on-primary-fixed: "#002113"
  on-primary-fixed-variant: "#005236"
  secondary-fixed: "#ffdad7"
  secondary-fixed-dim: "#ffb3ad"
  on-secondary-fixed: "#410004"
  on-secondary-fixed-variant: "#930013"
  tertiary-fixed: "#c4e7ff"
  tertiary-fixed-dim: "#7bd0ff"
  on-tertiary-fixed: "#001e2c"
  on-tertiary-fixed-variant: "#004c69"
  background: "#101419"
  on-background: "#e0e2ea"
  surface-variant: "#31353b"
typography:
  display:
    fontFamily: Geist
    fontSize: 48px
    fontWeight: "600"
    lineHeight: 56px
    letterSpacing: -0.03em
  headline-lg:
    fontFamily: Geist
    fontSize: 32px
    fontWeight: "600"
    lineHeight: 40px
    letterSpacing: -0.02em
  headline-lg-mobile:
    fontFamily: Geist
    fontSize: 26px
    fontWeight: "600"
    lineHeight: 34px
    letterSpacing: -0.02em
  headline-md:
    fontFamily: Geist
    fontSize: 24px
    fontWeight: "500"
    lineHeight: 32px
    letterSpacing: -0.015em
  headline-sm:
    fontFamily: Geist
    fontSize: 18px
    fontWeight: "500"
    lineHeight: 26px
    letterSpacing: -0.01em
  body-lg:
    fontFamily: JetBrains Mono
    fontSize: 15px
    fontWeight: "400"
    lineHeight: 24px
    letterSpacing: -0.01em
  body-md:
    fontFamily: JetBrains Mono
    fontSize: 13px
    fontWeight: "400"
    lineHeight: 20px
    letterSpacing: -0.005em
  body-sm:
    fontFamily: JetBrains Mono
    fontSize: 12px
    fontWeight: "400"
    lineHeight: 18px
    letterSpacing: 0em
  code-inline:
    fontFamily: JetBrains Mono
    fontSize: 13px
    fontWeight: "500"
    lineHeight: 20px
    letterSpacing: 0em
  label-md:
    fontFamily: JetBrains Mono
    fontSize: 12px
    fontWeight: "500"
    lineHeight: 16px
    letterSpacing: 0.04em
  label-sm:
    fontFamily: JetBrains Mono
    fontSize: 11px
    fontWeight: "600"
    lineHeight: 14px
    letterSpacing: 0.06em
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  gutter: 1rem
  gutter-desktop: 1.5rem
  margin: 1rem
  margin-tablet: 1.5rem
  margin-desktop: 2.5rem
  space-xs: 0.25rem
  space-sm: 0.5rem
  space-md: 1rem
  space-lg: 1.5rem
  space-xl: 2rem
---

## Brand & Style

This design system delivers an ultra-sharp, high-precision developer platform aesthetic tailored for technical security practitioners, reverse engineers, and defensive infrastructure operators. The design rejects juvenile, neon-saturated "hacker" tropes in favor of the controlled, architectural ergonomics found in modern engineering workbenches.

The interface prioritizes dense, hyper-legible information displays, low visual latency, and immediate spatial clarity. Surface interactions reflect mechanical certitude: deliberate high-contrast edge demarcation, razor-sharp focus states, and zero non-functional ornamentation. Visual energy is channeled purely into code blocks, exploit execution streams, security state indicators, and verified assessment progress.

## Colors

The color architecture is built around a dark canvas with high-contrast structural layers:

- **Surface Neutral Layering:** The foundational viewport uses `#0B0F14` (Deep Canvas). Elevated containers and workspaces sit on `#121821` (Card/Panel Surface), `#182230` (Interactive Hover Surface), and `#1F2A37` (Structural Borders and Dividers). Text layers map strictly from high-contrast primary values (`#F9FAFB`) to secondary metadata (`#9CA3AF`) and disabled labels (`#4B5563`).
- **Primary Operational (`#10B981`):** The primary terminal emerald is reserved for system affirmative states, successful mitigation steps, live terminal connectivity, and primary execution triggers. Pair with dark backgrounds at an absolute minimum contrast ratio of 4.5:1 (WCAG AA compliance).
- **Secondary Alert / Vulnerability (`#EF4444`):** Used exclusively for high-severity vulnerability markers, attack surface alerts, exploits, critical difficulty indicators, and destructive actions.
- **Tertiary Inspection (`#38BDF8`):** A disciplined cyan used for network packet inspectors, technical architecture tags, protocol types, and variable debugging callouts.

## Typography

Typography establishes an intentional cadence between editorial structural frames (Geist) and technical runtime data (JetBrains Mono):

- **Geist (Headlines & Structural Titles):** Provides tight geometric rendering with negative tracking at display scales. Headlines articulate course titles, lab modules, and architecture diagrams without consuming disproportionate vertical canvas.
- **JetBrains Mono (Body, Code, Controls, and Labels):** Deployed across all core explanatory text, terminal streams, and interaction labels. The increased ligature width and distinct glyph differentiation (zeros, ones, backticks) eliminate visual ambiguity during live debugging and security walk-throughs.

## Layout & Spacing

The layout model uses a rigid 12-column fluid grid system pinned to a maximum canvas constraint of 1600px for panoramic multi-panel IDE views, or centered at 1200px for structured syllabus views.

- **Breakpoints:**
  - `Desktop (>= 1280px)`: 12 columns, 24px (`1.5rem`) gutters, 40px (`2.5rem`) canvas margins. Sidebars lock to persistent 280px fixed tracks.
  - `Tablet (768px - 1279px)`: 8 columns, 16px (`1rem`) gutters, 24px (`1.5rem`) margins. Toolbars compress into icon-strip drawers.
  - `Mobile (< 768px)`: 4 columns, 16px (`1rem`) gutters, 16px (`1rem`) margins. Panels stack vertically; terminal interfaces collapse into split horizontal swipe trays.
- **Rhythm:** Spacing follows an absolute 4px base increment (`space-xs` = 4px, `space-sm` = 8px, `space-md` = 16px, `space-lg` = 24px, `space-xl` = 32px) to ensure strict vertical coordinate alignment across monospace blocks and adjacent control rails.

## Elevation & Depth

This design system avoids heavy, diffused dropshadows. Depth is achieved via **tonal elevation combined with low-contrast structural borders**:

- **Layer 0 (Canvas):** `#0B0F14` baseline.
- **Layer 1 (Card & Module Surfaces):** `#121821` enclosed with a 1px solid border of `#1F2A37`.
- **Layer 2 (Interactive Floating Flyouts & Tooltips):** `#182230` with a 1px solid border of `#2D3B4E`, accented with a minimal, dark edge-blur: `0 4px 12px rgba(0, 0, 0, 0.45)`.
- **Terminal Gridlines:** Background surfaces in code inspection contexts feature an optional faint matrix grid using 1px rules at 24px offsets with 4% opacity (`rgba(255, 255, 255, 0.04)`).
- **Focus & Selection Indication:** Active panels display a razor-thin 1px border highlight using `#10B981` accompanied by a localized inner rim light (`inset 0 0 0 1px #10B981`), creating instant spatial focus without spatial displacement.

## Shapes

The shape vocabulary uses restrained radii (Level 1 / Soft):

- **Default UI Containers, Buttons, and Inputs:** 4px (`0.25rem`) border radius. This preserves a dense, technical instrument aesthetic.
- **Outer Card Frames & Modal Panels:** 8px (`0.5rem`) border radius (`rounded-lg`).
- **Code Run Blocks & Terminal Containers:** 4px (`0.25rem`) border radius to ensure flush horizontal scanning against parent containers.
- **Pills / Status Dots:** Circular elements strictly for live ping lights, port indicators, and threat-level avatars (9999px).

## Components

### Buttons

- **Primary Action (Run Exploit / Verify Mitigation):** Background `#10B981`, foreground `#0B0F14`, font `JetBrains Mono` weight 500, border-radius 4px. Hover elevates to `#34D399`. Active state transitions to `#059669`.
- **Secondary Action (Inspect / Trace):** Background `#121821`, border 1px solid `#1F2A37`, text `#F9FAFB`. Hover shifts background to `#182230` and border to `#374151`.
- **Danger Action (Terminate Lab / Reset State):** Background transparent, border 1px solid `#EF4444`, text `#EF4444`. Hover fills with `#EF4444` at 15% opacity.

### Badges & Category Chips

- Rendered in `JetBrains Mono` 11px uppercase (`label-sm`), 4px border-radius, 1px border, padding `2px 8px`.
- **Severity Tag (Critical/Exploit):** Surface `rgba(239, 68, 68, 0.1)`, border `rgba(239, 68, 68, 0.35)`, text `#EF4444`.
- **Verified / Clean Tag:** Surface `rgba(16, 185, 129, 0.1)`, border `rgba(16, 185, 129, 0.35)`, text `#10B981`.
- **Protocol / Technical Tag:** Surface `rgba(56, 189, 248, 0.1)`, border `rgba(56, 189, 248, 0.35)`, text `#38BDF8`.

### Input Fields & Terminal Consoles

- **Input Fields:** Background `#0B0F14`, border 1px solid `#1F2A37`, radius 4px, text `JetBrains Mono` 13px `#F9FAFB`. Focus ring shifts border to `#10B981` without browser-native glow.
- **Terminal Console Block:** Background `#0B0F14`, header bar `#121821` with 1px bottom border `#1F2A37`. Output text rendered in `JetBrains Mono` 12px with 18px line height.

### Cards & Module Panels

- Background `#121821`, border 1px solid `#1F2A37`, padding 24px (`space-lg`), radius 8px. Hover on interactive curriculum cards triggers a transition of the border to `#374151`.

### Checkboxes & Radios

- Box size 16x16px, background `#0B0F14`, border 1px solid `#1F2A37`, radius 2px for checkboxes, full circle for radios. Checked state fills with `#10B981`, rendering an internal dark `#0B0F14` micro-check.
