# An unofficial Overcooked keyboard controls patch

I wanted to play Overcooked 1, but the default keyboard controls are bad.
The game uses ctrl and shift, but this makes Windows switch keyboard layout, which breaks the game for me.

This mod allows you to rebind the keyboard controls to whatever you want.

1. [Download OvercookecControlsPatcher_Merged.exe from latest release](https://github.com/rjfalconer/OvercookedControlsPatch/releases)
2. Copy to the Overcooked installation folder.
3. Run the patch.
4. If all went well, you now have `input_combined.txt` and `input_split.txt`. You can edit these files to change the keybindings for 1-keyboard-player and 2-keyboard-players respectively. Change only the parts after the equals sign (`=`).

<details>
  <summary>Valid key names</summary>
  
- Alt
- Command
- Control
- LeftShift
- LeftAlt
- LeftCommand
- LeftControl
- RightShift
- RightAlt
- RightCommand
- RightControl
- Escape
- F1
- F2
- F3
- F4
- F5
- F6
- F7
- F8
- F9
- F10
- F11
- F12
- Key0
- Key1
- Key2
- Key3
- Key4
- Key5
- Key6
- Key7
- Key8
- Key9
- A
- B
- C
- D
- E
- F
- G
- H
- I
- J
- K
- L
- M
- N
- O
- P
- Q
- R
- S
- T
- U
- V
- W
- X
- Y
- Z
- Backquote
- Minus
- Equals
- Backspace
- Tab
- LeftBracket
- RightBracket
- Backslash
- Semicolon
- Quote
- Return
- Comma
- Period
- Slash
- Space
- Insert
- Delete
- Home
- End
- PageUp
- PageDown
- LeftArrow
- RightArrow
- UpArrow
- DownArrow
- Pad0
- Pad1
- Pad2
- Pad3
- Pad4
- Pad5
- Pad6
- Pad7
- Pad8
- Pad9
- Numlock
- PadDivide
- PadMultiply
- PadMinus
- PadPlus
- PadEnter
- PadPeriod
- Clear
- PadEquals
- F13
- F14
- F15
- AltGr
- CapsLock
- ExclamationMark
- Tilde
- At
- Hash
- Dollar
- Percent
- Caret
- Ampersand
- Asterisk
- LeftParen
- RightParen
- Underscore
- Plus
- LeftBrace
- RightBrace
- Pipe
- Colon
- DoubleQuote
- LessThan
- GreaterThan
- QuestionMark
</details>

## Contributing
Any PRs welcome

## Build
1. Change `OvercookedPath` in `Directory.Build.props` to the directory containing your Overcooked install, e.g.:
* `C:\Program Files\Epic Games\Overcooked`
* `D:\Games\Steam\steamapps\common\Overcooked`
2. Build solution

## Run
Copy `OvercookedControlsPatcher_Merged.exe` from `OvercookedControlsPatcher\bin\Release` to your Overcooked install directory, and run.
