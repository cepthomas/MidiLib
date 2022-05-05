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
        /// <summary>Special value for Patches.</summary>
        public const int NO_PATCH = -1;

        /// <summary>Pattern name. Empty indicates single pattern aka plain midi file.</summary>
        public string Name { get; set; } = "";

        /// <summary>Tempo, if supplied by file. Default indicates invalid which will be filled in during read.</summary>
        public int Tempo { get; set; } = 0;

        /// <summary>Time signature, if supplied by file.</summary>
        public string TimeSig { get; set; } = "";

        /// <summary>Key signature, if supplied by file.</summary>
        public string KeySig { get; set; } = "";

        /// <summary>All the channel patches. Index is 0-based channel number.</summary>
        public PatchInfo[] Patches { get; set; } = new PatchInfo[MidiDefs.NUM_CHANNELS];

        /// <summary>Normal constructor.</summary>
        public PatternInfo()
        {
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                Patches[i] = new();
            }
        }

        /// <summary>Readable version.</summary>
        /// <returns></returns>
        public override string ToString()
        {
            List<string> content = new();
            content.Add($"Name:{(Name == "" ? "None" : Name)}");
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
                switch(Patches[i].Modifier)
                {
                    case PatchInfo.PatchModifier.NotAssigned:
                        // Ignore.
                        break;

                    case PatchInfo.PatchModifier.None:
                        content.Add($"Ch:{i + 1} Patch:{MidiDefs.GetInstrumentDef(Patches[i].Patch)}");
                        break;

                    case PatchInfo.PatchModifier.IsDrums:
                        content.Add($"Ch:{i + 1} Patch:IsDrums");
                        break;
                }
            }

            return string.Join(' ', content);
        }
    }

    /// <summary>Properties associated with a pattern patch.</summary>
    public class PatchInfo
    {
        public enum PatchModifier
        {
            None,           // Normal, Patch is valid.
            NotAssigned,    // Patch is unknown.
            IsDrums         // Patch is actually drums, treat as such on output.
        }

        /// <summary>Patch may not be what you think.</summary>
        public PatchModifier Modifier { get; set; } = PatchModifier.NotAssigned;

        /// <summary>The channel patch number.</summary>
        public int Patch { get; set; } = 0;
    }
}