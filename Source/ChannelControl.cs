using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace MidiLib
{
    /// <summary>Channel events and other properties.</summary>
    public partial class ChannelControl : UserControl
    {
        #region Events
        /// <summary>Notify client of asynchronous changes from user.</summary>
        public event EventHandler<ChannelChangeEventArgs>? ChannelChange;
        public class ChannelChangeEventArgs : EventArgs
        {
            public bool PatchChange { get; set; } = false;
            public bool StateChange { get; set; } = false;
        }
        #endregion

        #region Properties
        /// <summary>Bound object.</summary>
        public Channel Channel { get; set; } = new();

        /// <summary>Actual 1-based midi channel number for UI.</summary>
        public int ChannelNumber
        {
            get { return Channel.ChannelNumber; }
        }

        /// <summary>For muting/soloing.</summary>
        public ChannelState State
        {
            get { return Channel.State; }
            set { Channel.State = value; UpdateUi(); }
        }

        /// <summary>Current patch.</summary>
        public PatchInfo Patch
        {
            get { return Channel.Patch; }
            set { Channel.Patch = value; UpdateUi(); }
        }

        /// <summary>Current volume.</summary>
        public double Volume
        {
            get { return Channel.Volume; }
            set { Channel.Volume = value; }
        }

        ///<summary>The duration of the whole channel.</summary>
        public int MaxSubdiv
        {
            get { return Channel.MaxSubdiv; }
        }

        /// <summary>User has selected this channel.</summary>
        public bool Selected
        {
            get { return Channel.Selected; }
            set { Channel.Selected = value; UpdateUi(); }
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
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_Load(object? sender, EventArgs e)
        {
            sldVolume.DrawColor = SelectedColor;
            sldVolume.Minimum = Channel.MIN_VOLUME;
            sldVolume.Maximum = Channel.MAX_VOLUME;
            sldVolume.ValueChanged += Volume_ValueChanged;
            lblSolo.Click += SoloMute_Click;
            lblMute.Click += SoloMute_Click;
            lblChannelNumber.Click += ChannelNumber_Click;
            lblDrums.Click += Drums_Click;

            UpdateUi();
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
                    ChannelChange?.Invoke(this, new ChannelChangeEventArgs() { StateChange = true });
                }
            }
        }

        /// <summary>
        /// Handles is drums button.
        /// </summary>
        void Drums_Click(object? sender, EventArgs e)
        {
            if (sender is not null)
            {
                Channel.Patch.Modifier = Channel.Patch.Modifier == PatchInfo.PatchModifier.IsDrums ?
                    PatchInfo.PatchModifier.None : PatchInfo.PatchModifier.IsDrums;
                UpdateUi();
            }
        }

        /// <summary>
        /// User wants to change the patch.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Patch_Click(object sender, EventArgs e)
        {
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

            lv.Items.Add("NoPatch");
            for (int i = 0; i < MidiDefs.MAX_MIDI; i++)
            {
                lv.Items.Add(MidiDefs.GetInstrumentDef(i));
            }

            lv.Click += (object? sender, EventArgs e) =>
            {
                int ind = lv.SelectedIndices[0];
                Channel.Patch.PatchNumber = ind - 1; // skip NoPatch entry

                UpdateUi();
                ChannelChange?.Invoke(this, new() { PatchChange = true });
                f.Close();
            };

            f.Controls.Add(lv);
            f.ShowDialog();
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
            lblDrums.BackColor = Channel.Patch.Modifier == PatchInfo.PatchModifier.IsDrums ? SelectedColor : UnselectedColor;
            lblChannelNumber.BackColor = Selected ? SelectedColor : UnselectedColor;

            switch(Channel.Patch.Modifier)
            {
                case PatchInfo.PatchModifier.None:
                    lblPatch.Text = MidiDefs.GetInstrumentDef(Channel.Patch.PatchNumber);
                    break;

                case PatchInfo.PatchModifier.NotAssigned:
                    lblPatch.Text = "NoPatch";
                    break;

                case PatchInfo.PatchModifier.IsDrums:
                    lblPatch.Text = "IsDrums";
                    break;
            }
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
