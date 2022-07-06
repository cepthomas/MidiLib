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
    /// A midi output layer - associated with a single device.
    /// </summary>
    public sealed class MidiSender : IMidiOutputDevice
    {
        #region Fields
        /// <summary>Low level midi output device.</summary>
        readonly MidiOut? _midiOut = null;

        /// <summary>Midi send logging.</summary>
        readonly Logger _logger = LogManager.CreateLogger("MidiSender");
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; }

        /// <inheritdoc />
        public bool Valid { get { return _midiOut is not null; } }

        /// <inheritdoc />
        public bool LogEnable { get { return _logger.Enable; } set { _logger.Enable = value; } }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        public MidiSender(string deviceName)
        {
            DeviceName = deviceName;

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
