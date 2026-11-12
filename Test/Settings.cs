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


// A typical way to handle settings for MidiLib configuration. Very application-specific.


namespace Ephemera.MidiLib.Test
{
//????    [Serializable]
    public class TestSettings
    {
        // [Editor(typeof(PatchTypeEditor), typeof(UITypeEditor))]
        // public string PatchName { get; set; } = "???";

        public List<InputConfig> InputConfigs { get; set; } = [];

        public List<ChannelConfig> ChannelConfigs { get; set; } = [];

        public double MasterVolume { get; init; } = 0.5;

        [JsonConverter(typeof(JsonColorConverter))]
        public Color DrawColor { get; set; } = Color.DodgerBlue;

        [JsonConverter(typeof(JsonColorConverter))]
        public Color SelectedColor { get; set; } = Color.Moccasin;

        #region Device names - YMMV
        public const string INDEV = "NebLoopback1";
        public const string OUTDEV1 = "VirtualMIDISynth #1";
        public const string OUTDEV2 = "Microsoft GS Wavetable Synth";
        #endregion

        public static TestSettings GenFake()
        {
            // Init fake settings.
            TestSettings settings = new()
            {
                InputConfigs = [],
                ChannelConfigs = [],
                MasterVolume = 0.77,
                DrawColor = Color.SpringGreen,
                SelectedColor = Color.Magenta,
            };

            settings.InputConfigs.Add(new()
            {
                ChannelNumber = 8,
                ChannelName = "Some input",
                DeviceName = INDEV
            });

            settings.ChannelConfigs.Add(new()
            {
                ChannelNumber = 1,
                ChannelName = "Output 1!",
                DeviceName = OUTDEV1,
                PatchName = "Harpsichord",
                Volume = 0.67,
//???                State = ChannelState.Normal
            });

            settings.ChannelConfigs.Add(new()
            {
                ChannelNumber = 2,
                ChannelName = "Output 2!",
                DeviceName = OUTDEV2,
                PatchName = "Violin",
                Volume = 1.19,
                //???                State = ChannelState.Mute
            });

            return settings;
        }
    }
}
