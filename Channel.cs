using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows.Forms.Design;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Ephemera.MidiLib
{
    /// <summary>Describes one output channel.</summary>
    public class Channel
    {
        #region Fields
        /// <summary>Associated device.</summary>
        readonly IOutputDevice _device;

        /// <summary>Channel instrument patches. Optionally from alias file.</summary>
        BiLut _instruments = MidiDefs.Instruments;

        /// <summary>Channel controllers.</summary>
        BiLut _controllers = MidiDefs.Controllers;

        /// <summary>Channel drums.</summary>
        BiLut _drums = MidiDefs.Drums;

        /// <summary>Channel drum kits.</summary>
        BiLut _drumKits = MidiDefs.DrumKits;
        #endregion

        #region Properties - configuration
        /// <summary>Actual 1-based midi channel number. Default is invalid or unknown.</summary>
        public int ChannelNumber { get; init; } = 0;

        /// <summary>Instrument names. Optional.</summary>
        public string AliasFile { get; set; } = "";

        /// <summary>Handle for use by scripts.</summary>
        public ChannelHandle Handle { get; init; }
        #endregion

        #region Properties - runtime
        /// <summary>True if channel is not muted.</summary>
        public bool EnableSound { get; set; } = true;

        /// <summary>Instrument/patch.</summary>
        public int Patch { get; set; } = 0;

        /// <summary>Patch is a drumkit.</summary>
        public bool IsDrums { get; private set; } = false;

        /// <summary>Current instrument/patch name. Set may update internal collection and sends a midi patch message.</summary>
        public string? PatchName
        {
            get
            {
                if (IsDrums) { return _drumKits.GetName(Patch) ?? $"Kit {Patch}"; }
                else { return _instruments.GetName(Patch) ?? $"Inst {Patch}"; }
            }
            set
            {
                if (value is null) { throw new ArgumentException($"Invalid patch:null"); }
                var id = _instruments.GetId(value);
                if (id is null) { throw new ArgumentException($"Invalid patch:{value}"); }
                Patch = (int)id;
                _device.Send(new Patch(ChannelNumber, Patch));
            }
        }

        /// <summary>Associated events - optional depending on implementation.</summary>
        public EventCollection Events { get; set; } = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor for channel.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="channelNumber"></param>
        public Channel(IOutputDevice device, int channelNumber)
        {
            if (channelNumber is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"channelNumber:{(channelNumber)}"); }

            _device = device;
            ChannelNumber = channelNumber;
            Handle = new(device.Id, ChannelNumber, true);
        }

        /// <summary>
        /// Human readable.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Ch {ChannelNumber} on {_device.DeviceName}";
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Init instrument/patches from patch name.
        /// </summary>
        /// <param name="patchName"></param>
        /// <param name="aliasFile">Custom patch names for this channel</param>
        public void InitInstruments(string patchName, string? aliasFile = null)
        {
            InitDefs(aliasFile);

            Patch = -1;

            // Is it drums?
            IsDrums = false;
            var id = _drumKits.GetId(patchName);
            if (id is not null) // GM drum kit
            {
                IsDrums = true;
                Patch = (int)id;
            }
            else // assume normal instroument
            {
                id = _instruments.GetId(patchName);
                if (id is not null) // plain GM instrument
                {
                    IsDrums = false;
                    Patch = (int)id;
                }
                else // unknown
                {
                    throw new ArgumentException($"Invalid instrument:{patchName}");
                }
            }

            if (Patch is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"Unresolved patch:{patchName}"); }

            _device.Send(new Patch(ChannelNumber, Patch));
        }

        /// <summary>
        /// Init instrument/patches from patch number.
        /// </summary>
        /// <param name="patchNum"></param>
        /// <param name="isDrums">Client has to specify this</param>
        /// <param name="aliasFile">Optional custom patch names for this channel</param>
        public void InitInstruments(int patchNum, bool isDrums, string? aliasFile = null)
        {
            InitDefs(aliasFile);

            Patch = patchNum;
            IsDrums = isDrums;

            if (Patch is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"Unresolved patch:{Patch}"); }

            _device.Send(new Patch(ChannelNumber, Patch));
        }

        /// <summary>
        /// Get instrument name from id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The instrument name or a fabricated one if unknown.</returns>
        public string GetInstrumentName(int id)
        {
            if (id is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"Instrument:{id}"); }

            string? name;

            if (IsDrums)
            {
                name = MidiDefs.DrumKits.GetName(id);
            }
            else
            {
                name = _instruments.GetName(id);
            }

            //if (name is null) { throw new ArgumentException($"Invalid instrument id:{id}"); }
            if (name is null) { name = $"Patch_{id}"; }

            return name;
        }

        /// <summary>
        /// Send a midi message. Honors muting. Assumes host has fixed up volume etc.
        /// </summary>
        /// <param name="evt"></param>
        public void Send(BaseEvent evt)
        {
            if (EnableSound || evt is Controller || evt is Patch || evt is Other)
            {
                evt.ChannelNumber = ChannelNumber;
                _device.Send(evt);
            }
        }

        /// <summary>
        /// Hard stop the channel.
        /// </summary>
        public void Kill()
        {
            int cc = 123; // => AllNotesOff
            Send(new Controller(ChannelNumber, cc, 0));
        }
        #endregion

        /// <summary>
        /// Client may specify custom patch names. Otherwise use GM defaults.
        /// </summary>
        /// <param name="aliasFile"></param>
        /// <exception cref="MidiLibException"></exception>
        void InitDefs(string? aliasFile = null)
        {
            if (aliasFile is not null) // explicit defs file otherwise use defaults
            {
                try
                {
                    var ir = new IniReader();
                    ir.ParseFile(aliasFile);

                    var defs = MidiDefs.ReadDefs(ir, "instruments");
                    if (defs is not null) { _instruments = defs; }

                    defs = MidiDefs.ReadDefs(ir, "controllers");
                    if (defs is not null) { _controllers = defs; }

                    defs = MidiDefs.ReadDefs(ir, "drums");
                    if (defs is not null) { _drums = defs; }

                    defs = MidiDefs.ReadDefs(ir, "drumkits");
                    if (defs is not null) { _drumKits = defs; }
                }
                catch (Exception ex)
                {
                    throw new MidiLibException($"Failed to load alias file {aliasFile}: {ex.Message}");
                }
            }
        }
    }
}
