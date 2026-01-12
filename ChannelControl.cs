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

    /// <summary>Some flavors of control may need to be defeatured.</summary>
    [Flags]
    public enum DisplayOptions
    {
        None = 0x00,        // none
        SoloMute = 0x01,    // solo and mute buttons
        Controller = 0x02,  // controller select and send
        All = 0x0F,         // all of above
    }

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
    public class ChannelControl : UserControl
    {
        #region Fields
        ChannelState _state = ChannelState.Normal;
        const int PAD = 4;
        const int SIZE = 42;

        readonly Container components = new();
        protected ToolTip toolTip = new();

        // always:
        readonly Label lblInfo = new();
        readonly Slider sldVolume = new();

        // optional:
        readonly Label lblSolo = new();
        readonly Label lblMute = new();

        // optional:
        readonly Slider sldControllerValue = new();
        readonly Button btnSend = new();

        // backing:
        UserRenderer? _userRenderer = null;
        Color _selectedColor = Color.Red;
        int _controllerId = 0;
        int _controllerValue = 0;
        bool _selected = false;


        #endregion

        #region Properties
        /// <summary>My channel.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public OutputChannel BoundChannel { get; set; }

        /// <summary>My custom renderer - optional.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public UserRenderer? UserRenderer
        {
            get { return _userRenderer; }
            set { _userRenderer = value;
                  if (_userRenderer is not null)
                  {
                      _userRenderer.Location = new(PAD, Height);
                      Height += _userRenderer.Height + PAD;
                      Controls.Add(_userRenderer);
                  }
                }
        }

        /// <summary>Display options.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public DisplayOptions Options { get; set; } = DisplayOptions.All;

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
        public int ControllerId
        {
            get { return _controllerId; }
            set { if (value is < 0 or > MidiDefs.MAX_MIDI) throw new ArgumentOutOfRangeException($"ControllerId:{value}");
                  else _controllerId = value; }
        }

        /// <summary>Controller payload.</summary>
        public int ControllerValue
        {
            get { return _controllerId; }
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

        /// <summary>UI midi send.</summary>
        public event EventHandler<BaseEvent>? SendMidi;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public ChannelControl()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            // Dummy channel to satisfy designer. Will be overwritten by the real one.
            var dev = new NullOutputDevice("DUMMY_DEVICE");
            BoundChannel = new OutputChannel(dev, 9, "AcousticGrandPiano");
        }

        /// <summary>
        /// Apply customization from system. Properties should be valid now.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            SuspendLayout();
            
            // Create the controls per the config.
            int xPos = PAD;
            int xMax = xPos;
            int yPos = PAD;
            int yMax = yPos;

            // The standard stuff.
            {
                lblInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                lblInfo.BorderStyle = BorderStyle.FixedSingle;
                lblInfo.Location = new(xPos, yPos);
                lblInfo.Size = new(55, SIZE);
                lblInfo.Click += ChannelInfo_Click;
                Controls.Add(lblInfo);

                xPos = lblInfo.Right + PAD;
                xMax = xPos;
                yMax = Math.Max(lblInfo.Bottom, yMax);
            }

            {
                sldVolume.Minimum = 0.0;
                sldVolume.Maximum = VolumeDefs.MAX_VOLUME;
                sldVolume.Resolution = 0.05;
                sldVolume.Value = 1.0;
                sldVolume.BorderStyle = BorderStyle.FixedSingle;
                sldVolume.Orientation = Orientation.Horizontal;
                sldVolume.Location = new(xPos, yPos);
                sldVolume.Size = new(80, SIZE);
                sldVolume.Label = "volume";
                sldVolume.ValueChanged += (sender, e) => BoundChannel.Volume = (sender as Slider)!.Value;
                Controls.Add(sldVolume);

                xPos = sldVolume.Right + PAD;
                xMax = xPos;
                yMax = Math.Max(sldVolume.Bottom, yMax);
            }

            if (Options.HasFlag(DisplayOptions.SoloMute))
            {
                lblSolo.Location = new(xPos, yPos + 1);
                lblSolo.Size = new(20, 20);
                lblSolo.Text = "S";
                lblSolo.Click += SoloMute_Click;
                Controls.Add(lblSolo);

                lblMute.Location = new(xPos, yPos  + SIZE / 2 + 1);
                lblMute.Size = new(20, 20);
                lblMute.Text = "M";
                lblMute.Click += SoloMute_Click;
                Controls.Add(lblMute);

                xPos = lblSolo.Right + PAD;
                xMax = xPos;
                yMax = Math.Max(lblMute.Bottom, yMax);
            }

            if (Options.HasFlag(DisplayOptions.Controller))
            {
                btnSend.FlatStyle = FlatStyle.Flat;
                btnSend.UseVisualStyleBackColor = true;
                btnSend.Location = new(xPos, yPos);
                btnSend.Size = new(SIZE, SIZE);
                btnSend.Text = "!";
                btnSend.Click += Send_Click;
                Controls.Add(btnSend);

                xPos = btnSend.Right + PAD;
                yMax = Math.Max(btnSend.Bottom, yMax);

                sldControllerValue.Minimum = 0;
                sldControllerValue.Maximum = MidiDefs.MAX_MIDI;
                sldControllerValue.Resolution = 1;
                sldControllerValue.Value = 50;
                sldControllerValue.BorderStyle = BorderStyle.FixedSingle;
                sldControllerValue.Orientation = Orientation.Horizontal;
                sldControllerValue.Location = new(xPos, yPos);
                sldControllerValue.Size = new(80, SIZE);
                sldControllerValue.Label = "value";
                sldControllerValue.ValueChanged += Controller_ValueChanged;
                Controls.Add(sldControllerValue);

                xPos = sldControllerValue.Right + PAD;
                xMax = xPos;
                yMax = Math.Max(sldControllerValue.Bottom, yMax);
            }

            if (_userRenderer is not null)
            {
                _userRenderer.Location = new(PAD, yMax + PAD);
                xMax = Math.Max(_userRenderer.Right, xPos);
                yMax = Math.Max(_userRenderer.Bottom, yMax);
                Controls.Add(_userRenderer);
            }

            // Form itself.
            Size = new Size(xMax + PAD, yMax + PAD);

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

        /// <summary>
        /// Notify client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Controller_ValueChanged(object? sender, EventArgs e)
        {
            // No need to check limits.
            ControllerValue = (int)(sender as Slider)!.Value;
        }

        /// <summary>
        /// Notify client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Send_Click(object? sender, EventArgs e)
        {
            // No need to check limits.
            SendMidi?.Invoke(this, new Controller(BoundChannel.ChannelNumber, ControllerId, ControllerValue, MusicTime.ZERO));
        }
        #endregion

        #region Misc
        /// <summary>Draw mode checkboxes etc.</summary>
        void UpdateUi()
        {
            StringBuilder sb = new();

            sb.AppendLine($"Channel {BoundChannel.ChannelNumber} {BoundChannel.ChannelName}");
            sb.AppendLine($"{BoundChannel.GetInstrumentName(BoundChannel.Patch)} {BoundChannel.Patch:000}");
            sb.AppendLine($"Dev{BoundChannel.Device.Id} {BoundChannel.Device.DeviceName}");
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
