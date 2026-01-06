using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using NAudio.Midi;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    //----------------------------------------------------------------
    /// <summary>A midi input device.</summary>
    public class MidiInputDevice : IInputDevice
    {
        #region Fields
        /// <summary>NAudio midi input device.</summary>
        readonly MidiIn? _midiIn = null;
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; }

        /// <inheritdoc />
        public int Id { get; init; }

        /// <inheritdoc />
        public bool CaptureEnable { get; set; }

        /// <inheritdoc />
        public bool Valid { get { return _midiIn is not null; } }
        #endregion

        #region Events
        /// <summary>Client needs to deal with this.</summary>
        public event EventHandler<BaseEvent>? MessageReceive;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        public MidiInputDevice(string deviceName)
        {
            // Figure out which midi device.
            var devs = GetAvailableDevices();
            var ind = devs.IndexOf(deviceName);
            if (ind >= 0)
            {
                DeviceName = deviceName;
                Id = ind;
                _midiIn = new MidiIn(ind);
                _midiIn.MessageReceived += MidiIn_MessageReceived;
                _midiIn.ErrorReceived += MidiIn_ErrorReceived;
                _midiIn.Start();
            }
            else
            {
                DeviceName = "";
                throw new MidiLibException($"Invalid input midi device name [{deviceName}]");
            }
        }

        /// <summary>Resource clean up.</summary>
        public void Dispose()
        {
            _midiIn?.Stop();
            _midiIn?.Dispose();
        }
        #endregion

        /// <summary>
        /// Process driver level midi input event.
        /// </summary>
        void MidiIn_MessageReceived(object? sender, MidiInMessageEventArgs e)
        {
            if (!CaptureEnable) return;

            // Decode the message. We only care about a few.
            var mevt = MidiEvent.FromRawMessage(e.RawMessage);
            var chnum = mevt.Channel;

            BaseEvent evt = mevt switch
            {
                NoteOnEvent onevt => new NoteOn(chnum, onevt.NoteNumber, onevt.Velocity, MusicTime.ZERO),
                NoteEvent offevt => offevt.Velocity == 0 ?
                    new NoteOff(chnum, offevt.NoteNumber, MusicTime.ZERO) :
                    new NoteOn(chnum, offevt.NoteNumber, offevt.Velocity, MusicTime.ZERO),
                ControlChangeEvent ctlevt => new Controller(chnum, (int)ctlevt.Controller, ctlevt.ControllerValue, MusicTime.ZERO),
                _ => new Other(chnum, e.RawMessage, MusicTime.ZERO)
            };

            // Tell the boss.
            MessageReceive?.Invoke(this, evt);
        }

        /// <summary>
        /// Process error midi event - parameter 1 is invalid. Do I care?
        /// </summary>
        void MidiIn_ErrorReceived(object? sender, MidiInMessageEventArgs e)
        {
            // Just ignore? or ErrorInfo = $"Message:0x{e.RawMessage:X8}";
        }

        /// <summary>
        /// Get a list of available device names.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAvailableDevices()
        {
            List<string> devs = [];

            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                devs.Add(MidiIn.DeviceInfo(i).ProductName);
            }

            return devs;
        }
    }

    //----------------------------------------------------------------
    /// <summary>A midi output device.</summary>
    public class MidiOutputDevice : IOutputDevice
    {
        #region Fields
        /// <summary>NAudio midi output device.</summary>
        readonly MidiOut? _midiOut = null;
        #endregion

        #region Events
        /// <summary>Client needs to deal with this.</summary>
        public event EventHandler<BaseEvent>? MessageSend;
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; }

        /// <inheritdoc />
        public int Id { get; init; }

        /// <inheritdoc />
        public bool Valid { get { return _midiOut is not null;} }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor. OK to throw in here.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        public MidiOutputDevice(string deviceName)
        {
            // Figure out which midi device.
            var devs = GetAvailableDevices();
            var ind = devs.IndexOf(deviceName);
            if (ind >= 0)
            {
                DeviceName = deviceName;
                Id = ind;
                _midiOut = new MidiOut(ind);
            }
            else
            {
                DeviceName = "";
                throw new MidiLibException($"Invalid output midi device name [{deviceName}]");
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

        /// <inheritdoc />
        public void Send(BaseEvent bevt)
        {
            MidiEvent mevt = bevt switch
            {
                NoteOn evt => new NoteOnEvent(0, evt.ChannelNumber, evt.Note, evt.Velocity, 0),
                NoteOff evt => new NoteEvent(0, evt.ChannelNumber, MidiCommandCode.NoteOff, evt.Note, 0),
                Controller evt => new ControlChangeEvent(0, evt.ChannelNumber, (MidiController)evt.ControllerId, evt.Value),
                Patch evt => new PatchChangeEvent(0, evt.ChannelNumber, evt.Value),
                //Other evt => MidiEvent.FromRawMessage(evt.RawMessage),
                _ => throw new MidiLibException($"Invalid send event: {bevt}")
            };

            _midiOut?.Send(mevt.GetAsShortMessage());

            // Tell the boss.
            MessageSend?.Invoke(this, bevt);
        }

        /// <summary>
        /// Get a list of available device names.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAvailableDevices()
        {
            List<string> devs = [];

            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                devs.Add(MidiOut.DeviceInfo(i).ProductName);
            }

            return devs;
        }
    }
}
