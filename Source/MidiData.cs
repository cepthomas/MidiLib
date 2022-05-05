using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using NAudio.Midi;
using NBagOfTricks;


// TODO1 tracks? for (int track = 0; track < _mdata.Tracks; track++) events.GetTrackEvents(track))

// TODO2: auto-determine which channels have drums? adjust quiet drums?
// see 2 non-std drum channels in "C:\\Users\\cepth\\OneDrive\\Audio\\Midi\\styles\\Gary USB\\g-70 styles\\G-70 #1\\ContempBeat_G70.S423.STY",

// TODO2 where/how to manage time (not subdivs)?
// // /// <summary>Total length.</summary>
// public TimeSpan Length { get { return new TimeSpan(0, 0, 0, 0, (int)(_totalSubdivs * _msecPerSubdiv)); } }
// // /// <summary>Current time.</summary>
// public TimeSpan Current
// {
//    get { return new TimeSpan(0, 0, 0, 0, (int)(_currentSubdiv * _msecPerSubdiv)); }
//    set { _currentSubdiv = (int)(value.TotalMilliseconds / _msecPerSubdiv); _currentSubdiv = MathUtils.Constrain(_currentSubdiv, 0, _totalSubdivs); }
// }
//
// public string GetInfo()
// {
//     int bars = _totalSubdivs / BEATS_PER_BAR / PPQ;
//     int beats = _totalSubdivs / PPQ % BEATS_PER_BAR;
//     int subdivs = _totalSubdivs % PPQ;

//     int inc = _zeroBased ? 0 : 1;
//     string s = $"{_tempo} bpm {Length:mm\\:ss\\.fff} ({bars + inc}:{beats + inc}:{subdivs + inc:00})";
//     return s;
// }


namespace MidiLib
{
    /// <summary>Placeholder to avoid handling null everywhere.</summary>
    public class NullMidiEvent : MidiEvent
    {
    }

    /// <summary>
    /// Internal representation of one midi event.
    /// </summary>
    public class EventDesc
    {
        /// <summary>From whence this came. Empty for simple midi files.</summary>
        public string Pattern { get; set; } = "";
        
        /// <summary>One-based channel number.</summary>
        public int ChannelNumber { get; set; }

        /// <summary>Time from original file.</summary>
        public long AbsoluteTime { get; set; }

        /// <summary>Time scaled to internal units.</summary>
        public int ScaledTime { get; set; }

        /// <summary>The raw event.</summary>
        public MidiEvent MidiEvent { get; set; } = new NullMidiEvent();
    }

    /// <summary>
    /// Reads in and processes standard midi or yamaha style files.
    /// </summary>
    public class MidiData
    {
        #region Fields
        /// <summary>Include events like controller changes, pitch wheel, ...</summary>
        bool _includeNoisy = false;

        /// <summary>Save this for logging/debugging.</summary>
        long _lastStreamPos = 0;

        /// <summary>Default values if not supplied in pattern. Mainly for managing patches.</summary>
        readonly PatternInfo _patternDefaults = new();
        #endregion

        #region Properties gleaned from file
        /// <summary>What is it.</summary>
        public int MidiFileType { get; private set; } = 0;

        /// <summary>How many tracks.</summary>
        public int Tracks { get; private set; } = 0;

        /// <summary>Original resolution for all events.</summary>
        public int DeltaTicksPerQuarterNote { get; private set; } = 0;

        /// <summary>Stuff found while reading the file.</summary>
        public List<(string, string)> Messages { get; private set; } = new();
        #endregion

        #region Properties - patterns and events
        /// <summary>All file pattern sections. Plain midi files will have only one, unnamed.</summary>
        public List<PatternInfo> Patterns { get; private set; } = new();

        /// <summary>All the midi events. This is the verbatim content of the file.</summary>
        public List<EventDesc> AllEvents { get; private set; } = new();
        #endregion

