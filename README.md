## 0xdd
### Hexadecimal Console File Viewer

![Old screenshot](http://didi.wilomgfx.net/p/0xdd1.png)

(Old screenshot)

Inspired by nano, 0xdd is a straightforward, simple, interactive, hexadecimal file viewer for Windows.

0xdd is still a work in progress, so only a lot of functions are still in the works.

Written in C# 6.0 for .NET 4.5 with Visual Studio 2015.

Compatible with Mono, and probably Xamarin Studio and MonoDevelop as well.

# Requirements

- .NET 4.5 (Windows) or Mono (OS X and GNU/Linux).
- ISO/ANSI Screen size, which is 80x24, with a Command Prompt or Terminal.
- A working computer.

# Arguments
It's possible to give direct commands to 0xdd. Note that slashes (/) can be replaced by dashes (-) for parameters. Both can work at the same time.

The order of the arguments may matter, but the last argument must be the file to open:

`0xdd [/v {h|d|o}] [/w <Number>] [/U] [/dump] <file>`

`/v` : Define the offset view as Hexadecimal, Decimal, or Octal. Example: `/v d`

`/w` : Define the number of bytes to show in a single row.
Example: `/w 8`

`/U` : Update 0xdd. __Not recommended for now.__
   
`/dump` : Dump specified `<file>` in plain text and exit.

Examples:

- `0xdd /v d /dump NOTEPAD.EXE` - Dump the data from NOTEPAD.EXE to NOTEPAD.EXE.datdmp with the decimal offset view.
 
# Navigation
In 0xdd, navigation happens when a user changes the position to read of the file with a variety of keys:

Move by...
- One byte: __Left__ and __right__ arrow keys.
- One line: __Up__ and __down__ arrow keys.
- One page: __PageUp__ and __PageDown__ keys.

Move to...
- The beginning of the file: __Home__ key.
- The beginning of a line: To be decided.
- The end of the file: __End__ key.
- A specific position: __CTRL+G__ keys.

# Actions
Actions, defined at the bottom, are activated by shortcuts.

__^__ is a short way of saying __CTRL+__, so per example, `^P` is `CTRL+P`.

## Dump
It is possible to dump a hexadecimal view of the file with the Dump action. (^D)

0xdd will simply add the "datdmp" extention (for Data dump) to the current filename being worked on.

# FAQ
___Nothing here yet!___

# Notes


Keys stolen by Windows (At least in 10) (and unuseable):
- F11 : Fullscreen
- CTRL+F : Search DialogBox
- CTRL+HOME and CTRL+END : No idea
- CTRL+A : Selects all output
- CTRL+C and CTRL+V : Copy and paste

# Development Notes
- Messages are at the same TopPosition as the InfoPanel, so thus why the InfoPanel is showing in fullscreen mode.
- Even if the `using static` feature is wonderful, I try to limit its use because I don't want to be confused with other methods and classes.
- Parameters are usually expressed with a slash (e.g. /w 16)
- Eventually every panel will have their own structs.
- Eventually optimization will occur.

- When doing commits, I try to stick with the following legend for descriptions:
  - `+` : Addition
  - `*` : Fix
  - `~` : Notes
  - `-` : Removal