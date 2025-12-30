using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Ephemera.MidiLib
{
    public class Manager
    {
        #region Fields
        /// <summary>All midi devices to use for send. Index is the id.</summary>
        readonly List<IOutputDevice> _outputDevices = [];

        /// <summary>All midi devices to use for receive. Index is the id.</summary>
        readonly List<IInputDevice> _inputDevices = [];

        /// <summary>All the output channels.</summary>
        readonly List<OutputChannel> _outputChannels = [];

        /// <summary>All the input channels.</summary>
        readonly List<InputChannel> _inputChannels = [];
        #endregion

        #region Properties
        /// <summary>Readonly collection access.</summary>
        public IEnumerable<OutputChannel> OutputChannels { get { return _outputChannels.AsEnumerable(); } }
        #endregion

        #region Events
        /// <summary>Handler for message arrived.</summary>
        public event EventHandler<BaseMidi>? MessageReceive;

        /// <summary>Handler for message sent. This is the actual message on the wire.</summary>
        public event EventHandler<BaseMidi>? MessageSend;
        #endregion

        #region Channels
        /// <summary>
        /// Open an input channel. Lazy inits the device. Throws if anything is invalid.
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="channelNumber"></param>
        /// <param name="channelName"></param>
        /// <returns></returns>
        public InputChannel OpenInputChannel(string deviceName, int channelNumber, string channelName)
        {
            // Check args.
            if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException("Invalid deviceName"); }
            if (channelNumber is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channelNumber)); }

            var indev = GetInputDevice(deviceName) ?? throw new MidiLibException($"Invalid input device [{deviceName}]");

            // Add the channel.
            InputChannel ch = new(indev, channelNumber)
            {
                ChannelName = channelName,
                Enable = true,
            };

            _inputChannels.Add(ch);

            return ch;
        }

        /// <summary>
        /// Open an output channel. Lazy inits the device. Throws if anything is invalid.
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="channelNumber"></param>
        /// <param name="channelName"></param>
        /// <param name="patch"></param>
        /// <returns></returns>
        public OutputChannel OpenOutputChannel(string deviceName, int channelNumber, string channelName, int patch)
        {
            // Check args.
            if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException("Invalid deviceName"); }
            if (channelNumber is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException(nameof(channelNumber)); }

            var outdev = GetOutputDevice(deviceName) ?? throw new MidiLibException($"Invalid output device [{deviceName}]");

            // Add the channel.
            OutputChannel ch = new(outdev, channelNumber)
            {
                ChannelName = channelName,
                Patch = patch,
                Enable = true,
                Volume = Defs.DEFAULT_VOLUME
            };
            _outputChannels.Add(ch);

            // Send patch now. 
            //outdev.Send(new Patch(channelNumber, patch));

            return ch;
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void DestroyChannels()
        {
            _inputChannels.Clear();
            _outputChannels.Clear();
        }
        #endregion

        #region Devices
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
                // Is it a new device? Try to create it.

                // Midi input device?
                if (MidiInputDevice.GetAvailableDevices().Contains(deviceName))
                {
                    dev = new MidiInputDevice(deviceName) { Id = _inputDevices.Count + 1 };
                }

                // Others?
                var parts = deviceName.SplitByToken(":");
                switch (parts[0].ToLower(), parts.Count)
                {
                    case ("oscin", 2):
                        dev = new OscInputDevice(parts[1]) { Id = _inputDevices.Count + 1 };
                        break;

                    case ("nullin", 2):
                        dev = new NullInputDevice(deviceName) { Id = _inputDevices.Count + 1 };
                        break;
                }

                if (dev is not null)
                {
                    _inputDevices.Add(dev);
                    dev.CaptureEnable = true;
                    // Just pass inputs up.
                    dev.MessageReceive += (sender, e) => MessageReceive?.Invoke((MidiInputDevice)sender!, e);
                }
            }
            else
            {
                dev = indevs.ElementAt(0);
            }

            return dev;
        }

        /// <summary>
        /// Get I/O device. Lazy creation.
        /// </summary>
        /// <param name="deviceName"></param>
        /// <returns>The device or null if invalid.</returns>
        public IOutputDevice? GetOutputDevice(string deviceName)
        {
            IOutputDevice? dev = null;

            // Check for known.
            var outdevs = _outputDevices.Where(o => o.DeviceName == deviceName);

            if (!outdevs.Any())
            {
                // Is it a new device? Try to create it.

                // Midi output device?
                if (MidiOutputDevice.GetAvailableDevices().Contains(deviceName))
                {
                    dev = new MidiOutputDevice(deviceName) { Id = _outputDevices.Count + 1 };
                }

                // Others?
                var parts = deviceName.SplitByToken(":");
                switch (parts[0].ToLower(), parts.Count)
                {
                    case ("oscout", 3):
                        dev = new OscOutputDevice(parts[1], parts[2]) { Id = _outputDevices.Count + 1 };
                        break;

                    case ("nullout", 2):
                        dev = new NullOutputDevice(deviceName) { Id = _outputDevices.Count + 1 };
                        break;
                }

                if (dev is not null)
                {
                    _outputDevices.Add(dev);
                    dev.MessageSend += (sender, e) => MessageSend?.Invoke((MidiOutputDevice)sender!, e);
                }
            }
            else
            {
                dev = outdevs.ElementAt(0);
            }

            return dev;
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void DestroyDevices()
        {
            _inputDevices.ForEach(d => d.Dispose());
            _inputDevices.Clear();
            _outputDevices.ForEach(d => d.Dispose());
            _outputDevices.Clear();
        }
        #endregion

        #region Misc
        /// <summary>
        /// Helper.
        /// </summary>
        /// <param name="chnum"></param>
        /// <returns>The channel or null if invalid handle.</returns>
        public OutputChannel? GetOutputChannel(int chnum)
        {
           return _outputChannels.Find(ch => ch.ChannelNumber == chnum);
        }

        /// <summary>
        /// Stop all midi. Doesn't throw.
        /// </summary>
        /// <param name="channel">Specific channel or all if null.</param>
        public void Kill(OutputChannel? channel = null)
        {
            int cc = 123; // TODO fix magical knowledge => "AllNotesOff"

            if (channel is null)
            {
                _outputChannels.ForEach(ch => ch.Device.Send(new Controller(ch.ChannelNumber, cc, 0)));
            }
            else
            {
                channel.Device.Send(new Controller(channel.ChannelNumber, cc, 0));
            }
        }
        #endregion
    }
}
