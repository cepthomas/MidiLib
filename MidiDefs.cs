using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    public class MidiDefs
    {
        #region Global collection
        /// <summary>The singleton instance.</summary>
        public static MidiDefs Instance { get { _instance ??= new MidiDefs(); return _instance; } }

        /// <summary>The singleton instance.</summary>
        static MidiDefs? _instance;
        #endregion

        #region Fields
        /// <summary>Midi constant.</summary>
        public const int MAX_MIDI = 127;

        /// <summary>Per device.</summary>
        public const int NUM_CHANNELS = 16;

        /// <summary>The normal drum channel.</summary>
        public const int DEFAULT_DRUM_CHANNEL = 10;

        /// <summary>All the GM instruments - default.</summary>
        readonly Dictionary<int, string> _instruments = [];

        /// <summary>All the GM controllers.</summary>
        readonly Dictionary<int, string> _controllerIds = [];

        /// <summary>Standard set plus unnamed ones.</summary>
        readonly Dictionary<int, string> _controllerIdsAll = [];

        /// <summary>All the GM drums.</summary>
        readonly Dictionary<int, string> _drums = [];

        /// <summary>All the GM drum kits.</summary>
        readonly Dictionary<int, string> _drumKits = [];
        #endregion

        #region Lifecycle
        /// <summary>Initialize some collections.</summary>
        MidiDefs()
        {
            var ir = new IniReader();
            ir.ParseString(Properties.Resources.gm_defs);

            // Populate the defs.
            DoSection("instruments", _instruments);
            DoSection("controllers", _controllerIds);
            DoSection("drums", _drums);
            DoSection("drumkits", _drumKits);

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
        /// <summary>Default instruments.</summary>
        /// <returns></returns>
        public Dictionary<int, string> GetDefaultInstrumentDefs()
        {
            return _instruments;
        }

        /// <summary>All controllers.</summary>
        /// <param name="includeUnnamed"></param>
        /// <returns></returns>
        public Dictionary<int, string> GetControllerIdDefs(bool includeUnnamed)
        {
            return includeUnnamed ? _controllerIdsAll : _controllerIds;
        }

        /// <summary>
        /// Get drum name.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The drum name or a fabricated one if unknown.</returns>
        public string GetDrumName(int which)
        {
            if (which is < 0 or > MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(which)); }

            return _drums.TryGetValue(which, out string? value) ? value : $"DRUM_{which}";
        }

        /// <summary>
        /// Get controller name.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The controller name or a fabricated one if unknown.</returns>
        public string GetControllerName(int which)
        {
            if (which is < 0 or > MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(which)); }

            return _controllerIds.TryGetValue(which, out string? value) ? value : $"CTLR_{which}";
        }

        /// <summary>
        /// Get GM drum kit name.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The drumkit name or a fabricated one if unknown.</returns>
        public string GetDrumKitName(int which)
        {
            if (which is < 0 or > MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(which)); }

            return _drumKits.TryGetValue(which, out string? value) ? value : $"KIT_{which}";
        }

        /// <summary>
        /// Make content from the definitions.
        /// </summary>
        /// <returns>Content.</returns>
        public string GenMarkdown(string fn)
        {
            var ir = new IniReader();
            ir.ParseString(Properties.Resources.gm_defs);

            List<string> ls = [];
            ls.Add("# Midi GM Instruments");
            ls.Add("|Instrument          | Number|");
            ls.Add("|----------          | ------|");
            ir.GetValues("instruments").ForEach(kv => { ls.Add($"|{kv.Value}|{kv.Key}|"); });
            ls.Add("");

            ls.Add("# Midi GM Controllers");
            ls.Add("- Undefined: 3, 9, 14-15, 20-31, 85-90, 102-119");
            ls.Add("- For most controllers marked on/off, on=127 and off=0");
            ls.Add("|Controller          | Number|");
            ls.Add("|----------          | ------|");
            ir.GetValues("controllers").ForEach(kv => { ls.Add($"|{kv.Value}|{kv.Key}|"); });
            ls.Add("");

            ls.Add("# Midi GM Drums");
            ls.Add("- These will vary depending on your Soundfont file.");
            ls.Add("|Drum                | Number|");
            ls.Add("|----                | ------|");
            ir.GetValues("drums").ForEach(kv => { ls.Add($"|{kv.Value}|{kv.Key}|"); });
            ls.Add("");

            ls.Add("# Midi GM Drum Kits");
            ls.Add("- These will vary depending on your Soundfont file.");
            ls.Add("|Kit        | Number|");
            ls.Add("|---        | ------|");
            ir.GetValues("drumkits").ForEach(kv => { ls.Add($"|{kv.Value}|{kv.Key}|"); });
            ls.Add("");

            return string.Join(Environment.NewLine, ls);
        }

        /// <summary>
        /// Make content from the definitions.
        /// </summary>
        /// <returns>Content.</returns>
        public string GenLua(string fn)
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

            return string.Join(Environment.NewLine, ls);
        }
        #endregion
    }
}
