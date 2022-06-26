using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiLib
{
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
