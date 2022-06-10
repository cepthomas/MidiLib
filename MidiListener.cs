﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using NBagOfTricks;
using NBagOfTricks.Slog;


namespace MidiLib
{
    /// <summary>
    /// Midi input handler.
    /// </summary>
    public class MidiListener : IDisposable
    {
        #region Fields
        /// <summary>Midi input device.</summary>
        readonly MidiIn? _midiIn = null;

        /// <summary>Midi send logging.</summary>
        readonly Logger _logger = LogManager.CreateLogger("MidiListener");
        #endregion

        #region Properties
        /// <summary>Are we ok?</summary>
        public bool Valid { get { return _midiIn is not null; } }

        /// <summary>Log inbound traffic at Trace level.</summary>
        public bool LogMidi { get { return _logger.Enable; } set { _logger.Enable = value; } }

        /// <summary>Capture on/off.</summary>
        public bool Enable 
        { 
            set
            {
                if(_midiIn is not null)
                {
                    if (value) _midiIn.Start(); else _midiIn.Stop();
                }
            }
        }
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
        public MidiListener(string midiDevice)
        {
            LogMidi = false;
            
            // Figure out which midi output device.
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                if (midiDevice == MidiIn.DeviceInfo(i).ProductName)
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

            if (mevt is not null && InputEvent is not null)
            {
                // Pass it up for client handling.
                InputEvent.Invoke(this, mevt);
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
            if (LogMidi)
            {
                _logger.LogTrace(evt.ToString());
            }
        }
        #endregion
    }

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
