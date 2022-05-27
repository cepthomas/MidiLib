using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiLib
{
    #region Types
    /// <summary>Player state.</summary>
    public enum MidiState { Stopped = 0, Playing = 1, Complete = 2 }

    /// <summary>Channel state.</summary>
    public enum ChannelState { Normal = 0, Solo = 1, Mute = 2 }

    /// <summary>User selection options.</summary>
    public enum SnapType { Subdiv, Beat, Bar }

    /// <summary>Placeholder to avoid handling null everywhere.</summary>
    public class NullMidiEvent : MidiEvent { }
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

    public class LibSettings
    {
        /// <summary>Option for engineers instead of musicians.</summary>
        public static bool ZeroBased { get; set; } = false; //TODOX

        /// <summary>How to snap.</summary>
        public static SnapType Snap { get; set; } = SnapType.Beat;
    }

    /// <summary>
    /// Midi has received something. It's up to the client to make sense of it.
    /// Property value of -1 indicates invalid or not pertinent e.g a controller event doesn't have velocity.
    /// </summary>
    public class MidiEventArgs : EventArgs
    {
        /// <summary>Channel number.</summary>
        public int Channel { get; set; } = -1;

        /// <summary>The note number to play.</summary>%
        public int Note { get; set; } = -1;

        /// <summary>The volume.</summary>
        public int Velocity { get; set; } = -1;

        /// <summary>Specific controller.</summary>
        public int ControllerId { get; set; } = -1;

        /// <summary>The controller payload.</summary>
        public int ControllerValue { get; set; } = -1;

        /// <summary>Something to tell the client.</summary>
        public string ErrorInfo { get; set; } = "";

        /// <summary>Special id to carry pitch info.</summary>
        public const int PITCH_CONTROL = 1000;

        /// <summary>Read me.</summary>
        public override string ToString()
        {
            StringBuilder sb = new($"Channel:{Channel} ");

            if (ErrorInfo != "")
            {
                sb.Append($"Error:{ErrorInfo} ");
            }
            else
            {
                if (Note != -1)
                {
                    sb.Append($"Note:{Note} ");
                }
                if (Velocity != -1)
                {
                    sb.Append($"Velocity:{Velocity} ");
                }
                if (ControllerId != -1)
                {
                    sb.Append($"ControllerId:{ControllerId} ");
                }
                if (ControllerValue != -1)
                {
                    sb.Append($"ControllerValue:{ControllerValue} ");
                }
            }

            return sb.ToString();
        }
    }
}
