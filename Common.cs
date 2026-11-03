using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms.Design;
using Ephemera.NBagOfTricks;


// Shut up warnings in this file. Using manual checking where needed.
// #pragma warning disable CS8602

namespace Ephemera.MidiLib
{
    ///// <summary>Some flavors of control may need to be defeatured.</summary>
    //[Flags]
    //public enum DisplayOptions { None = 0x00, SoloMute = 0x01, Fx = 0x02, Pan = 0x04, All = 0x0F, }

    ///// <summary>Selector operation mode.</summary>
    //public enum SelectMode { Off, Single, Multi, }

    /// <summary>User selection options.</summary>
    public enum SnapType { Tick, Beat, Bar, FourBar }

    /// <summary>Library error.</summary>
    public class MidiLibException(string message) : Exception(message) { }

    /// <summary>Misc definitions.</summary>
    public class VolumeDefs
    {
        /// <summary>Default value.</summary>
        public const double DEFAULT_VOLUME = 0.8;

        /// <summary>Allow UI controls some more headroom.</summary>
        public const double MAX_VOLUME = 2.0;
    }

    /// <summary>Channel handle for use in scripts.</summary>
    public class ChannelHandle
    {
        static readonly int OUTPUT_FLAG = 0x0800;
        readonly int _raw = 0;

        public int Raw { get { return _raw; } }
        public int DeviceId { get { return (_raw >> 4) & 0x0F; } }
        public int ChannelNumber { get { return _raw & 0x0F; } }
        public bool Output { get { return (_raw & OUTPUT_FLAG) > 0; } }

        public ChannelHandle(int deviceId, int channelNumber, bool output)
        {
            _raw = (deviceId << 4) | channelNumber | (output ? OUTPUT_FLAG : OUTPUT_FLAG);
        }

        public ChannelHandle(int handle)
        {
            _raw = handle;
        }

        public new string ToString()
        {
            return $"{(Output ? "out" : "in")} {ChannelNumber} on D{DeviceId}";
        }
    }

    /// <summary>Select a midi value from a range. Handles special case of channel number.</summary>
    public class MidiValueTypeEditor : UITypeEditor
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService _service || context is null || context.Instance is null) { return null; }

            var isChan = context!.PropertyDescriptor!.Name == "ChannelNumber";
            int start = isChan ? 1 : 0;
            int end = isChan ? MidiDefs.MAX_CHANNEL : MidiDefs.MAX_MIDI;

            var lb = new ListBox(); // {Width = 50,SelectionMode = SelectionMode.One};
            Enumerable.Range(start, end).ForEach(v => lb.Items.Add(v.ToString()));
            lb.Click += (_, __) => _service.CloseDropDown();
            _service.DropDownControl(lb);

            return lb.SelectedItem is null ? value : int.Parse((string)lb.SelectedItem);
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) { return UITypeEditorEditStyle.DropDown; }
    }
}
