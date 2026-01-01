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

        /// <summary>All the GM instruments.</summary>
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
        /// <summary>
        /// Get controller name.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The controller name or a fabricated one if unknown.</returns>
        public string GetInstrumentName(int id)
        {
            if (id is < 0 or > MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(id)); }

            return _instruments.TryGetValue(id, out string? value) ? value : $"INST_{id}";
        }

        /// <summary>
        /// Get controller name. Throws if invalid.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The controller name or a fabricated one if unknown.</returns>
        public string GetControllerName(int id)
        {
            if (id is < 0 or > MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(id)); }

            return _controllerIds.TryGetValue(id, out string? value) ? value : $"CTLR_{id}";
        }

        /// <summary>
        /// Get drum name. Throws if invalid.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The drum name or a fabricated one if unknown.</returns>
        public string GetDrumName(int id)
        {
            if (id is < 0 or > MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(id)); }

            return _drums.TryGetValue(id, out string? value) ? value : $"DRUM_{id}";
        }

        /// <summary>
        /// Get GM drum kit name. Throws if invalid.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The drumkit name or a fabricated one if unknown.</returns>
        public string GetDrumKitName(int id)
        {
            if (id is < 0 or > MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(id)); }

            return _drumKits.TryGetValue(id, out string? value) ? value : $"DKIT_{id}";
        }

        /// <summary>
        /// Get corresponding number. Throws if invalid.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetInstrumentNumber(string name)// TODO1 all these
        {
            if (_instruments)

            return -1;
            //if (id is < 0 or > MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(id)); }

            //return _drumKits.TryGetValue(id, out string? value) ? value : $"DKIT_{id}";
        }

        /// <summary>
        /// Get corresponding number. Throws if invalid.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetControllerNumber(string name)
        {
            return -1;
            //if (id is < 0 or > MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(id)); }
            //return _drumKits.TryGetValue(id, out string? value) ? value : $"DKIT_{id}";
        }

        /// <summary>
        /// Get corresponding number. Throws if invalid.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetDrumNumber(string name)
        {
            return -1;
            //if (id is < 0 or > MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(id)); }
            //return _drumKits.TryGetValue(id, out string? value) ? value : $"DKIT_{id}";
        }

        /// <summary>
        /// Get corresponding number. Throws if invalid.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetDrumKitNumber(string name)
        {
            return -1;
            //if (id is < 0 or > MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(id)); }
            //return _drumKits.TryGetValue(id, out string? value) ? value : $"DKIT_{id}";
        }
        #endregion
    }
}
