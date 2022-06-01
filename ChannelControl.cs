using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NBagOfTricks;


namespace MidiLib
{
    /// <summary>A simple channel UI component.</summary>
    public partial class ChannelControl : UserControl
    {
        #region Properties
        /// <summary>Actual 1-based midi channel number.</summary>
        public int ChannelNumber
        {
            get
            {
                return _channelNumber;
            }
            set
            {
                _channelNumber = MathUtils.Constrain(value, 1, MidiDefs.NUM_CHANNELS);
                lblChannelNumber.Text = $"{_channelNumber}:{_deviceNumber}";
            }
        }
        int _channelNumber = 0;

        /// <summary>Actual 1-based midi device number. Client responsible.</summary>
        public int DeviceNumber
        {
            get
            {
                return _deviceNumber;
            }
            set
            {
                _deviceNumber = value;
                lblChannelNumber.Text = $"{_channelNumber}:{_deviceNumber}";
            }
        }
        int _deviceNumber = 1;

        /// <summary>Current patch.</summary>
        public int Patch
        {
            get
            {
                return _patch;
            }
            set
            {
                _patch = MathUtils.Constrain(value, 0, MidiDefs.MAX_MIDI);
                lblPatch.Text = MidiDefs.GetInstrumentName(_patch);
            }
        }
        int _patch = -1;

        /// <summary>Current volume.</summary>
        public double Volume
        {
            get
            {
                return sldVolume.Value;
            }
            set
            {
                sldVolume.Value = MathUtils.Constrain(value, InternalDefs.VOLUME_MIN, InternalDefs.VOLUME_MAX, InternalDefs.VOLUME_RESOLUTION);
            }
        }

        /// <summary>Cosmetics.</summary>
        public Color ControlColor { get; set; } = Color.MediumOrchid;
        #endregion

        #region Events
        /// <summary>Notify host of asynchronous changes from user.</summary>
        public event EventHandler? PatchChange;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public ChannelControl()
        {
            InitializeComponent();

            sldVolume.DrawColor = ControlColor;
            sldVolume.Minimum = InternalDefs.VOLUME_MIN;
            sldVolume.Maximum = InternalDefs.VOLUME_MAX;
            sldVolume.Value = InternalDefs.VOLUME_DEFAULT;

            lblChannelNumber.Click += ChannelNumber_Click;
            lblPatch.Click += Patch_Click;
        }
        #endregion

        #region Handlers for user selections
        /// <summary>
        /// Handle selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelNumber_Click(object? sender, EventArgs e)
        {
            using Form f = new()
            {
                Text = "Channel",
                Size = new Size(50, 400),
                StartPosition = FormStartPosition.Manual,
                Location = Cursor.Position,
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            };
            ListBox lv = new()
            {
                Dock = DockStyle.Fill,
            };

            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                lv.Items.Add((i+1).ToString());
            }

            lv.Click += (object? sender, EventArgs e) =>
            {
                int ind = lv.SelectedIndices[0];
                ChannelNumber = ind + 1;
                f.Close();
            };

            f.Controls.Add(lv);
            f.ShowDialog();
        }

        /// <summary>
        /// User wants to change the patch.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Patch_Click(object? sender, EventArgs e)
        {
            int currentPatch = Patch;

            using Form f = new()
            {
                Text = "Select Patch",
                Size = new Size(800, 500),
                StartPosition = FormStartPosition.Manual,
                Location = Cursor.Position,
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            };
            ListView lv = new()
            {
                Dock = DockStyle.Fill,
                View = View.List,
                HideSelection = false
            };

            for (int i = 0; i < MidiDefs.MAX_MIDI; i++)
            {
                lv.Items.Add(MidiDefs.GetInstrumentName(i));
            }

            lv.Click += (object? sender, EventArgs e) =>
            {
                int ind = lv.SelectedIndices[0];
                Patch = ind;
                f.Close();
            };

            f.Controls.Add(lv);
            f.ShowDialog();

            // Patch change?
            if(Patch != currentPatch)
            {
                PatchChange?.Invoke(this, EventArgs.Empty);
            }
        }
        #endregion
    }
}
