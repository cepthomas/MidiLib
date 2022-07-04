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
        /// <param name="outFileName">Where to boss?</param>
        /// <param name="patterns">Specific patterns.</param>
        /// <param name="channels">Specific channnels or all if empty.</param>
        /// <param name="meta">File meta data to include.</param>
        public static void ExportAllEvents(string outFileName, List<PatternInfo> patterns, List<Channel> channels, Dictionary<string, int> meta)
        {
            var channelNumbers = channels.Select(ch => ch.ChannelNumber).ToList();

            List<string> contentText = new();
            if (meta.Any())
            {
                contentText.Add($"Meta,==================");
                meta.ForEach(m => contentText.Add($"{m.Key},{m.Value}"));
            }

            contentText.Add("AbsoluteTime,Event,Pattern,Channel,Content,==================");
            foreach(PatternInfo pi in patterns)
            {
                var descs = pi.GetFilteredEvents(channelNumbers);
                descs?.ForEach(evt => contentText.Add($"{evt.AbsoluteTime},{evt.MidiEvent!.GetType().ToString().Replace("NAudio.Midi.", "")}," +
                    $"{pi.PatternName},{evt.ChannelNumber},{evt.MidiEvent}"));
            }

            File.WriteAllLines(outFileName, contentText);
        }

        /// <summary>
        /// Makes csv dumps of some events grouped by pattern/channel. This is as the events appear in the original file.
        /// </summary>
        /// <param name="outFileName">Where to boss?</param>
        /// <param name="pattern">Specific pattern.</param>
        /// <param name="channels">Specific channnels or all if empty.</param>
        /// <param name="meta">File meta data to include.</param>
        /// <param name="includeAll">False if just notes or true if everything.</param>
        public static void ExportGroupedEvents(string outFileName, PatternInfo pattern, List<Channel> channels, Dictionary<string, int> meta, bool includeAll)
        {
            var channelNumbers = channels.Select(ch => ch.ChannelNumber).ToList();
            // Special handling of drums.
            var drumChannelNumbers = channels.Where(ch => ch.IsDrums).Select(ch => ch.ChannelNumber).ToHashSet();

            // Build meta info.
            List<string> metaText = new() { $"Meta,==================" };
            meta.ForEach(m => metaText.Add($"{m.Key},{m.Value}"));
            metaText.Add($"Pattern,{pattern.PatternName}");
            metaText.Add($"Tempo,{pattern.Tempo}");
            metaText.Add($"TimeSig,{pattern.TimeSig}");
            metaText.Add($"KeySig,{pattern.KeySig}");
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                if (pattern.Patches[i] >= 0)
                {
                    var sp = drumChannelNumbers.Contains(i+1) ? "Drums" : MidiDefs.GetInstrumentName(pattern.Patches[i]);
                    metaText.Add($"Patch{i+1},{sp}");
                }
            }

            // Build note info.
            List<string> notesText = new()
            {
                "Notes,==================",
                "AbsoluteTime,Channel,Event,NoteNum,NoteName,Velocity,Duration",
            };

            List<string> otherText = new()
            {
                "Other,==================",
                "AbsoluteTime,Channel,Event,Val1,Val2,Val3",
            };

            var descs = pattern.GetFilteredEvents(channelNumbers);
            descs?.ForEach(me =>
            {
                // Boilerplate.
                string ntype = me.MidiEvent!.GetType().ToString().Replace("NAudio.Midi.", "");
                string sc = $"{me.MidiEvent.AbsoluteTime},{me.MidiEvent.Channel},{ntype}";

                switch (me.MidiEvent)
                {
                    case NoteOnEvent evt:
                        int len = evt.OffEvent is null ? 0 : evt.NoteLength; // NAudio NoteLength bug.

                        string nname = drumChannelNumbers.Contains(me.MidiEvent.Channel) ?
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
                        string pname = drumChannelNumbers.Contains(me.MidiEvent.Channel) ?
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

                    //Others as needed:
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

            File.WriteAllLines(outFileName, metaText);
            File.AppendAllLines(outFileName, notesText);
            if (includeAll)
            {
                File.AppendAllLines(outFileName, otherText);
            }
        }

        /// <summary>
        /// Export pattern parts to individual midi files. This is as the events appear in the original file.
        /// </summary>
        /// <param name="outFileName">Where to boss?</param>
        /// <param name="pattern">Specific pattern.</param>
        /// <param name="channels">Specific channnels or all if empty.</param>
        /// <param name="meta">File meta data to include.</param>
        public static void ExportMidi(string outFileName, PatternInfo pattern, IEnumerable<Channel> channels, Dictionary<string, int> meta)
        {
            var channelNumbers = channels.Select(ch => ch.ChannelNumber).ToList();

            // Init output file contents.
            MidiEventCollection outColl = new(1, meta["DeltaTicksPerQuarterNote"]);
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
                if (pattern.Patches[i] >= 0)
                {
                    outEvents.Add(new PatchChangeEvent(0, i+1, pattern.Patches[i]));
                }
            }

            // Gather the midi events for the pattern ordered by time.
            var events = pattern.GetFilteredEvents(channelNumbers);
            events?.ForEach(e =>
            {
                outEvents.Add(e.MidiEvent);
            });

            // Add end track.
            long ltime = outEvents.Last().AbsoluteTime;
            var endt = new MetaEvent(MetaEventType.EndTrack, 0, ltime);
            outEvents.Add(endt);

            // Use NAudio function to create out file.
            MidiFile.Export(outFileName, outColl);
        }
    }
}
