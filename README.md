# MidiLib


TODOX fix all this doc

```
New:
|   Channel.* - just like MidiStyleExplorer
|   ChannelControl.* - just like MidiStyleExplorer
|   MidiData.cs - new "file"
|   MidiDefs.cs - ok
|   MidiTime.cs - ok, from nbui
|   PatternInfo.cs - just like MidiStyleExplorer
|   Player.cs - new midi player
+---Test
|   |   MainForm.* - kinda like MidiStyleExplorer
|   |   - tests from old projects?
```

# Diagrams?

```mermaid
  graph TD;
      A-->B;
      A-->C;
      B-->D;
      C-->D;
```

# Design
- discuss apis for Player, ControlCollection,...
- `NoteEvent` is used to represent Note Off and Key After Touch messages. It is also the base class for `NoteOnEvent`.


## Data is MidiData and PatternInfo.
- Processes and contains a massaged version of the midi/style file contents.
- Translates from raw file to MidiData internal representation.
- Lots of utility functions too.


## Runtime/headless is Player and Channel.
- Channel represents a physical channel - in a way usable by UI and Player.
- Channel[] owned by Player. Player api is by channel number.
- Translates from MidiData to the wire.
- Units are in subdivs - essentially midi ticks.
- Writes native midi events to the output device every Tick(). Host is responsible for timing/frequency.
- ChannelCollection - uses api for host, back end uses Channel objects.


## UI is MainForm (a typical example) and ChannelControl.
- MainForm Creates a Player. Opens a file using MidiData. Stars and runs the timer based on user tempo selection/change.
ChannelControl owns ref to Channel. Given by MainForm.

## Test
- A windows tool for opening, playing, and manipulating midi and Yamaha style files.
- Opens style files and plays the individual sections.
- Export style files as their component parts.
- Export current selection(s) to a new midi file. Useful for snipping style patterns.
- Click on the settings icon to edit your options.
- Some midi files with single instruments are sloppy with channel numbers so there are a couple of options for simple remapping.
- In the log view: C for clear, W for word wrap toggle.

# Notes
- Since midi files and NAudio use 1-based channel numbers, so does this application, except when used as an array index.
- Because the windows multimedia timer has inadequate accuracy for midi notes, resolution is limited to 32nd notes.
- If midi file type is `1`, all tracks are combined. Because.
- Tons of styles and info at https://psrtutorial.com/.

# More

To that end, and because the windows multimedia timer has inadequate accuracy for midi notes, resolution is 
limited to 32nd notes.
Likewise, minimal attention has been paid to aesthetics over functionality. This explains the poor color choices.
Audio and midi play devices are limited to the ones available on your box. (Hint- VirtualMidiSynth).

There are some limitations: Windows multimedia timer has 1 msec resolution at best. This causes a trade-off between
ppq resolution and accuracy. The timer is also inherently wobbly.

Requires VS2019 and .NET5.

Probably I should make this into a nuget package at some point.

Exports use the user assigned drum channel!

Channel:
        /// <summary>Actual 1-based midi channel number.</summary>
        public int ChannelNumber { get; set; } = -1;

        /// <summary>All the channels. Index is 0-based, not channel number.</summary>
        readonly Channel[] _channels = new Channel[MidiDefs.NUM_CHANNELS];

        /// <summary>All the channel patches. Index is 0-based, not channel number.</summary>
        public int[] Patches { get; set; } = new int[MidiDefs.NUM_CHANNELS];  >>>>>>>>>> int

See [Midi Definitions](MidiDefinitions.md).


# Style notes - edit or locate

Each of the other markers (Intro A, Main B, etc) defines musical patterns that are triggered by
the keying chords. Intros play only once when triggered and then turn control over to the next
section selected by the panel buttons. Main sections (A, B, C, and D) repeat until the style is
stopped or an Ending or an Intro is selected. Ending sections play once and the style is
stopped. Fill Ins are triggered manually, or play automatically (if Auto Fill is On) when a new
main section is selected.

 Get events.strictChecking: If true will error on non-paired note events

The common order of the sections in the file is at follows:
1. Midi section
2. CASM section
3. OTS (One Touch Setting) section
4. MDB (Music Finder) section
5. MH section

In very rare cases there is a MH section at the end of the style file. Nothing is known about the purpose of this section.

Internally, a style starts by specifying the tempo, the time signature and the copyright followed by several sections that are defined by marker events.

The first two sections, SFF1 (or SFF2) and SInt, occupying the first measure of the midi part, include a Midi On plus midi commands to setup the default instruments and the amount of DSP (only DSP1 as a system effect is available for styles) used for each track.

Each of the other markers (Intro A, Main B, etc) defines musical patterns that are triggered by the keying chords. Intros play only once when triggered and then turn control over to the next section selected by the panel buttons. Main sections (A, B, C, and D) repeat until the style is stopped or an Ending or an Intro is selected. Ending sections play once and the style is stopped. Fill Ins are triggered manually, or play automatically (if Auto Fill is On) when a new main section is selected.

When a style is playing in the instrument, the SFF and SInt sections are executed when a style section is changed. This resets the voices and other channel parameters to their initial values. Because of this, if its is desired to change the voice or other settings for a single section, new settings can be inserted in only this section and the style will revert to the default whenever another section is selected. 

The first two sections, SFF1 (or SFF2) and SInt, occupying the first measure of the midi part,
include a Midi On plus midi commands to setup the default instruments and the amount of
DSP (only DSP1 as a system effect is available for styles) used for each track.

Each of the other markers (Intro A, Main B, etc) defines musical patterns that are triggered by
the keying chords. Intros play only once when triggered and then turn control over to the next
section selected by the panel buttons. Main sections (A, B, C, and D) repeat until the style is
stopped or an Ending or an Intro is selected. Ending sections play once and the style is
stopped. Fill Ins are triggered manually, or play automatically (if Auto Fill is On) when a new
main section is selected.

When a style is playing in the instrument, the SFF and SInt sections are executed when a
style section is changed. This resets the voices and other channel parameters to their initial
values. Because of this, if it is desired to change the voice or other settings for a single
section, new settings can be inserted in only this section and the style will revert to the
default whenever another section is selected.

Fill Ins are limited to one measure in length; other sections can be any length up to 255
measures, but are typically 2-8 measures. 

An extended style file consists of one or more different sections of the following types:
- MIDI section (mandatory)
- CASM section (optional)
- OTS (One Touch Setting) section (optional)
- MDB (Music Finder) section (optional)
- MH section (optional) (very rarely used)

The midi section is the only mandatory section. It contains the musical sequences of the style.
An optional CASM section contains extended information for the keyboard how to interpret
and control playing of the style section. While its inclusion is optional, very likely the style’s
author used it to convey important information and the style will not reproduce properly if
removed. The OTS (One Touch Setting) section contains information for the four settings
selectable from the keyboard. These can be used to easily setup the keyboard before using
the style. The MDB (Music Finder) section contains information for what songs this particular
style is appropriate. This information is automatically added to the Music Finder function, if the
keyboard supports it. In very rare cases there is a MH section at the end of the style file.
Nothing is known about the purpose of this section. 

Only one section of each type may be present in a style file. 


# Third Party

This application uses these FOSS components:
- [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).


# License

https://github.com/cepthomas/MidiLib/blob/master/LICENSE
