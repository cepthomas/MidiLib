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
using System.Runtime.CompilerServices;


namespace Ephemera.MidiLib
{   
    /// <summary>Notify host of UI changes.</summary>
    public class ChannelChangeEventArgs : EventArgs
    {
        /// <summary>Solo/mute.</summary>
        public bool State { get; set; } = false;

        /// <summary>User clicked control.</summary>
        public bool Select { get; set; } = false;

        /// <summary>User clicked info box.</summary>
        public bool Info { get; set; } = false;

        /// <summary>User changed volume.</summary>
        public bool Volume { get; set; } = false;
    }

    [DesignTimeVisible(true)]
    public partial class ChannelControl : UserControl
    {
        #region Fields


        //ChannelState _state = ChannelState.Normal;
        Color _selectedColor = Color.LightPink;
        Color _smColor = Color.Blue;//TODO1 ?
        Color _bgColor;

        // TODO1 whole control selected/colored?
        bool _selected = false;



        // The controls.
        readonly Container components = new();
        readonly Label lblInfo = new();
        readonly Slider sldVolume = new();
        readonly Label lblSolo = new();
        readonly Label lblMute = new();
        readonly Pan panChannel = new();
        ToolTip toolTip = new();
        #endregion

        //???? DesignerSerializationVisibility
        //    /// The code generator will not produce code for the object.//    Hidden,
        //    /// The code generator will produce code for the object.    //    Visible,
        //    /// The code generator will produce code for the contents of the object, rather than for the object itself. //    Content




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
            set { sldVolume.DrawColor = value; panChannel.DrawColor = value; }
        }

        /// <summary>Drawing the control when selected.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Color SelectedColor { set { _selectedColor = value; } }

        ///// <summary>Display options.</summary>
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //public DisplayOptions Options { get; set; } = DisplayOptions.None;

        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //public bool ShowSoloMute { get; set; } = false;

        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //public bool ShowPan { get; set; } = false;


        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //public bool ShowFx { get; set; } = false;


        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //public bool ShowMeter { get; set; } = false;


        ///// <summary>What the mouse click does.</summary>
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[EditorBrowsable(EditorBrowsableState.Never)]
        //public SelectionMode Mode { get; set; } = SelectionMode.None;

        /// <summary>For muting/soloing.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        public ChannelState State { get; set; }
        //{
        //    get { return _state; }
        //    set { _state = value; }
        //}

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
            set { _selected = value; BackColor = _selected ? _selectedColor : _bgColor;  }
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


            // TODO1 options:
            // min: volume/level, info
            // mid: Solo/Mute, Meters
            // max: Fx, Pan


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
                State = lblSolo.BackColor == _smColor ? ChannelState.Normal : ChannelState.Solo;
            }
            else if (sender == lblMute)
            {
                State = lblMute.BackColor == _smColor ? ChannelState.Normal : ChannelState.Mute;
            }

            // Notify client.
            ChannelChange?.Invoke(this, new() { State = true });
        }

        #endregion


        /////////////////////////////////// select stuff ////////////////////////////


        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseEnter(EventArgs e)
        {
            //Debug.WriteLine($"OnMouseEnter {e}");

            //if (MouseButtons == MouseButtons.Left)
            //{
            //    PressVKey(CalcVelocity());
            //}

            base.OnMouseEnter(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            //Debug.WriteLine($"OnMouseLeave {e}");

            //if (IsPressed)
            //{
            //    ReleaseVKey();
            //}

            base.OnMouseLeave(e);
        }

        /// <summary>
        /// Send info to client.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            Debug.WriteLine($"OnMouseDown {_ctrl}");


            //var res = MouseToUser();
            //if (res is not null)
            //{
            //    _lastNote = res.Value.ux;
            //    SendMidi?.Invoke(this, new NoteOn(MidiDefs.TEMP_CHANNEL, res.Value.ux, res.Value.uy));
            //}

            base.OnMouseDown(e);
        }

        /// <summary>
        /// Send info to client.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            Debug.WriteLine($"OnMouseUp {_ctrl}");

            //if (_lastNote != -1)
            //{
            //    SendMidi?.Invoke(this, new NoteOff(MidiDefs.TEMP_CHANNEL, _lastNote));
            //    _lastNote = -1;
            //}

            base.OnMouseUp(e);
        }


        bool _ctrl = false;


        protected override void OnKeyDown(KeyEventArgs e)
        {
            _ctrl = e.Control;
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            _ctrl = false;
            base.OnKeyUp(e);
        }

        /// <summary>
        /// User item selection(s). Could be select or click.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseClick(MouseEventArgs e)
        {

            Debug.WriteLine($"OnMouseClick {_ctrl}");

            // ///  Gets which mouse button was pressed.
            // public MouseButtons Button { get; }

            // ///  Gets the number of times the mouse button was pressed and released.
            // public int Clicks { get; }

            // ///  Gets the x-coordinate of a mouse click.
            // public int X { get; }

            // ///  Gets the y-coordinate of a mouse click.
            // public int Y { get; }

            // ///  Gets a signed count of the number of detents the mouse wheel has rotated.
            // public int Delta { get; }

            // ///  Gets the location of the mouse during MouseEvent.
            // public Point Location => new(X, Y);






            //ArgumentNullException.ThrowIfNull(sender);

            //var itemd = (ItemDisplay)sender;

            //switch (e.Button, _config.Mode, itemd.Selected)
            //{
            //    case (MouseButtons.Left, SelectMode.Click, _):
            //        ItemClick?.Invoke(this, new(itemd.Item));
            //        break;

            //    case (MouseButtons.Left, SelectMode.SingleSelect, true):
            //        // Toggle select.
            //        itemd.Selected = false;
            //        ItemSelection?.Invoke(this, new(GetSelectedItems()));
            //        break;

            //    case (MouseButtons.Left, SelectMode.SingleSelect, false):
            //        // Deselect others first.
            //        _itemds.ForEach(itemd => itemd.Selected = false);
            //        // Select this one.
            //        itemd.Selected = true;
            //        ItemSelection?.Invoke(this, new(GetSelectedItems()));
            //        break;

            //    case (MouseButtons.Left, SelectMode.MultiSelect, true):
            //        itemd.Selected = false;
            //        ItemSelection?.Invoke(this, new(GetSelectedItems()));
            //        break;

            //    case (MouseButtons.Left, SelectMode.MultiSelect, false):
            //        itemd.Selected = true;
            //        ItemSelection?.Invoke(this, new(GetSelectedItems()));
            //        break;

            //    case (_, _, _):
            //        // ignored
            //        break;
            //}

            //Invalidate(true); // refresh everything

            base.OnMouseClick(e);

        }


        #region Misc
        /// <summary>Read me.</summary>
        public override string ToString()
        {
          return lblInfo.Text;
        }
        #endregion
    }
}
