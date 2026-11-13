using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms.Design;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    /// <summary>User selection options.</summary>
    public enum SnapType { Tick, Beat, Bar, FourBar }

    /// <summary>Library error.</summary>
    public class MidiLibException(string message) : Exception(message) { }

    /// <summary>Misc definitions.</summary>
    public class MidiLibDefs
    {
        /// <summary>Default value.</summary>
        public const double DEFAULT_VOLUME = 0.8;

        /// <summary>Allow UI controls some more headroom.</summary>
        public const double MAX_VOLUME = 2.0;

        /// <summary>Resolution of UI.</summary>
        public const double RES_VOLUME = MAX_VOLUME / 50;
    }
}
