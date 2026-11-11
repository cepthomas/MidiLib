using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms.Design;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    /// <summary>Select a midi value from a range. Handles special case of channel number.</summary>
    public class MidiValueTypeEditor : UITypeEditor
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService _service || context is null || context.Instance is null) { return null; }

            var isChan = context!.PropertyDescriptor!.Name == "ChannelNumber"; //TODO1 this?
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


    /// <summary>Select one from available devices.</summary>
    public class MidiOutputDeviceTypeEditor : UITypeEditor
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService _service || context is null || context.Instance is null) { return null; }

            var lb = new ListBox(); // {Width = 50,SelectionMode = SelectionMode.One};
            MidiOutputDevice.GetAvailableDevices().ForEach(v => lb.Items.Add(v));
            lb.Click += (_, __) => _service.CloseDropDown();
            _service.DropDownControl(lb);

            return lb.SelectedItem is null ? value : lb.SelectedItem;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) { return UITypeEditorEditStyle.DropDown; }
    }


    /// <summary>Select one from available devices.</summary>
    public class MidiInputDeviceTypeEditor : UITypeEditor
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService _service || context is null || context.Instance is null) { return null; }

            var lb = new ListBox(); // {Width = 50,SelectionMode = SelectionMode.One};
            MidiInputDevice.GetAvailableDevices().ForEach(v => lb.Items.Add(v));
            lb.Click += (_, __) => _service.CloseDropDown();
            _service.DropDownControl(lb);

            return lb.SelectedItem is null ? value : lb.SelectedItem;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) { return UITypeEditorEditStyle.DropDown; }
    }


    /// <summary>Select one from available patch names.</summary>
    public class PatchTypeEditor : UITypeEditor // or TypeConverter???
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService _service || context is null || context.Instance is null) { return null; }

            // TODO1 impl - not this, needs channel-specific!
            List<string> instList = [];
            Enumerable.Range(0, MidiDefs.MAX_MIDI + 1).ForEach(i => instList.Add(MidiDefs.Instruments.GetName(i) ?? $"{i:000}"));

            var lb = new ListBox(); // {Width = 50,SelectionMode = SelectionMode.One};
            MidiInputDevice.GetAvailableDevices().ForEach(v => lb.Items.Add(v));
            lb.Click += (_, __) => _service.CloseDropDown();
            _service.DropDownControl(lb);

            return lb.SelectedItem is null ? value : lb.SelectedItem;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) { return UITypeEditorEditStyle.DropDown; }
    }
}
