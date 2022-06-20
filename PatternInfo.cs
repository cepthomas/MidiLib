using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using NAudio.Midi;


namespace MidiLib
{
    /// <summary>Properties associated with a pattern.</summary>
    public class PatternInfo
    {
        /// <summary>Pattern name. Empty indicates single pattern aka plain midi file.</summary>
        public string PatternName { get; set; } = "";

        /// <summary>Tempo, if supplied by file. Default indicates invalid which will be filled in during read.</summary>
        public int Tempo { get; set; } = 0;

        /// <summary>Time signature, if supplied by file.</summary>
        public string TimeSig { get; set; } = "";

        /// <summary>Key signature, if supplied by file.</summary>
        public string KeySig { get; set; } = "";

        /// <summary>All the channel patches. Index is 0-based, not channel number.</summary>
        public int[] Patches { get; set; } = new int[MidiDefs.NUM_CHANNELS];

        ///// <summary>All the pattern midi events, (usually) ordered by time.</summary>
        // TODOX Consider PatternInfo holding the events. _patternDefaults holds the all set + default pattern. Remove EventDesc.PatternName.
        //public List<EventDesc> AllEvents { get; private set; } = new();

        /// <summary>Normal constructor.</summary>
        public PatternInfo()
        {
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                Patches[i] = -1;
            }
        }

        /// <summary>Readable version.</summary>
        /// <returns></returns>
        public override string ToString()
        {
            List<string> content = new();
            content.Add($"Name:{(PatternName == "" ? "None" : PatternName)}");
            content.Add($"Tempo:{Tempo}");

            if (TimeSig != "")
            {
                content.Add($"TimeSig:{TimeSig}");
            }

            if (KeySig != "")
            {
                content.Add($"KeySig:{KeySig}");
            }

            for(int i = 0; i <MidiDefs.NUM_CHANNELS; i++)
            {
                int chnum = i + 1;
                if(Patches[i] >= 0)
                {
                    content.Add($"Ch:{chnum} Patch:{MidiDefs.GetInstrumentName(Patches[i])}");
                }
            }

            return string.Join(' ', content);
        }
    }
}