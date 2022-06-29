using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using NAudio.Midi;
using NBagOfTricks;


// TODO Properly handle tracks from original files?
// TODO auto-determine which channels have drums? https://www.midi.org/forum/8860-general-midi-level-2-ch-11-percussion
// This is from the General MIDI 2.0 specification:
// "Bank Select 78H/xxH followed by a Program Change will cause the Channel to become a Rhythm Channel, using the Drum Set selected by the Program Change."
// For more details get the GM2 specification here: https://www.midi.org/specifications/midi1-specifications/general-midi-specifications/general-midi-2


namespace MidiLib
{
    /// <summary>
    /// Internal representation of one midi event.
    /// </summary>
    public class MidiEventDesc
    {
        /// <summary>One-based channel number.</summary>
        public int ChannelNumber { get { return MidiEvent.Channel; } }

        /// <summary>Time (subdivs) from original file.</summary>
        public long AbsoluteTime { get { return MidiEvent.AbsoluteTime; } }

        /// <summary>Time (subdivs) scaled to internal units using send PPQ.</summary>
        public int ScaledTime { get; set; } = -1;

        /// <summary>The raw midi event.</summary>
        public MidiEvent MidiEvent { get; init; }

        /// <summary>Normal constructor.</summary>
        public MidiEventDesc(MidiEvent evt)
        {
            MidiEvent = evt;
        }
    }

    /// <summary>
    /// Represents one complete collection of midi events, usually from a midi file.
    /// Reads and processes standard midi or yamaha style files.
    /// Writes subsets to various output formats.
    /// </summary>
    public class MidiData
    {
        #region Fields
        ///// <summary>The internal channel objects.</summary>
        //readonly ChannelCollection _allChannels = new();

        /// <summary>Include events like controller changes, pitch wheel, ...</summary>
        bool _includeNoisy = false;

        /// <summary>Save this for logging/debugging.</summary>
        long _lastStreamPos = 0;

        /// <summary>All file pattern sections. Plain midi files will have only one, unnamed.</summary>
        readonly List<PatternInfo> _patterns = new();

        /// <summary>Default values if not supplied in pattern. Mainly for managing patches.</summary>
        PatternInfo _patternDefaults = new();

        /// <summary>Current loaded file.</summary>
        string _fn = "";
        #endregion

        #region Properties
        /// <summary>What is it.</summary>
        public int MidiFileType { get; private set; } = 0;

        /// <summary>How many tracks.</summary>
        public int NumTracks { get; private set; } = 0;

        /// <summary>Original resolution for all events.</summary>
        public int DeltaTicksPerQuarterNote { get; private set; } = 0;

        /// <summary>Number of patterns contained.</summary>
        public int NumPatterns { get { return _patterns.Count; } }
        #endregion

