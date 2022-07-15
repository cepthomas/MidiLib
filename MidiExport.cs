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
        public static void ExportCsv(string outFileName, IEnumerable<PatternInfo> patterns, IEnumerable<Channel> channels, Dictionary<string, int> meta)
        {
            var channelNumbers = channels.Select(ch => ch.ChannelNumber).ToList();
            var drumChannelNumbers = channels.Where(ch => ch.IsDrums).Select(ch => ch.ChannelNumber).ToHashSet();

            List<string> contentText = new();

            // Any meta.
            meta.ForEach(m => contentText.Add($"-1,Meta,{m.Key},{m.Value}"));

            // Midi events.
            foreach (PatternInfo pi in patterns)
            {
                //contentText.Add($"Pattern,{pattern.PatternName}");
                //contentText.Add($"Tempo,{pattern.Tempo}");
                //contentText.Add($"TimeSig,{pattern.TimeSig}");
                //contentText.Add($"KeySig,{pattern.KeySig}");
                contentText.Add($"-1,Pattern,name:{pi.PatternName},");
                contentText.Add("ScaledTime,AbsoluteTime,DeltaTime,Event,Channel,,,");
                var descs = pi.GetFilteredEvents(channelNumbers);
                descs?.ForEach(evt => contentText.Add(evt.Format(drumChannelNumbers.Contains(evt.ChannelNumber))));
            }

            File.WriteAllLines(outFileName, contentText);
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
