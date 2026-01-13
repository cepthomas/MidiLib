using System;
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
    public class MidiManager
    {
        #region Singleton
        public static MidiManager Instance { get { _instance ??= new MidiManager(); return _instance; } }
        static MidiManager? _instance;
        MidiManager() { }
        #endregion

        #region Fields TODO1 should these all be Dicts for fast lookup?
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
        /// <summary>Handler for midi message arrived.</summary>
        public event EventHandler<BaseEvent>? MessageReceive;

        /// <summary>Handler for midi message sent.</summary>
        public event EventHandler<BaseEvent>? MessageSend;
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
            if (channelNumber is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException($"channelNumber:{channelNumber}"); }

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
        /// Open a normal output channel. Lazy inits the device. Throws if anything is invalid.
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="channelNumber"></param>
        /// <param name="channelName"></param>
        /// <param name="patchName"></param>
        /// <param name="aliasFile"></param>
        /// <returns></returns>
        public OutputChannel OpenOutputChannel(string deviceName, int channelNumber, string channelName, string patchName, string? aliasFile = null)
        {
            // Check args.
            if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException("Invalid deviceName"); }
            if (channelNumber is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException($"channelNumber:{channelNumber}"); }

            var outdev = GetOutputDevice(deviceName) ?? throw new MidiLibException($"Invalid output device [{deviceName}]");

            // Add the channel.
            OutputChannel ch = new(outdev, channelNumber)
            {
                ChannelName = channelName,
                Enable = true,
                Volume = VolumeDefs.DEFAULT_VOLUME,
            };

            // Init instrument name stuff.
            ch.InitInstruments(patchName, aliasFile);

            _outputChannels.Add(ch);

            return ch;
        }

        /// <summary>
        /// Like full function but opens a channel in anonymous mode.
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
            if (channelNumber is < 1 or > MidiDefs.NUM_CHANNELS) { throw new ArgumentOutOfRangeException($"channelNumber:{channelNumber}"); }
            if (patch is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"patch:{patch}"); }

            var outdev = GetOutputDevice(deviceName) ?? throw new MidiLibException($"Invalid output device [{deviceName}]");

            // Add the channel.
            OutputChannel ch = new(outdev, channelNumber, patch)
            {
                ChannelName = channelName,
                Enable = true,
                Volume = VolumeDefs.DEFAULT_VOLUME,
            };

            outdev.Send(new Patch(channelNumber, patch));
            _outputChannels.Add(ch);

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
                        // Just pass inputs up.
                        dev.MessageReceive += (sender, e) => MessageReceive?.Invoke((MidiInputDevice)sender!, e);
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
                // Is it a new device? Try to create specific flavor based on name.
                try
                {
                    if (MidiOutputDevice.GetAvailableDevices().Contains(deviceName))
                    {
                        dev = new MidiOutputDevice(deviceName) { Id = _outputDevices.Count + 1 };
                    }
                    else if (deviceName.Contains("oscout"))
                    {
                        dev = new OscOutputDevice(deviceName) { Id = _outputDevices.Count + 1 };
                    }
                    else if (deviceName.Contains("nullout"))
                    {
                        dev = new NullOutputDevice(deviceName) { Id = _outputDevices.Count + 1 };
                    }

                    if (dev is not null)
                    {
                        _outputDevices.Add(dev);
                        dev.MessageSend += (sender, e) => MessageSend?.Invoke((IOutputDevice)sender!, e);
                    }
                }
                catch (Exception)
                {
                    dev = null;
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
        /// Helper.
        /// </summary>
        /// <param name="chname"></param>
        /// <returns>The channel or null if invalid handle.</returns>
        public OutputChannel? GetOutputChannel(string chname)
        {
            return _outputChannels.Find(ch => ch.ChannelName == chname);
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
                _outputChannels.ForEach(ch => ch.Send(new Controller(ch.ChannelNumber, cc, 0)));
            }
            else
            {
                channel.Send(new Controller(channel.ChannelNumber, cc, 0));
            }
        }
        #endregion
    }
}
