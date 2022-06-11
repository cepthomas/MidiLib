using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiLib
{
    #region Types
    /// <summary>Channel state.</summary>
    public enum ChannelState { Normal = 0, Solo = 1, Mute = 2 }

    /// <summary>User selection options.</summary>
    public enum SnapType { Bar, Beat, Subdiv }

    /// <summary>Placeholder to avoid handling null everywhere.</summary>
    public class NullMidiEvent : MidiEvent { }

    /// <summary>Notify host of asynchronous changes from user.</summary>
    public class ChannelChangeEventArgs : EventArgs
    {
        public bool PatchChange { get; set; } = false;
        public bool StateChange { get; set; } = false;
        public bool ChannelNumberChange { get; set; } = false;
    }
    #endregion

    public class InternalDefs
    {
        public const double VOLUME_MIN = 0.0;
        public const double VOLUME_MAX = 2.0;
        public const double VOLUME_DEFAULT = 0.8;
        public const double VOLUME_RESOLUTION = 0.1;

        /// <summary>Only 4/4 time supported.</summary>
        public const int BEATS_PER_BAR = 4;

        /// <summary>Internal time resolution aka ppq or DeltaTicksPerQuarterNote.</summary>
        public const int SUBDIVS_PER_BEAT = 32;

        /// <summary>Convenience.</summary>
        public const int SUBDIVS_PER_BAR = SUBDIVS_PER_BEAT * BEATS_PER_BAR;
    }

    /// <summary>
    /// Global things.
    /// </summary>
    public class MidiSettings
    {
        /// <summary>Option for engineers instead of musicians.</summary>
        public static bool ZeroBased { get; set; } = false;

        /// <summary>How to snap.</summary>
        public static SnapType Snap { get; set; } = SnapType.Beat;
    }
}