        #region Public functions
        /// <summary>
        /// Read a file.
        /// </summary>
        /// <param name="fn"></param>
        /// <param name="defaultTempo">Specified by client.</param>
        /// <param name="includeNoisy"></param>
        public void Read(string fn, int defaultTempo, bool includeNoisy)
        {
            if(AllEvents.Count > 0)
            {
                throw new InvalidOperationException("You don't want to do this more than once");
            }

            _patternDefaults.Tempo = defaultTempo;
            _includeNoisy = includeNoisy;

            // Always at least one pattern - for plain midi.
            Patterns.Add(new PatternInfo() { Name = "" });

            using var br = new BinaryReader(File.OpenRead(fn));
            bool done = false;

            while (!done)
            {
                var sectionName = Encoding.UTF8.GetString(br.ReadBytes(4));

                //Debug.WriteLine($"{sectionName}:{_lastStreamPos}");

                switch (sectionName)
                {
                    case "MThd":
                        ReadMThd(br);
                        break;

                    case "MTrk":
                        ReadMTrk(br);
                        break;

                    case "CASM":
                        ReadCASM(br);
                        break;

                    case "CSEG":
                        ReadCSEG(br);
                        break;

                    case "Sdec":
                        ReadSdec(br);
                        break;

                    case "Ctab":
                        ReadCtab(br);
                        break;

                    case "Cntt":
                        ReadCntt(br);
                        break;

                    case "OTSc": // One Touch Setting section
                        ReadOTSc(br);
                        break;

                    case "FNRc": // MDB (Music Finder) section
                        ReadFNRc(br);
                        break;

                    default:
                        done = true;
                        break;
                }
            }

            // Last one.
            CleanUpPattern();
        }
        #endregion

        #region Section readers
        /// <summary>
        /// Read the midi header section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadMThd(BinaryReader br)
        {
            uint chunkSize = ReadStream(br, 4);

            if (chunkSize != 6)
            {
                throw new FormatException("Unexpected header chunk length");
            }

            // Midi file type.
            MidiFileType = (int)ReadStream(br, 2);

            // Number of tracks.
            Tracks = (int)ReadStream(br, 2);

            // Resolution.
            DeltaTicksPerQuarterNote = (int)ReadStream(br, 2);
        }

