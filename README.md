# MidiLibLite TODO1 update doc
Lighter version of MidiLib. Mainly the low level stuff.



No logging, up to host. except...
Errors throw MidiLibException,...

define "sub" => sub-beat + others

gen md and lua files



other devices:
 - OSCin uses oscin:port for DeviceName
 - OSCout uses oscout:host:port for DeviceName
 - NULL uses nullout:name for DeviceName


# ------------ from original ---------------

This library contains a bunch of components and controls accumulated over the years. It supports:
- Midi input handler.
- Requires VS2022 and .NET6.


## Notes
- Since midi files and NAudio use 1-based channel numbers, so does this application, except when used internally as an array index.
- Time is represented by `bar.beat.tick ` but 0-based, unlike typical music representation.
- Because the windows multimedia timer has inadequate accuracy for midi notes, resolution is limited to 32nd notes.
- NAudio `NoteEvent` is used to represent Note Off and Key After Touch messages. It is also the base class for `NoteOnEvent`. Not sure why it was done that way.
- Midi devices are limited to the ones available on your box. (Hint - try VirtualMidiSynth).

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

## UI

ChannelControl
- Bound to a Channel object.
- Provides volume, mute, solo.
- Patch selection.

TimeBar, MusicTime
- Shows progress in musical bars and beats.
- User can select time.

## Other

- MidiDefs: The GM definitions plus conversion functions.
- ???: All the other stuff.


# Example

The Test project contains a fairly complete demo application.

See also >>> Nebulua etc....

[Midifrier](https://github.com/cepthomas/Midifrier) also uses this extensively.

# External Components

- [NAudio](https://github.com/naudio/NAudio) (MIT).

