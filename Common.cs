using System;
using System.Collections.Generic;
using System.Linq;


namespace Ephemera.MidiLib
{
    /// <summary>Library error.</summary>
    public class MidiLibException(string message) : Exception(message) { }

    /// <summary>User selection options.</summary>
    public enum SnapType { Tick, Beat, Bar, FourBar }

    /// <summary>Misc definitions.</summary>
    public class VolumeDefs
    {
        /// <summary>Default value.</summary>
        public const double DEFAULT_VOLUME = 0.8;

        /// <summary>Allow UI controls some more headroom.</summary>
        public const double MAX_VOLUME = 2.0;
    }

    public class MidiUtils
    {
        /// <summary>
        /// Convert a midi dictionary into ordered list of strings.
        /// </summary>
        /// <param name="source">The dictionary to process</param>
        /// <param name="addKey">Add the index number to the entry</param>
        /// <param name="fill">Add mising midi values</param>
        /// <returns></returns>
        public static List<string> CreateOrderedMidiList(Dictionary<int, string> source, bool addKey, bool fill)
        {
            List<string> res = [];

            for (int i = 0; i < MidiDefs.MAX_MIDI; i++)
            {
                if (source.ContainsKey(i))
                {
                    res.Add(addKey ? $"{i:000} {source[i]}" : $"{source[i]}");
                }
                else if (fill)
                {
                    res.Add($"{i:000}");
                }
            }

            return res;
        }
    }
}