        /// <summary>
        /// Read a midi track chunk.
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        int ReadMTrk(BinaryReader br)
        {
            uint chunkSize = ReadStream(br, 4);
            long startPos = br.BaseStream.Position;
            int absoluteTime = 0;

            // Read all midi events.
            MidiEvent? me = null; // current
            while (br.BaseStream.Position < startPos + chunkSize)
            {
                _lastStreamPos = br.BaseStream.Position;

                me = MidiEvent.ReadNextEvent(br, me);
                absoluteTime += me.DeltaTime;
                me.AbsoluteTime = absoluteTime;

                switch (me)
                {
                    ///// Standard midi events /////
                    case NoteOnEvent evt:
                        AddMidiEvent(evt);
                        break;

                    case NoteEvent evt:
                        AddMidiEvent(evt);
                        break;

                    case ControlChangeEvent evt:
                        if (_includeNoisy)
                        {
                            AddMidiEvent(evt);
                        }
                        break;

                    case PitchWheelChangeEvent evt:
                        if (_includeNoisy)
                        {
                            AddMidiEvent(evt);
                        }
                        break;

                    case PatchChangeEvent evt:
                        var index = evt.Channel - 1;
                        PatchInfo patch = new() { Patch = evt.Patch, Modifier = PatchInfo.PatchModifier.None };
                        _patternDefaults.Patches[index] = patch;
                        Patterns.Last().Patches[index] = patch;
                        AddMidiEvent(evt);
                        break;

                    case SysexEvent evt:
                        if (_includeNoisy)
                        {
                            AddMidiEvent(evt);
                        }
                        break;

                    ///// Meta events /////
                    case TrackSequenceNumberEvent evt:
                        AddMidiEvent(evt);
                        break;

                    case TempoEvent evt:
                        var tempo = (int)Math.Round(evt.Tempo);
                        _patternDefaults.Tempo = tempo;
                        Patterns.Last().Tempo = tempo;
                        AddMidiEvent(evt);
                        break;

                    case TimeSignatureEvent evt:
                        var tsig = evt.TimeSignature;
                        _patternDefaults.TimeSig = tsig;
                        Patterns.Last().TimeSig = tsig;
                        AddMidiEvent(evt);
                        break;

                    case KeySignatureEvent evt:
                        var ksig = evt.ToString();
                        _patternDefaults.KeySig = ksig;
                        Patterns.Last().KeySig = ksig;
                        AddMidiEvent(evt);
                        break;

                    case TextEvent evt when evt.MetaEventType == MetaEventType.SequenceTrackName:
                        AddMidiEvent(evt);
                        break;

                    case TextEvent evt when evt.MetaEventType == MetaEventType.Marker:
                        // Indicates start of a new midi pattern.
                        if(Patterns.Last().Name == "")
                        {
                            // It's the default/single pattern so update its name.
                            Patterns.Last().Name = evt.Text;
                        }
                        else
                        {
                            // Tidy up missing parts of current info.
                            CleanUpPattern();

                            // Add a new pattern with defaults set to previous one.
                            Patterns.Add(new PatternInfo() { Name = evt.Text });
                        }

                        absoluteTime = 0;
                        AddMidiEvent(evt);
                        break;

                    case TextEvent evt when evt.MetaEventType == MetaEventType.TextEvent:
                        AddMidiEvent(evt);
                        break;

                    case MetaEvent evt when evt.MetaEventType == MetaEventType.EndTrack:
                        // Indicates end of current midi track.
                        AddMidiEvent(evt);
                        //_currentPattern = "";
                        break;

                    default:
                        // Other MidiCommandCodes: AutoSensing, ChannelAfterTouch, ContinueSequence, Eox, KeyAfterTouch, StartSequence, StopSequence, TimingClock
                        // Other MetaEventType: Copyright, CuePoint, DeviceName, Lyric, MidiChannel, MidiPort, ProgramName, SequencerSpecific, SmpteOffset, TrackInstrumentName
                        break;
                }
            }

            ///// Local function. /////
            void AddMidiEvent(MidiEvent evt)
            {
                var pi = Patterns.Last();
                AllEvents.Add(new EventDesc()
                {
                    Pattern = pi.Name,
                    ChannelNumber = evt.Channel,
                    AbsoluteTime = evt.AbsoluteTime,
                    ScaledTime = -1, // scale later
                    MidiEvent = evt
                });
            }

            return absoluteTime;
        }

        /// <summary>
        /// Read the CASM section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadCASM(BinaryReader br)
        {
            /*uint chunkSize =*/ ReadStream(br, 4);
        }

        /// <summary>
        /// Read the CSEG section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadCSEG(BinaryReader br)
        {
            /*uint chunkSize =*/ ReadStream(br, 4);
        }

        /// <summary>
        /// Read the Sdec section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadSdec(BinaryReader br)
        {
            uint chunkSize = ReadStream(br, 4);
            br.ReadBytes((int)chunkSize);
        }

        /// <summary>
        /// Read the Ctab section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadCtab(BinaryReader br)
        {
            // Has some key and chord info.
            uint chunkSize = ReadStream(br, 4);
            br.ReadBytes((int)chunkSize);
        }

        /// <summary>
        /// Read the Cntt section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadCntt(BinaryReader br)
        {
            uint chunkSize = ReadStream(br, 4);
            br.ReadBytes((int)chunkSize);
        }

        /// <summary>
        /// Read the OTSc section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadOTSc(BinaryReader br)
        {
            uint chunkSize = ReadStream(br, 4);
            br.ReadBytes((int)chunkSize);
        }

