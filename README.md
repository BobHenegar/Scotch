# Scotch

A small Windows clock that stays above normal desktop windows and moves out of the pointer's way.

Scotch starts near the upper-right corner of the primary monitor. It uses Windows app light/dark colors and the system accent, updates when the theme changes, and uses opaque system colors in contrast themes. Time follows your Windows regional format.

## Use

Download **Scotch-win-x64** from the latest successful [Build Scotch workflow](https://github.com/BobHenegar/scotch/actions/workflows/build.yml), extract the entire ZIP to a permanent folder, and run **Scotch.exe**. The portable build includes the .NET runtime. It is unsigned; Windows may identify it as an unrecognized application.

- Move the mouse near the clock: it slides away quickly.
- Move clear of its path home: it waits one second, then returns over about two seconds.
- Press **Ctrl+Alt+C** to freeze or resume avoidance. When paused, drag the clock to choose a new home.
- Right-click the notification-area clock icon for avoidance, date display, reset position, launch at sign-in, and Exit. When paused, the clock also has a right-click menu.
- An outlined status dot means avoidance is paused. Hovering the clock shows its status and hotkey.

If another app owns Ctrl+Alt+C, Scotch explains the conflict; the tray toggle still works. Release the shortcut in the other app and restart Scotch to register it.

Launch at sign-in is optional and initially off. Choose a permanent folder before enabling it; moving the application afterward requires disabling and re-enabling the option. Exit Scotch before replacing its portable files.

Settings live in `%LOCALAPPDATA%\Scotch\settings.json`. To remove Scotch, disable Launch at Sign-in, exit, and delete the portable folder. Delete its settings folder to remove saved preferences. No administrator access, account, network access, or telemetry is used.

Topmost behavior applies to the ordinary Windows desktop; secure desktop prompts and exclusive fullscreen apps are outside its scope. One instance runs per user session. If a saved monitor is missing, Scotch returns to the primary monitor.

## Build from source

Use Windows with the .NET 10 SDK:

```powershell
dotnet restore Scotch.sln
dotnet test Scotch.sln -c Release
dotnet build Scotch.sln -c Release
dotnet publish src/Scotch/Scotch.csproj -c Release -r win-x64 --self-contained true -o artifacts/Scotch
```

Run `artifacts\Scotch\Scotch.exe`. Core tests can run on other operating systems with the .NET 10 SDK. See [manual desktop checks](docs/manual-validation.md) for behavior requiring a real Windows desktop.
