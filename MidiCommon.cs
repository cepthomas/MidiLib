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

    /// <summary>Custom default type to avoid handling null everywhere.</summary>
    public class NullMidiEvent : MidiEvent
    {
        public override string ToString()
        {
            return $"NullMidiEvent: {base.ToString()}";
        }
    }

    /// <summary>Notify host of asynchronous changes from user.</summary>
    public class ChannelChangeEventArgs : EventArgs
    {
        public bool PatchChange { get; set; } = false;
        public bool StateChange { get; set; } = false;
        public bool ChannelNumberChange { get; set; } = false;
    }

    // /// <summary>Virtual device event info.</summary>
    // public class DeviceEventArgs : EventArgs // TODOX use MidiEventArgs? combine into InputEventArgs?
    // {
    //     /// <summary>Midi note number.</summary>
    //     public int Note { get; set; } = 0;

    //     /// <summary>Midi control value, usually velocity but could be anything.</summary>
    //     public int Control { get; set; } = 0;
    // }


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

    /// <summary>Global things.</summary>
    public class MidiSettings
    {
        /// <summary>Option for engineers instead of musicians.</summary>
        public static bool ZeroBased { get; set; } = false;

        /// <summary>How to snap.</summary>
        public static SnapType Snap { get; set; } = SnapType.Beat;
    }


    #region Interfaces
    /// <summary>Abstraction layer to support other midi-like devices e.g. OSC.</summary>
    public interface IMidiInputDevice : IDisposable
    {
        #region Properties
        /// <summary>Device name as defined by the system.</summary>
        string DeviceName { get; }

        /// <summary>Are we ok?</summary>
        bool Valid { get; }

        /// <summary>Log inbound traffic at Trace level.</summary>
        bool LogEnable { get; set; }

        /// <summary>Capture on/off.</summary>
        bool CaptureEnable { set; }
        #endregion

        #region Events
        /// <summary>Handler for message arrived.</summary>
        event EventHandler<InputEventArgs>? InputEvent;
        #endregion

        #region Functions
        // /// <summary>Interfaces don't allow constructors so do this instead.</summary>
        // /// <returns></returns>
        // bool Init();

        ///// <summary>Start listening.</summary>
        //void Start();

        ///// <summary>Stop listening.</summary>
        //void Stop();
        #endregion
    }

    /// <summary>Abstraction layer to support other midi-like devices e.g. OSC.</summary>
    public interface IMidiOutputDevice : IDisposable
    {
        #region Properties
        /// <summary>Device name as defined by the system.</summary>
        string DeviceName { get; }

        /// <summary>Are we ok?</summary>
        bool Valid { get; }

        /// <summary>Log outbound traffic at Trace level.</summary>
        bool LogEnable { get; set; }
        #endregion

        #region Functions
        // /// <summary>Interfaces don't allow constructors so do this instead.</summary>
        // /// <returns></returns>
        // bool Init();

        ///// <summary>Send patch.</summary>
        ///// <param name="channelNumber">1-based channel</param>
        ///// <param name="patch">Which.</param>
        //void SendPatch(int channelNumber, int patch);

        /// <summary>Send all notes off.</summary>
        /// <param name="channelNumber">1-based channel</param>
        void Kill(int channelNumber);

        /// <summary>Send all notes off.</summary>
        void KillAll();

        /// <summary>Send midi event.</summary>
        /// <param name="evt"></param>
        void SendEvent(MidiEvent evt);

        /// <summary>Background operations such as process any stop notes.</summary>
        void Housekeep();
        #endregion
    }
    #endregion

    #region Definitions
    public class InternalDefs
    {
        /// <summary>Corresponds to midi velocity = 0.</summary>
        public const double VOLUME_MIN = 0.0;

        /// <summary>Corresponds to midi velocity = 127.</summary>
        public const double VOLUME_MAX = 1.0;

        /// <summary>Allow UI controls some more headroom.</summary>
        public const double GAIN_MAX = 2.0;

        /// <summary>Default value.</summary>
        public const double VOLUME_DEFAULT = 0.8;

        /// <summary>UI control smoothness.</summary>
        public const double VOLUME_RESOLUTION = 0.05;

        /// <summary>Only 4/4 time supported.</summary>
        public const int BEATS_PER_BAR = 4;

        /// <summary>Internal time resolution aka ppq or DeltaTicksPerQuarterNote.</summary>
        public const int SUBDIVS_PER_BEAT = 32;

        /// <summary>Convenience.</summary>
        public const int SUBDIVS_PER_BAR = SUBDIVS_PER_BEAT * BEATS_PER_BAR;
    }
    #endregion
}
