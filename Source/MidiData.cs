using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using NAudio.Midi;
using NBagOfTricks;


// TODO Handle tracks in origin files?
// TODO auto-determine which channels have drums? adjust quiet drums?


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
        public string PatternName { get; set; } = "";
        
        /// <summary>One-based channel number.</summary>
        public int ChannelNumber { get; set; }

        /// <summary>Time (subdivs) from original file.</summary>
        public long AbsoluteTime { get; set; }

        /// <summary>Time (subdivs) scaled to internal units using send PPQ.</summary>
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

        /// <summary>Current loaded file.</summary>
        string _fn = "";
        #endregion

        #region Properties gleaned from file
        /// <summary>What is it.</summary>
        public int MidiFileType { get; private set; } = 0;

        /// <summary>How many tracks.</summary>
        public int Tracks { get; private set; } = 0;

        /// <summary>Original resolution for all events.</summary>
        public int DeltaTicksPerQuarterNote { get; private set; } = 0;
        #endregion

        #region Properties for client use
        /// <summary>All file pattern sections. Plain midi files will have only one, unnamed.</summary>
        public List<PatternInfo> AllPatterns { get; private set; } = new();

        /// <summary>All the midi events. This is the verbatim ordered content of the file.</summary>
        public List<EventDesc> AllEvents { get; private set; } = new();

        /// <summary>Where to put output products.</summary>
        public string ExportPath { get; set; } = "SET_ME!";
        #endregion



        /// <summary>One-based channel numbers for drums.</summary>
        public List<int> DrumChannels { get; set; } = new();




        #region Public functions
        /// <summary>
        /// Read a file.
        /// </summary>
        /// <param name="fn">The file to open.</param>
        /// <param name="defaultTempo">Specified by client.</param>
        /// <param name="includeNoisy"></param>
        public void Read(string fn, int defaultTempo, bool includeNoisy)
        {
            _fn = fn;
            DrumChannels.Add(MidiDefs.DEFAULT_DRUM_CHANNEL);

            if(AllEvents.Count > 0)
            {
                throw new InvalidOperationException("You don't want to do this more than once");
            }

            _patternDefaults.Tempo = defaultTempo;
            _includeNoisy = includeNoisy;

            // Always at least one pattern - for plain midi.
            AllPatterns.Add(new PatternInfo() { PatternName = "" });

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
                        //PatchInfo patch = new() { PatchNumber = evt.Patch, Modifier = PatchInfo.PatchModifier.None };
                        _patternDefaults.Patches[index] = evt.Patch;
                        AllPatterns.Last().Patches[index] = evt.Patch;
                        AddMidiEvent(evt);
//                        Debug.WriteLine(evt.ToString());
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
                        AllPatterns.Last().Tempo = tempo;
                        AddMidiEvent(evt);
                        break;

                    case TimeSignatureEvent evt:
                        var tsig = evt.TimeSignature;
                        _patternDefaults.TimeSig = tsig;
                        AllPatterns.Last().TimeSig = tsig;
                        AddMidiEvent(evt);
                        break;

                    case KeySignatureEvent evt:
                        var ksig = evt.ToString();
                        _patternDefaults.KeySig = ksig;
                        AllPatterns.Last().KeySig = ksig;
                        AddMidiEvent(evt);
                        break;

                    case TextEvent evt when evt.MetaEventType == MetaEventType.SequenceTrackName:
                        AddMidiEvent(evt);
                        break;

                    case TextEvent evt when evt.MetaEventType == MetaEventType.Marker:
                        // Indicates start of a new midi pattern.
                        if(AllPatterns.Last().PatternName == "")
                        {
                            // It's the default/single pattern so update its name.
                            AllPatterns.Last().PatternName = evt.Text;
                        }
                        else
                        {
                            // Tidy up missing parts of current info.
                            CleanUpPattern();

                            // Add a new pattern with defaults set to previous one.
                            AllPatterns.Add(new PatternInfo() { PatternName = evt.Text });
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
                var pi = AllPatterns.Last();
                AllEvents.Add(new EventDesc()
                {
                    PatternName = pi.PatternName,
                    ChannelNumber = evt.Channel,
                    AbsoluteTime = evt.AbsoluteTime,
                    ScaledTime = -1, // scale later
                    MidiEvent = evt
                });
            }

            //File.Delete("events.txt");
            //AllEvents.ForEach(e => File.AppendAllText("events.txt", $"{e.MidiEvent}{Environment.NewLine}"));

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
            var pi = AllPatterns.Last();
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

                //if (pi.Patches[i].Modifier == PatchInfo.PatchModifier.NotAssigned &&
                //    _patternDefaults.Patches[i].Modifier != PatchInfo.PatchModifier.NotAssigned)
                //{
                //    pi.Patches[i] = _patternDefaults.Patches[i];
                //}
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
        #endregion

        #region Utilities and diagnostics
        /// <summary>
        /// Export the contents in a csv readable form. This is as the events appear in the original file.
        /// </summary>
        /// <param name="channels">Specific channnels or all if empty.</param>
        /// <returns>File name of dump file.</returns>
        public string ExportAllEvents(List<int> channels)
        {
            List<string> contentText = new()
            {
                $"Meta,Value",
                $"MidiFileType,{MidiFileType}",
                $"DeltaTicksPerQuarterNote,{DeltaTicksPerQuarterNote}",
                $"Tracks,{Tracks}",
            };

            contentText.Add("AbsoluteTime,Event,Pattern,Channel,Content");

            GetFilteredEvents("", channels, false).
                ForEach(evt => contentText.Add($"{evt.AbsoluteTime},{evt.MidiEvent!.GetType().ToString().Replace("NAudio.Midi.", "")}," +
                $"{evt.PatternName},{evt.ChannelNumber},{evt.MidiEvent}"));

            // Export away.
            var newfn = MakeExportFileName("all", "csv");
            File.WriteAllLines(newfn, contentText);
            return newfn;            
        }

        /// <summary>
        /// Makes csv dumps of some events grouped by pattern/channel.
        /// </summary>
        /// <param name="patternName">Specific pattern.</param>
        /// <param name="channels">Specific channnels or all if empty.</param>
        /// <param name="includeOther">false if just notes or true if everything.</param>
        /// <returns>File name of dump file.</returns>
        public string ExportGroupedEvents(string patternName, List<int> channels, bool includeOther)
        {
            var pattern = AllPatterns.Where(p => p.PatternName == patternName).First();

            StringBuilder patches = new();
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                int chnum = i + 1;

                if(pattern.Patches[i] >= 0)
                {
                    patches.Append($"{chnum}:{MidiDefs.GetInstrumentDef(pattern.Patches[i])} ");
                }


                //switch (pattern.Patches[i].Modifier)
                //{
                //    case PatchInfo.PatchModifier.NotAssigned:
                //        // Ignore.
                //        break;

                //    case PatchInfo.PatchModifier.None:
                //        patches.Append($"{chnum}:{MidiDefs.GetInstrumentDef(pattern.Patches[i].PatchNumber)} ");
                //        break;

                //    case PatchInfo.PatchModifier.IsDrums: TODO1
                //        patches.Append($"{chnum}:IsDrums ");
                //        break;
                //}
            }

            List<string> metaText = new()
            {
                $"Meta,======",
                $"Meta,Value",
                $"MidiFileType,{MidiFileType}",
                $"DeltaTicksPerQuarterNote,{DeltaTicksPerQuarterNote}",
                $"Tracks,{Tracks}",
                $"Pattern,{pattern.PatternName}",
                $"Tempo,{pattern.Tempo}",
                $"TimeSig,{pattern.TimeSig}",
                $"KeySig,{pattern.KeySig}",
                $"Patches,{patches}",
            };

            List<string> notesText = new()
            {
                "Notes,======",
                "AbsoluteTime,Pattern,Channel,Event,NoteNum,NoteName,Velocity,Duration",
            };

            List<string> otherText = new()
            {
                "Other,======",
                "AbsoluteTime,Pattern,Channel,Event,Val1,Val2,Val3",
            };

            foreach (var me in GetFilteredEvents(patternName, channels, true))
            {
                // Boilerplate.
                string ntype = me.MidiEvent!.GetType().ToString().Replace("NAudio.Midi.", "");
                string sc = $"{me.MidiEvent.AbsoluteTime},{me.PatternName},{me.MidiEvent.Channel},{ntype}";

                switch (me.MidiEvent)
                {
                    case NoteOnEvent evt:
                        int len = evt.OffEvent is null ? 0 : evt.NoteLength; // NAudio NoteLength bug.
                        //string nname = pattern.Patches[evt.Channel - 1].Modifier == PatchInfo.PatchModifier.IsDrums ? // TODO This is clumsy. Static file needs runtime information.
                        //    $"{MidiDefs.GetDrumDef(evt.NoteNumber)}" :
                        //    $"{MidiDefs.NoteNumberToName(evt.NoteNumber)}";
                        string nname = MidiDefs.NoteNumberToName(evt.NoteNumber);
                        notesText.Add($"{sc},{evt.NoteNumber},{nname},{evt.Velocity},{len}");
                        break;

                    case NoteEvent evt: // used for NoteOff
                        notesText.Add($"{sc},{evt.NoteNumber},,{evt.Velocity},");
                        break;

                    case TempoEvent evt:
                        metaText.Add($"Tempo,{evt.Tempo}");
                        otherText.Add($"{sc},{evt.Tempo},{evt.MicrosecondsPerQuarterNote}");
                        break;

                    case TimeSignatureEvent evt:
                        otherText.Add($"{sc},{evt.TimeSignature},,");
                        break;

                    case KeySignatureEvent evt:
                        otherText.Add($"{sc},{evt.SharpsFlats},{evt.MajorMinor},");
                        break;

                    case PatchChangeEvent evt:
                        //string pname = pattern.Patches[evt.Channel - 1].Modifier == PatchInfo.PatchModifier.IsDrums ? // TODO This is clumsy. Static file needs runtime information.
                        //    $"" :
                        //    $"{MidiDefs.GetInstrumentDef(evt.Patch)}"; // drum kit?
                        string pname = MidiDefs.GetInstrumentDef(evt.Patch);
                        otherText.Add($"{sc},{evt.Patch},{pname},");
                        break;

                    case ControlChangeEvent evt:
                        otherText.Add($"{sc},{(int)evt.Controller},{MidiDefs.GetControllerDef((int)evt.Controller)},{evt.ControllerValue}");
                        break;

                    case PitchWheelChangeEvent evt:
                        otherText.Add($"{sc},{evt.Pitch},,");
                        break;

                    case TextEvent evt:
                        otherText.Add($"{sc},{evt.Text},,,");
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

            // Export away.
            var newfn = MakeExportFileName(patternName, "csv");
            File.WriteAllLines(newfn, metaText);
            File.AppendAllLines(newfn, notesText);
            if (includeOther)
            {
                File.AppendAllLines(newfn, otherText);
            }

            return newfn;
        }

        /// <summary>
        /// Export pattern parts to individual midi files.
        /// </summary>
        /// <param name="patternName">Specific pattern.</param>
        /// <param name="channels">Specific channnels or all if empty.</param>
        /// <param name="ppq">Export at this resolution.</param>
        /// <param name="zip">TODO export as zip.</param>
        /// <returns>File name of export file.</returns>
        public string ExportMidi(string patternName, List<int> channels, int ppq, bool zip)
        {
            string name = Path.GetFileNameWithoutExtension(_fn);

            var pattern = AllPatterns.Where(p => p.PatternName == patternName).First();
            var newfn = MakeExportFileName(patternName, "mid");

            // Init output file contents.
            MidiEventCollection outColl = new(1, ppq);
            IList<MidiEvent> outEvents = outColl.AddTrack();

            // Tempo.
            outEvents.Add(new TempoEvent(0, 0) { Tempo = pattern.Tempo });

            // General info.
            var info = $"Export {patternName}";
            outEvents.Add(new TextEvent(info, MetaEventType.TextEvent, 0));

            // Optional.
            if (pattern.TimeSig != "")
            {
                // TODO figure out TimeSignatureEvent(0, 4, 2, (int)ticksPerClick, _ppq).
            }

            if (pattern.KeySig != "")
            {
                outEvents.Add(new KeySignatureEvent(0, 0, 0));
            }

            // Patches.
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                int chnum = i + 1;
                if (pattern.Patches[i] >= 0)
                {
                    outEvents.Add(new PatchChangeEvent(0, chnum, pattern.Patches[i]));
                }
                //if (pattern.Patches[i].Modifier == PatchInfo.PatchModifier.None)
                //{
                //    outEvents.Add(new PatchChangeEvent(0, chnum, pattern.Patches[i].PatchNumber));
                //}
            }

            // Gather the midi events for the pattern ordered by timestamp.
            GetFilteredEvents(patternName, channels, true).ForEach(e =>
            {
                // TODO adjust velocity for noteon based on channel slider value. This is clumsy. Static file needs runtime information.
                outEvents.Add(e.MidiEvent);
            });

            // Add end track.
            long ltime = outEvents.Last().AbsoluteTime;
            var endt = new MetaEvent(MetaEventType.EndTrack, 0, ltime);
            outEvents.Add(endt);

            MidiFile.Export(newfn, outColl);

            return newfn;
        }

        /// <summary>
        /// Get enumerator for events using supplied filters.
        /// </summary>
        /// <param name="patternName">Specific pattern or all if empty.</param>
        /// <param name="channels">Specific channnels or all if empty.</param>
        /// <param name="sortTime">Optional sort.</param>
        /// <returns>Enumerator</returns>
        IEnumerable<EventDesc> GetFilteredEvents(string patternName, List<int> channels, bool sortTime)
        {
            IEnumerable<EventDesc> descs;

            switch (((uint)patternName.Length, (uint)channels.Count))
            {
                case (0, 0):
                    descs = AllEvents.AsEnumerable();
                    break;
                case (0, >0):
                    descs = AllEvents.Where(e => channels.Contains(e.ChannelNumber));
                    break;
                case (>0, 0):
                    descs = AllEvents.Where(e => patternName == e.PatternName);
                    break;
                case (>0, >0):
                    descs = AllEvents.Where(e => patternName == e.PatternName && channels.Contains(e.ChannelNumber));
                    break;
            }

            // Always order.
            return sortTime ? descs.OrderBy(e => e.AbsoluteTime) : descs;
        }

        /// <summary>
        /// Create a new clean filename for export. Creates path if it doesn't exist.
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="ext"></param>
        /// <returns></returns>
        string MakeExportFileName(string mod, string ext)
        {
            string name = Path.GetFileNameWithoutExtension(_fn);

            // Clean the file name.
            name = name.Replace('.', '-').Replace(' ', '_');
            mod = mod.Replace(' ', '_');

            var newfn = Path.Join(ExportPath, $"{name}_{mod}.{ext}");
            return newfn;
        }
        #endregion
    }
}
