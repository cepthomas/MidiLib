
# MidiLib

This library contains a bunch of components and controls accumulated over the years. It supports:
- Reading and playing midi files.
- Reading and playing the patterns in Yamaha style files.
- Remapping channel patches.
- Various export functions including specific style patterns.
- Midi input handler.

## Notes
- Since midi files and NAudio use 1-based channel numbers, so does this application, except when used as an array index.
- Because the windows multimedia timer has inadequate accuracy for midi notes, resolution is limited to 32nd notes.
- If midi file type is `1`, all tracks are combined. Because.
- `NoteEvent` is used to represent Note Off and Key After Touch messages. It is also the base class for `NoteOnEvent`.
- Tons of styles and technical info at https://psrtutorial.com/.
- Midi devices are limited to the ones available on your box. (Hint- VirtualMidiSynth).
- Some midi files are sloppy with channel numbers so there are a couple of options for simple remapping.

# Components

## Core

MidiPlayer
- The top level component for sending midi data.
- Translates from MidiData to the wire.
- Writes native midi events to the output device every Tick(). Host is responsible for timing/frequency.

MidiListener
- A simple midi input component.
- You supply the handler.

Channel
- Represents a physical channel in a way usable by PlayerControl UI and MidiPlayer.

ChannelCollection
- Container for all the channels.
- Provides info about the collection e.g. maximum length.
- API is by channel number.
- Supports enumeration.

MidiData and PatternInfo
- Processes and contains a massaged version of the midi/style file contents.
- Translates from raw file to MidiData internal representation.
- Units are in subdivs - essentially midi ticks.
- Lots of utility and export functions too.

## UI

PlayerControl
- Bound to a Channel object.
- Provides volume, mute, solo.
- Patch selection.

BarBar
- Shows progress in musical bars and beats.
- User can select time.

VirtualKeyboard
- Piano control based loosely on Leslie Sanford's [Midi Toolkit](https://github.com/tebjan/Sanford.Multimedia.Midi).

## Other

- MidiDefs: The GM definitions.
- MidiTime: Used for mapping between data sets using different resolutions.
- MidiCommon: All the stuff.

# Example

The Test project contains a fairly complete demo application.

- MainForm Creates a Player. Opens a file using MidiData. Stars and runs the timer based on user tempo selection/change.
- Click on the settings icon to edit your options.
- Some midi files with single instruments are sloppy with channel numbers so there are a couple of options for simple remapping.
- In the log view: C for clear, W for word wrap toggle.
