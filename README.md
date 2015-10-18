## 0xdd
### Hexadecimal Console File Viewer

![Old screenshot](http://didi.wilomgfx.net/p/0xdd1.png)

(Old screenshot)

0xdd is a straightforward, nano-like, interactive, hexadecimal file viewer for Windows.

0xdd is still a work in progress, so only a lot of functions are still in the works.

Written in C# 6.0 on .NET 4.5 with Visual Studio 2015, compatible with Mono.

# Arguments
It's possible to give direct commands to 0xdd.

The order of the arguments matter, the last argument must be the file to open:

`0xdd [-v {h|d|o}] [-U] [-dump] <file>`

`-v` : Starts with the offset view with either Hexadecimal, Decimal, or Octal. Example: `-v d`

`-U` : Update. Please note that this feature is marked as "Please do not use this for now".
   
`-dump` : Dump specified `<file>` in plain text and exit.

Examples:

- `0xdd -v d -dump NOTEPAD.EXE` - Dump the data from NOTEPAD.EXE to NOTEPAD.EXE.datdmp with the decimal offset view.
 
# Navigation
In 0xdd, navigation happens when a user changes the position to read of the file with a variety of keys:

Move by...
- One byte: __Up__ and __down__ arrow keys.
- One line: __Left__ and __right__ arrow keys.
- One page: __PageUp and __PageDown__ keys.

Move to...
- The beginning of the file: __CTRL+Home__ keys.
- The beginning of a line: __Home__ key.
- The end of the file: __CTRL+End__ keys.
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

# Development Notes
___Nothing here yet!___