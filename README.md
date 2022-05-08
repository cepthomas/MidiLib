# MidiLib


TODO1 fix all this doc

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

Requires VS2019 and .NET5.

Probably I should make this into a nuget package at some point.



Channel:
        /// <summary>Actual 1-based midi channel number.</summary>
        public int ChannelNumber { get; set; } = -1;

        /// <summary>All the channels. Index is 0-based, not channel number.</summary>
        readonly Channel[] _channels = new Channel[MidiDefs.NUM_CHANNELS];

        /// <summary>All the channel patches. Index is 0-based, not channel number.</summary>
        public PatchInfo[] Patches { get; set; } = new PatchInfo[MidiDefs.NUM_CHANNELS];


# Third Party

This application uses these FOSS components:
- [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).


# License

https://github.com/cepthomas/MidiLib/blob/master/LICENSE
