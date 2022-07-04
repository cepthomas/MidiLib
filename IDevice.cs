using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace MidiLib
{
    /// <summary>Abstraction layer to support all midi-like devices.</summary>
    public interface IDevice : IDisposable
    {
        #region Properties
        /// <summary>Device name as defined by the system.</summary>
        string DeviceName { get; }

        /// <summary>Are we ok?</summary>
        bool Valid { get; }

        /// <summary>Log traffic at Trace level.</summary>
        bool LogEnable { get; set; }
        #endregion
    }

    /// <summary>Abstraction layer to support input devices.</summary>
    public interface IMidiInputDevice : IDevice
    {
        #region Properties
        /// <summary>Capture on/off.</summary>
        bool CaptureEnable { get; set; }
        #endregion

        #region Events
        /// <summary>Handler for message arrived.</summary>
        event EventHandler<InputEventArgs>? InputEvent;
        #endregion
    }

    /// <summary>Abstraction layer to support output devices.</summary>
    public interface IMidiOutputDevice : IDevice
    {
        #region Properties

        #endregion

        #region Functions
        /// <summary>Send a patch.</summary>
        /// <param name="channelNumber"></param>
        /// <param name="patch"></param>
        void SendPatch(int channelNumber, int patch);

        /// <summary>Send all notes off.</summary>
        /// <param name="channelNumber">1-based channel</param>
        void Kill(int channelNumber);

        /// <summary>Send all notes off.</summary>
        void KillAll();

        /// <summary>Send midi event.</summary>
        /// <param name="evt"></param>
        void SendEvent(MidiEvent evt);
        #endregion
    }
}
