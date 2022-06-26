using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using NAudio.Midi;
using NBagOfTricks;
using NBagOfUis;


namespace MidiLib
{
    [Serializable]
    public class MidiSettings : Settings
    {
        /// <summary>Current midi user settings.</summary>
        public static MidiSettings TheSettings { get; set; } = new();

        #region Properties - persisted editable
        [DisplayName("Midi Input 1")]
        [Description("Valid device if handling midi input.")]
        [Category("Devices")]
        [Browsable(true)]
        [TypeConverter(typeof(MidiDeviceTypeConverter))]
        public string MidiIn1 { get; set; } = "";

        [DisplayName("Midi Input 2")]
        [Description("Valid device if handling midi input.")]
        [Category("Devices")]
        [Browsable(true)]
        [TypeConverter(typeof(MidiDeviceTypeConverter))]
        public string MidiIn2 { get; set; } = "";

        [DisplayName("Midi Output 1")]
        [Description("Valid device if sending midi output.")]
        [Category("Devices")]
        [Browsable(true)]
        [TypeConverter(typeof(MidiDeviceTypeConverter))]
        public string MidiOut1 { get; set; } = "";

        [DisplayName("Midi Output 2")]
        [Description("Valid device if sending midi output.")]
        [Category("Devices")]
        [Browsable(true)]
        [TypeConverter(typeof(MidiDeviceTypeConverter))]
        public string MidiOut2 { get; set; } = "";

        [DisplayName("Internal Time Resolution")]
        [Description("PPQ or DeltaTicksPerQuarterNote or subdivisions per beat.")]
        [Browsable(true)]
        public PPQ InternalTimeResolution { get; set; } = PPQ.PPQ_8;

        [DisplayName("Zero Based Time")]
        [Description("Use 0:0:0 time instead of 1:1:1.")]
        [Browsable(true)]
        public bool ZeroBased { get; set; } = false;

        /// <summary>How to snap.</summary>
        [DisplayName("Snap Type")]
        [Description("How to snap to grid.")]
        [Browsable(true)]
        public SnapType Snap { get; set; } = SnapType.Beat;

        //[DisplayName("OSC Input")]
        //[Description("Valid port number if handling OSC input.")]
        //[Browsable(true)]
        //public string OscIn { get; set; } = "None";

        //[DisplayName("OSC Output")]
        //[Description("Valid url:port if sending OSC output.")]
        //[Browsable(true)]
        //public string OscOut { get; set; } = "None";
        #endregion

        #region Properties - internal
        /// <summary>Only 4/4 time supported.</summary>
        public const int BeatsPerBar = 4;

        /// <summary>Convenience.</summary>
        [Browsable(false)]
        [JsonIgnore()]
        public int SubdivsPerBeat { get { return (int)InternalTimeResolution; } }

        /// <summary>Convenience.</summary>
        [Browsable(false)]
        [JsonIgnore()]
        public int SubdivsPerBar { get { return (int)InternalTimeResolution * BeatsPerBar; } }
        #endregion
    }

    /// <summary>Converter for selecting property value from known lists.</summary>
    public class MidiDeviceTypeConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }

        // Get the specific list based on the property name.
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string>? rec = null;

            switch (context.PropertyDescriptor.Name)
            {
                case "MidiIn1":
                case "MidiIn2":
                    rec = new() { "" };
                    for (int devindex = 0; devindex < MidiIn.NumberOfDevices; devindex++)
                    {
                        rec.Add(MidiIn.DeviceInfo(devindex).ProductName);
                    }
                    break;

                case "MidiOut1":
                case "MidiOut2":
                    rec = new() { "" };
                    for (int devindex = 0; devindex < MidiOut.NumberOfDevices; devindex++)
                    {
                        rec.Add(MidiOut.DeviceInfo(devindex).ProductName);
                    }
                    break;

                default:
                    System.Windows.Forms.MessageBox.Show($"This should never happen: {context.PropertyDescriptor.Name}");
                    break;
            }

            return new StandardValuesCollection(rec);
        }
    }
}
