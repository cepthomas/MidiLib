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
        Fx = 0x02,          // effects
        All = 0x0F,         // all of above
    }
    #endregion

    [DesignTimeVisible(true)]
    public partial class ChannelControl : UserControl
    {
        #region Fields
        ChannelState _state = ChannelState.Normal;
        const int PAD = 4;
        const int SIZE = 42;

        // optional:
        // readonly Pan panChannel = new();

        // backing:
        UserRenderer? _userRenderer = null;
        Color _selectedColor = Color.Red;
        bool _selected = false;
        #endregion

        #region Properties
        /// <summary>My channel.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public OutputChannel BoundChannel { get; set; }

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
                  panChannel.DrawColor = value;
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

            InitializeComponent();
        }

        /// <summary>
        /// Apply customization from system. Properties should be valid now.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            lblInfo.Click += ChannelInfo_Click;
            sldVolume.ValueChanged += (sender, e) => BoundChannel.Volume = (sender as Slider)!.Value;
            lblSolo.Click += SoloMute_Click;
            lblMute.Click += SoloMute_Click;

            if (Options.HasFlag(DisplayOptions.Fx))
            {
  
            }

            if (_userRenderer is not null)
            {
                _userRenderer.Location = new(PAD, 100 + PAD);
                //_userRenderer.Location = new(PAD, yMax + PAD);
                //xMax = Math.Max(_userRenderer.Right, xPos);
                //yMax = Math.Max(_userRenderer.Bottom, yMax);
                Controls.Add(_userRenderer);
            }

            // Control itself.
//            Size = new Size(xMax + PAD, yMax + PAD);

            // Other inits.
            sldVolume.Value = BoundChannel.Volume;
            UpdateUi();

            base.OnLoad(e);
        }

        /// <summary>
        /// A custom renderer - optional.
        /// </summary>
        /// <param name="rend"></param>
        public void SetRenderer(UserRenderer rend)
        {
            _userRenderer = rend;

            // Handle custom renderer events. Fix the channel number.
            rend.SendMidi += (object? sender, BaseEvent e) =>
            {
                e.ChannelNumber = BoundChannel.ChannelNumber;
                BoundChannel.Send(e);
             //   SendMidi?.Invoke(this, e);
            };

            rend.Location = new(PAD, Height);
            Height += rend.Height + PAD;
            Controls.Add(rend);
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


    #region Supplementary classes
    /// <summary>Notify host of UI changes.</summary>
    public class ChannelChangeEventArgs : EventArgs
    {
        /// <summary>User clicked info.</summary>
        public bool Info { get; set; } = false;
        /// <summary>Solo/mute.</summary>
        public bool State { get; set; } = false;
    }

    /// <summary>Base class for custom renderers.</summary>
    public class UserRenderer : UserControl
    {
        /// <summary>Parent hooks this.</summary>
        public event EventHandler<BaseEvent>? SendMidi;

        /// <summary>Derived control helper.</summary>
        protected void OnSendMidi(BaseEvent e)
        {
            SendMidi?.Invoke(this, e);
        }
    }
    #endregion

}
