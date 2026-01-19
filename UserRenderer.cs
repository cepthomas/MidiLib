using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Ephemera.MidiLib
{
    /// <summary>Base class for custom renderers.</summary>
    public class UserRenderer : UserControl
    {
        /// <summary>For midi sends.</summary>
//        public int ChannelNumber { get; init; }
        public int Handle { get; init; }

        /// <summary>Parent hooks this.</summary>
        public event EventHandler<BaseEvent>? SendMidi;

        /// <summary>Derived control helper.</summary>
        protected void OnSendMidi(BaseEvent e)
        {
            SendMidi?.Invoke(this, e);
        }
    }
}
