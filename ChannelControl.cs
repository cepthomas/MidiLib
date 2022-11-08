using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    /// <summary>Channel events and other properties.</summary>
    public partial class ChannelControl : UserControl
    {
        #region Events
        /// <summary>Notify host of asynchronous changes from user.</summary>
        public event EventHandler<ChannelChangeEventArgs>? ChannelChange;
        #endregion

        #region Properties
        /// <summary>Bound object.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public Channel BoundChannel { get; set; } = new();

        /// <summary>Actual 1-based midi channel number for UI.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public int ChannelNumber
        {
            get { return BoundChannel.ChannelNumber; }
        }

        /// <summary>For muting/soloing.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public ChannelState State
        {
            get { return BoundChannel.State; }
            set { BoundChannel.State = value; UpdateUi(); }
        }

        /// <summary>Current patch.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public int Patch
        {
            get { return BoundChannel.Patch; }
            set { BoundChannel.Patch = value; UpdateUi(); }
        }

        /// <summary>Current volume. Channel.Volume performs the constraints.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public double Volume
        {
            get { return BoundChannel.Volume; }
            set { BoundChannel.Volume = value; }
        }

        ///<summary>The duration of the whole channel.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public int MaxSubdiv
        {
            get { return BoundChannel.MaxSubdiv; }
        }

        /// <summary>Drum channel changed.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public bool IsDrums
        {
            get { return BoundChannel.IsDrums; }
            set { BoundChannel.IsDrums = value; UpdateUi(); }
        }

        /// <summary>User has selected this channel.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public bool Selected
        {
            get { return BoundChannel.Selected; }
            set { BoundChannel.Selected = value; UpdateUi(); }
        }

        /// <summary>Indicate user selected.</summary>
        public Color SelectedColor { get; set; } = Color.Aquamarine;

        /// <summary>Indicate user not selected.</summary>
        public Color UnselectedColor { get; set; } = DefaultBackColor;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public ChannelControl()
        {
            InitializeComponent();
            sldVolume.ValueChanged += Volume_ValueChanged;
            lblSolo.Click += SoloMute_Click;
            lblMute.Click += SoloMute_Click;
            lblChannelNumber.Click += ChannelNumber_Click;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {

            sldVolume.Value = BoundChannel.Volume;
            sldVolume.DrawColor = SelectedColor;
            sldVolume.Minimum = MidiLibDefs.VOLUME_MIN;
            sldVolume.Maximum = MidiLibDefs.MAX_GAIN;

            UpdateUi();

            base.OnLoad(e);
        }
        #endregion

        #region Handlers for user selections
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Volume_ValueChanged(object? sender, EventArgs e)
        {
            if (sender is not null)
            {
                Volume = (sender as NBagOfUis.Slider)!.Value;
            }
        }

        /// <summary>
        /// Handles solo and mute.
        /// </summary>
        void SoloMute_Click(object? sender, EventArgs e)
        {
            if (sender is not null)
            {
                var lbl = sender as Label;

                // Figure out state.
                ChannelState newState = ChannelState.Normal; // default

                // Toggle control. Get current.
                bool soloSel = lblSolo.BackColor == SelectedColor;
                bool muteSel = lblMute.BackColor == SelectedColor;

                if (lbl == lblSolo)
                {
                    if(soloSel) // unselect
                    {
                        if(muteSel)
                        {
                            newState = ChannelState.Mute;
                        }
                    }
                    else // select
                    {
                        newState = ChannelState.Solo;
                    }
                }
                else // lblMute
                {
                    if (muteSel) // unselect
                    {
                        if (soloSel)
                        {
                            newState = ChannelState.Solo;
                        }
                    }
                    else // select
                    {
                        newState = ChannelState.Mute;
                    }
                }

                if(newState != State)
                {
                    State = newState;
                    UpdateUi();
                    ChannelChange?.Invoke(this, new() { StateChange = true });
                }
            }
        }

        /// <summary>
        /// User wants to change the patch.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Patch_Click(object sender, EventArgs e)
        {
            PatchPicker pp = new();
            pp.ShowDialog();
            if (pp.PatchNumber != -1)
            {
                BoundChannel.Patch = pp.PatchNumber;
                UpdateUi();
                ChannelChange?.Invoke(this, new() { PatchChange = true });
            }
        }

        /// <summary>
        /// Handle selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelNumber_Click(object? sender, EventArgs e)
        {
            Selected = !Selected;
            UpdateUi();
        }
        #endregion

        #region Misc
        /// <summary>
        /// Draw mode checkboxes etc.
        /// </summary>
        void UpdateUi()
        {
            // Solo/mute state.
            switch (State)
            {
                case ChannelState.Normal:
                    lblSolo.BackColor = UnselectedColor;
                    lblMute.BackColor = UnselectedColor;
                    break;
                case ChannelState.Solo:
                    lblSolo.BackColor = SelectedColor;
                    lblMute.BackColor = UnselectedColor;
                    break;
                case ChannelState.Mute:
                    lblSolo.BackColor = UnselectedColor;
                    lblMute.BackColor = SelectedColor;
                    break;
            }

            // General.
            lblChannelNumber.Text = $"Ch{ChannelNumber}";
            lblChannelNumber.BackColor = Selected ? SelectedColor : UnselectedColor;
            lblPatch.Text = IsDrums ? "Drums" : MidiDefs.GetInstrumentName(BoundChannel.Patch);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"ChannelControl: ChannelNumber:{ChannelNumber} Patch:{Patch} State:{State}";
        }
        #endregion
    }
}