        /// <summary>
        /// Read the FNRc section of a style file.
        /// </summary>
        /// <param name="br"></param>
        void ReadFNRc(BinaryReader br)
        {
            uint chunkSize = ReadStream(br, 4);
            br.ReadBytes((int)chunkSize);
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Fill in missing info using defaults.
        /// </summary>
        void CleanUpPattern()
        {
            var pi = Patterns.Last();
            if (pi.Tempo == 0)
            {
                pi.Tempo = _patternDefaults.Tempo;
            }

            if (pi.TimeSig == "")
            {
                pi.TimeSig = _patternDefaults.TimeSig;
            }

            if (pi.KeySig == "")
            {
                pi.KeySig = _patternDefaults.KeySig;
            }

            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                if (pi.Patches[i].Modifier == PatchInfo.PatchModifier.NotAssigned &&
                    _patternDefaults.Patches[i].Modifier != PatchInfo.PatchModifier.NotAssigned)
                {
                    pi.Patches[i] = _patternDefaults.Patches[i];
                }
            }
        }

        /// <summary>
        /// Read a number from stream and adjust endianess.
        /// </summary>
        /// <param name="br"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        uint ReadStream(BinaryReader br, int size)
        {
            uint i;

            _lastStreamPos = br.BaseStream.Position;

            switch (size)
            {
                case 2:
                    i = br.ReadUInt16();
                    if (BitConverter.IsLittleEndian)
                    {
                        i = (UInt16)(((i & 0xFF00) >> 8) | ((i & 0x00FF) << 8));
                    }
                    break;

                case 4:
                    i = br.ReadUInt32();
                    if (BitConverter.IsLittleEndian)
                    {
                        i = ((i & 0xFF000000) >> 24) | ((i & 0x00FF0000) >> 8) | ((i & 0x0000FF00) << 8) | ((i & 0x000000FF) << 24);
                    }
                    break;

                default:
                    throw new FormatException("Unsupported read size");
            }

            return i;
        }

        /// <summary>
        /// Info to return to caller.
        /// </summary>
        /// <param name="cat"></param>
        /// <param name="message"></param>
        void AddMessage(string cat, string message)
        {
            Messages.Add((cat, message));
        }
        #endregion

        #region Utilities and diagnostics
        /// <summary>
        /// Dump the contents in a csv readable form.
        /// This is as the events appear in the original file.
        /// </summary>
        /// <returns></returns>
        public List<string> GetSequentialEvents()
        {
            List<string> contents = new();
            contents.Add("AbsoluteTime,Event,Pattern,Channel,Content");

            AllEvents.OrderBy(v => v.AbsoluteTime).
                ForEach(evt => contents.Add($"{evt.AbsoluteTime},{evt.MidiEvent!.GetType().ToString().Replace("NAudio.Midi.", "")}," +
                $"{evt.Pattern},{evt.ChannelNumber},{evt.MidiEvent}"));

            return contents;
        }