        #region Public functions
        /// <summary>
        /// Read a file.
        /// </summary>
        /// <param name="fn">The file to open.</param>
        /// <param name="defaultTempo">Specified by client.</param>
        /// <param name="includeNoisy"></param>
        public void Read(string fn, int defaultTempo, bool includeNoisy)
        {
            Reset();

            _fn = fn;
            _patternDefaults.Tempo = defaultTempo;
            _includeNoisy = includeNoisy;

            // Always at least one pattern - for plain midi.
            _patterns.Add(new PatternInfo() { PatternName = "" });

            using var br = new BinaryReader(File.OpenRead(fn));
            bool done = false;

            while (!done)
            {
                var sectionName = Encoding.UTF8.GetString(br.ReadBytes(4));

                switch (sectionName)
                {
                    case "MThd": ReadMThd(br); break;
                    case "MTrk": ReadMTrk(br); break;
                    case "CASM": ReadCASM(br); break;
                    case "CSEG": ReadCSEG(br); break;
                    case "Sdec": ReadSdec(br); break;
                    case "Ctab": ReadCtab(br); break;
                    case "Cntt": ReadCntt(br); break;
                    case "OTSc": ReadOTSc(br); break; // One Touch Setting section
                    case "FNRc": ReadFNRc(br); break; // MDB (Music Finder) section
                    default:     done = true;  break;
                }
            }

            // Last one.
            CleanUpPattern();
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void Reset()
        {
            _lastStreamPos = 0;
            _patternDefaults = new();

            MidiFileType = 0;
            NumTracks = 0;
            DeltaTicksPerQuarterNote = 0;

            _patterns.Clear();
        }

        /// <summary>
        /// Get the pattern by index.
        /// </summary>
        /// <param name="index">Which</param>
        /// <returns>The pattern or null if invalid request.</returns>
        public PatternInfo? GetPattern(int index)
        {
            PatternInfo? ret = null;

            if (index < _patterns.Count)
            {
                ret = _patterns[index];
            }
            return ret;
        }

        /// <summary>
        /// Get the pattern by name.
        /// </summary>
        /// <param name="name">Which</param>
        /// <returns>The pattern or null if invalid request.</returns>
        public PatternInfo? GetPattern(string name)
        {
            PatternInfo? ret = null;

            var pinfo = _patterns.Where(p => p.PatternName == name);
            if (pinfo is not null && pinfo.Any())
            {
                ret = pinfo.First();
            }
            return ret;
        }

        /// <summary>
        /// Get all pattern names.
        /// </summary>
        /// <returns>List of names.</returns>
        public List<string> GetPatternNames()
        {
            List<string> ret = _patterns.Select(p => p.PatternName).ToList();
            return ret;
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
            NumTracks = (int)ReadStream(br, 2);

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
            MidiEvent? me = null; // current event
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

                    case NoteEvent evt: // aka NoteOff
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
                        _patternDefaults.Patches[index] = evt.Patch;
                        _patterns.Last().Patches[index] = evt.Patch;
                        AddMidiEvent(evt);
                        break;

                    case SysexEvent evt:
                        if (_includeNoisy)
                        {
                            AddMidiEvent(evt);
                        }
                        break;

                    ///// Meta events /////
                    //case TrackSequenceNumberEvent evt:
                    //    AddMidiEvent(evt);
                    //    break;

                    case TempoEvent evt:
                        var tempo = (int)Math.Round(evt.Tempo);
                        _patternDefaults.Tempo = tempo;
                        _patterns.Last().Tempo = tempo;
                        AddMidiEvent(evt);
                        break;

                    case TimeSignatureEvent evt:
                        var tsig = evt.TimeSignature;
                        _patternDefaults.TimeSig = tsig;
                        _patterns.Last().TimeSig = tsig;
                        AddMidiEvent(evt);
                        break;

                    case KeySignatureEvent evt:
                        var ksig = evt.ToString();
                        _patternDefaults.KeySig = ksig;
                        _patterns.Last().KeySig = ksig;
                        AddMidiEvent(evt);
                        break;

                    case TextEvent evt when evt.MetaEventType == MetaEventType.SequenceTrackName:
                        AddMidiEvent(evt);
                        break;

                    case TextEvent evt when evt.MetaEventType == MetaEventType.Marker:
                        // Indicates start of a new midi pattern.
                        if(_patterns.Last().PatternName == "")
                        {
                            // It's the default/single pattern so update its name.
                            _patterns.Last().PatternName = evt.Text;
                        }
                        else
                        {
                            // Tidy up missing parts of current info.
                            CleanUpPattern();

                            // Add a new pattern with defaults set to previous one.
                            _patterns.Add(new PatternInfo() { PatternName = evt.Text });
                        }

                        absoluteTime = 0;
                        AddMidiEvent(evt);
                        break;

                    case TextEvent evt when evt.MetaEventType == MetaEventType.TextEvent:
                        AddMidiEvent(evt);
                        break;

                    //case MetaEvent evt when evt.MetaEventType == MetaEventType.EndTrack:
                    //    // Indicates end of current midi track.
                    //    AddMidiEvent(evt);
                    //    break;

                    default:
                        // Other MidiCommandCodes: AutoSensing, ChannelAfterTouch, ContinueSequence, Eox, KeyAfterTouch, StartSequence, StopSequence, TimingClock
                        // Other MetaEventType: Copyright, CuePoint, DeviceName, Lyric, MidiChannel, MidiPort, ProgramName, SequencerSpecific, SmpteOffset, TrackInstrumentName
                        break;
                }
            }

