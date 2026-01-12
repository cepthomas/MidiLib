using System;
using System.Collections.Generic;
using System.Linq;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    public class MidiDefs
    {
        #region Fields
        /// <summary>Midi constant.</summary>
        public const int MAX_MIDI = 127;

        /// <summary>Per device.</summary>
        public const int NUM_CHANNELS = 16;

        /// <summary>The normal drum channel.</summary>
        public const int DEFAULT_DRUM_CHANNEL = 10;

        /// <summary>All the GM controllers. TODO future custom list like instruments?</summary>
        static readonly Dictionary<int, string> _controllerIds = [];

        /// <summary>Standard set plus unnamed ones.</summary>
        static readonly Dictionary<int, string> _controllerIdsAll = [];

        /// <summary>All the GM drums.</summary>
        static readonly Dictionary<int, string> _drums = [];
        #endregion

        #region Lifecycle
        /// <summary>Initialize some collections.</summary>
        static MidiDefs()
        {
            var ir = new IniReader();
            ir.ParseString(Properties.Resources.gm_defs);

            // Populate the defs.
            DoSection("controllers", _controllerIds);
            DoSection("drums", _drums);

            void DoSection(string section, Dictionary<int, string> target)
            {
                ir.GetValues(section).ForEach(kv =>
                {
                    int index = int.Parse(kv.Key); // can throw
                    if (index < 0 || index > MAX_MIDI) { throw new InvalidOperationException($"Invalid section {section}"); }
                    target[index] = kv.Value.Length > 0 ? kv.Value : "";
                });
            }
        }
        #endregion

        #region Public
        /// <summary>
        /// Get controller name. Throws if invalid.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The controller name or a fabricated one if unknown.</returns>
        public static string GetControllerName(int id)
        {
            if (id is < 0 or > MAX_MIDI) { throw new ArgumentOutOfRangeException($"Controller:{id}"); }

            return _controllerIds.TryGetValue(id, out string? value) ? value : $"CTLR_{id}";
        }

        /// <summary>
        /// Get drum name. Throws if invalid.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The drum name or a fabricated one if unknown.</returns>
        public static string GetDrumName(int id)
        {
            if (id is < 0 or > MAX_MIDI) { throw new ArgumentOutOfRangeException($"Drum:{id}"); }

            return _drums.TryGetValue(id, out string? value) ? value : $"DRUM_{id}";
        }

        /// <summary>
        /// Get corresponding number.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>The id or -1 if not valid</returns>
        public static int GetControllerId(string name)
        {
            var i = _controllerIds.Where(v => v.Value == name);
            return i.Any() ? i.First().Key : -1;
        }

        /// <summary>
        /// Get corresponding number.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>The id or -1 if not valid</returns>
        public static int GetDrumId(string name)
        {
            var i = _drums.Where(v => v.Value == name);
            return i.Any() ? i.First().Key : -1;
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Make content from the definitions.
        /// </summary>
        /// <returns>Content.</returns>
        public static List<string> GenMarkdown()
        {
            var ir = new IniReader();
            ir.ParseString(Properties.Resources.gm_defs);

            List<string> ls = [];
            ls.Add("# Midi GM Instruments");
            ls.Add("|Instrument   | Number|");
            ls.Add("|----------   | ------|");
            ir.GetValues("instruments").ForEach(kv => { ls.Add($"|{kv.Value}|{kv.Key}|"); });
            ls.Add("");

            ls.Add("# Midi GM Controllers");
            ls.Add("- Undefined: 3, 9, 14-15, 20-31, 85-90, 102-119");
            ls.Add("- For most controllers marked on/off, on=127 and off=0");
            ls.Add("|Controller   | Number|");
            ls.Add("|----------   | ------|");
            ir.GetValues("controllers").ForEach(kv => { ls.Add($"|{kv.Value}|{kv.Key}|"); });
            ls.Add("");

            ls.Add("# Midi GM Drums");
            ls.Add("- These will vary depending on your Soundfont file.");
            ls.Add("|Drum         | Number|");
            ls.Add("|----         | ------|");
            ir.GetValues("drums").ForEach(kv => { ls.Add($"|{kv.Value}|{kv.Key}|"); });
            ls.Add("");

            ls.Add("# Midi GM Drum Kits");
            ls.Add("- These will vary depending on your Soundfont file.");
            ls.Add("|Kit          | Number|");
            ls.Add("|---          | ------|");
            ir.GetValues("drumkits").ForEach(kv => { ls.Add($"|{kv.Value}|{kv.Key}|"); });
            ls.Add("");

            return ls;
        }

        /// <summary>
        /// Make content from the definitions.
        /// </summary>
        /// <returns>Content.</returns>
        public static List<string> GenLua()
        {
            var ir = new IniReader();
            ir.ParseString(Properties.Resources.gm_defs);

            List<string> ls = [];
            ls.Add("-------------- GM midi definitions ----------------");
            ls.Add("-- Autogenerated from midi_defs.ini -- do not edit!");

            ls.Add("");
            ls.Add("local M = {}");
            ls.Add("");
            ls.Add("M.MAX_MIDI = 127");
            ls.Add("M.NO_PATCH = -1");

            ls.Add("");
            ls.Add("-- Instruments");
            ls.Add("M.instruments =");
            ls.Add("{");
            ir.GetValues("instruments").ForEach(kv => ls.Add($"    {kv.Value} = {kv.Key},"));
            ls.Add("}");

            ls.Add("");
            ls.Add("-- Controllers");
            ls.Add("M.controllers =");
            ls.Add("{");
            ir.GetValues("controllers").ForEach(kv => ls.Add($"    {kv.Value} = {kv.Key},"));
            ls.Add("}");

            ls.Add("");
            ls.Add("-- Drums");
            ls.Add("M.drums =");
            ls.Add("{");
            ir.GetValues("drums").ForEach(kv => ls.Add($"    {kv.Value} = {kv.Key},"));
            ls.Add("}");

            ls.Add("");
            ls.Add("-- Drum kits");
            ls.Add("M.drum_kits =");
            ls.Add("{");
            ir.GetValues("drumkits").ForEach(kv => ls.Add($"    {kv.Value} = {kv.Key},"));
            ls.Add("}");

            ls.Add("");
            ls.Add("return M");

            return ls;
        }

        /// <summary>
        /// Make content from user configuration.
        /// </summary>
        /// <returns>Content.</returns>
        public static List<string> GenUserDeviceInfo()
        {
            // Show them what they have.
            var outs = MidiOutputDevice.GetAvailableDevices();
            var ins = MidiInputDevice.GetAvailableDevices();

            List<string> ls = [];
            ls.Add($"# Your Midi Devices");

            ls.Add($"## Inputs");
            if (!ins.Any()) { ls.Add($"None"); }
            else { ins.ForEach(d => ls.Add($"[{d}]")); }

            ls.Add($"## Outputs");
            if (!outs.Any()) { ls.Add($"None"); }
            else { outs.ForEach(d => ls.Add($"[{d}]")); }

            return ls;
        }

        /// <summary>
        /// Convert a midi dictionary into ordered list of strings. TODO doesn't really belong here.
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
        #endregion
    }
}