        /// <summary>
        /// Makes csv dumps of some events grouped by pattern/channel.
        /// </summary>
        /// <returns></returns>
        public List<string> GetGroupedEvents()
        {
            bool includeOther = false;
            Dictionary<string, PatternInfo> patternLUT = new();

            List<string> meta = new()
            {
                $"---------------------Meta---------------------",
                $"Meta,Value",
                $"MidiFileType,{MidiFileType}",
                $"DeltaTicksPerQuarterNote,{DeltaTicksPerQuarterNote}",
                $"Tracks,{Tracks}"
            };

            List<string> patterns = new()
            {
                "",
                "---------------------Patterns---------------------",
                "Name,Tempo,TimeSig,KeySig,Patches",
            };

            foreach (var pattern in Patterns)
            {
                patternLUT.Add(pattern.Name, pattern);

                StringBuilder sb = new();
                for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
                {
                    switch (pattern.Patches[i].Modifier)
                    {
                        case PatchInfo.PatchModifier.NotAssigned:
                            // Ignore.
                            break;

                        case PatchInfo.PatchModifier.None:
                            sb.Append($"Ch:{i + 1} Patch:{MidiDefs.GetInstrumentDef(pattern.Patches[i].Patch)}");
                            break;

                        case PatchInfo.PatchModifier.IsDrums:
                            sb.Append($"Ch:{i + 1} Patch:IsDrums");
                            break;
                    }
                }
                patterns.Add($"{pattern.Name},{pattern.Tempo},{pattern.TimeSig},{pattern.KeySig},{sb}");
            }

            List<string> notes = new()
            {
                "",
                "---------------------Notes---------------------",
                "AbsoluteTime,Pattern,Channel,Event,NoteNum,NoteName,Velocity,Duration",
            };

            List<string> other = new()
            {
                "",
                "---------------------Other---------------------",
                "AbsoluteTime,Pattern,Channel,Event,Val1,Val2,Val3",
            };

            string lastPattern = "";
            foreach (var me in AllEvents)
            {
                if (me.Pattern != lastPattern)
                {
                    notes.Add($"---------------------{me.Pattern}-------------------------");
                    other.Add($"---------------------{me.Pattern}-------------------------");
                    lastPattern = me.Pattern;
                }

                // Boilerplate.
                string ntype = me.MidiEvent!.GetType().ToString().Replace("NAudio.Midi.", "");
                string sc = $"{me.MidiEvent.AbsoluteTime},{me.Pattern},{me.MidiEvent.Channel},{ntype}";

                switch (me.MidiEvent)
                {
                    case NoteOnEvent evt:
                        int len = evt.OffEvent is null ? 0 : evt.NoteLength; // NAudio NoteLength bug.
                        string nname = patternLUT[me.Pattern].Patches[evt.Channel].Modifier == PatchInfo.PatchModifier.IsDrums ?
                            $"{MidiDefs.GetDrumDef(evt.NoteNumber)}" :
                            $"{MidiDefs.NoteNumberToName(evt.NoteNumber)}";
                        notes.Add($"{sc},{evt.NoteNumber},{nname},{evt.Velocity},{len}");
                        break;

                    case NoteEvent evt: // used for NoteOff
                        notes.Add($"{sc},{evt.NoteNumber},,{evt.Velocity},");
                        break;

                    case TempoEvent evt:
                        meta.Add($"Tempo,{evt.Tempo}");
                        other.Add($"{sc},{evt.Tempo},{evt.MicrosecondsPerQuarterNote}");
                        break;

                    case TimeSignatureEvent evt:
                        other.Add($"{sc},{evt.TimeSignature},,");
                        break;

                    case KeySignatureEvent evt:
                        other.Add($"{sc},{evt.SharpsFlats},{evt.MajorMinor},");
                        break;

                    case PatchChangeEvent evt:
                        string pname = patternLUT[me.Pattern].Patches[evt.Channel].Modifier == PatchInfo.PatchModifier.IsDrums ?
                            $"" :
                            $"{MidiDefs.GetInstrumentDef(evt.Patch)}"; // drum kit?
                        other.Add($"{sc},{evt.Patch},{pname},");
                        break;

                    case ControlChangeEvent evt:
                        other.Add($"{sc},{(int)evt.Controller},{MidiDefs.GetControllerDef((int)evt.Controller)},{evt.ControllerValue}");
                        break;

                    case PitchWheelChangeEvent evt:
                        other.Add($"{sc},{evt.Pitch},,");
                        break;

                    case TextEvent evt:
                        other.Add($"{sc},{evt.Text},,,");
                        break;

                    //case ChannelAfterTouchEvent:
                    //case SysexEvent:
                    //case MetaEvent:
                    //case RawMetaEvent:
                    //case SequencerSpecificEvent:
                    //case SmpteOffsetEvent:
                    //case TrackSequenceNumberEvent:
                    default:
                        break;
                }
            }

            List<string> ret = new();
            ret.AddRange(meta);
            ret.AddRange(patterns);
            ret.AddRange(notes);
            if (includeOther)
            {
                ret.AddRange(other);
            }

            return ret;
        }

