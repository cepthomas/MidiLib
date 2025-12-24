using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.IO;
using System.ComponentModel;
using System.Drawing.Design;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    //----------------------------------------------------------------
    /// <summary>Encode device/channel info for round trip through script.</summary>
    public static class ChannelHandle
    {
        const int OUTPUT_FLAG = 0x0800;

        public static int Create(int deviceId, int channelNumber, bool output)
        {
            return (deviceId << 4) | channelNumber | (output ? OUTPUT_FLAG : OUTPUT_FLAG);
        }

        public static int DeviceId(int handle) { return (handle >> 4) & 0x0F; }
        public static int ChannelNumber(int handle) { return handle & 0x0F; }
        public static bool Output(int handle) { return (handle & OUTPUT_FLAG) > 0; }

        /// <summary>See me.</summary>
        public static string Format(int handle)
        {
            return $"{(Output(handle) ? "OUT" : "IN")} {DeviceId(handle)}:{ChannelNumber(handle):00}";
        }
    }

    //----------------------------------------------------------------
    /// <summary>Describes one midi input channel.</summary>
    public class InputChannel
    {
        #region Properties
        /// <summary>Channel name - optional.</summary>
        public string ChannelName { get; set; } = "";

        /// <summary>Actual 1-based midi channel number.</summary>
        [Range(1, MidiDefs.NUM_CHANNELS)]
        public int ChannelNumber { get; set; } = 1;

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
            Handle = ChannelHandle.Create(device.Id, ChannelNumber, false);
        }
    }

    //----------------------------------------------------------------
    /// <summary>Describes one midi output channel.</summary>
    public class OutputChannel
    {
        #region Properties
        /// <summary>Channel name - optional.</summary>
        public string ChannelName { get; set; } = "";

        /// <summary>Actual 1-based midi channel number.</summary>
        [Range(1, MidiDefs.NUM_CHANNELS)]
        public int ChannelNumber { get; private set; } = 1;

        /// <summary>Override default instrument presets.</summary>
        public string AliasFile
        {
            get { return _aliasFile; }
            set { _aliasFile = value; LoadInstruments(); }
        }
        string _aliasFile = "";

        /// <summary>Current instrument/patch number.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int Patch
        {
            get {  return _patch; }
            set { _patch = value; Device.Send(new Patch(ChannelNumber, _patch)); }
        }
        int _patch = 0;

        /// <summary>Current volume.</summary>
        [Range(0.0, Defs.MAX_VOLUME)]
        public double Volume { get; set; } = Defs.DEFAULT_VOLUME;
    
        /// <summary>Edit current controller number.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int ControllerId { get; set; } = 0;

        /// <summary>Controller payload.</summary>
        [Range(0, MidiDefs.MAX_MIDI)]
        public int ControllerValue { get; set; } = 50;

        /// <summary>Associated device.</summary>
        public IOutputDevice Device { get; init; }

        /// <summary>Handle for use by scripts.</summary>
        public int Handle { get; init; }

        /// <summary>True if channel is active.</summary>
        public bool Enable { get; set; } = true;

        /// <summary>Current list for this channel.</summary>
        public Dictionary<int, string> Instruments { get; private set; } = MidiDefs.Instance.GetDefaultInstrumentDefs();
        #endregion

        /// <summary>
        /// Constructor with required args.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="channelNumber"></param>
        public OutputChannel(IOutputDevice device, int channelNumber)
        {
            Device = device;
            ChannelNumber = channelNumber;
            Volume = Defs.DEFAULT_VOLUME;
            Handle = ChannelHandle.Create(device.Id, ChannelNumber, true);
        }

        /// <summary>
        /// Get patch name.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The name or a fabricated one if unknown.</returns>
        public string GetPatchName(int which)
        {
            return Instruments.TryGetValue(which, out string? value) ? value : $"PATCH_{which}";
        }

        /// <summary>Load default or aliases.</summary>
        void LoadInstruments()
        {
            // Alternate instrument names?
            if (_aliasFile != "")
            {
                try
                {
                    Instruments.Clear();
                    var ir = new IniReader();
                    ir.ParseFile(_aliasFile);

                    var defs = ir.GetValues("instruments");

                    defs!.ForEach(kv =>
                    {
                        int i = int.Parse(kv.Key); // can throw
                        i = MathUtils.Constrain(i, 0, MidiDefs.MAX_MIDI);
                        Instruments.Add(i, kv.Value.Length > 0 ? kv.Value : "");
                    });
                }
                catch (Exception ex)
                {
                    throw new MidiLibException($"Failed to load alias file {_aliasFile}: {ex.Message}");
                }
            }
            else
            {
                Instruments = MidiDefs.Instance.GetDefaultInstrumentDefs();
            }
        }
    }
}
