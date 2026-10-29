using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
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
        /// <summary>User clicked info.</summary>
        public bool Info { get; set; } = false;
        /// <summary>Solo/mute.</summary>
        public bool State { get; set; } = false;
    }
    #endregion

    [DesignTimeVisible(true)]
    public partial class ChannelControl : UserControl
    {
        #region Fields
        ChannelState _state = ChannelState.Normal;

        readonly Container components = new();
        protected ToolTip toolTip = new();
        const int PAD = 4;

        // always:
        readonly Label lblInfo = new();
        readonly Slider sldVolume = new();

        // optional:
        readonly Label lblSolo = new();
        readonly Label lblMute = new();

        // optional:
        readonly Slider sldControllerValue = new();
        readonly Button btnSend = new();
        readonly Pan panChannel = new();

        // backing:
        Color _selectedColor = Color.Red;
        int _controller = 0;
        int _controllerValue = 0;
        bool _selected = false;
        #endregion

        #region Properties
        /// <summary>My channel.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public OutputChannel BoundChannel { get; set; }

        /// <summary>Drawing the active elements of a control.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Color DrawColor
        {
            get { return sldVolume.DrawColor; }
            set { sldVolume.DrawColor = value;
                  sldControllerValue.DrawColor = value;
                  btnSend.BackColor = value;
                }
        }

        /// <summary>Drawing the control when selected.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Color SelectedColor
        {
            get { return _selectedColor; }
            set { _selectedColor = value; }
        }

        /// <summary>For muting/soloing.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ChannelState State
        {
            get { return _state; }
            set { _state = value; UpdateUi(); }
        }

        /// <summary>Current volume.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public double Volume
        {
            get { return sldVolume.Value; }
            set { sldVolume.Value = value; }
        }

        /// <summary>Edit current controller number.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Controller
        {
            get { return _controller; }
            set { if (value is < 0 or > MidiDefs.MAX_MIDI) throw new ArgumentOutOfRangeException($"Controller:{value}");
                  else _controller = value; }
        }

        /// <summary>Controller payload.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int ControllerValue
        {
            get { return _controller; }
            set { if (value is < 0 or > MidiDefs.MAX_MIDI) throw new ArgumentOutOfRangeException($"ControllerValue:{value}");
                  else _controllerValue = value; }
        }

        /// <summary>User selection.</summary>
        public bool Selected
        {
            get { return _selected; }
            private set { _selected = value; lblInfo.BackColor = _selected ? SelectedColor : BackColor; }
        }
        #endregion

        #region Events
        /// <summary>UI channel config change.</summary>
        public event EventHandler<ChannelChangeEventArgs>? ChannelChange;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public ChannelControl()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            // Dummy channel to satisfy designer. Will be overwritten by the real one.
            var dev = new NullOutputDevice("nullout:DUMMY_DEVICE");
            BoundChannel = new OutputChannel(dev, 9, 0);
        }

        /// <summary>
        /// Apply customization from system. Properties should be valid now.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            SuspendLayout();

            // Create the controls per the config.
            const int HT1 = 24;
            const int HT2 = 42;

            //int xPos = PAD;
            //int yPos = HEIGHT;

            {
                sldVolume.Minimum = 0.0;
                sldVolume.Maximum = VolumeDefs.MAX_VOLUME;
                sldVolume.Resolution = 0.05;
                sldVolume.Value = 1.0;
                sldVolume.BorderStyle = BorderStyle.FixedSingle;
                sldVolume.Orientation = Orientation.Horizontal;
                sldVolume.Location = new(PAD, PAD + HT1);
                sldVolume.Size = new(80, HT2);
                sldVolume.Label = "volume";
                sldVolume.ValueChanged += (sender, e) => BoundChannel.Volume = (sender as Slider)!.Value;
                Controls.Add(sldVolume);
            }

            //if (Options.HasFlag(DisplayOptions.SoloMute))
            {
                int smSize = 20;

                lblSolo.Location = new(sldVolume.Right + PAD, sldVolume.Top);
                //lblSolo.Location = new(lblMute.Right + PAD, sldVolume.Height);
                lblSolo.Size = new(smSize, smSize);
                lblSolo.Text = "S";
                lblSolo.Click += SoloMute_Click;
                Controls.Add(lblSolo);
                //xPos = lblSolo.Right + PAD;
                //xMax = xPos;

                //lblMute.Location = new(sldVolume.Right + PAD, sldVolume.Height);
                lblMute.Location = new(sldVolume.Right + PAD, sldVolume.Top + smSize + 2);
                lblMute.Size = new(smSize, smSize);
                lblMute.Text = "M";
                lblMute.Click += SoloMute_Click;
                Controls.Add(lblMute);
            }

            {
                lblInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                lblInfo.BorderStyle = BorderStyle.FixedSingle;
                lblInfo.Location = new(PAD, PAD);
                lblInfo.Size = new(lblSolo.Right, HT1 - PAD);
                lblInfo.Click += ChannelInfo_Click;
                Controls.Add(lblInfo);
            }

            Size = new Size(lblInfo.Right + PAD, sldVolume.Bottom + PAD);

            ResumeLayout(false);

            PerformLayout();

            toolTip = new(components);

            // Other inits.
            sldVolume.Value = BoundChannel.Volume;
            UpdateUi();

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
        // /// <summary>User clicked info aka selected it. Maybe host would like to do something.</summary>
        void ChannelInfo_Click(object? sender, EventArgs e)
        {
            Selected = !Selected;

            // Notify client.
            ChannelChange?.Invoke(this, new() { Info = true });
        }

        /// <summary>Handles solo and mute.</summary>
        void SoloMute_Click(object? sender, EventArgs e)
        {
            var lbl = sender as Label;

            // Figure out state.
            if (sender == lblSolo)
            {
                State = lblSolo.BackColor == SelectedColor ? ChannelState.Normal : ChannelState.Solo;
            }
            else if (sender == lblMute)
            {
                State = lblMute.BackColor == SelectedColor ? ChannelState.Normal : ChannelState.Mute;
            }

            // Notify client.
            ChannelChange?.Invoke(this, new() { State = true });
        }
        #endregion

        #region Misc
        /// <summary>Draw mode checkboxes etc.</summary>
        void UpdateUi()
        {
            StringBuilder sb = new();

            sb.AppendLine($"Channel {BoundChannel.ChannelNumber} {BoundChannel.ChannelName}");
            sb.AppendLine($"{BoundChannel.PatchName} {BoundChannel.Patch:000}");
            sb.AppendLine(BoundChannel.DeviceInfo);
            toolTip.SetToolTip(lblInfo, sb.ToString());

            lblInfo.Text = HandleOps.Format(BoundChannel.Handle);

            lblSolo.BackColor = _state == ChannelState.Solo ? SelectedColor :  BackColor;
            lblMute.BackColor = _state == ChannelState.Mute ? SelectedColor : BackColor;
        }

        /// <summary>Read me.</summary>
        public override string ToString()
        {
           return $"{HandleOps.Format(BoundChannel.Handle)}";
        }
        #endregion
    }
}