            ///// Local function. /////
            void AddMidiEvent(MidiEvent evt)
            {
                var pi = _patterns.Last();
                pi.AddEvent(new MidiEventDesc(evt));
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
            var pi = _patterns.Last();
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
                if (pi.Patches[i] < 0)
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
            _lastStreamPos = br.BaseStream.Position;
            uint i;

            switch (size)
            {
                case 2:
                    i = MiscUtils.FixEndian(br.ReadUInt16());
                    break;

                case 4:
                    i = MiscUtils.FixEndian(br.ReadUInt32());
                    break;

                default:
                    throw new FormatException("Unsupported read size");
            }

            return i;
        }
        #endregion

        #region Export functions
        /// <summary>
        /// Export the contents in a csv readable form. This is as the events appear in the original file.
        /// </summary>
        /// <param name="outPath">Where to boss?</param>
        /// <param name="channels">Specific channnels or all if empty.</param>
        /// <returns>File name of dump file.</returns>
        public string ExportAllEvents(string outPath, List<Channel> channels)
        {
            var newfn = MidiExport.MakeExportFileName(outPath, _fn, "all", "csv");
            MidiExport.ExportAllEvents(newfn, _patterns, channels, MakeMeta());
            return newfn;            
        }

        /// <summary>
        /// Makes csv dumps of some events grouped by pattern/channel. This is as the events appear in the original file.
        /// </summary>
        /// <param name="outPath">Where to boss?</param>
        /// <param name="patternName">Specific pattern.</param>
        /// <param name="channels">Specific channnels or all if empty.</param>
        /// <param name="includeAll">False if just notes or true if everything.</param>
        /// <returns>File name of dump file.</returns>
        public string ExportGroupedEvents(string outPath, string patternName, List<Channel> channels, bool includeAll)
        {
            var pattern = _patterns.Where(p => p.PatternName == patternName).First();
            var newfn = MidiExport.MakeExportFileName(outPath, _fn, patternName, "csv");
            MidiExport.ExportGroupedEvents(newfn, pattern, channels, MakeMeta(), includeAll);
            return newfn;
        }

        /// <summary>
        /// Export pattern parts to individual midi files. This is as the events appear in the original file. TODO export as zip?
        /// </summary>
        /// <param name="outPath">Where to boss?</param>
        /// <param name="patternName">Specific pattern.</param>
        /// <param name="channels">Specific channnels or all if empty.</param>
        /// <param name="ppq">Export at this resolution.</param>
        /// <returns>File name of export file.</returns>
        public string ExportMidi(string outPath, string patternName, List<Channel> channels, int ppq)
        {
            var pattern = _patterns.Where(p => p.PatternName == patternName).First();
            var newfn = MidiExport.MakeExportFileName(outPath, _fn, patternName, "mid");
            MidiExport.ExportMidi(newfn, pattern, channels, MakeMeta());
            return newfn;
        }

        /// <summary>
        /// Common utility.
        /// </summary>
        /// <returns></returns>
        Dictionary<string, int> MakeMeta()
        {
            Dictionary<string, int> meta = new()
            {
                { "MidiFileType", MidiFileType },
                { "DeltaTicksPerQuarterNote", DeltaTicksPerQuarterNote },
                { "NumTracks", NumTracks }
            };

            return meta;
        }
        #endregion
    }
}
