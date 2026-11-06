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
    #endregion

    [DesignTimeVisible(true)]
    public partial class ChannelControl : UserControl
    {
        #region Fields
        /// <summary>Initial bg color.</summary>
        Color _bgColor;

        /// <summary>Control is selected</summary>
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

        #region Events
        /// <summary>Notify host of UI changes.</summary>
        public class StateChangeEventArgs : EventArgs { }
        public event EventHandler<StateChangeEventArgs>? StateChange;

        /// <summary>Notify host of UI changes.</summary>
        public class SelectionChangeEventArgs : EventArgs { public bool MultiSelect { get; set; } = false; }
        public event EventHandler<SelectionChangeEventArgs>? SelectionChange;

        /// <summary>Notify host of UI changes.</summary>
        public class InfoClickEventArgs : EventArgs { }
        public event EventHandler<InfoClickEventArgs>? InfoClick;
        #endregion

        #region Properties
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
            set
            {
                sldVolume.DrawColor = value;
                panChannel.DrawColor = value;
            }
            get
            {
                return sldVolume.DrawColor;
            }
        }

        /// <summary>Drawing the control when selected.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Color SelectedColor { get; set; } = Color.LightPink;

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
        /// <summary>User clicked info box. Maybe the host would like to do something.</summary>
        void ChannelInfo_Click(object? sender, EventArgs e)
        {
            InfoClick?.Invoke(this, new());
        }

        /// <summary>User changed volume. Usually not needed by host.</summary>
        void SldVolume_ValueChanged(object? sender, EventArgs e)
        {
        }

        /// <summary>Handles solo and mute.</summary>
        void SoloMute_Click(object? sender, EventArgs e)
        {
            var lbl = sender as Label;

            // Figure out state.
            if (lbl == lblSolo)
            {
                if (lblSolo.BackColor == DrawColor) // on
                {
                    State = ChannelState.Normal;
                    lblSolo.BackColor = _bgColor;
                }
                else // off
                {
                    State = ChannelState.Solo;
                    lblSolo.BackColor = DrawColor;
                    lblMute.BackColor = _bgColor;
                }
            }
            else if (lbl == lblMute)
            {
                if (lblMute.BackColor == DrawColor) // on
                {
                    State = ChannelState.Normal;
                    lblMute.BackColor = _bgColor;
                }
                else // off
                {
                    State = ChannelState.Mute;
                    lblMute.BackColor = DrawColor;
                    lblSolo.BackColor = _bgColor;
                }
            }
            else
            {
                throw new MidiLibException($"Unknown label {lbl}");
            }

            // Notify host.
            StateChange?.Invoke(this, new());
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
                SelectionChange?.Invoke(this, new() { MultiSelect = true });
            }
            else
            {
                // Single select
                Selected = !Selected;
                SelectionChange?.Invoke(this, new() { MultiSelect = false });
            }

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
