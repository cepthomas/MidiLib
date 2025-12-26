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
            set { _aliasFile = value; LoadAliases(); }
        }
        string _aliasFile = "";

        /// <summary>Instrument aliases - optional.</summary>
        /*public*/ Dictionary<int, string> Aliases { get; set; } = [];

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
    
        /// <summary>Associated device.</summary>
        public IOutputDevice Device { get; init; }

        /// <summary>Handle for use by scripts.</summary>
        public int Handle { get; init; }

        /// <summary>Meta info.</summary>
        public bool IsDrums { get; set; } = false; //TODO1 set? from defs - DEFAULT_DRUM_CHANNEL, from script, ...

        /// <summary>True if channel is active.</summary>
        public bool Enable { get; set; } = true;
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
            return Aliases.Count > 0 ?
                Aliases.TryGetValue(which, out string? value) ? value : $"INST_{which}" :
                MidiDefs.Instance.GetInstrumentName(which);
        }

        /// <summary>Load aliases.</summary>
        void LoadAliases()
        {
            Aliases.Clear();

            // Alternate instrument names?
            if (_aliasFile != "")
            {
                try
                {
                    var ir = new IniReader();
                    ir.ParseFile(_aliasFile);

                    var defs = ir.GetValues("instruments");

                    defs.ForEach(kv =>
                    {
                        int id = int.Parse(kv.Key); // can throw
                        if (id is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(id)); }
                        if (kv.Value.Length == 0) { throw new ArgumentOutOfRangeException($"{id} has no value"); }

                        Aliases.Add(id, kv.Value);
                    });
                }
                catch (Exception ex)
                {
                    throw new MidiLibException($"Failed to load alias file {_aliasFile}: {ex.Message}");
                }
            }
        }
    }
}