        /// <summary>
        /// Export pattern parts to individual midi files.
        /// </summary>
        /// <param name="patterns">Specific patterns.</param>
        /// <param name="name"></param>
        /// <param name="exportPath"></param>
        /// <param name="ppq"></param>
        public void ExportMidi(List<PatternInfo> patterns, string name, string exportPath, int ppq)
        {
            //TODO2 export options: as zip.
            //TODO2 export options: start/end time range?

            if (Directory.Exists(exportPath))
            {
                foreach (var pattern in patterns)
                {
                    var newfn = Path.Join(exportPath, $"{name}_{pattern.Name.Replace(' ', '_')}.mid");
                    var info = $"Export {pattern.Name}";

                    ExportOnePattern(pattern, newfn, info, ppq);
                }
                AddMessage("INF", $"Midi exported to {exportPath}");
            }
            else
            {
                AddMessage("ERR", "Invalid export path");
            }
        }

        /// <summary>
        /// Output part of the file to a new midi file.
        /// </summary>
        /// <param name="pattern">Specific pattern if a style file.</param>
        /// <param name="fn">Where to put the midi file.</param>
        /// <param name="info">Extra info to add to midi file.</param>
        /// <param name="ppq"></param>
        public void ExportOnePattern(PatternInfo pattern, string fn, string info, int ppq)
        {
            // Init output file contents.
            MidiEventCollection outColl = new(1, ppq);
            IList<MidiEvent> outEvents = outColl.AddTrack();

            // Tempo.
            outEvents.Add(new TempoEvent(0, 0) { Tempo = pattern.Tempo });

            // General info.
            outEvents.Add(new TextEvent(info, MetaEventType.TextEvent, 0));

            // Optional.
            if (pattern.TimeSig != "")
            {
                //TODO2 outEvents.Add(new TimeSignatureEvent(0, 4, 2, (int)ticksPerClick, _ppq));
                //FF 58 04 nn dd cc bb Time Signature
                //The time signature is expressed as four numbers.nn and dd represent the numerator and denominator of the time
                //signature as it would be notated.The denominator is a negative power of two: 2 represents a quarter - note, 3
                //represents an eighth - note, etc.The cc parameter expresses the number of MIDI clocks in a metronome click.The
                //bb parameter expresses the number of notated 32nd - notes in a MIDI quarter - note(24 MIDI clocks).This was
                //added because there are already multiple programs which allow a user to specify that what MIDI thinks of as a
                //quarter - note(24 clocks) is to be notated as, or related to in terms of, something else.
                //Therefore, the complete event for 6/8 time, where the metronome clicks every three eighth-notes, but there are
                //24 clocks per quarter-note, 72 to the bar, would be (in hex):
                //FF 58 04 06 03 24 08
                //That is, 6/8 time(8 is 2 to the 3rd power, so this is 06 03), 36 MIDI clocks per dotted-quarter(24 hex!), and
                //eight notated 32nd-notes per quarter-note.
            }

            if (pattern.KeySig != "")
            {
                outEvents.Add(new KeySignatureEvent(0, 0, 0));
            }

            // Patches.
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                if (pattern.Patches[i].Modifier == PatchInfo.PatchModifier.None)
                {
                    outEvents.Add(new PatchChangeEvent(0, i + 1, pattern.Patches[i].Patch));
                }
            }

            // Gather the midi events for the pattern ordered by timestamp.
            AllEvents.Where(p => p.Pattern == pattern.Name).OrderBy(e => e.AbsoluteTime).ForEach(e => 
            {
                outEvents.Add(e.MidiEvent);
                // TODO1 adjust velocity for noteon based on channel slider value.
                //{
                //    e.AbsoluteTime = kv.Key;
                //        e.Channel = control.ChannelNumber;
                //        noteEvents.Add(e);
                //});
            });

            // Add end track.
            long ltime = outEvents.Last().AbsoluteTime;
            var endt = new MetaEvent(MetaEventType.EndTrack, 0, ltime);
            outEvents.Add(endt);

            MidiFile.Export(fn, outColl);
        }
        #endregion
    }
}
