using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using NAudio.Midi;
using NBagOfTricks;


namespace MidiLib
{
    /// <summary>
    /// Represents one complete collection of midi events, usually from a midi file.
    /// Reads and processes standard midi or yamaha style files.
    /// Writes subsets to various output formats.
    /// </summary>
    public class MidiDataFile
    {
        #region Fields
        /// <summary>Include events like controller changes, pitch wheel, ...</summary>
        bool _includeNoisy = false;

        /// <summary>Save this for logging/debugging.</summary>
        long _lastStreamPos = 0;

        /// <summary>It's a style file.</summary>
        bool _styleFile = false;

        /// <summary>All file pattern sections. Plain midi files will have only one, unnamed.</summary>
        readonly List<PatternInfo> _patterns = new();

        /// <summary>Currently collecting this pattern.</summary>
        PatternInfo _currentPattern = new();

        /// <summary>Default values if not supplied in pattern. Mainly for managing patches.</summary>
        readonly PatternInfo _patternDefaults = new();

        /// <summary>Phased parsing of style files.</summary>
        bool _styleDefaults = true; // Assumes defaults at beginning of file.
        #endregion

        #region Properties
        /// <summary>Current loaded file.</summary>
        public string FileName { get; private set; } = "";

        /// <summary>What is it.</summary>
        public int MidiFileType { get; private set; } = 0;

        /// <summary>How many tracks.</summary>
        public int NumTracks { get; private set; } = 0;// TODO Properly handle tracks from original files?

        /// <summary>Original resolution for all events.</summary>
        public int DeltaTicksPerQuarterNote { get; private set; } = 0;
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
            if(_patterns.Any())
            {
                throw new InvalidOperationException($"Already processed - delete me first");
            }

            FileName = fn;
            _styleFile = MidiLibDefs.STYLE_FILE_TYPES.Contains(Path.GetExtension(fn).ToLower());
            _patternDefaults.Tempo = defaultTempo;
            _includeNoisy = includeNoisy;

            using var br = new BinaryReader(File.OpenRead(fn));
            bool done = false;

            while (!done)
            {
                var sectionName = Encoding.UTF8.GetString(br.ReadBytes(4));

                switch (sectionName)
                {
                    case "MThd":
                        ReadMThd(br);
                        // Always at least one pattern - for plain midi. Safe to init now.
                        _currentPattern = new("", DeltaTicksPerQuarterNote);
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
                    case "OTSc":
                        // One Touch Setting section
                        ReadOTSc(br);
                        break;
                    case "FNRc":
                        // MDB (Music Finder) section
                        ReadFNRc(br);
                        break;
                    default:
                        done = true;
                        break;
                }
            }

            // Clean up straggler.
            CleanUpPattern();
            _patterns.Add(_currentPattern);

            // TODO auto-determine which channels have drums? https://www.midi.org/forum/8860-general-midi-level-2-ch-11-percussion
            // This is from the General MIDI 2.0 specification:
            // "Bank Select 78H/xxH followed by a Program Change will cause the Channel to become a Rhythm Channel, using the Drum Set selected by the Program Change."
            // For more details get the GM2 specification here: https://www.midi.org/specifications/midi1-specifications/general-midi-specifications/general-midi-2
            // >>> Drum channels will probably have the most notes. Also durations will be short.
            // >>> Could also remember user's reassignments in the settings file.
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
        /// Get all useful pattern names.
        /// </summary>
        /// <returns>List of names.</returns>
        public List<string> GetPatternNames()
        {
            var names = _patterns.Select(p => p.PatternName).ToList();

            return names;
        }

