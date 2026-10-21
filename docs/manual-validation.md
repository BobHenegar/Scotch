# Scotch desktop validation

Status: pending on a user's Windows desktop. CI build/unit tests do not establish pointer feel or interactive desktop acceptance.

- [ ] Launch above a maximized application; check upper-right padding and readable time.
- [ ] Type in another app while Scotch launches/moves/returns; keyboard focus must stay there.
- [ ] Approach minimize/maximize/close controls slowly and rapidly; clock yields and controls remain accessible.
- [ ] Park the pointer at home: clock stays away without oscillating.
- [ ] Clear the pointer: clock waits roughly one second then glides home in roughly two seconds.
- [ ] Move into the return path: clock interrupts its return.
- [ ] Hold Ctrl+Alt+C: only one toggle until the chord is released and pressed again.
- [ ] Pause halfway through movement: clock freezes; drag and release sets home; resume yields normally.
- [ ] Restart: avoidance starts enabled, saved home and date are restored.
- [ ] Reserve Ctrl+Alt+C in another app: notification explains conflict and tray toggle still works.
- [ ] Switch app light/dark mode, accent color, and Windows contrast themes while running.
- [ ] Disable Windows animation effects: positioning becomes immediate, return delay remains.
- [ ] Change regional time to 12/24-hour and toggle date; verify clock fits without cropping.
- [ ] Drag (paused) to monitors at 100%, 150%, 200%, including monitors left/above primary; home and pointer margin remain correct.
- [ ] Unplug the saved monitor, change resolution/scaling, and relocate taskbar; clock remains within the active work area.
- [ ] Sleep/resume: no erratic jumps, correct local time.
- [ ] Restart Windows Explorer: tray icon and controls return.
- [ ] Enable Launch at Sign-in and sign out/in; one instance starts. Disable and verify no later automatic launch.
- [ ] Start a second instance: existing clock remains and no second tray icon appears.
- [ ] Exit from tray: window/icon disappear and hotkey is released.
