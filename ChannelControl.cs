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


//using System.Runtime.CompilerServices;


namespace Ephemera.MidiLib
{
    #region Types
    /// <summary>Channel playing.</summary>
    public enum ChannelState { Normal, Solo, Mute }

    /// <summary>UI change type.</summary>
    public enum UiChange { None, State, Select, MultiSelect, Info, Volume }

    /// <summary>Notify host of UI changes.</summary>
    public class ChannelChangeEventArgs : EventArgs
    {
        /// <summary>What happened.</summary>
        public UiChange Change { get; set; } = UiChange.None;

        // /// <summary>Solo/mute.</summary>
        // public bool State { get; set; } = false;

        // /// <summary>User clicked control.</summary>
        // public bool Select { get; set; } = false;

        // /// <summary>User clicked info box.</summary>
        // public bool Info { get; set; } = false;

        // /// <summary>User changed volume.</summary>
        // public bool Volume { get; set; } = false;
    }
    #endregion



    [DesignTimeVisible(true)]
    public partial class ChannelControl : UserControl
    {
        #region Fields
        /// <summary></summary>
        Color _bgColor;

        /// <summary>Solo/mute sel color TODO1 - use border instead or inverse color?</summary>
        Color _smColor = Color.Blue;

        /// <summary></summary>
        bool _selected = false;

        // The UI controls.
        readonly Container components = new();
        readonly Label lblInfo = new();
        readonly Slider sldVolume = new();
        readonly Label lblSolo = new();
        readonly Label lblMute = new();
        readonly Pan panChannel = new();
        ToolTip toolTip = new();
        #endregion

        #region Properties
        //???? DesignerSerializationVisibility
        //    /// The code generator will not produce code for the object.//    Hidden,
        //    /// The code generator will produce code for the object.    //    Visible,
        //    /// The code generator will produce code for the contents of the object, rather than for the object itself. //    Content

        /// <summary>Tell the user about me. Tuple of (ui, tooltip)</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
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
        public Color SelectedColor { get; set; } = Color.LightPink;


        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //public bool ShowSoloMute { get; set; } = false;
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //public bool ShowPan { get; set; } = false;
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //public bool ShowFx { get; set; } = false;
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //public bool ShowMeter { get; set; } = false;


        /// <summary>For muting/soloing.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        public ChannelState State { get; set; }

        /// <summary>Current volume.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        public double Volume
        {
            get { return sldVolume.Value; }
            set { sldVolume.Value = value; }
        }

        /// <summary>User selection.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool Selected
        {
            get { return _selected; }
            set { _selected = value; BackColor = _selected ? SelectedColor : _bgColor;  }
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
        /// Layout UI. Properties are valid now.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            SuspendLayout();

            _bgColor = BackColor;

            BackColor = Color.Aqua;

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

            //if (Options.HasFlag(DisplayOptions.SoloMute))
            {
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
            }

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
            ChannelChange?.Invoke(this, new() { Change = UiChange.Info });
        }

        /// <summary>User clicked info aka selected it. Maybe host would like to do something.</summary>
        void SldVolume_ValueChanged(object? sender, EventArgs e)
        {
            ChannelChange?.Invoke(this, new() { Change = UiChange.Volume });
        }

        /// <summary>Handles solo and mute.</summary>
        void SoloMute_Click(object? sender, EventArgs e)
        {
            var lbl = sender as Label;

            // Figure out state.
            if (sender == lblSolo)
            {
                State = lblSolo.BackColor == _smColor ? ChannelState.Normal : ChannelState.Solo;
            }
            else if (sender == lblMute)
            {
                State = lblMute.BackColor == _smColor ? ChannelState.Normal : ChannelState.Mute;
            }

            // Notify client.
            ChannelChange?.Invoke(this, new() { Change = UiChange.State });
        }

        /// <summary>
        /// User item selection(s).
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseClick(MouseEventArgs e)
        {
            // Debug.WriteLine($"OnMouseClick {_ctrl}");

            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                // Multiselect

                Selected = !Selected;
                ChannelChange?.Invoke(this, new() { Change = UiChange.MultiSelect });
            }
            else
            {
                // Single select
                Selected = !Selected;
                ChannelChange?.Invoke(this, new() { Change = UiChange.Select });
            }

            // Notify client.

            base.OnMouseClick(e);
        }

        // protected override void OnMouseEnter(EventArgs e)
        // protected override void OnMouseLeave(EventArgs e)
        // protected override void OnMouseDown(MouseEventArgs e)
        // protected override void OnMouseUp(MouseEventArgs e)
        #endregion

        #region Misc
        /// <summary>Read me.</summary>
        public override string ToString()
        {
          return lblInfo.Text;
        }
        #endregion
    }
}
