# MidiLib

TODOX fix all this doc:

WinForms controls collected over the years. Companion to [NBagOfTricks](https://github.com/cepthomas/NBagOfTricks/blob/main/README.md).

Requires VS2019 and .NET5.

No dependencies on third party components.

Probably I should make this into a nuget package at some point.

![logo](felixui.png)

# Contents

## Controls for audio (or other) apps
- BarBar: Similar to TimeBar but shows musical bars and beats.
- VirtualKeyboard: Piano control based loosely on Leslie Sanford's [Midi Toolkit](https://github.com/tebjan/Sanford.Multimedia.Midi).

See [MidiLib Doc](MidiLibDoc.md).


See [Midi Definitions](MidiDefinitions.md).


Uses:
- [NBagOfTricks](https://github.com/cepthomas/NBagOfTricks/blob/main/README.md)
- [NBagOfUis](https://github.com/cepthomas/NBagOfUis/blob/main/README.md)


# Documentation

- [Main Documentation](DocFiles/MidiLib.md)
- [Midi Definitions](DocFiles/MidiDefinitions.md)


# Third Party

This application uses these FOSS components:
- [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).


# License

https://github.com/cepthomas/MidiLib/blob/master/LICENSE


=====================================================

## General purpose UI components
- PropertyGridEx: Added a few features for custom buttons and labels.
- Settings: Base class for custom settings with persistence and editing.
- FilTree: Folder/file tree control with tags/filters and notifications.
- OptionsEditor: User can select from a list of strings, or add/delete elements.
- ClickGrid: Essentially a grid array of buttons.
- TimeBar: Elapsed time control.
- CpuMeter: Standalone display control.
- TextViewer: With colorizing.
- WaitCursor: Easy to use cursor.

## Various utilities and extensions
- KeyUtils: Keyboard input.
- UiUtils: Control helpers, formatters, etc.
