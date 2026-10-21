# Desktop Clock — Windows design

## Purpose
Keep the local time visible above normal desktop windows without blocking the pointer or window controls. Windows only; native C# desktop application. Working name: Desktop Clock.

## Appearance and theme
Compact borderless digital clock with a subtle translucent background, no taskbar entry, and a notification-area icon. Use the selected Windows app light/dark appearance, system accent color for trim/status, and corresponding readable foreground/background colors. Update live when Windows appearance changes. In Windows contrast themes, use system foreground/background colors and an opaque background for readability.

Display local time using the Windows regional time format. Date can be toggled from the tray menu. Do not activate the clock or steal keyboard focus during movement.

## Home position
Initially place the clock 16 device-independent pixels from the upper-right of the primary monitor's usable work area, respecting taskbar placement and display scaling. When avoidance is disabled, dragging sets a new home position. Persist that position and monitor; if the monitor disappears, return to the primary monitor and clamp within its work area. Reset Position restores the default upper-right home.

## Pointer avoidance and return
Observe pointer position without recording pointer history. Start yielding when the pointer enters a 48-DIP margin around the clock. Select a nearby position clear of that margin and slide toward it quickly, approximately 200 ms. Prefer minimal displacement, remaining within the same monitor's work area. Reassess while the pointer moves; avoid oscillation by retaining a safe target until it becomes unsafe.

After the pointer remains outside the avoidance margin for one second, return smoothly toward home over approximately two seconds. Interrupt the return immediately when necessary to keep clear of the pointer, including when it is near home or along the return path. Ordinary automatic movement never changes home. Respect Windows disabled-animation settings by moving immediately while retaining the return delay.

## Hotkey and interaction
Ctrl+Alt+C globally toggles avoidance. Disabling freezes the clock at its current position, cancels automatic return, and permits dragging and clock context-menu interaction. Re-enabling resumes avoidance and delayed return. Show the state in the tray menu and with a small themed status indicator. Keep the hotkey active regardless of which application has focus. If registration fails because another app owns it, explain the conflict and retain the tray toggle.

## Tray controls and persistence
Tray menu: Enable Avoidance (checked state and hotkey label), Show Date, Reset Position, Launch at Sign-in, and Exit. Launch at sign-in is opt-in and defaults off. Persist preferences locally in the current user's application-data directory. Start each process with avoidance enabled to prevent an accidentally immobile clock after restart. Run a single instance per user.

## Components
Separate the clock window, pure movement/geometry logic, Windows integration (pointer, hotkey, monitor, theme, tray, startup), and settings persistence. Use a native C# Windows desktop UI; choose the specific framework/runtime during implementation planning after checking current supported tooling. No backend, accounts, telemetry, or network dependency.

## Error handling
Use safe defaults for missing or invalid settings. Keep tray controls available if hotkey registration fails. Recompute bounds after resolution, scaling, taskbar, or monitor changes. Explain failures to change sign-in startup without changing the displayed setting to a false success.

## Validation
Automated geometry/state tests cover corner avoidance, clamping, hysteresis, interrupted return, freeze/re-enable, and unavailable monitors. Build verification covers the Windows application. Windows manual checks cover topmost behavior, non-activation, hotkey conflicts, dragging, regional time, live light/dark/accent and contrast-theme changes, mixed-DPI monitors, taskbar relocation, and startup. Pointer feel and actual desktop integration require a Windows run before release.

## Initial scope
One clock, one home monitor, local time, optional date, avoidance toggle, saved home, theme integration, and tray controls. No alarms or additional time zones.
