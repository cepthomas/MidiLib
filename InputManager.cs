using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Diagnostics;
using System.Security;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Ephemera.MidiLib
{
    /// <summary>In charge of all midi inputs.</summary>
    public class InputManager
    {
        #region Fields
        /// <summary>All midi devices to use for receive. Index is the id.</summary>
        readonly List<IInputDevice> _inputDevices = [];

        /// <summary>All the midi inputs.</summary>
        readonly List<MidiInput> _midiInputs = [];
        #endregion

        #region Properties

        #endregion

        #region Events
        /// <summary>Handler for midi message arrived.</summary>
        public event EventHandler<BaseEvent>? MessageReceived;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public InputManager()
        {
        }
        #endregion

        #region API
        /// <summary>
        /// Open a midi input. Lazy inits the device. Throws if anything is invalid.
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="channelNumber"></param>
        /// <returns></returns>
        public MidiInput OpenMidiInput(string deviceName, int channelNumber)
        {
            // Check args.
            if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException("Invalid deviceName"); }
            if (channelNumber is < 1 or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"channelNumber:{channelNumber}"); }

            var indev = GetInputDevice(deviceName) ?? throw new MidiLibException($"Invalid input device [{deviceName}]");

            // Add the channel.
            MidiInput ch = new(indev, channelNumber);
            _midiInputs.Add(ch);

            return ch;
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        void DestroyChannels()
        {
            _midiInputs.Clear();
        }

        /// <summary>
        /// Get I/O device. Lazy creation.
        /// </summary>
        /// <param name="deviceName"></param>
        /// <returns>The device or null if invalid.</returns>
        public IInputDevice? GetInputDevice(string deviceName)
        {
            IInputDevice? dev = null;

            // Check for known.
            var indevs = _inputDevices.Where(o => o.DeviceName == deviceName);

            if (!indevs.Any())
            {
                // Is it a new device? Try to create specific flavor based on name.
                try
                {
                    if (MidiInputDevice.GetAvailableDevices().Contains(deviceName))
                    {
                        dev = new MidiInputDevice(deviceName) { Id = _inputDevices.Count + 1 };
                    }
                    else if (deviceName.Contains("oscin"))
                    {
                        dev = new OscInputDevice(deviceName) { Id = _inputDevices.Count + 1 };
                    }
                    else if (deviceName.Contains("nullin"))
                    {
                        dev = new NullInputDevice(deviceName) { Id = _inputDevices.Count + 1 };
                    }

                    if (dev is not null)
                    {
                        _inputDevices.Add(dev);
                        dev.CaptureEnable = true;
                        // Just pass inputs up and let client make sense of them.
                        dev.MessageReceived += (sender, e) => MessageReceived?.Invoke((MidiInputDevice)sender!, e);
                    }
                }
                catch (Exception)
                {
                    dev = null;
                }
            }
            else
            {
                dev = indevs.ElementAt(0);
            }

            return dev;
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        void DestroyDevices()
        {
            _inputDevices.ForEach(d => d.Dispose());
            _inputDevices.Clear();
        }
        #endregion

        #region User functions

        /// <summary>
        /// Clean up everything.
        /// </summary>
        public void Destroy()
        {
            DestroyDevices();
        }
        #endregion
    }

    /// <summary>Describes one midi input.</summary>
    public class MidiInput
    {
        #region Fields
        /// <summary>Associated device. Required.</summary>
        readonly IInputDevice _device;
        #endregion

        #region Properties
        /// <summary>Actual 1-based midi channel number. Required.</summary>
        public int ChannelNumber { get; init; }

        /// <summary>True if channel is active.</summary>
        public bool Enable { get; set; } = true;

        /// <summary>Handle for use by scripts.</summary>
        public ChannelHandle Handle { get; init; }
        #endregion

        /// <summary>
        /// Constructor with required args.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="channelNumber"></param>
        public MidiInput(IInputDevice device, int channelNumber)
        {
            if (channelNumber is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"channelNumber:{(channelNumber)}"); }

            _device = device;
            ChannelNumber = channelNumber;
            Handle = new(device.Id, ChannelNumber, false);
        }

        public override string ToString()
        {
            return $"In Ch:{ChannelNumber} Dev:{_device.DeviceName}";
        }
    }
}
