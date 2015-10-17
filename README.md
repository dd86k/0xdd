## 0xdd
## Hexadecimal Console File Viewer

![Early screenshot](http://didi.wilomgfx.net/p/0xdd1.png)

(Early screenshot)

0xdd is a straightforward, nano-like, hexadecimal file viewer for Windows.

0xdd is still a work in progress, so only a lot of functions are still in the works.

Written in C# 6.0 on .NET 4.5 with Visual Studio 2015, compatible with Mono.

# Arguments
It's possible to give direct commands to 0xdd.

The order of the arguments matter, the last argument must be the file to open:

`0xdd [-v {h|d|o}] [-U] [-dump] <file>`

`-v` : Starts with the offset view with either Hexadecimal, Decimal, or Octal. Example: `-v d`

`-U` : Update. Please note that this feature is not marked as usable.
   
`-dump` : Dump specified `<file>` in plain text and exit.

Examples:
`0xdd -v d -dump NOTEPAD.EXE` - Dump the data from NOTEPAD.EXE to NOTEPAD.EXE.datdmp with the decimal offset view.
 
# Navigation
In 0xdd, navigation happens when a user changes the position to read of the file with a variety of keys:

- Left and right arrowkeys: Moves by one byte.
- Up and down arrowkeys: Moves by a line.
- PageUp and PageDown: Moves by a page.
- Home key: Moves the position to the start of the file.
- End key: Moves the position to the end of the file.

# Actions
Actions, defined at the bottom, are activated by shortcuts.

__^__ is a short way of saying __CTRL+__, per example, `^P` is `CTRL+P`.

## Dump
It is possible to dump a hexadecimal view of the file with the Dump action. (^D)

0xdd will simply add the "datdmp" extention to the current filename being worked on.

# FAQ
### Nothing here yet!

# Notes
- This README file is updated as 0xdd gets the features.
- The reason the Find action is now ^W instead of ^F is due to Windows 10 overriding (system wise) the CTRL+F shortcut which brings a Find dialog box.

# Development Notes
### Nothing here yet!