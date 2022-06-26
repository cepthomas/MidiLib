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
    public sealed class MidiSender : IMidiOutputDevice
    {
        #region Fields
        /// <summary>Midi output device.</summary>
        readonly MidiOut? _midiOut = null;

        /// <summary>Midi send logging.</summary>
        readonly Logger _logger = LogManager.CreateLogger("MidiSender");
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; }

        /// <inheritdoc />
        public string DeviceId { get; }

        /// <inheritdoc />
        public bool Valid { get { return _midiOut is not null; } }

        /// <inheritdoc />
        public bool LogEnable { get { return _logger.Enable; } set { _logger.Enable = value; } }





        ///// <summary>Current master volume.</summary>
        //public double Volume { get; set; } = InternalDefs.VOLUME_DEFAULT;

        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        /// <param name="deviceId">Client must supply id of device.</param>
        public MidiSender(string deviceName, string deviceId)
        {
            DeviceName = deviceName;
            DeviceId = deviceId;

            LogEnable = false;

            // Figure out which midi output device.
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                if (deviceName == MidiOut.DeviceInfo(i).ProductName)
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
        /// <inheritdoc />
        public void SendPatch(int channelNumber, int patch)
        {
            if (patch >= 0)
            {
                PatchChangeEvent evt = new(0, channelNumber, patch);
                SendEvent(evt);
            }
        }

        /// <inheritdoc />
        public void Kill(int channelNumber)
        {
            ControlChangeEvent nevt = new(0, channelNumber, MidiController.AllNotesOff, 0);
            SendEvent(nevt);
        }

        /// <inheritdoc />
        public void KillAll()
        {
            // Send midi stop all notes just in case.
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                int chnum = i + 1;
                Kill(chnum);
            }
        }

        /// <inheritdoc />
        public void SendEvent(MidiEvent evt)
        {
            if(_midiOut is not null)
            {
                _midiOut.Send(evt.GetAsShortMessage());
            }
            if (LogEnable)
            {
                _logger.Trace(evt.ToString());
            }
        }
        #endregion
    }
}
