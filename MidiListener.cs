using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;


namespace MidiLib
{
    /// <summary>
    /// Midi input handler.
    /// </summary>
    public class MidiListener : IDisposable
    {
        #region Fields
        /// <summary>Midi input device.</summary>
        readonly MidiIn _midiIn;

        /// <summary>Where to log to.</summary>
        readonly string _midiTraceFile = "";
        #endregion

        #region Properties
        /// <summary>Log outbound traffic. Warning - can get busy.</summary>
        public bool LogMidi { get; set; } = false;

        /// <summary>Capture on/off.</summary>
        public bool Enable { set { if (value) _midiIn.Start(); else _midiIn.Stop(); } }
        #endregion

        #region Events
        /// <summary>Handler for message arrived.</summary>
        public event EventHandler<MidiEventArgs>? InputEvent;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="midiDevice">Client supplies name of device.</param>
        /// <param name="midiTracePath">Where to log.</param>
        public MidiListener(string midiDevice, string midiTracePath)
        {
            if (midiTracePath != "")
            {
                _midiTraceFile = Path.Combine(midiTracePath, "midi_in.txt");
                File.Delete(_midiTraceFile);
            }

            // Figure out which midi output device.
            int devIndex = -1;
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                if (midiDevice == MidiIn.DeviceInfo(i).ProductName)
                {
                    _midiIn = new MidiIn(devIndex);
                    _midiIn.MessageReceived += MidiIn_MessageReceived;
                    _midiIn.ErrorReceived += MidiIn_ErrorReceived;
                    break;
                }
            }

            if (_midiIn is null)
            {
                throw new ArgumentException($"Invalid midi device: {midiDevice}");
            }
        }

        /// <summary>
        /// Empty constructor to satisfy nullability.
        /// </summary>
        MidiListener()
        {
            _midiIn = new MidiIn(0);
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            _midiIn.Stop();
            _midiIn.Dispose();
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Process input midi event.
        /// </summary>
        void MidiIn_MessageReceived(object? sender, MidiInMessageEventArgs e)
        {
            // Decode the message. We only care about a few.
            MidiEvent me = MidiEvent.FromRawMessage(e.RawMessage);
            MidiEventArgs? mevt = null;

            switch (me)
            {
                case NoteOnEvent evt:
                    mevt = new MidiEventArgs()
                    {
                        Channel = evt.Channel,
                        Note = evt.NoteNumber,
                        Velocity = evt.Velocity
                    };
                    break;

                case NoteEvent evt:
                    mevt = new MidiEventArgs()
                    {
                        Channel = evt.Channel,
                        Note = evt.NoteNumber,
                        Velocity = 0
                    };
                    break;

                case ControlChangeEvent evt:
                    mevt = new MidiEventArgs()
                    {
                        Channel = evt.Channel,
                        ControllerId = (int)evt.Controller,
                        ControllerValue = evt.ControllerValue
                    };
                    break;

                case PitchWheelChangeEvent evt:
                    mevt = new MidiEventArgs()
                    {
                        Channel = evt.Channel,
                        ControllerId = MidiEventArgs.PITCH_CONTROL,
                        ControllerValue = evt.Pitch
                    };
                    break;

                default:
                    // Ignore.
                    break;
            }

            if (mevt is not null)
            {
                // Pass it up for client handling.
                InputEvent?.Invoke(this, mevt);
                Log(mevt);
            }
        }

        /// <summary>
        /// Process error midi event - Parameter 1 is invalid.
        /// </summary>
        void MidiIn_ErrorReceived(object? sender, MidiInMessageEventArgs e)
        {
            MidiEventArgs evt = new();
            evt.ErrorInfo = $"Message:0x{e.RawMessage:X8}";
            Log(evt);
        }

        /// <summary>
        /// Send event information to the client to sort out.
        /// </summary>
        /// <param name="evt"></param>
        void Log(MidiEventArgs evt)
        {
            if (LogMidi && _midiTraceFile != "")
            {
                File.AppendAllText(_midiTraceFile, $"{DateTime.Now:mm\\:ss\\.fff} {evt}{Environment.NewLine}");
            }

        }
        #endregion
    }
}
