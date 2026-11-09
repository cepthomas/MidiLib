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
    /// <summary>User selection options.</summary>
    public enum SnapType { Tick, Beat, Bar, FourBar }

    /// <summary>Library error.</summary>
    public class MidiLibException(string message) : Exception(message) { }

    /// <summary>Misc definitions.</summary>
    public class MidiLibDefs
    {
        /// <summary>Default value.</summary>
        public const double DEFAULT_VOLUME = 0.8;

        /// <summary>Allow UI controls some more headroom.</summary>
        public const double MAX_VOLUME = 2.0;

        /// <summary>Resolution of UI.</summary>
        public const double RES_VOLUME = MAX_VOLUME / 50;
    }

    /// <summary>Channel handle class for interacting with scripts.</summary>
    public class ChannelHandle
    {
        /// <summary>Differentiate inputs and outputs.</summary>
        static readonly int OUTPUT_FLAG = 0x0800;

        /// <summary>Script-side handle.</summary>
        public int Raw { get { return _raw; } }
        readonly int _raw = 0;

        /// <summary>App-side id.</summary>
        public int DeviceId { get { return (_raw >> 4) & 0x0F; } }

        /// <summary>App-side value.</summary>
        public int ChannelNumber { get { return _raw & 0x0F; } }

        /// <summary>App-side value.</summary>
        public bool Output { get { return (_raw & OUTPUT_FLAG) > 0; } }

        /// <summary>App-side constructor.</summary>
        public ChannelHandle(int deviceId, int channelNumber, bool output)
        {
            _raw = (deviceId << 4) | channelNumber | (output ? OUTPUT_FLAG : OUTPUT_FLAG);
        }

        /// <summary>Script-side constructor.</summary>
        public ChannelHandle(int handle)
        {
            _raw = handle;
        }

        /// <summary>Human readable.</summary>
        public override string ToString()
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
