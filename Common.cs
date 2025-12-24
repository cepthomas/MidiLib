using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    /// <summary>Library error.</summary>
    public class MidiLibException(string message) : Exception(message) { }

    /// <summary>User selection options.</summary>
    public enum SnapType { Bar, Beat, Tick }

    /// <summary>Misc definitions.</summary>
    public class Defs
    {
        /// <summary>Default value.</summary>
        public const double DEFAULT_VOLUME = 0.8;

        /// <summary>Allow UI controls some more headroom.</summary>
        public const double MAX_VOLUME = 2.0;
    }
}
