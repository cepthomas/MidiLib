﻿using System;
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
                "ScaledTime,AbsoluteTime,DeltaTime,Event,Channel,Content,,"
            };

            // Any global.
            global.ForEach(m => contentText.Add($"-1,0,0,Global,0,{m.Key}:{m.Value},,"));

            // Midi events.
            foreach (PatternInfo pi in patterns)
            {
                contentText.Add($"0,0,0,Pattern,0,name:{pi.PatternName},tempo:{pi.Tempo},");
                contentText.Add($"0,0,0,Pattern,0,name:{pi.PatternName},timesig:{pi.TimeSig},keysig:{pi.KeySig}");

                pi.ValidPatches.ForEach(p =>
                {
                    var pname = drumChannelNumbers.Contains(p.Key) ? MidiDefs.GetDrumKitName(p.Value) : MidiDefs.GetInstrumentName(p.Value);
                    contentText.Add($"0,0,0,Patch,{p.Key},patch:{pname},,");
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

            // Tempo.
            outEvents.Add(new TempoEvent(0, 0) { Tempo = pattern.Tempo });

            // General info.
            var info = $"Export {pattern.PatternName}";
            outEvents.Add(new TextEvent(info, MetaEventType.TextEvent, 0));

            // Optional.
            if (pattern.TimeSig != "")
            {
                var parts = pattern.TimeSig.SplitByToken("/");
                if (parts.Count == 2)
                {
                    try
                    {
                        int num = int.Parse(parts[0]);
                        int denom = int.Parse(parts[1]);
                        switch(denom)
                        {
                            case 2: denom = 1; break;
                            case 4: denom = 2; break;
                            case 8: denom = 3; break;
                            case 16: denom = 4; break;
                            case 32: denom = 5; break;
                            default: throw new ArgumentException("Bad denominator");
                        }

                        outEvents.Add(new TimeSignatureEvent(0, num, denom, 2, 8));
                    }
                    catch { } // do something?
                }
            }

            // Optional.
            if (pattern.KeySig != "")
            {
                var parts = pattern.KeySig.SplitByToken(" ");
                int num = parts.Count;
                if (num > 2)
                {
                    try
                    {
                        int sharpsFlats = int.Parse(parts[num - 2]);
                        int majorMinor = int.Parse(parts[num - 1]);
                        outEvents.Add(new KeySignatureEvent(sharpsFlats, majorMinor, 0));
                    }
                    catch { } // do something?
                }
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
