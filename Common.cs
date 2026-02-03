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

    /// <summary>Both ways luts.</summary>
    public class BiLut
    {
        /// <summary>Both ways luts.</summary>
        Dictionary<int, string> _lut1 = [];
        Dictionary<string, int> _lut2 = [];

        /// <summary>Iterate everything.</summary>
        public IEnumerable<KeyValuePair<int, string>> Contents { get { return _lut1.AsEnumerable(); } }

        /// <summary>Add entry.</summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        public void Add(int id, string name)
        {
            _lut1.Add(id, name);
            _lut2.Add(name, id);
        }

        /// <summary>Get name.</summary>
        /// <param name="id"></param>
        /// <returns>The name if valid else throws.</returns>
        public string GetName(int id)
        {
            try { return _lut1[id]; }
            catch { throw new ArgumentException($"Invalid id:{id}"); }
        }

        /// <summary>Get id.</summary>
        /// <param name="name"></param>
        /// <returns>The id if valid else -1.</returns>
        public int GetId(string name)
        {
            try { return _lut2[name]; }
            catch { return -1; }
        }
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
