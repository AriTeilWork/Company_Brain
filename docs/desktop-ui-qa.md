# Desktop UI verification

Scope: the four Avalonia placeholder screens for issue #11. Backend connectivity,
real search, PDF viewing, ticket workflows, AI responses and persistence are out
of scope. All displayed records and answers are static sample data.

## Screenshots

The screenshots below were rendered from the application views using Avalonia
and Skia at 1440 × 810 logical units and density 1.

- [Devices](screenshots/desktop-devices.png)
- [Customers](screenshots/desktop-customers.png)
- [Support guides](screenshots/desktop-guides.png)
- [Ask](screenshots/desktop-ask.png)

The four supplied design references were compared locally against these screens.
The reference attachments are not included in this repository.

## Visual checks

- Checked typography, spacing, colors, icons and sample copy against the references.
- Checked all four screens at 1440 × 810 and 1100 × 700.
- Fixed sidebar subtitle clipping, narrow PDF buttons, and agreement-label wrapping.
- Adjusted the device table, related actions and Ask spacing to match the references.
- At the smaller size, content scrolls while navigation and window controls remain available.
- One consistent sidebar width is used across all screens. The demo flame and
  native vector icons are intentionally not exact copies of the raster artwork.

## Interaction and build checks

- Sidebar navigation and related-screen links use local state only.
- Placeholder actions display dismissible feedback.
- Minimize, maximize, restore and close routed events and Avalonia window states
  passed automated checks in the headless platform.
- The desktop application builds without errors or warnings.
- Migration to `desktop/` requires a fresh package restore; build outputs and local
  tooling are excluded from the commit.

## Limits

Direct Windows window capture was blocked by the capture tool's window-owner
lookup. Verification therefore used actual Avalonia/Skia view rendering. Physical
Windows dragging, resizing and OS window-state transitions remain manual checks.

final result: passed
