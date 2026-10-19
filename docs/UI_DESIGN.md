# Compact desktop design — 2.3

Requested direction: Windows 11, compact laptop layout, white and navy/blue,
separate Vietnamese and English with a flag switch.

## Skill applied

UI UX Pro Max from https://github.com/nextlevelbuilder/ui-ux-pro-max-skill.
Installed in the user's Codex skills directory. Search scripts were run locally:

- "desktop system monitoring compact" --design-system --density 8 --variance 2
- "analytics dashboard desktop" --design-system --density 8 --variance 2
- "minimalism" --domain style
- "dragging movements" --domain ux

The minimalism style, readable blue/slate palette and dragging alternatives
matched this utility. The generated landing-page/enterprise marketing patterns
did not match a desktop fan controller and were not used or persisted as a design
system. The Windows-specific style query found no match; the desktop layout uses
the user's Windows 11 preference and the skill's general platform-adaptive rules.
WinForms is not a supported stack in the skill's database; no other stack's
implementation is presented as WinForms guidance.

## Implemented design

- Top navigation replaces the wide sidebar; no new window chrome.
- Segoe UI follows the native Windows font and supports Vietnamese.
- Consistent vector outline icons, 8 px corner radius and 4/8 px spacing rhythm.
- Primary #2563EB; primary dark #1E40AF; muted text #475569;
  page #F8FAFC; white cards. CPU marker #C2410C.
- Metrics have fixed space; sensor updates do not shift the layout.
- A horizontal control strip separates the master switch, mode and manual output.
- The curve has an explicit current-CPU marker and directly labelled points.
- A dropdown and native numeric fields provide single-pointer and keyboard
  alternatives to dragging. Arrow keys edit the selected chart point;
  Ctrl+Left/Right select a point. Point order and nondecreasing speeds are kept.
- Native settings controls remain labelled, with separate feedback/status.
- No decorative animation; reduced-motion users get the same immediate interface.
- Runtime-created controls are scaled once from 96-DPI design dimensions before
  attachment to the already-scaled form. Layout is checked at 120 DPI (125%).

## Verification scope

Real WinForms rendering in preview mode: both languages, both pages, minimum
window, dragging, numeric editing, keyboard editing, reset, language switching,
tray startup and restore. Preview mode does not read or write the ASUS driver.
The existing PsExec PID regression and Windows startup task definition checks
remain in the smoke test. Hardware, service and launcher source are unchanged
by this UI redesign.

## 2.4 revision

- Removed the custom brand/status header and overview title/subtitle.
- One three-state control: Off, Manual, Curve. Only the active editor is visible.
- Removed the side point editor. Add point offers start/end positions (0–110°C
  storage range); the plot expands when needed.
- Dragging clamps temperature order but propagates speed changes: earlier
  higher speeds are capped, later lower speeds are raised. Other valid points stay.
- Selected-point geometry follows sub-integer pointer coordinates; storage is
  rounded on commit. Axes stay fixed throughout a drag. Sensor refresh no longer
  rebuilds point-picker controls during pointer movement.
- Right-click and arrow keys still offer step editing. Reset is a text button.
- Native reads/writes run off the UI thread, serialized through a single lock.
  Writes are coalesced; stale queued requests are discarded. During drag the
  controller uses the last committed curve until release, while temperature
  monitoring continues.
- Smoothing applies a time-based rise/fall rate (20/5 percentage points/sec by
  default). First enable uses the requested target directly: firmware output
  percentage is unknown. Off bypasses smoothing. An observed CPU temperature
  >=90°C bypasses upward smoothing. The rates are configurable from 1 to 100.
- Actual hardware smoothing and perceived pointer fluidity need on-device
  confirmation. Automated checks cover the ramp math and preview UI, not the
  ASUS driver's runtime latency.