        /// <summary>
        /// Utility to contain midi file meta info.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, int> GetGlobal()
        {
            Dictionary<string, int> global = new()
            {
                { "MidiFileType", MidiFileType },
                { "DeltaTicksPerQuarterNote", DeltaTicksPerQuarterNote },
                { "NumTracks", NumTracks }
            };

            return global;
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

                    case PatchChangeEvent evt:
                        if(_styleFile && _styleDefaults)
                        {
                            _patternDefaults.SetChannelPatch(evt.Channel, evt.Patch);
                        }
                        else
                        {
                            _currentPattern.SetChannelPatch(evt.Channel, evt.Patch);
                        }
                        AddMidiEvent(evt);
                        //Debug.WriteLine($"{evt}");
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
                        if (_styleFile && _styleDefaults)
                        {
                            _patternDefaults.Tempo = tempo;
                        }
                        else
                        {
                            _currentPattern.Tempo = tempo;
                        }
                        AddMidiEvent(evt);
                        break;

                    case TimeSignatureEvent evt:
                        if (_styleFile && _styleDefaults)
                        {
                            _patternDefaults.TimeSigNumerator = evt.Numerator;
                            _patternDefaults.TimeSigDenominator = evt.Denominator;
                        }
                        else
                        {
                            _currentPattern.TimeSigNumerator = evt.Numerator;
                            _currentPattern.TimeSigDenominator = evt.Denominator;
                        }
                        AddMidiEvent(evt);
                        break;

                    case KeySignatureEvent evt:
                        if (_styleFile && _styleDefaults)
                        {
                            _patternDefaults.KeySigSharpsFlats = evt.SharpsFlats;
                            _patternDefaults.KeySigMajorMinor = evt.MajorMinor;
                        }
                        else
                        {
                            _currentPattern.KeySigSharpsFlats = evt.SharpsFlats;
                            _currentPattern.KeySigMajorMinor = evt.MajorMinor;
                        }
                        AddMidiEvent(evt);
                        break;

                    case TextEvent evt when evt.MetaEventType == MetaEventType.SequenceTrackName:
                        AddMidiEvent(evt);
                        break;

                    case TextEvent evt when evt.MetaEventType == MetaEventType.Marker:
                        // This optional event is used to label points within a sequence, e.g. rehearsal letters, loop points, or section
                        // names (such as 'First verse'). For a format 1 MIDI le, Marker Meta events should only occur within the first MTrk chunk.

                        if (_styleFile)
                        {
                            // Indicates start of a new midi pattern or global info.

                            // Do something with the current if it's notes.
                            if(!_styleDefaults)
                            {
                                // Tidy up any missing parts of current info and save it.
                                CleanUpPattern();
                                _patterns.Add(_currentPattern);
                                _styleDefaults = false; // reset
                            }

                            // Start a new pattern.
                            _currentPattern = new PatternInfo(evt.Text, DeltaTicksPerQuarterNote);
                            absoluteTime = 0;
                            AddMidiEvent(evt);

                            // Does it contain defaults?
                            if(_styleFile)
                            {
                                _styleDefaults = _currentPattern.PatternName switch
                                {
                                    // These don't contain pattern notes.
                                    "SFF1" or "SFF2" or "SInt" or "" => true,
                                    _ => false,
                                };
                            }
                        }
                        else
                        {
                            // Simple add of one only pattern.
                            AddMidiEvent(evt);
                        }
                        break;

                    case TextEvent evt when evt.MetaEventType == MetaEventType.TextEvent:
                        AddMidiEvent(evt);
                        break;

                    case MetaEvent evt when evt.MetaEventType == MetaEventType.EndTrack:
                        // Indicates end of current midi track.
                        AddMidiEvent(evt);
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
                _currentPattern.AddEvent(new MidiEventDesc(evt, $"chan{evt.Channel}"));
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
            if (_currentPattern.Tempo == 0)
            {
                _currentPattern.Tempo = _patternDefaults.Tempo;
            }

            if (_currentPattern.TimeSigNumerator == -1 && _currentPattern.TimeSigDenominator == -1)
            {
                _currentPattern.TimeSigNumerator = _patternDefaults.TimeSigNumerator;
                _currentPattern.TimeSigDenominator = _patternDefaults.TimeSigDenominator;
            }

            if (_currentPattern.KeySigSharpsFlats == -1 && _currentPattern.KeySigMajorMinor == -1)
            {
                _currentPattern.KeySigSharpsFlats = _patternDefaults.KeySigSharpsFlats;
                _currentPattern.KeySigMajorMinor = _patternDefaults.KeySigMajorMinor;
            }

            _currentPattern.GetValidChannels().ForEach(ch =>
            {
                if (ch.patch < 0)
                {
                    _currentPattern.SetChannelPatch(ch.number, _patternDefaults.GetPatch(ch.number));
                }
            });
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
            var i = size switch
            {
                2 => MiscUtils.FixEndian(br.ReadUInt16()),
                4 => MiscUtils.FixEndian(br.ReadUInt32()),
                _ => throw new FormatException("Unsupported read size"),
            };
            return i;
        }
        #endregion
    }
}
