using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace MidiLib
{
    public partial class DevicesEditor : Form
    {
        /// <summary>
        /// Items to edit.
        /// </summary>
        public List<DeviceSpec> Devices { get; set; } = new();

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DevicesEditor()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Fill the grid with current selections.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            Devices.ForEach(d => dgvDevices.Rows.Add(d.DeviceId, d.DeviceName));
            base.OnLoad(e);
        }

        /// <summary>
        /// Save current selections.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Devices.Clear();

            var rows = dgvDevices.Rows;
            for (int i = 0; i < rows.Count-1; i++) // skip header row
            {
                var row = rows[i];
                Devices.Add(new() { DeviceId = row.Cells[0].Value.ToString()!, DeviceName = row.Cells[1].Value.ToString()! });
            }

            base.OnFormClosing(e);
        }

        /// <summary>
        /// Get the clicked cell and fill with the selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDeviceNameMenu_Click(object? sender, EventArgs e)
        {
            dgvDevices.CurrentCell.Value = sender!.ToString();
        }

        /// <summary>
        /// Make a list of options and add selection handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DeviceNameMenu_Opening(object sender, CancelEventArgs e)
        {
            contextMenuStrip.Items.Clear();
            if (Devices == MidiSettings.LibSettings.OutputDevices)
            {
                for (int i = 0; i < MidiOut.NumberOfDevices; i++)
                {
                    contextMenuStrip.Items.Add(MidiOut.DeviceInfo(i).ProductName, null, OnDeviceNameMenu_Click);
                }
                contextMenuStrip.Items.Add("OSC:127.0.0.1:port", null, OnDeviceNameMenu_Click);
            }
            else
            {
                for (int i = 0; i < MidiIn.NumberOfDevices; i++)
                {
                    contextMenuStrip.Items.Add(MidiOut.DeviceInfo(i).ProductName, null, OnDeviceNameMenu_Click);
                }
                contextMenuStrip.Items.Add("OSC:port", null, OnDeviceNameMenu_Click);
                contextMenuStrip.Items.Add("VirtualKeyboard", null, OnDeviceNameMenu_Click);
                contextMenuStrip.Items.Add("BingBong", null, OnDeviceNameMenu_Click);
            }
        }
    }

    /// <summary>
    /// Plug in to property grid.
    /// </summary>
    public class DevicesTypeEditor : UITypeEditor
    {
        /// <summary>
        /// Do the work.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="provider"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            IWindowsFormsEditorService? editorService = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;

            if (editorService is not null && value is not null && value is List<DeviceSpec>)
            {
                DevicesEditor ed = new();

                switch (context.PropertyDescriptor.Name)
                {
                    case "InputDevices":
                        ed.Text = "Edit Input Devices";
                        ed.Devices = MidiSettings.LibSettings.InputDevices;
                        break;

                    case "OutputDevices":
                        ed.Text = "Edit Output Devices";
                        ed.Devices = MidiSettings.LibSettings.OutputDevices;
                        break;

                    default:
                        throw new InvalidOperationException("This should never happen!");
                }

                editorService.ShowDialog(ed);

                value = ed.Devices;
            }

            return value;
        }

        /// <summary>
        /// Identify yourself.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }
    }

    /// <summary>Converter for selecting property value from known lists. TODOX go away</summary>
    public class DeviceTypeConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) { return true; }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) { return true; }

        // Get the specific list based on the property name.
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
        {
            List<string>? rec = null;

            if (context!.PropertyDescriptor.Name.Contains("InputDevice"))//TODOX also OSC input = 1234, output = 123.456.0.0:5678.
            {
                rec = new() { "" };
                for (int devindex = 0; devindex < MidiIn.NumberOfDevices; devindex++)
                {
                    rec.Add(MidiIn.DeviceInfo(devindex).ProductName);
                }
                rec.Add("OSC");
                rec.Add("VirtualKeyboard");
                rec.Add("BingBong");
            }
            else if (context!.PropertyDescriptor.Name.Contains("OutputDevice"))
            {
                rec = new() { "" };
                for (int devindex = 0; devindex < MidiOut.NumberOfDevices; devindex++)
                {
                    rec.Add(MidiOut.DeviceInfo(devindex).ProductName);
                }
                rec.Add("OSC");
            }
            else
            {
                System.Windows.Forms.MessageBox.Show($"This should never happen: {context.PropertyDescriptor.Name}");
            }

            return new StandardValuesCollection(rec);
        }
    }
}
