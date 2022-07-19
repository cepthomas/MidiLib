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
        /// <param name="global">File meta data to include.</param>
        public static void ExportCsv(string outFileName, IEnumerable<PatternInfo> patterns, IEnumerable<Channel> channels, Dictionary<string, int> global)
        {
            var channelNumbers = channels.Select(ch => ch.ChannelNumber).ToList();
            var drumChannelNumbers = channels.Where(ch => ch.IsDrums).Select(ch => ch.ChannelNumber).ToHashSet();

            // Header
            List<string> contentText = new()
            {
                "ScaledTime,AbsoluteTime,DeltaTime,Event,Channel,Content1,Content2"
            };

            // Any globals.
            global.ForEach(m => contentText.Add($"-1,0,0,Global,0,{m.Key}:{m.Value},"));

            // Midi events.
            foreach (PatternInfo pi in patterns)
            {
                contentText.Add($"0,0,0,Pattern,0,{pi.PatternName},");
                //contentText.Add($"0,0,0,Pattern,0,name:{pi.PatternName},tempo:{pi.Tempo},");
                //contentText.Add($"0,0,0,Pattern,0,name:{pi.PatternName},timesig:{pi.TimeSig},keysig:{pi.KeySig}");

                pi.ValidChannels.ForEach(p =>
                {
                    var pname = drumChannelNumbers.Contains(p.Key) ? MidiDefs.GetDrumKitName(p.Value) : MidiDefs.GetInstrumentName(p.Value);
                    contentText.Add($"0,0,0,Patch,{p.Key},patch:{pname},");
                });

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
        /// <param name="global">File meta data to include.</param>
        public static void ExportMidi(string outFileName, PatternInfo pattern, IEnumerable<Channel> channels, Dictionary<string, int> global)
        {
            var channelNumbers = channels.Select(ch => ch.ChannelNumber).ToList();

            // Init output file contents.
            int ppq = global["DeltaTicksPerQuarterNote"];
            MidiEventCollection outColl = new(1, ppq);
            IList<MidiEvent> outEvents = outColl.AddTrack();

            // Build the event collection.
            outEvents.Add(new TempoEvent(0, 0) { Tempo = pattern.Tempo });
            outEvents.Add(new TextEvent($"Export {pattern.PatternName}", MetaEventType.TextEvent, 0));

            if (pattern.TimeSigNumerator != -1 && pattern.TimeSigDenominator != -1)
            {
                outEvents.Add(new TimeSignatureEvent(0, pattern.TimeSigNumerator, pattern.TimeSigDenominator, 2, 8));
            }

            if(pattern.KeySigSharpsFlats != -1 && pattern.KeySigMajorMinor != -1)
            {
                outEvents.Add(new KeySignatureEvent(pattern.KeySigSharpsFlats, pattern.KeySigMajorMinor, 0));
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
