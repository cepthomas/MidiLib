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
    public class ChannelNumberTypeEditor : UITypeEditor
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService _service ||
                context is null || context.Instance is null) { return null; }

            var lb = new ListBox(); // {Width = 50,SelectionMode = SelectionMode.One};
            Enumerable.Range(1, MidiDefs.MAX_CHANNEL).ForEach(v => lb.Items.Add(v.ToString()));
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
            if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService _service ||
                context is null || context.Instance is null) { return null; }

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
            if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService _service ||
                context is null || context.Instance is null) { return null; }

            var lb = new ListBox(); // {Width = 50,SelectionMode = SelectionMode.One};
            MidiInputDevice.GetAvailableDevices().ForEach(v => lb.Items.Add(v));
            lb.Click += (_, __) => _service.CloseDropDown();
            _service.DropDownControl(lb);

            return lb.SelectedItem is null ? value : lb.SelectedItem;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) { return UITypeEditorEditStyle.DropDown; }
    }


    /// <summary>Select one from available patch names.</summary>
    public class PatchTypeEditor : UITypeEditor
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService _service ||
                context is null || context.Instance is null) { return null; }
            // context is {System.Windows.Forms.PropertyGridInternal.PropertyDescriptorGridEntry PatchName}
            // context.Instance is { Ephemera.MidiLib.Test.OutputConfig}
            // _service is {System.Windows.Forms.PropertyGridInternal.PropertyGridView}



            // TODO1 impl - not this??, needs channel-specific!


            Key Tips for Dynamic Data Sources
            
• Context Inspection: Use context.Instance inside EditValue to inspect the object being edited(e.g., to load options filtered by another property on the same object).
            • Runtime Injection: If you cannot modify the source code of the target class to add[Editor(...)], you can register the custom editor globally at runtime using TypeDescriptor.AddAttributes:csharp
            TypeDescriptor.AddAttributes(typeof(MyTargetClass), new EditorAttribute(typeof(CustomOptionsTypeEditor), typeof(UITypeEditor)));



            //var options = GetCustomOptionsDataSource(context);
            //foreach (var option in options)
            //{
            //    lb.Items.Add(option);
            //}


            var lb = new ListBox() { Width = 50, Height = 250, SelectionMode = SelectionMode.One};
            for (int i = 0; i <= MidiDefs.MAX_MIDI; i++) lb.Items.Add(MidiDefs.Instruments.GetName(i) ?? $"{i:000}");
            lb.Click += (_, __) => _service.CloseDropDown();
            _service.DropDownControl(lb);

            return lb.SelectedItem is null ? value : lb.SelectedItem;
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) { return UITypeEditorEditStyle.DropDown; }


        // Example method to fetch your custom data source
        private string[] GetCustomOptionsDataSource(ITypeDescriptorContext context)
        {
            // Access context.Instance if you need to inspect the host object
            // e.g., var owner = context.Instance as MyTargetClass;
            return new[] { "Option A", "Option B", "Option C", "Dynamic Item 1" };
        }

    }
}
