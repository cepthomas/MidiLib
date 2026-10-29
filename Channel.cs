using System;
using System.Collections.Generic;
using System.Linq;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    #region Types
    /// <summary>Channel handle for use in scripts.</summary>
    public static class HandleOps
    {
        const int OUTPUT_FLAG = 0x0800;

        public static int Create(int deviceId, int channelNumber, bool output)
        {
            return (deviceId << 4) | channelNumber | (output ? OUTPUT_FLAG : OUTPUT_FLAG);
        }

        public static int DeviceId(int handle) { return (handle >> 4) & 0x0F; }
        public static int ChannelNumber(int handle) { return handle & 0x0F; }
        public static bool Output(int handle) { return (handle & OUTPUT_FLAG) > 0; }

        public static string Format(int handle)
        {
            return $"{(Output(handle) ? "out" : "in")} {ChannelNumber(handle)} on D{DeviceId(handle)}";
        }
    }
    #endregion

    //----------------------------------------------------------------
    /// <summary>Describes one midi input channel.</summary>
    public class InputChannel
    {
        #region Fields
        /// <summary>Associated device.</summary>
        readonly IInputDevice _device;
        #endregion

        #region Properties
        /// <summary>Channel name - optional.</summary>
        public string ChannelName { get; set; } = "";

        /// <summary>Actual 1-based midi channel number. Required.</summary>
        public int ChannelNumber { get; init; } = 0;// MidiDefs.TEMP_CHANNEL;

        /// <summary>True if channel is active.</summary>
        public bool Enable { get; set; } = true;

        /// <summary>Handle for use by scripts.</summary>
        public int Handle { get; init; }
        #endregion

        /// <summary>
        /// Constructor with required args.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="channelNumber"></param>
        public InputChannel(IInputDevice device, int channelNumber)
        {
            if (channelNumber is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"channelNumber:{(channelNumber)}"); }

            _device = device;
            ChannelNumber = channelNumber;
            Handle = HandleOps.Create(device.Id, ChannelNumber, false);
        }
    }

    //----------------------------------------------------------------
    /// <summary>Describes one midi output channel.</summary>
    public class OutputChannel
    {
        #region Fields
        /// <summary>Associated device.</summary>
        readonly IOutputDevice _device;

        /// <summary>Channel instruments. May be from alias file.</summary>
        BiLut _instruments = MidiDefs.Instruments;

        /// <summary>Channel controllers.</summary>
        BiLut _controllers = MidiDefs.Controllers;

        /// <summary>Channel drums.</summary>
        BiLut _drums = MidiDefs.Drums;

        /// <summary>Channel drum kits.</summary>
        BiLut _drumKits = MidiDefs.DrumKits;

        // Backing fields.
        double _volume = VolumeDefs.DEFAULT_VOLUME;
        #endregion

        #region Properties
        /// <summary>Channel name - optional.</summary>
        public string ChannelName { get; set; } = "";

        /// <summary>Actual 1-based midi channel number. Required</summary>
        public int ChannelNumber { get; init; } = 0; // MidiDefs.TEMP_CHANNEL;

        /// <summary>Handle for use by scripts.</summary>
        public int Handle { get; init; }

        /// <summary>True if channel is active.</summary>
        public bool Enable { get; set; } = true;

        public bool IsDrums { get; private set; } = false;

        /// <summary>Current volume.</summary>
        public double Volume
        {
            get { return _volume; }
            set { _volume = MathUtils.Constrain(value, 0, VolumeDefs.MAX_VOLUME); }
        }

        /// <summary>Associated events - optional depending on implementation.</summary>
        public EventCollection Events { get; set; } = new();

        /// <summary>Info about the device.</summary>
        public string DeviceInfo { get { return $"Dev{_device.Id} {_device.DeviceName}"; } }

        /// <summary>Current instrument/patch number.</summary>
        public int Patch { get; private set; } = -1;

        /// <summary>Current instrument/patch name. Set may update internal collection and sends a midi patch message.</summary>
        public string PatchName
        {
            get
            {
                return _instruments.GetName(Patch) ?? throw new ArgumentException($"Invalid patch:{Patch}");
            }
            set
            {
                var id = _instruments.GetId(value) ?? throw new ArgumentException($"Invalid patch:{value}");
                _device.Send(new Patch(ChannelNumber, id));
            }
        }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor for patch name aware version.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="channelNumber"></param>
        public OutputChannel(IOutputDevice device, int channelNumber)
        {
            if (channelNumber is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"channelNumber:{(channelNumber)}"); }

            _device = device;
            ChannelNumber = channelNumber;
            _volume = VolumeDefs.DEFAULT_VOLUME;
            Handle = HandleOps.Create(device.Id, ChannelNumber, true);
        }

        /// <summary>
        /// Constructor for anonymous patch version.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="channelNumber"></param>
        /// <param name="patch"></param>
        public OutputChannel(IOutputDevice device, int channelNumber, int patch)
        {
            if (channelNumber is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"channelNumber:{(channelNumber)}"); }

            _device = device;
            ChannelNumber = channelNumber;
            Patch = patch;
            // Anonymous so give it some placeholder patch names.
            _instruments = new();
            Enumerable.Range(0, MidiDefs.MAX_MIDI + 1).ForEach(i => _instruments.Add(i, $"INST_{i}"));
            //_aliases = [];
            //Enumerable.Range(0, MidiDefs.MAX_MIDI + 1).ForEach(i => _aliases.Add(i, $"INST_{i}"));
            _volume = VolumeDefs.DEFAULT_VOLUME;
            Handle = HandleOps.Create(device.Id, ChannelNumber, true);
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Fix up instrument/patch names - depends on flavor: GM/drums/aliased.
        /// </summary>
        /// <param name="patchName"></param>
        /// <param name="aliasFile">Custom patch names for this channel</param>
        /// <exception cref="MidiLibException"></exception>
        public void InitInstruments(string patchName, string? aliasFile = null)
        {
            if (aliasFile is not null) // explicit defs file
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
            else // internal set
            {
                var id = MidiDefs.DrumKits.GetId(patchName);
                if (id is not null) // GM drum kit
                {
                    IsDrums = true;
                    Patch = (int)id;
                }
                else
                {
                    id = MidiDefs.Instruments.GetId(patchName);
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
            }

            if (Patch is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"Unresolved patch:{patchName}"); }

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
                name = _drumKits.GetName(id);
            }
            else
            {
                name = _instruments.GetName(id);
            }

            if (name is null) { throw new ArgumentException($"Invalid instrument id:{id}"); }

            return name;
        }

        /// <summary>
        /// Hide direct device access. Assumes client has fixed up volume etc.
        /// </summary>
        /// <param name="evt"></param>
        public void Send(BaseEvent evt)
        {
            evt.ChannelNumber = ChannelNumber;
            _device.Send(evt);
        }
        #endregion
    }
}
