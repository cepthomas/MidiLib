﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    /// <summary>
    /// Midi input handler.
    /// </summary>
    public sealed class MidiInput : IInputDevice
    {
        #region Fields
        /// <summary>Midi input device.</summary>
        readonly MidiIn? _midiIn = null;

        /// <summary>Midi send logging.</summary>
        readonly Logger _logger = LogManager.CreateLogger("MidiInput");

        /// <summary>Control.</summary>
        bool _capturing = false;
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; }

        /// <inheritdoc />
        public bool Valid { get { return _midiIn is not null; } }

        /// <inheritdoc />
        public bool LogEnable { get { return _logger.Enable; } set { _logger.Enable = value; } }

        /// <summary>Capture on/off.</summary>
        public bool CaptureEnable
        {
            get { return _capturing; }
            set { if (value) _midiIn?.Start(); else _midiIn?.Stop(); _capturing = value; }
        }
        #endregion

        #region Events
        /// <inheritdoc />
        public event EventHandler<InputReceiveEventArgs>? InputReceive;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        public MidiInput(string deviceName)
        {
            DeviceName = deviceName;
            LogEnable = false;
            
            // Figure out which midi input device.
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                if (deviceName == MidiIn.DeviceInfo(i).ProductName)
                {
                    _midiIn = new MidiIn(i);
                    _midiIn.MessageReceived += MidiIn_MessageReceived;
                    _midiIn.ErrorReceived += MidiIn_ErrorReceived;
                    break;
                }
            }
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            _midiIn?.Stop();
            _midiIn?.Dispose();
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
            InputReceiveEventArgs? mevt = null;

            switch (me)
            {
                case NoteOnEvent evt:
                    mevt = new InputReceiveEventArgs()
                    {
                        Channel = evt.Channel,
                        Note = evt.NoteNumber,
                        Value = evt.Velocity
                    };
                    break;

                case NoteEvent evt:
                    mevt = new InputReceiveEventArgs()
                    {
                        Channel = evt.Channel,
                        Note = evt.NoteNumber,
                        Value = 0
                    };
                    break;

                case ControlChangeEvent evt:
                    mevt = new InputReceiveEventArgs()
                    {
                        Channel = evt.Channel,
                        Controller = (int)evt.Controller,
                        Value = evt.ControllerValue
                    };
                    break;

                case PitchWheelChangeEvent evt:
                    mevt = new InputReceiveEventArgs()
                    {
                        Channel = evt.Channel,
                        Controller = InputReceiveEventArgs.PITCH_CONTROL,
                        Value = evt.Pitch
                    };
                    break;

                default:
                    // Ignore.
                    break;
            }

            if (mevt is not null && InputReceive is not null)
            {
                // Pass it up for client handling.
                InputReceive.Invoke(this, mevt);
                Log(mevt);
            }
        }

        /// <summary>
        /// Process error midi event - Parameter 1 is invalid.
        /// </summary>
        void MidiIn_ErrorReceived(object? sender, MidiInMessageEventArgs e)
        {
            InputReceiveEventArgs evt = new()
            {
                ErrorInfo = $"Message:0x{e.RawMessage:X8}"
            };
            Log(evt);
        }

        /// <summary>
        /// Send event information to the client to sort out.
        /// </summary>
        /// <param name="evt"></param>
        void Log(InputReceiveEventArgs evt)
        {
            if (LogEnable)
            {
                _logger.Trace(evt.ToString());
            }
        }
        #endregion
    }
}
