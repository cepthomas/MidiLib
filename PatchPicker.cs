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
    public partial class PatchPicker : Form
    {
        /// <summary>Control.</summary>
        readonly ListView _lv = new();

        /// <summary>User selection.</summary>
        public int PatchNumber { get; private set; } = -1;

        /// <summary>
        /// Constructor.
        /// </summary>
        public PatchPicker()
        {
            Location = Cursor.Position;
            AutoScaleDimensions = new(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new(782, 453);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Name = "PatchPicker";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "Select Patch";

            _lv.BorderStyle = BorderStyle.FixedSingle;
            _lv.Dock = DockStyle.Fill;
            _lv.HideSelection = false;
            _lv.Name = "lv";
            _lv.View = View.List;
            _lv.SelectedIndexChanged += List_SelectedIndexChanged;
            _lv.KeyDown += new KeyEventHandler(this.List_KeyDown);
            Controls.Add(_lv);

            for (int i = 0; i < MidiDefs.MAX_MIDI; i++)
            {
                _lv.Items.Add(MidiDefs.GetInstrumentName(i));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void List_SelectedIndexChanged(object? sender, EventArgs e)
        {
            int ind = _lv.SelectedIndices[0];
            PatchNumber = ind;
            Close();
        }

        /// <summary>
        /// Check for escape.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void List_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                PatchNumber = -1;
                Close();
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
