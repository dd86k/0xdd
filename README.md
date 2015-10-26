## 0xdd
### Hexadecimal Console File Viewer

![0xdd](http://didi.wilomgfx.net/p/0xdd2.png)

Inspired by nano, 0xdd is a straightforward, simple, interactive, hexadecimal file viewer for Windows.

0xdd is still a work in progress, so only a lot of functions are still in the works.

Written in C# 6.0 for .NET 4.5 with Visual Studio 2015.

Compatible with Mono, and probably compatible with Xamarin Studio and MonoDevelop as well.

I do not guarantee Mono compability.

The Wiki is available [here](https://github.com/guitarxhero/0xDD/wiki). For now, it's restricted to collaborators.

# Requirements

- .NET 4.5 (Windows) or Mono (OS X and GNU/Linux).
- ISO/ANSI Screen size, which is 80x24, with a Command Prompt or Terminal.
- A working computer.

# FAQ
___Nothing here yet!___

# Notes

Keys stolen by Windows (10) (Unuseable by 0xdd):
- F11 : Fullscreen.
- CTRL+F : Search DialogBox.
- CTRL+HOME and CTRL+END : No idea, assuming to scroll to the end of cmd.
- CTRL+A : Selects all output.
- CTRL+C and CTRL+V : Copy and paste.

# Development Notes
- Messages are at the same TopPosition as the InfoPanel, so thus why the InfoPanel is showing in fullscreen mode.
- Even if the `using static` feature is wonderful, I try to limit its use because I don't want to be confused with other methods and classes.
- Parameters are expressed with a slashes (e.g. /w 16) in documents.
- Eventually every panel will have their own structs.
- Eventually optimization will occur.

- When doing commits, I try to stick with the following legend for descriptions:
  - `+` : Addition
  - `*` : Fix
  - `~` : Notes
  - `-` : Removal