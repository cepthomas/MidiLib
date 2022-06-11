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
        /// <summary>User selection.</summary>
        public int PatchNumber { get; private set; } = -1;

        /// <summary>
        /// Constructor.
        /// </summary>
        public PatchPicker()
        {
            InitializeComponent();
            Location = Cursor.Position;

            for (int i = 0; i < MidiDefs.MAX_MIDI; i++)
            {
                lv.Items.Add(MidiDefs.GetInstrumentName(i));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void List_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ind = lv.SelectedIndices[0];
            PatchNumber = ind;
            Close();
        }

        /// <summary>
        /// Check for escape.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void List_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                PatchNumber = -1;
                Close();
            }
        }
    }
}
