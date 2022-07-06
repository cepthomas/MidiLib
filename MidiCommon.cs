using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiLib
{
    #region Enums
    /// <summary>Channel state.</summary>
    public enum ChannelState { Normal = 0, Solo = 1, Mute = 2 }

    /// <summary>User selection options.</summary>
    public enum SnapType { Bar, Beat, Subdiv }

    ///// <summary>Resolution.</summary>
    //public enum PPQ { PPQ_4 = 4, PPQ_8 = 8, PPQ_16 = 16, PPQ_32 = 32 }
    #endregion

    #region Special event types
    /// <summary>Custom default type to avoid handling null everywhere.</summary>
    public class NullMidiEvent : MidiEvent
    {
        /// <summary>Single constructor.</summary>
        public NullMidiEvent() : base(0, 0, MidiCommandCode.MetaEvent)
        {
        }

        public override string ToString()
        {
            return $"NullMidiEvent: {base.ToString()}";
        }
    }

    /// <summary>Custom type to support runtime functions.</summary>
    public class FunctionMidiEvent : MidiEvent
    {
        /// <summary>The function to call.</summary>
        public Action ScriptFunction { get; init; }

        /// <summary>
        /// Single constructor.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="channel"></param>
        /// <param name="scriptFunc"></param>
        public FunctionMidiEvent(int time, int channel, Action scriptFunc) : base(time, channel, MidiCommandCode.MetaEvent)
        {
            ScriptFunction = scriptFunc;
        }

        /// <summary>For viewing pleasure.</summary>
        public override string ToString()
        {
            return $"FunctionMidiEvent: {base.ToString()} function:{ScriptFunction}";
        }
    }
    #endregion

    #region Event args
    /// <summary>Notify host of asynchronous changes from user.</summary>
    public class ChannelChangeEventArgs : EventArgs
    {
        public bool PatchChange { get; set; } = false;
        public bool StateChange { get; set; } = false;
        public bool ChannelNumberChange { get; set; } = false;
    }

    /// <summary>
    /// Midi (real or sim) has received something. It's up to the client to make sense of it.
    /// Property value of -1 indicates invalid or not pertinent e.g a controller event doesn't have velocity.
    /// </summary>
    public class InputEventArgs : EventArgs
    {
        /// <summary>Channel number 1-based. Required.</summary>
        public int Channel { get; set; } = 0;

        /// <summary>The note number to play. NoteOn/Off only.</summary>
        public int Note { get; set; } = -1;

        /// <summary>Specific controller id.</summary>
        public int Controller { get; set; } = -1;

        /// <summary>For Note = velocity. For controller = payload.</summary>
        public int Value { get; set; } = -1;

        /// <summary>Something to tell the client.</summary>
        public string ErrorInfo { get; set; } = "";

        /// <summary>Special controller id to carry pitch info.</summary>
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
                sb.Append($"Channel:{Channel} Note:{Note} Controller:{Controller} Value:{Value}");
            }

            return sb.ToString();
        }
    }
    #endregion

    #region Definitions
    public class Definitions
    {
        /// <summary>Internal resolution.</summary>
        public const int InternalPPQ = 32;
    }

    public class VolumeDefs
    {
        /// <summary>Corresponds to midi velocity = 0.</summary>
        public const double MIN = 0.0;

        /// <summary>Corresponds to midi velocity = 127.</summary>
        public const double MAX = 1.0;

        /// <summary>Default value.</summary>
        public const double DEFAULT = 0.8;

        /// <summary>Allow UI controls some more headroom.</summary>
        public const double MAX_GAIN = 2.0;
    }
    #endregion
}
