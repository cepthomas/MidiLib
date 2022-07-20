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
    /// <summary>A simpler channel UI component.</summary>
    public partial class SimpleChannelControl : UserControl
    {
        #region Properties
        /// <summary>Actual 1-based midi channel number.</summary>
        public int ChannelNumber
        {
            get { return _channelNumber; }
            set { _channelNumber = MathUtils.Constrain(value, 1, MidiDefs.NUM_CHANNELS); cmbChannel.SelectedIndex = _channelNumber - 1; }
        }
        int _channelNumber = 0;

        /// <summary>Current patch.</summary>
        public int Patch
        {
            get { return _patch; }
            set { _patch = MathUtils.Constrain(value, 0, MidiDefs.MAX_MIDI); lblPatch.Text = MidiDefs.GetInstrumentName(_patch); }
        }
        int _patch = -1;

        /// <summary>Current volume.</summary>
        public double Volume
        {
            get { return sldVolume.Value; }
            set { sldVolume.Value = value; }
        }

        /// <summary>Cosmetics.</summary>
        public Color ControlColor { get; set; } = Color.Crimson;
        #endregion

        #region Events
        /// <summary>Notify host of asynchronous changes from user.</summary>
        public event EventHandler<ChannelChangeEventArgs>? ChannelChangeEvent;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public SimpleChannelControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            sldVolume.DrawColor = ControlColor;
            sldVolume.Minimum = MidiLibDefs.VOLUME_MIN;
            sldVolume.Maximum = MidiLibDefs.MAX_GAIN;
            sldVolume.Value = MidiLibDefs.VOLUME_DEFAULT;

            lblPatch.Click += Patch_Click;

            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                cmbChannel.Items.Add($"{i + 1}");
            }
            cmbChannel.SelectedIndex = ChannelNumber - 1;
            cmbChannel.SelectedIndexChanged += (_, __) => { _channelNumber = cmbChannel.SelectedIndex + 1; };
            
            base.OnLoad(e);
        }
        #endregion

        #region Handlers for user selections
        /// <summary>
        /// User wants to change the patch.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Patch_Click(object? sender, EventArgs e)
        {
            int currentPatch = Patch;

            PatchPicker pp = new();
            pp.ShowDialog();
            if (pp.PatchNumber != -1)
            {
                Patch = pp.PatchNumber;
                ChannelChangeEvent?.Invoke(this, new() { PatchChange = true } );
            }
        }
        #endregion
    }
}
