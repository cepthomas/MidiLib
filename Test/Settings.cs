using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using System.IO;
using System.Drawing.Design;
using System.Windows.Forms.Design;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


// A typical way to handle settings and persistence for MidiLib configuration. Very application-specific.


namespace Ephemera.MidiLib.Test
{
    [Serializable]
    public class TestSettings
    {
        //public List<InputConfig> InputConfigs { get; set; } = [];

        //public List<OutputConfig> OutputConfigs { get; set; } = [];

        public double MasterVolume { get; init; } = 0.5;

        [JsonConverter(typeof(JsonColorConverter))]
        public Color DrawColor { get; set; } = Color.DodgerBlue;

        [JsonConverter(typeof(JsonColorConverter))]
        public Color SelectedColor { get; set; } = Color.Moccasin;

        //////////////// Inputs defined in designer //////////////////
        public string InputChannelName { get; set; } = "???";

        [Editor(typeof(ChannelNumberTypeEditor), typeof(UITypeEditor))]
        public int InputChannelNumber { get; init; } = 0;

        [Editor(typeof(MidiInputDeviceTypeEditor), typeof(UITypeEditor))]
        public string InputDeviceName { get; set; } = "";


        //////////////// Outputs defined in designer //////////////////
        public string KeysChannelName { get; set; } = "???";

        [Editor(typeof(ChannelNumberTypeEditor), typeof(UITypeEditor))]
        public int KeysChannelNumber { get; init; } = 0;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MidiDefs.Instrument KeysPatch { get; set; }

        [Editor(typeof(MidiOutputDeviceTypeEditor), typeof(UITypeEditor))]
        public string KeysDeviceName { get; set; } = "";


        public string DrumChannelName { get; set; } = "???";

        [Editor(typeof(ChannelNumberTypeEditor), typeof(UITypeEditor))]
        public int DrumChannelNumber { get; init; } = 0;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MidiDefs.DrumKit DrumPatch { get; set; }

        [Editor(typeof(MidiOutputDeviceTypeEditor), typeof(UITypeEditor))]
        public string DrumDeviceName { get; set; } = "";
    }

    [Serializable]
    public class OutputConfig_not
    {
        public string ChannelName { get; set; } = "???";

        [Editor(typeof(ChannelNumberTypeEditor), typeof(UITypeEditor))]
        public int ChannelNumber { get; init; } = 0;

        //[Editor(typeof(PatchTypeEditor), typeof(UITypeEditor))]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MidiDefs.Instrument Patch { get; set; }

        //[Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        //public string? AliasFile { get; set; } = null;

        [Editor(typeof(MidiOutputDeviceTypeEditor), typeof(UITypeEditor))]
        public string DeviceName { get; set; } = "";

        #region These would actually be persisted separately
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ChannelState State { get; set; } = ChannelState.Normal;

        public double Volume { get; init; } = 0.5;

        // public double Pan { get; init; } = 0.0;
        #endregion
    }

    [Serializable]
    public class InputConfig_not
    {
        public string ChannelName { get; set; } = "???";

        [Editor(typeof(ChannelNumberTypeEditor), typeof(UITypeEditor))]
        public int ChannelNumber { get; init; } = 0;

        [Editor(typeof(MidiInputDeviceTypeEditor), typeof(UITypeEditor))]
        public string DeviceName { get; set; } = "";
    }
}
