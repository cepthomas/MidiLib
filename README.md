# MidiLib

This library contains a bunch of components and controls accumulated over the years. It supports:
- Reading and playing midi files.
- Reading and playing the patterns in Yamaha style files.
- Remapping channel patches.
- Various export functions including specific style patterns.
- Midi input handler.

Requires VS2022 and .NET6.

![logo](felixui.png)


## Notes
- Since midi files and NAudio use 1-based channel numbers, so does this application, except when used internally as an array index.
- Time is represented by `bar.beat.subdiv ` but 0-based, unlike typical music representation.
- Because the windows multimedia timer has inadequate accuracy for midi notes, resolution is limited to 32nd notes.
- If midi file type is `1`, all tracks are combined. Because.
- NAudio `NoteEvent` is used to represent Note Off and Key After Touch messages. It is also the base class for `NoteOnEvent`. Not sure why it was done that way.
- Tons of styles and technical info at https://psrtutorial.com/.
- Midi devices are limited to the ones available on your box. (Hint - try VirtualMidiSynth).
- Some midi files use different drum channel numbers so there are a couple of options for simple remapping.

# Components

## Core

MidiOutput
- The top level component for sending midi data.
- Translates from MidiData to the wire.

MidiInput
- A simple midi input component.
- You supply the handler.

MidiOsc
- Implementation of midi over [OSC](https://opensoundcontrol.stanford.edu).

Channel
- Represents a physical output channel in a way usable by ChannelControl UI and MidiOutput.

MidiDataFile, PatternInfo, MidiExport
- Processes and contains a massaged version of the midi/style file contents.
- Translates from raw file to MidiData internal representation.
- Units are in subdivs - essentially midi ticks.
- Lots of utility and export functions too.

## UI

ChannelControl
- Bound to a Channel object.
- Provides volume, mute, solo.
- Patch selection.

SimpleChannelControl
- Simple/dumb UI control.
- Provides volume, channel, patch selection.

BarBar, BarTime
- Shows progress in musical bars and beats.
- User can select time.

PatchPicker
- Select from the standard GM list.

DevicesEditor
- Used for selecting inputs and outputs in settings editing.

VirtualKeyboard
- Piano control based loosely on Leslie Sanford's [Midi Toolkit](https://github.com/tebjan/Sanford.Multimedia.Midi).

BingBong
- Experimental UI component.

## Other

- MidiDefs: The GM definitions plus conversion functions.
- MidiTimeConverter: Used for mapping between data sets using different resolutions.
- MidiSettings container/editor for use by clients.
- MidiCommon: All the other stuff.

# Example

The Test project contains a fairly complete demo application.

- MainForm Creates a Player. Opens a file using MidiData. Stars and runs the timer based on user tempo selection/change.
- Click on the settings icon to edit your options.
- Some midi files with single instruments are sloppy with channel numbers so there are a couple of options for simple remapping.
- In the log view: C for clear, W for word wrap toggle.


This application uses these FOSS components:
- [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).
- [NBagOfTricks](https://github.com/cepthomas/NBagOfTricks/blob/main/README.md)
- [NBagOfUis](https://github.com/cepthomas/NBagOfUis/blob/main/README.md)
- [NebOsc](https://github.com/cepthomas/NebOsc/blob/main/README.md)

