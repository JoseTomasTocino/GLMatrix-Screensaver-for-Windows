<p align="center">
  <img src="Assets/project-icon.png" width="220" alt="GLMatrix Screensaver for Windows icon">
</p>

<h1 align="center">GLMatrix Screensaver for Windows</h1>

<p align="center">
  A native, GPU-accelerated Windows port of the classic XScreenSaver GLMatrix screensaver.
</p>

GLMatrix Screensaver for Windows preserves the original's falling 3D glyph
strips, glyph feeders, erase phase, brightness waves, depth fog, and smooth
camera movement. It is packaged as a standard Windows `.scr` file and renders
through WGL/OpenGL, including multi-monitor support and an embedded preview in
the Windows Screen Saver Settings dialog.

## Features

- Hardware-accelerated OpenGL rendering
- Perspective, camera tilt, fog, waves, and additive glow
- Configurable speed, density, frame-rate limit, camera motion, and alphabet
- Full virtual-desktop rendering across multiple monitors
- Native `/s`, `/p`, and `/c` Windows screensaver modes
- No installer, runtime download, or external asset files required

## Install

1. Download [`dist/GLMatrix.scr`](dist/GLMatrix.scr).
2. Right-click the file and choose **Show more options > Install**.
3. Select **GLMatrix** in the Windows Screen Saver Settings dialog and choose
   the desired wait time.

If **Install** is not available, copy `GLMatrix.scr` to
`C:\Windows\System32` with administrator permissions, then select it from the
Screen Saver Settings dialog.

> [!NOTE]
> Windows starts the screensaver in preview mode while the Screen Saver
> Settings dialog is open. The preview process exits automatically when that
> dialog closes. A full-screen run exits on mouse movement or keyboard input.

## Configure

Choose **Settings** in the Windows Screen Saver Settings dialog, or run:

```powershell
.\dist\GLMatrix.scr /c
```

Settings are stored for the current user in
`HKCU\Software\MatrixSCR`.

Windows screensaver command-line modes:

- `/s` — run full-screen on the complete virtual desktop.
- `/p <HWND>` — render an embedded preview in the supplied window.
- `/c` — open the configuration window.

## Build from source

The project requires no Visual Studio installation or third-party packages. In
PowerShell, run:

```powershell
.\build.ps1
```

The script uses the .NET Framework C# compiler included with Windows and
creates `dist\GLMatrix.scr`. The original glyph atlas and the project icon are
embedded in the binary.

## How it renders

The screensaver creates a native WGL context for each display surface. Glyph
geometry is submitted in batches, while perspective projection, rotation,
texturing, depth fog, and additive blending run on the GPU. The frame-rate
limit prevents needless rendering above the selected rate; it does not change
the refresh rate of either monitor.

## Origin and license

The rendering algorithm was ported from XScreenSaver's
[`hacks/glx/glmatrix.c`](https://github.com/xscreensaver/xscreensaver/blob/master/hacks/glx/glmatrix.c),
originally written by Jamie Zawinski. `Assets/matrix3.png` comes from the same
project. The original code, atlas, and this Windows adaptation are distributed
under the MIT/X11 terms reproduced in [`LICENSE.txt`](LICENSE.txt).

The project icon was created specifically for this port and is included in the
repository under the same license.
