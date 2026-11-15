using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks;


// A typical way to handle settings and persistence for MidiLib configuration. Very application-specific.


namespace Ephemera.MidiLib.Test
{
    [Serializable]
    public class TestSettings
    {
        #region System stuff
        public double MasterVolume { get; init; } = 0.5;

        [JsonConverter(typeof(JsonColorConverter))]
        public Color DrawColor { get; set; } = Color.DodgerBlue;

        [JsonConverter(typeof(JsonColorConverter))]
        public Color SelectedColor { get; set; } = Color.Moccasin;
        #endregion

        #region Inputs
        public string InputChannelName { get; set; } = "???";

        [Editor(typeof(ChannelNumberTypeEditor), typeof(UITypeEditor))]
        public int InputChannelNumber { get; init; } = 0;

        [Editor(typeof(MidiInputDeviceTypeEditor), typeof(UITypeEditor))]
        public string InputDeviceName { get; set; } = "";
        #endregion

        #region Outputs defined in designer
        public string KeysChannelName { get; set; } = "???";

        [Editor(typeof(ChannelNumberTypeEditor), typeof(UITypeEditor))]
        public int KeysChannelNumber { get; init; } = 0;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MidiDefs.InstrumentId KeysPatch { get; set; }

        [Editor(typeof(MidiOutputDeviceTypeEditor), typeof(UITypeEditor))]
        public string KeysDeviceName { get; set; } = "";

        public string DrumChannelName { get; set; } = "???";

        [Editor(typeof(ChannelNumberTypeEditor), typeof(UITypeEditor))]
        public int DrumChannelNumber { get; init; } = 0;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MidiDefs.DrumKitId DrumKitPatch { get; set; }

        [Editor(typeof(MidiOutputDeviceTypeEditor), typeof(UITypeEditor))]
        public string DrumDeviceName { get; set; } = "";
        #endregion
    }
}
