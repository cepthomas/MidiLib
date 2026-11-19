# MidiLib

This library contains a bunch of midi components and controls accumulated over the years.

The Test project contains a fairly complete demo application.

Requires VS2026 and .NET10.


# Components

## Core

- MidiOutput
    - Represents midi outputs and devices in a way usable by `ChannelControl`.
    - Intelligent handling of patch names.
    - Primary talker to NAudio.

- MidiInput
    - Represents midi inputs and devices.

- MidiManager
    - Coordinates all component lifetimes and interactions.
    - Hides all the messy logic behind API.

- MidiOutputDevice/MidiInputDevice
    - The top level components for sending and receiving midi data.
    - Translates from BaseEvent+ to the wire.
    - Client supplies the handlers.

- MidiDefs
    - Everything associated with GM midi definitions plus conversion functions.
    - Generate markdown and lua files.

- MusicTime
    - Time is represented by `bar.beat.subbeat` but 0-based, unlike typical music representation.
    - Resolution is limited to 32nd notes.

- BaseEvent and derived
    - Higher level event classes for use by client apps.

- OscOutputDevice/OscInputDevice
    - Implementation of midi over [OSC](https://opensoundcontrol.stanford.edu).

- NullOutputDevice/NullInputDevice
    - Dummy devices for dev and debug.

## UI

- ChannelControl
    - Bound to an `OutputChannel` object.
    - Provides volume, mute, solo.

- TimeBar
    - Shows progress in `MusicTime`.
    - User can select time ranges and looping.

- UiEditors
    - Various stuff to make UI life better.

# External Components

- [NAudio](https://github.com/naudio/NAudio) (MIT).
