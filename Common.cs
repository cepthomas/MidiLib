using System;
using System.Collections.Generic;
using System.Linq;


namespace Ephemera.MidiLib
{
    /// <summary>Library error.</summary>
    public class MidiLibException(string message) : Exception(message) { }

    /// <summary>User selection options.</summary>
    public enum SnapType { Tick, Beat, Bar, FourBar }

    /// <summary>Misc definitions.</summary>
    public class VolumeDefs
    {
        /// <summary>Default value.</summary>
        public const double DEFAULT_VOLUME = 0.8;

        /// <summary>Allow UI controls some more headroom.</summary>
        public const double MAX_VOLUME = 2.0;
    }
}
