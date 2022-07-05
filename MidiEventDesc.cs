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

        /// <summary>Time (subdivs) from original file.</summary>
        public long AbsoluteTime { get { return MidiEvent.AbsoluteTime; } }

        /// <summary>Time (subdivs) scaled to internal units using send PPQ.</summary>
        public int ScaledTime { get; set; } = -1;

        /// <summary>The raw midi event.</summary>
        public MidiEvent MidiEvent { get; init; }

        /// <summary>Normal constructor from NAudio event.</summary>
        public MidiEventDesc(MidiEvent evt)
        {
            MidiEvent = evt;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            return $"Ch:{ChannelNumber} Atime:{AbsoluteTime} Stime:{ScaledTime} Evt:{MidiEvent}";
        }
    }
}
