## 0xdd
### Hexadecimal Console File Viewer

![0xdd](http://didi.wilomgfx.net/p/0xdd2.png)

Inspired by nano, 0xdd is a straightforward, simple, interactive, hexadecimal file viewer for Windows.

Written in C# 6.0 for .NET 4.5 with Visual Studio 2015.

Compatible with Mono, and probably compatible with Xamarin Studio and MonoDevelop as well.

I do not guarantee Mono compability.

The Wiki is available [here](https://github.com/guitarxhero/0xDD/wiki).

# Requirements

- .NET 4.5 (Windows) or Mono (OS X and GNU/Linux).

# FAQ
___Nothing here yet!___

# Notes

Keys stolen by Windows 10 (Unuseable by 0xdd):
- F11 : Fullscreen.
- CTRL+F : Search DialogBox.
- CTRL+HOME and CTRL+END : No idea, assuming to scroll to the end of cmd.
- CTRL+A : Selects all output.
- CTRL+C and CTRL+V : Copy and paste.

# Development Notes
- Messages are at the same TopPosition as the InfoPanel, so thus why the InfoPanel is showing in fullscreen mode.
- Even if the `using static` feature is wonderful, I try to limit its use because I don't want to be confused with other methods and classes.
- Parameters are expressed with a slashes (e.g. /w 16) in documents.

| Panel | Description |
| -- | -- |
| MainPanel | Main panel with the offsets base, bytes and data as ASCII. |
| InfoPanel | Where messages and the current positions are shown. |
| OffsetPanel | Current offset base, and byte offsets. |
| ControlPanel | Actions and shortcuts. |

- Eventually optimization will occur.

- When doing commits, I try to stick with the following legend for descriptions:
  - `+` : Addition
  - `*` : Fix
  - `~` : Notes
  - `-` : Removal