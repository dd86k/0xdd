## 0xdd
### Hexadecimal Console File Viewer

![Old screenshot](http://didi.wilomgfx.net/p/0xdd1.png)

(Old screenshot)

Inspired by nano, 0xdd is a straightforward, simple, interactive, hexadecimal file viewer for Windows.

0xdd is still a work in progress, so only a lot of functions are still in the works.

Written in C# 6.0 on .NET 4.5 with Visual Studio 2015

Compatible with Mono.

# Requirements

- .NET 4.5 (Windows) or Mono (OS X and GNU/Linux).
- ISO/ANSI Screen size (80x24) cmd (Command Prompt) or Terminal.
- A working computer.

# Arguments
It's possible to give direct commands to 0xdd. Note that slashes (/) can be replaced by dashes (-). Both can work at the same time.

The order of the arguments may matter, but the last argument must be the file to open:

`0xdd [/v {h|d|o}] [-w n] [/U] [/dump] <file>`

`/v` : Define the offset view as Hexadecimal, Decimal, or Octal. Example: `/v d`

`/w` : Define the number of bytes to show in a single row. Example: `/w 8`

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

__^__ is a short way of saying __CTRL+__, per example, `^P` is `CTRL+P`.

## Dump
It is possible to dump a hexadecimal view of the file with the Dump action. (^D)

0xdd will simply add the "datdmp" extention to the current filename being worked on.

# FAQ
___Nothing here yet!___

# Notes
- This README file is updated as 0xdd gets the features.
- The reason the Find action is now ^W instead of ^F is due to Windows 10 overriding (system wise) the CTRL+F shortcut which brings a Find dialog box.
- CTRL+Home and CTRL+End don't seem to work on cmd.

# Development Notes
___Nothing here yet!___