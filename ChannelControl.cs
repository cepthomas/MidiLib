using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Ephemera.MidiLib
{
    #region Types
    /// <summary>Channel playing.</summary>
    public enum ChannelState { Normal, Solo, Mute }

    /// <summary>Notify host of UI changes.</summary>
    public class ChannelChangeEventArgs : EventArgs
    {
        /// <summary>User clicked info box.</summary>
        public bool Info { get; set; } = false;

        /// <summary>User changed volume.</summary>
        public bool Volume { get; set; } = false;

        /// <summary>Solo/mute.</summary>
        public bool State { get; set; } = false;
    }
    #endregion

    [DesignTimeVisible(true)]
    public partial class ChannelControl : UserControl
    {
        #region Fields
        ChannelState _state = ChannelState.Normal;
        Color _selectedColor = Color.Red;
        bool _selected = false;

        readonly Container components = new();
        readonly Label lblInfo = new();
        readonly Slider sldVolume = new();
        readonly Label lblSolo = new();
        readonly Label lblMute = new();
        readonly Pan panChannel = new();
        ToolTip toolTip = new();
        #endregion

        #region Properties
        /// <summary>Tell the user about me. Tuple of (ui, tooltip)</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public (string ui, string tt) Info
        {
            set
            {
                lblInfo.Text = value.ui;
                toolTip.SetToolTip(lblInfo, $"{value.ui}{Environment.NewLine}{value.tt}");
            }
        }


        /// <summary>Drawing the active elements of a control.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Color DrawColor
        {
            set { sldVolume.DrawColor = value; panChannel.DrawColor = value; }
        }

        /// <summary>Drawing the control when selected.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Color SelectedColor
        {
            set { _selectedColor = value; }
        }

        /// <summary>For muting/soloing.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ChannelState State
        {
            get { return _state; }
        }

        /// <summary>Current volume.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public double Volume
        {
            get { return sldVolume.Value; }
            set { sldVolume.Value = value; }
        }

        /// <summary>User selection.</summary>
        public bool Selected
        {
            get { return _selected; }
        }
        #endregion

        #region Events
        /// <summary>User clicked stuff.</summary>
        public event EventHandler<ChannelChangeEventArgs>? ChannelChange;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public ChannelControl()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        /// <summary>
        /// Apply customization from system. Properties should be valid now.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            SuspendLayout();

            // Create the controls per the config.
            const int HT1 = 24; // first row
            const int HT2 = 42; // second row
            const int SM_SZ = 20;
            const int PAD = 4;

            // Volume slider.
            sldVolume.Minimum = 0.0;
            sldVolume.Maximum = VolumeDefs.MAX_VOLUME;
            sldVolume.Resolution = 0.05;
            sldVolume.BorderStyle = BorderStyle.FixedSingle;
            sldVolume.Orientation = Orientation.Horizontal;
            sldVolume.Location = new(PAD, PAD + HT1);
            sldVolume.Size = new(80, HT2);
            sldVolume.Label = "volume";
            sldVolume.ValueChanged += SldVolume_ValueChanged;
            Controls.Add(sldVolume);

            // Solo/mute checkboxes.
            lblSolo.Location = new(sldVolume.Right + PAD, sldVolume.Top);
            lblSolo.Size = new(SM_SZ, SM_SZ);
            lblSolo.Text = "S";
            lblSolo.Click += SoloMute_Click;
            Controls.Add(lblSolo);

            lblMute.Location = new(sldVolume.Right + PAD, sldVolume.Top + SM_SZ + 2);
            lblMute.Size = new(SM_SZ, SM_SZ);
            lblMute.Text = "M";
            lblMute.Click += SoloMute_Click;
            Controls.Add(lblMute);

            // Info textbox.
            lblInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            lblInfo.BorderStyle = BorderStyle.FixedSingle;
            lblInfo.Location = new(PAD, PAD);
            lblInfo.Size = new(lblSolo.Right, HT1 - PAD);
            lblInfo.Click += ChannelInfo_Click;
            Controls.Add(lblInfo);

            // Finalize the control.
            Size = new Size(lblInfo.Right + PAD, sldVolume.Bottom + PAD);
            ResumeLayout(false);
            PerformLayout();
            toolTip = new(components);

            base.OnLoad(e);
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Handlers for user selections
        /// <summary>User clicked info aka selected it. Maybe host would like to do something.</summary>
        void ChannelInfo_Click(object? sender, EventArgs e)
        {
            //Selected = !Selected;
            ChannelChange?.Invoke(this, new() { Info = true });
        }

        /// <summary>User clicked info aka selected it. Maybe host would like to do something.</summary>
        void SldVolume_ValueChanged(object? sender, EventArgs e)
        {
            //Selected = !Selected;
            ChannelChange?.Invoke(this, new() { Volume = true });
        }

        /// <summary>Handles solo and mute.</summary>
        void SoloMute_Click(object? sender, EventArgs e)
        {
            var lbl = sender as Label;

            // Figure out state.
            if (sender == lblSolo)
            {
                _state = lblSolo.BackColor == _selectedColor ? ChannelState.Normal : ChannelState.Solo;
            }
            else if (sender == lblMute)
            {
                _state = lblMute.BackColor == _selectedColor ? ChannelState.Normal : ChannelState.Mute;
            }

            // Notify client.
            ChannelChange?.Invoke(this, new() { State = true });
        }
        #endregion

        #region Misc
        /// <summary>Read me.</summary>
        public override string ToString()
        {
          return Info.ui;
        }
        #endregion
    }
}
