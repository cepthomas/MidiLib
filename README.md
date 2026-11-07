# MidiLib

This library contains a bunch of midi components and controls accumulated over the years.

The Test project contains a fairly complete demo application.

Requires VS2026 and .NET10.


# Components

## Core

- Channel
    - Represents physical channels in a way usable by `ChannelControl` and output devices.
    - Intelligent handling of patch names.
    - Primary talker to NAudio.

- ChannelControl
    - UI aspects of a midi channel.
    - Primary talker to user.

- ChannelManager
    - Coordinates Channel/ChannelControl/MidiOutputDevice lifetimes and interactions.

- InputManager
    - Coordinates MidiInputDevice lifetimes and interactions.

- MidiOutputDevice/MidiInputDevice
    - The top level components for sending and receiving midi data.
    - Translates from BaseEvent+ to the wire.
    - Client supplies the handlers.

- MidiDefs
    - Everything associated with GM midi definitions plus conversion functions.
    - Generate markdown and lua files.

- MusicTime
    - Time is represented by `bar.beat.subbeat` but 0-based, unlike typical music representation.
    - Resolution is limited to 32nd notes due to accuracy of the windows multimedia timer.

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

- UserRenderer
    - Used to add UI or graphics to a customized `ChannelControl`.

- TimeBar
    - Shows progress in `MusicTime`.
    - User can select time ranges and looping.


# External Components

- [NAudio](https://github.com/naudio/NAudio) (MIT).
