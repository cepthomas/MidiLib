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
    /// Internal representation of one midi event.
    /// </summary>
    public class MidiEventDesc
    {
        /// <summary>One-based channel number.</summary>
        public int ChannelNumber { get { return MidiEvent.Channel; } }

        /// <summary>Associated channel na</summary>
        public string ChannelName { get; }

        /// <summary>Time (subdivs) from original file.</summary>
        public long AbsoluteTime { get { return MidiEvent.AbsoluteTime; } }

        /// <summary>Time (subdivs) scaled to internal units using send PPQ.</summary>
        public int ScaledTime { get; set; } = -1;

        /// <summary>The raw midi event.</summary>
        public MidiEvent MidiEvent { get; init; }

        /// <summary>Normal constructor from NAudio event.</summary>
        public MidiEventDesc(MidiEvent evt, string channelName)
        {
            MidiEvent = evt;
            ChannelName = channelName;
        }

        public string Format(bool IsDrums)
        {
            string ret = "???";

            // Boilerplate.
            string ntype = MidiEvent.CommandCode == MidiCommandCode.MetaEvent ? (MidiEvent as MetaEvent)!.MetaEventType.ToString() : MidiEvent.CommandCode.ToString();
            string sc = $"{ScaledTime},{MidiEvent.AbsoluteTime},{MidiEvent.DeltaTime},{ntype},{MidiEvent.Channel}";

            string NoteName(int nnum)
            {
                return IsDrums ? MidiDefs.GetDrumName(nnum) : MusicDefinitions.NoteNumberToName(nnum);
            }

            string PatchName(int pnum)
            {
                return IsDrums ? MidiDefs.GetDrumKitName(pnum) : MidiDefs.GetInstrumentName(pnum);
            }

            switch (MidiEvent)
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
                    ret = $"{sc},patch:{evt.Patch},name:{PatchName(evt.Patch)}";
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

        /// <summary>Read </summary>
        public override string ToString()
        {
            string ntype = MidiEvent.CommandCode == MidiCommandCode.MetaEvent ? (MidiEvent as MetaEvent)!.MetaEventType.ToString() : MidiEvent.CommandCode.ToString();
            string ret = $"Ch:{ChannelName}({ChannelNumber}) AbsoluteTime:{AbsoluteTime} ScaledTime:{ScaledTime} MidiEvent:{ntype}";
            return ret;
        }
    }
}
