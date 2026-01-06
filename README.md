# MidiLib

This library contains a bunch of midi components and controls accumulated over the years.

Requires VS2022 and .NET8.

## Notes
- Since midi files and NAudio use 1-based channel numbers, so does this application, except when used internally as an array index.
- Time is represented by `bar.beat.tick ` but 0-based, unlike typical music representation.
- Because the windows multimedia timer has inadequate accuracy for midi notes, resolution is limited to 32nd notes.
- NAudio `NoteEvent` is used to represent Note Off and Key After Touch messages. It is also the base class for `NoteOnEvent`. Not sure why it was done that way.
- Midi devices are limited to the ones available on your box. (Hint - try VirtualMidiSynth).

# Components

## Core

- Manager
    - Manages the nuts and bolts of midi interfaces. Primary talker to NAudio.

- MidiDefs
    - Everything associated with GM midi definitions plus conversion functions.
    - Generate markdown and lua files.

- BaseEvent and derived
    - Higher level event specifiers for use by apps.

- MidiOutputDevice/MidiInputDevice
    - The top level components for sending and receiving midi data.
    - Translates from BaseMidi+ to the wire.
    - You supply the handlers.

- OscOutputDevice/OscInputDevice
    - Implementation of midi over [OSC](https://opensoundcontrol.stanford.edu).

- NullOutputDevice/NullInputDevice
    - Dummy devices.
    - Can be used for dev and debug.

- OutputChannel/InputChannel
    - Represents physical channels in a way usable by ChannelControl UI and MidiOutput.

- MidiTimeConverter
    - Used for mapping between data sets using different resolutions.

## UI

- ChannelControl
    - Bound to a OutputChannel object.
    - Provides volume, mute, solo.

- TimeBar, MusicTime
    - Shows progress in musical bars and beats.
    - User can select time.


# Example

The Test project contains a fairly complete demo application.

[Nebulua](https://github.com/cepthomas/Nebulua) also uses this extensively.

# External Components

- [NAudio](https://github.com/naudio/NAudio) (MIT).
