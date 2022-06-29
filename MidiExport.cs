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
    /// Writes to various output formats.
    /// </summary>
    public class MidiExport
    {
        /// <summary>
        /// Export the contents in a csv readable form. This is as the events appear in the original file.
        /// </summary>
        /// <param name="outPath">Where to boss?</param>
        /// <param name="channels">Specific channnels or all if empty.</param>
        /// <returns>File name of dump file.</returns>
        public static string ExportAllEvents(string outPath, string baseFn, List<PatternInfo> patterns, List<int> channels)
        {
            List<string> contentText = new();

            //List<string> contentText = new()
            //{
            //    $"Meta,Value",
            //    $"MidiFileType,{MidiFileType}",
            //    $"DeltaTicksPerQuarterNote,{DeltaTicksPerQuarterNote}",
            //    $"Tracks,{NumTracks}",
            //};

            contentText.Add("AbsoluteTime,Event,Pattern,Channel,Content");

            foreach(PatternInfo pi in patterns)
            {
                var descs = pi.GetFilteredEvents(channels, false);
                descs?.ForEach(evt => contentText.Add($"{evt.AbsoluteTime},{evt.MidiEvent!.GetType().ToString().Replace("NAudio.Midi.", "")}," +
                    $"{pi.PatternName},{evt.ChannelNumber},{evt.MidiEvent}"));
            }

            // Export away.
            var newfn = MakeExportFileName(outPath, baseFn, "all", "csv");
            File.WriteAllLines(newfn, contentText);
            return newfn;            
        }

        /// <summary>
        /// Makes csv dumps of some events grouped by pattern/channel. This is as the events appear in the original file.
        /// </summary>
        /// <param name="outPath">Where to boss?</param>
        /// <param name="patternName">Specific pattern.</param>
        /// <param name="channels">Specific channnels or all if empty.</param>
        /// <param name="includeOther">false if just notes or true if everything.</param>
        /// <returns>File name of dump file.</returns>
        public static string ExportGroupedEvents(string outPath, string baseFn, PatternInfo pattern, ChannelCollection allChannels, List<int> channels, bool includeOther)
        {
            //var pattern = _patterns.Where(p => p.PatternName == patternName).First();

            StringBuilder patches = new();
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                int chnum = i + 1;

                if (pattern.Patches[i] >= 0)
                {
                    var sp = allChannels.IsDrums(chnum) ? "Drums" : MidiDefs.GetInstrumentName(pattern.Patches[i]);
                    patches.Append($"{chnum}:{sp} ");
                }
            }

            List<string> metaText = new()
            {
                $"Meta,======",
                $"Meta,Value",
                //$"MidiFileType,{MidiFileType}",
                //$"DeltaTicksPerQuarterNote,{DeltaTicksPerQuarterNote}",
                //$"Tracks,{NumTracks}",
                $"Pattern,{pattern.PatternName}",
                $"Tempo,{pattern.Tempo}",
                $"TimeSig,{pattern.TimeSig}",
                $"KeySig,{pattern.KeySig}",
                $"Patches,{patches}",
            };

            List<string> notesText = new()
            {
                "Notes,======",
                "AbsoluteTime,Channel,Event,NoteNum,NoteName,Velocity,Duration",
            };

            List<string> otherText = new()
            {
                "Other,======",
                "AbsoluteTime,Channel,Event,Val1,Val2,Val3",
            };

            var descs = pattern.GetFilteredEvents(channels, true);
            descs?.ForEach(me =>
            {
                // Boilerplate.
                string ntype = me.MidiEvent!.GetType().ToString().Replace("NAudio.Midi.", "");
                string sc = $"{me.MidiEvent.AbsoluteTime},{me.MidiEvent.Channel},{ntype}";

                switch (me.MidiEvent)
                {
                    case NoteOnEvent evt:
                        int len = evt.OffEvent is null ? 0 : evt.NoteLength; // NAudio NoteLength bug.

                        string nname = allChannels.IsDrums(me.MidiEvent.Channel) ?
                           $"{MidiDefs.GetDrumName(evt.NoteNumber)}" :
                           $"{MusicDefinitions.NoteNumberToName(evt.NoteNumber)}";
                        notesText.Add($"{sc},{evt.NoteNumber},{nname},{evt.Velocity},{len}");
                        break;

                    case NoteEvent evt: // aka NoteOff
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
                        string pname = allChannels.IsDrums(me.MidiEvent.Channel) ?
                           $"{MidiDefs.GetDrumKitName(evt.Patch)}" :
                           $"{MidiDefs.GetInstrumentName(evt.Patch)}";
                        otherText.Add($"{sc},{evt.Patch},{pname},");
                        break;

                    case ControlChangeEvent evt:
                        otherText.Add($"{sc},{(int)evt.Controller},{MidiDefs.GetControllerName((int)evt.Controller)},{evt.ControllerValue}");
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
            });

            // Export away.
            var newfn = MakeExportFileName(outPath, baseFn, pattern.PatternName, "csv");
            File.WriteAllLines(newfn, metaText);
            File.AppendAllLines(newfn, notesText);
            if (includeOther)
            {
                File.AppendAllLines(newfn, otherText);
            }

            return newfn;
        }

        /// <summary>
        /// Export pattern parts to individual midi files. This is as the events appear in the original file.
        /// </summary>
        /// <param name="outPath">Where to boss?</param>
        /// <param name="patternName">Specific pattern.</param>
        /// <param name="channels">Specific channnels or all if empty.</param>
        /// <param name="ppq">Export at this resolution.</param>
        /// <returns>File name of export file.</returns>
        public static string ExportMidi(string outPath, string baseFn, PatternInfo pattern, List<int> channels, int ppq)
        {
            // TODO export as zip?

            string name = Path.GetFileNameWithoutExtension(baseFn);

            var newfn = MakeExportFileName(outPath, baseFn, pattern.PatternName, "mid");

            // Init output file contents.
            MidiEventCollection outColl = new(1, ppq);
            IList<MidiEvent> outEvents = outColl.AddTrack();

            // Tempo.
            outEvents.Add(new TempoEvent(0, 0) { Tempo = pattern.Tempo });

            // General info.
            var info = $"Export {pattern.PatternName}";
            outEvents.Add(new TextEvent(info, MetaEventType.TextEvent, 0));

            // Optional.
            if (pattern.TimeSig != "")
            {
                // TODO figure out TimeSignatureEvent(0, 4, 2, (int)ticksPerClick, ppq).
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
            }

            // Gather the midi events for the pattern ordered by timestamp.
            var events = pattern.GetFilteredEvents(channels, true);
            events?.ForEach(e =>
            {
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
        /// Create a new clean filename for export. Creates path if it doesn't exist.
        /// </summary>
        /// <param name="path">Export path</param>
        /// <param name="mod">Modifier</param>
        /// <param name="ext">File extension</param>
        /// <returns></returns>
        static string MakeExportFileName(string path, string baseFn, string mod, string ext)
        {
            string name = Path.GetFileNameWithoutExtension(baseFn);

            // Clean the file name.
            name = name.Replace('.', '-').Replace(' ', '_');
            mod = mod.Replace(' ', '_');

            var newfn = Path.Join(path, $"{name}_{mod}.{ext}");
            return newfn;
        }
    }
}
