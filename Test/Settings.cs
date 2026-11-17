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
        public string InputName { get; set; } = "???";

        [Editor(typeof(MidiChannelTypeEditor), typeof(UITypeEditor))]
        public int InputMidiChannel { get; init; } = 0;

        [Editor(typeof(MidiInputDeviceTypeEditor), typeof(UITypeEditor))]
        public string InputDeviceName { get; set; } = "";
        #endregion

        #region Outputs defined in designer
        public string KeysName { get; set; } = "???";

        [Editor(typeof(MidiChannelTypeEditor), typeof(UITypeEditor))]
        public int KeysMidiChannel { get; init; } = 0;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MidiDefs.InstrumentId KeysPatch { get; set; }

        [Editor(typeof(MidiOutputDeviceTypeEditor), typeof(UITypeEditor))]
        public string KeysDeviceName { get; set; } = "";

        [Editor(typeof(MidiOutputDeviceTypeEditor), typeof(UITypeEditor))]
        public string DrumsDeviceName { get; set; } = "";

        public string DrumsName { get; set; } = "???";

        [Editor(typeof(MidiChannelTypeEditor), typeof(UITypeEditor))]
        public int DrumsMidiChannel { get; init; } = 0;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MidiDefs.DrumKitId DrumKitPatch { get; set; }
        #endregion
    }
}
