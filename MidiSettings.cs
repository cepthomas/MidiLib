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
    public class MidiSettings
    {
        /// <summary>Current midi settings. Client must set this before accessing!</summary>
        [Browsable(false)]
        public static MidiSettings LibSettings
        {
            get { if (_settings is null) throw new InvalidOperationException("Client must set this property before accessing"); return _settings; }
            set { _settings = value; }
        }
        static MidiSettings? _settings = null;

        #region Properties - persisted editable
        [DisplayName("Input Device 1")]
        [Description("Valid device if handling midi input.")]
        [Browsable(true)]
        [TypeConverter(typeof(MidiDeviceTypeConverter))]
        public string InputDevice1 { get; set; } = "";

        [DisplayName("Input Device 2")]
        [Description("Valid device if handling midi input.")]
        [Browsable(true)]
        [TypeConverter(typeof(MidiDeviceTypeConverter))]
        public string InputDevice2 { get; set; } = "";

        [DisplayName("Input Device 3")]
        [Description("Valid device if handling midi input.")]
        [Browsable(true)]
        [TypeConverter(typeof(MidiDeviceTypeConverter))]
        public string InputDevice3 { get; set; } = "";

        [DisplayName("Input Device 4")]
        [Description("Valid device if handling midi input.")]
        [Browsable(true)]
        [TypeConverter(typeof(MidiDeviceTypeConverter))]
        public string InputDevice4 { get; set; } = "";

        [DisplayName("Output Device 1")]
        [Description("Valid device if sending midi output.")]
        [Browsable(true)]
        [TypeConverter(typeof(MidiDeviceTypeConverter))]
        public string OutputDevice1 { get; set; } = "";

        [DisplayName("Output Device 2")]
        [Description("Valid device if sending midi output.")]
        [Browsable(true)]
        [TypeConverter(typeof(MidiDeviceTypeConverter))]
        public string OutputDevice2 { get; set; } = "";

        [DisplayName("Default Tempo")]
        [Description("Use this tempo if it's not in the file.")]
        [Browsable(true)]
        public int DefaultTempo { get; set; } = 100;

        [DisplayName("Internal Time Resolution")]
        [Description("aka DeltaTicksPerQuarterNote or subdivisions per beat.")]
        [Browsable(false)] // TODO Implement user selectable later maybe.
        [JsonIgnore()]
        public int InternalPPQ { get; set; } = 32;

        //[DisplayName("Zero Based Time")]
        //[Description("Use 0:0:0 time instead of 1:1:1.")]
        //[Browsable(true)]
        //public bool ZeroBased { get; set; } = false; // TODO Implement later maybe. For now it's always true.

        /// <summary>How to snap.</summary>
        [DisplayName("Snap Type")]
        [Description("How to snap to grid.")]
        [Browsable(true)]
        public SnapType Snap { get; set; } = SnapType.Beat;
        #endregion

        #region Properties - internal
        /// <summary>Only 4/4 time supported.</summary>
        [Browsable(false)]
        [JsonIgnore()]
        public int BeatsPerBar { get { return 4; } }

        /// <summary>Convenience.</summary>
        [Browsable(false)]
        [JsonIgnore()]
        public int SubdivsPerBeat { get { return Definitions.InternalPPQ; } }

        /// <summary>Convenience.</summary>
        [Browsable(false)]
        [JsonIgnore()]
        public int SubdivsPerBar { get { return Definitions.InternalPPQ * BeatsPerBar; } }
        #endregion

        /// <summary>
        /// Utility function to make client's life easier.
        /// </summary>
        /// <returns></returns>
        public List<(string id, string name)> GetInputDevices()
        {
            var devs = new List<(string, string)>();
            if (InputDevice1 != "")
            {
                devs.Add(("InputDevice1", InputDevice1));
            }
            if (InputDevice2 != "")
            {
                devs.Add(("InputDevice2", InputDevice2));
            }
            if (InputDevice3 != "")
            {
                devs.Add(("InputDevice3", InputDevice3));
            }
            if (InputDevice4 != "")
            {
                devs.Add(("InputDevice4", InputDevice4));
            }

            return devs;
        }

        /// <summary>
        /// Utility function to make client's life easier.
        /// </summary>
        /// <returns></returns>
        public List<(string id, string name)> GetOutputDevices()
        {
            var devs = new List<(string, string)>();
            if (OutputDevice1 != "")
            {
                devs.Add(("OutputDevice1", OutputDevice1));
            }
            if (OutputDevice2 != "")
            {
                devs.Add(("OutputDevice2", OutputDevice2));
            }

            return devs;
        }

    }

    /// <summary>Converter for selecting property value from known lists.</summary>
    public class MidiDeviceTypeConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) { return true; }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) { return true; }

        // Get the specific list based on the property name.
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
        {
            List<string>? rec = null;

            switch (context!.PropertyDescriptor.Name)
            {
                case "InputDevice1":
                case "InputDevice2":
                case "InputDevice3":
                case "InputDevice4":
                    rec = new() { "" };
                    for (int devindex = 0; devindex < MidiIn.NumberOfDevices; devindex++)
                    {
                        rec.Add(MidiIn.DeviceInfo(devindex).ProductName);
                    }
                    rec.Add("VirtualKeyboard");
                    rec.Add("BingBong");
                    break;

                case "OutputDevice1":
                case "OutputDevice2":
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
