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
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using System.Text.Json.Serialization;


namespace Ephemera.MidiLib.Test
{
    [Serializable]
    public class TestSettings
    {
        [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
        public List<string> DeviceNames { get; set; } = [];

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public List<InputSpec> InputSpecs { get; set; } = [];

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public List<ChannelSpec> ChannelSpecs { get; set; } = [];

        [Editor(typeof(MidiValueTypeEditor), typeof(UITypeEditor))]
        public int SomeOtherMidi { get; set; } = 0;

        public double MasterVolume { get; init; } = 0.5;

        [JsonConverter(typeof(JsonColorConverter))]
        public Color DrawColor { get; set; } = Color.DodgerBlue;

        [JsonConverter(typeof(JsonColorConverter))]
        public Color SelectedColor { get; set; } = Color.Moccasin;

        //[JsonConverter(typeof(JsonStringEnumConverter))]
        //public DisplayOptions Options { get; set; } = DisplayOptions.All;

        #region Fields
        public const string INDEV = "NebLoopback1";

        public const string OUTDEV1 = "VirtualMIDISynth #1";

        public const string OUTDEV2 = "Microsoft GS Wavetable Synth";
        #endregion

        public static TestSettings GenFake()
        {
            // Init fake settings.
            TestSettings settings = new()
            {
                DeviceNames = [INDEV, OUTDEV1, OUTDEV2],
                InputSpecs = [],
                ChannelSpecs = [],
                SomeOtherMidi = 88,
                MasterVolume = 0.77,
                DrawColor = Color.SpringGreen,
                SelectedColor = Color.Magenta,
                //Options = DisplayOptions.None
            };

            settings.InputSpecs.Add(new()
            {
                ChannelNumber = 8,
                ChannelName = "Some input",
                DeviceName = INDEV
            });

            settings.ChannelSpecs.Add(new()
            {
                ChannelNumber = 1,
                ChannelName = "Output 1!",
                DeviceName = OUTDEV1,
                PatchName = "Harpsichord",
                Volume = 0.67,
                State = ChannelState.Normal
            });

            settings.ChannelSpecs.Add(new()
            {
                ChannelNumber = 2,
                ChannelName = "Output 2!",
                DeviceName = OUTDEV2,
                PatchName = "Violin",
                Volume = 1.19,
                State = ChannelState.Mute
            });

            return settings;
        }
    }

    [Serializable]
    public class InputSpec
    {
        /// <summary>Channel name. Optional.</summary>
        public string ChannelName { get; set; } = "???";

        /// <summary>Actual 1-based midi channel number. Required.</summary>
        public int ChannelNumber { get; init; } = 0;

        [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
        // [TypeConverter(typeof(GenericConverter))]
        public string DeviceName { get; set; } = "";

        /// <summary>True if channel is active.</summary>
        public bool Enable { get; set; } = true;
    }

    [Serializable]
    public class ChannelSpec
    {
        public string ChannelName { get; set; } = "???";

        public int ChannelNumber { get; init; } = 0;

        public string PatchName { get; set; } = "???";

        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string AliasFile { get; set; } = "";

        [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
        // [TypeConverter(typeof(GenericConverter))]
        public string DeviceName { get; set; } = "";

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ChannelState State { get; set; } = ChannelState.Normal;

        public double Volume { get; init; } = 0.5;

        public double Pan { get; init; } = 0.0;
    }
}
