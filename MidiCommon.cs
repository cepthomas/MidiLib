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

    /// <summary>Placeholder to avoid handling null everywhere.</summary>
    public class NullMidiEvent : MidiEvent { }

    public class VolumeDefs
    {
        public const double MIN = 0.0;
        public const double MAX = 2.0;
        public const double DEFAULT = 0.8;
        public const double RESOLUTION = 0.1;
    }
    #endregion

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
