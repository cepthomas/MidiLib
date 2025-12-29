using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Ephemera.MidiLib
{
    /// <summary>Base class for custom renderers.</summary>
    public class UserRenderer : UserControl
    {
        /// <summary>For midi sends.</summary>
        public int ChannelNumber { get; init; }

        /// <summary>Parent hooks this.</summary>
        public event EventHandler<BaseMidi>? SendMidi;

        /// <summary>Derived control helper.</summary>
        protected void OnSendMidi(BaseMidi e)
        {
            SendMidi?.Invoke(this, e);
        }
    }
}
