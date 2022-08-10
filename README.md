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

# Style Files

There's tons of styles and technical info at https://psrtutorial.com/. An overview taken from `StyleFileDescription_v21.pdf`:

> A style is a special form of a type 0 midi file followed by several information sections. To function, it must be loaded into the PSR.
> This process reads the file and establishes some of the instrument settings based upon commands in the midi and information sections.
> When the accompaniment is started (via synch start, the Start button or an external midi command) the portions of the midi section are
> played in response to the state of the front panel style control buttons.
> 
> Internally, a style starts by specifying the tempo, the time signature and the copyright followed by several sections that are defined
> by marker events.
> 
> The first two sections, SFF1 (or SFF2) and SInt, occupying the first measure of the midi part, include a Midi On plus midi commands to
> setup the default instruments and the amount of DSP (only DSP1 as a system effect is available for styles) used for each track.
> 
> Each of the other markers (Intro A, Main B, etc) defines musical patterns that are triggered by the keying chords. Intros play only once
> when triggered and then turn control over to the next section selected by the panel buttons. Main sections(A, B, C, and D) repeat until
> the style is stopped or an Ending or an Intro is selected. Ending sections play once and the style is stopped. Fill Ins are triggered
> manually, or play automatically (if Auto Fill is On) when a new main section is selected.
> 
> When a style is playing in the instrument, the SFF and SInt sections are executed when a style section is changed. This resets the
> voices and other channel parameters to their initial values. Because of this, if it is desired to change the voice or other settings
> for a single section, new settings can be inserted in only this section and the style will revert to the default whenever another
> section is selected.
> 
> Fill Ins are limited to one measure in length; other sections can be any length up to 255 measures, but are typically 2-8 measures.


# Example

The Test project contains a fairly complete demo application.

- MainForm Creates a Player. Opens a file using MidiData. Stars and runs the timer based on user tempo selection/change.
- Click on the settings icon to edit your options.
- Some midi files with single instruments are sloppy with channel numbers so there are a couple of options for simple remapping.
- In the log view: C for clear, W for word wrap toggle.

# External Components

- [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).

