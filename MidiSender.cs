using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using NBagOfTricks;
using NBagOfUis;
using NBagOfTricks.Slog;


namespace MidiLib
{
    /// <summary>
    /// A simple midi output device.
    /// </summary>
    public class MidiSender : IDisposable
    {
        #region Fields
        /// <summary>Midi output device.</summary>
        readonly MidiOut? _midiOut = null;

        /// <summary>Midi send logging.</summary>
        readonly Logger _logger = LogManager.CreateLogger("MidiSender");
        #endregion

        #region Properties
        /// <summary>Are we ok?</summary>
        public bool Valid { get { return _midiOut is not null; } }

        /// <summary>Current master volume.</summary>
        public double Volume { get; set; } = InternalDefs.VOLUME_DEFAULT;

        /// <summary>Log outbound traffic at Trace level. Warning - can get busy.</summary>
        public bool LogMidi { get { return _logger.Enable; } set { _logger.Enable = value; } }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="midiDevice">Client supplies name of device.</param>
        public MidiSender(string midiDevice)
        {
            LogMidi = false;

            // Figure out which midi output device.
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                if (midiDevice == MidiOut.DeviceInfo(i).ProductName)
                {
                    _midiOut = new MidiOut(i);
                    break;
                }
            }
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        public void Dispose()
        {
            // Resources.
            _midiOut?.Dispose();
        }
        #endregion

        #region Public functions - midi
        public void SendPatch(int channelNumber, int patch)
        {
            if (patch >= 0)
            {
                PatchChangeEvent evt = new(0, channelNumber, patch);
                SendMidi(evt);
            }
        }

        /// <summary>
        /// Send all notes off.
        /// </summary>
        /// <param name="channelNumber">1-based channel</param>
        public void Kill(int channelNumber)
        {
            ControlChangeEvent nevt = new(0, channelNumber, MidiController.AllNotesOff, 0);
            SendMidi(nevt);
        }

        /// <summary>
        /// Send all notes off.
        /// </summary>
        public void KillAll()
        {
            // Send midi stop all notes just in case.
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                int chnum = i + 1;
                Kill(chnum);
            }
        }

        /// <summary>
        /// Send midi.
        /// </summary>
        /// <param name="evt"></param>
        public void SendMidi(MidiEvent evt)
        {
            if(_midiOut is not null)
            {
                _midiOut.Send(evt.GetAsShortMessage());
            }
            if (LogMidi)
            {
                _logger.LogTrace(evt.ToString());
            }
        }
        #endregion
    }
}
