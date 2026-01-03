using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms.Design;
using Ephemera.NBagOfTricks;


// Shut up warnings in this file. Using manual checking where needed.
#pragma warning disable CS8602

namespace Ephemera.MidiLib
{
    //----------------------------------------------------------------
    /// <summary>Select a midi value from a range. Handles special case of channel number.</summary>
    public class MidiValueTypeEditor : UITypeEditor
    {
        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService _service || context is null || context.Instance is null) { return null; }

            var isChan = context.PropertyDescriptor.Name == "ChannelNumber";
            int start = isChan ? 1 : 0;
            int end = isChan ? MidiDefs.NUM_CHANNELS : MidiDefs.MAX_MIDI;

            var lb = new ListBox(); // {Width = 50,SelectionMode = SelectionMode.One};
            Enumerable.Range(start, end).ForEach(v => lb.Items.Add(v.ToString()));
            lb.Click += (_, __) => _service.CloseDropDown();
            _service.DropDownControl(lb);

            return lb.SelectedItem is null ? value : int.Parse((string)lb.SelectedItem);
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) { return UITypeEditorEditStyle.DropDown; }
    }
}