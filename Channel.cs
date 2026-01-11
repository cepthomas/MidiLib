using System;
using System.Collections.Generic;
using System.Linq;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    #region Types
    /// <summary>Some channels have specialized behavior.</summary>
    public enum ChannelFlavor { Normal, Drums }

    /// <summary>Encode device/channel info for round trip through script.</summary>
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
            return $"{(Output(handle) ? "out" : "in")} {ChannelNumber(handle)}{Environment.NewLine}on D{DeviceId(handle)}";
        }
    }
    #endregion

    //----------------------------------------------------------------
    /// <summary>Describes one midi input channel.</summary>
    public class InputChannel
    {
        #region Fields

        // Backing fields.
        int _channelNumber;
        #endregion

        #region Properties
        /// <summary>Channel name - optional.</summary>
        public string ChannelName { get; set; } = "";

        /// <summary>Actual 1-based midi channel number.</summary>
        public int ChannelNumber
        {
            get { return _channelNumber; }
            set { if (value is < 1 or > MidiDefs.NUM_CHANNELS) throw new ArgumentOutOfRangeException($"ChannelNumber:{value}");
                  else _channelNumber = value; }
        }

        /// <summary>Associated device.</summary>
        public IInputDevice Device { get; init; }

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
            Device = device;
            ChannelNumber = channelNumber;
            Handle = HandleOps.Create(device.Id, ChannelNumber, false);
        }
    }

    //----------------------------------------------------------------
    /// <summary>Describes one midi output channel.</summary>
    public class OutputChannel
    {
        #region Fields
        /// <summary>All the possible instruments. May be GM, from alias file, drum kits, ...</summary>
        Dictionary<int, string> _instruments = [];

        /// <summary>If it's a drum channel use drum names instead of notes.</summary>
        Dictionary<int, string> _drums = [];

        // Backing fields.
        int _channelNumber;
        int _patch = 0;
        double _volume = VolumeDefs.DEFAULT_VOLUME;
        string _instrumentFile = "";
        #endregion

        #region Properties
        /// <summary>Channel name - optional.</summary>
        public string ChannelName { get; set; } = "";

        /// <summary>Actual 1-based midi channel number.</summary>
        public int ChannelNumber
        {
            get { return _channelNumber; }
            set { if (value is < 1 or > MidiDefs.NUM_CHANNELS) throw new ArgumentOutOfRangeException($"ChannelNumber:{value}");
                  else _channelNumber = value; }
        }

        /// <summary>Optional variation.</summary>
        public ChannelFlavor Flavor  { get; init; } = ChannelFlavor.Normal;

        /// <summary>Associated device.</summary>
        public IOutputDevice Device { get; init; }

        /// <summary>Handle for use by scripts.</summary>
        public int Handle { get; init; }

        /// <summary>True if channel is active.</summary>
        public bool Enable { get; set; } = true;

        /// <summary>Current instrument/patch number. Set sends a midi patch message.</summary>
        public int Patch
        {
            get { return _patch; }
            set { if (value is < 0 or > MidiDefs.MAX_MIDI) throw new ArgumentOutOfRangeException($"Patch:{value}");
                  else _patch = value;  Device.Send(new Patch(ChannelNumber, _patch, MusicTime.ZERO)); }
        }

        /// <summary>Current instrument/patch name. Set sends a midi patch message.</summary>
        public string PatchName
        {
            get { return GetInstrumentName(_patch); }
            set { Patch = GetInstrumentId(value); }
        }

        /// <summary>Current volume.</summary>
        public double Volume
        {
            get { return _volume; }
            set { _volume = MathUtils.Constrain(value, 0, VolumeDefs.MAX_VOLUME); }
        }

        /// <summary>Override default instrument list.</summary>
        public string InstrumentFile
        {
            set { _instrumentFile = value; LoadInstrumentFile(); }
        }

        /// <summary>Associated events - optional depending on implementation.</summary>
        public EventCollection Events { get; set; } = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor with required args.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="channelNumber"></param>
        /// <param name="flavor"></param>
        public OutputChannel(IOutputDevice device, int channelNumber, ChannelFlavor flavor)
        {
            Device = device;
            ChannelNumber = channelNumber;
            Flavor = flavor;
            Volume = VolumeDefs.DEFAULT_VOLUME;
            Handle = HandleOps.Create(device.Id, ChannelNumber, true);

            // Load default instruments list.
            var ir = new IniReader();
            ir.ParseString(Properties.Resources.gm_defs);

            if (flavor == ChannelFlavor.Drums)
            {
                ir.GetValues("drumkits").ForEach(kv => { _instruments[int.Parse(kv.Key)] = kv.Value; });
                ir.GetValues("drums").ForEach(kv => { _drums[int.Parse(kv.Key)] = kv.Value; });
            }
            else // normal
            {
                ir.GetValues("instruments").ForEach(kv => { _instruments[int.Parse(kv.Key)] = kv.Value; });
            }
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Get instrument name.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The instrument name or a fabricated one if unknown.</returns>
        public string GetInstrumentName(int id)
        {
            if (id is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"Instrument:{id}"); }
            return _instruments.TryGetValue(id, out string? value) ? value : $"INST_{id}";
        }

        /// <summary>
        /// Get corresponding number.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetInstrumentId(string name)
        {
            var i = _instruments.Where(v => v.Value == name);
            return i.Any() ? i.First().Key : -1;
        }
        #endregion

        #region Private functions
        /// <summary>Load instrument names.</summary>
        void LoadInstrumentFile()
        {
            _instruments.Clear();

            try
            {
                var ir = new IniReader();
                ir.ParseFile(_instrumentFile);

                var defs = ir.GetValues("instruments");

                defs.ForEach(kv =>
                {
                    int id = int.Parse(kv.Key); // can throw
                    if (id is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"Instrument:{id}"); }
                    if (kv.Value.Length == 0) { throw new ArgumentOutOfRangeException($"{id} has no value"); }

                    _instruments.Add(id, kv.Value);
                });
            }
            catch (Exception ex)
            {
                throw new MidiLibException($"Failed to load instruments file {_instrumentFile}: {ex.Message}");
            }
        }
        #endregion
    }
}
