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
                "ScaledTime,AbsoluteTime,DeltaTime,Event,Channel,Content1,Content2"
            };

            // Any globals.
            global.ForEach(m => contentText.Add($"-1,0,0,Global,0,{m.Key}:{m.Value},"));

            // Midi events.
            foreach (PatternInfo pi in patterns)
            {
                contentText.Add($"0,0,0,Pattern,0,{pi.PatternName},tempo:{pi.Tempo}");
                //contentText.Add($"0,0,0,Pattern,0,name:{pi.PatternName},timesig:{pi.TimeSig},keysig:{pi.KeySig}");

                pi.GetValidChannels().ForEach(p =>
                {
                    var pname = drumChannelNumbers.Contains(p.number) ? MidiDefs.GetDrumKitName(p.patch) : MidiDefs.GetInstrumentName(p.patch);
                    contentText.Add($"0,0,0,Patch,{p.number}:{pname},,");
                });

                var descs = pi.GetFilteredEvents(channelNumbers);
                descs?.ForEach(evt => contentText.Add(Format(evt, drumChannelNumbers.Contains(evt.ChannelNumber))));
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
            pattern.GetValidChannels().ForEach(p =>
            {
                if(p.patch >= 0)
                {
                    outEvents.Add(new PatchChangeEvent(0, p.number, p.patch));
                }
            });

            // Gather the midi events for the pattern ordered by time.
            var events = pattern.GetFilteredEvents(channelNumbers);
            events?.ForEach(e =>
            {
                outEvents.Add(e.RawEvent);
            });

            // Add end track.
            long ltime = outEvents.Last().AbsoluteTime;
            var endt = new MetaEvent(MetaEventType.EndTrack, 0, ltime);
            outEvents.Add(endt);

            // Use NAudio function to create out file.
            MidiFile.Export(outFileName, outColl);
        }

        /// <summary>
        /// Format event for export.
        /// </summary>
        /// <param name="evtDesc">Event to format</param>
        /// <param name="isDrums"></param>
        /// <returns></returns>
        static string Format(MidiEventDesc evtDesc, bool isDrums)
        {
            string ret = "???";
            var mevt = evtDesc.RawEvent;

            // Boilerplate.
            string ntype = mevt.CommandCode == MidiCommandCode.MetaEvent ? (mevt as MetaEvent)!.MetaEventType.ToString() : mevt.CommandCode.ToString();
            string sc = $"{evtDesc.ScaledTime},{mevt.AbsoluteTime},{mevt.DeltaTime},{ntype},{mevt.Channel}";

            string NoteName(int nnum)
            {
                return isDrums ? MidiDefs.GetDrumName(nnum) : MusicDefinitions.NoteNumberToName(nnum);
            }

            string PatchName(int pnum)
            {
                return isDrums ? MidiDefs.GetDrumKitName(pnum) : MidiDefs.GetInstrumentName(pnum);
            }

            switch (mevt)
            {
                case NoteOnEvent evt:
                    //string slen = evt.OffEvent is null ? "?" : evt.NoteLength.ToString(); // NAudio NoteLength bug.
                    ret = $"{sc},{evt.NoteNumber}:{NoteName(evt.NoteNumber)},vel:{evt.Velocity}";
                    break;

                case NoteEvent evt: // used for NoteOff
                    ret = $"{sc},{evt.NoteNumber}:{NoteName(evt.NoteNumber)},vel:{evt.Velocity}";
                    break;

                case TempoEvent evt:
                    ret = $"{sc},tempo:{evt.Tempo},mspqn:{evt.MicrosecondsPerQuarterNote}";
                    break;

                case TimeSignatureEvent evt:
                    ret = $"{sc},timesig:{evt.TimeSignature},";
                    break;

                case KeySignatureEvent evt:
                    ret = $"{sc},sharpsflats:{evt.SharpsFlats},majorminor:{evt.MajorMinor}";
                    break;

                case PatchChangeEvent evt:
                    ret = $"{sc},{evt.Patch}:{PatchName(evt.Patch)},";
                    break;

                case ControlChangeEvent evt:
                    ret = $"{sc},{(int)evt.Controller}:{MidiDefs.GetControllerName((int)evt.Controller)},value:{evt.ControllerValue}";
                    break;

                case PitchWheelChangeEvent evt:
                    //otherText.Add($"{sc},pitch:{evt.Pitch},"); too busy?
                    break;

                case TextEvent evt:
                    ret = $"{sc},text:{evt.Text},datalen:{evt.Data.Length}";
                    break;

                case TrackSequenceNumberEvent evt:
                    ret = $"{sc},seq:{evt},";
                    break;

                //Others as needed:
                //case ChannelAfterTouchEvent:
                //case SysexEvent:
                //case MetaEvent:
                //case RawMetaEvent:
                //case SequencerSpecificEvent:
                //case SmpteOffsetEvent:
                default:
                    ret = $"{sc},other:???,,";
                    break;
            }

            return ret;
        }
    }
}
