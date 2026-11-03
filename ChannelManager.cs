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
    public class ChannelManager
    {
        #region Singleton
        public static ChannelManager Instance { get { _instance ??= new ChannelManager(); return _instance; } }
        static ChannelManager? _instance;
        ChannelManager() { }
        #endregion

        #region Fields
        /// <summary>All midi devices to use for send. Index is the id.</summary>
        readonly List<IOutputDevice> _outputDevices = [];

        /// <summary>All midi devices to use for receive. Index is the id.</summary>
        readonly List<IInputDevice> _inputDevices = [];

        /// <summary>All the output channels with associated controls.</summary>
        readonly List<(OutputChannel chan, ChannelControl? ctrl)> _outputChannels = [];

        /// <summary>All the input channels.</summary>
        readonly List<InputChannel> _inputChannels = [];
        #endregion

        #region Properties
        /// <summary>What the mouse click does.</summary>
        public SelectionMode Mode { get; set; } = SelectionMode.None;

        #endregion

        #region Events
        /// <summary>Handler for midi message arrived.</summary>
        public event EventHandler<BaseEvent>? MessageReceived;

        /// <summary>Handler for midi message sent.</summary>
        public event EventHandler<BaseEvent>? MessageSent;
        #endregion

        #region Lifecycle

        #endregion

        #region UI event handlers
        /// <summary>
        /// User clicked something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_StateChange(object? sender, ChannelControl.StateChangeEventArgs e)
        {
            var cc = sender as ChannelControl;
            var chan = GetChannel(cc!);
            if (chan is null) return; // error?

            // TODO1
        }

        /// <summary>
        /// User clicked something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_SelectionChange(object? sender, ChannelControl.SelectionChangeEventArgs e)
        {
            var cc = sender as ChannelControl;
            var chan = GetChannel(cc!);
            if (chan is null) return; // error?

            // TODO1
        }
        #endregion

        #region Channels
        /// <summary>
        /// Open an input channel. Lazy inits the device. Throws if anything is invalid.
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="channelNumber"></param>
        /// <returns></returns>
        public InputChannel OpenInputChannel(string deviceName, int channelNumber)
        {
            // Check args.
            if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException("Invalid deviceName"); }
            if (channelNumber is < 1 or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"channelNumber:{channelNumber}"); }

            var indev = GetInputDevice(deviceName) ?? throw new MidiLibException($"Invalid input device [{deviceName}]");

            // Add the channel.
            InputChannel ch = new(indev, channelNumber);
            _inputChannels.Add(ch);

            return ch;
        }

        /// <summary>
        /// Open a normal output channel. Lazy inits the device. Throws if anything is invalid.
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="channelNumber"></param>
        /// <param name="patchName"></param>
        /// <param name="aliasFile">Alternate patch names from an ini file.</param>
        /// <returns></returns>
        public OutputChannel OpenOutputChannel(string deviceName, int channelNumber, string patchName, string? aliasFile = null)
        {
            // Check args.
            if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException("Invalid deviceName"); }
            if (channelNumber is < 1 or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"channelNumber:{channelNumber}"); }

            var outdev = GetOutputDevice(deviceName) ?? throw new MidiLibException($"Invalid output device [{deviceName}]");

            // Add the channel.
            OutputChannel ch = new(outdev, channelNumber);
            // Init instrument name stuff.
            ch.InitInstruments(patchName, aliasFile); // this does the midi send
            _outputChannels.Add((ch, null));

            return ch;
        }

        /// <summary>
        /// Like full function but opens a channel without parch name.
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="channelNumber"></param>
        /// <param name="patch"></param>
        /// <returns></returns>
        public OutputChannel OpenOutputChannel(string deviceName, int channelNumber, int patch)
        {
            // Check args.
            if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException("Invalid deviceName"); }
            if (channelNumber is < 1 or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"channelNumber:{channelNumber}"); }
            if (patch is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"patch:{patch}"); }

            var outdev = GetOutputDevice(deviceName) ?? throw new MidiLibException($"Invalid output device [{deviceName}]");

            // Add the channel.
            OutputChannel ch = new(outdev, channelNumber, patch);
            outdev.Send(new Patch(channelNumber, patch));
            _outputChannels.Add((ch, null));

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
                        dev.MessageSent += (sender, e) => MessageSent?.Invoke((IOutputDevice)sender!, e);
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

        #region User functions
        /// <summary>
        /// Bind a control to a channel.
        /// </summary>
        /// <param name="chan"></param>
        /// <param name="ctrl"></param>
        public void Bind(OutputChannel chan, ChannelControl ctrl)
        {
            for (int i = 0; i < _outputChannels.Count; i++)
            {
                if (_outputChannels[i].chan == chan)
                {
                    if (_outputChannels[i].ctrl is not null) { throw new MidiLibException($"Channel [{chan}] already bound to [{ctrl}]"); }

                    ctrl.StateChange += ChannelControl_StateChange;
                    ctrl.SelectionChange += ChannelControl_SelectionChange;
                    _outputChannels[i] = (chan, ctrl);
                    return;
                }
            }
            throw new MidiLibException($"Unknown channel [{chan}]");
        }

        /// <summary>
        /// Clean up everything.
        /// </summary>
        /// <param name="controls"></param>
        public void Destroy(Control.ControlCollection controls)
        {
            KillAll();

            // Clean out our current elements.
            _outputChannels.ForEach(b =>
            {
                controls.Remove(b.ctrl);
                b.ctrl?.Dispose();
            });
            _outputChannels.Clear();

            DestroyDevices();
        }

        /// <summary>
        /// Handles solo/mute.
        /// </summary>
        public void UpdateState()
        {
            // Update all channels. TODO1 needs more work!
            bool anySolo = _outputChannels.Where(c => c.ctrl is not null && c.ctrl.State == ChannelState.Solo).Any();

            foreach (var (chan, ctrl) in _outputChannels)
            {
                if (ctrl is null) continue; // never

                switch ((ctrl.State, anySolo))
                {
                    case (ChannelState.Solo, true):
                        chan.EnableSound = true;
                        break;

                    case (ChannelState.Mute, true):
                    case (ChannelState.Normal, true):
                        chan.EnableSound = false;
                        chan.Kill(); // just in case
                        break;

                    case (ChannelState.Solo, false): // never
                        chan.EnableSound = true;
                        break;

                    case (ChannelState.Mute, false):
                        chan.EnableSound = false;
                        chan.Kill(); // just in case
                        break;

                    case (ChannelState.Normal, false):
                        chan.EnableSound = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Die die die.
        /// </summary>
        public void KillAll()
        {
            _outputChannels.ForEach(b => b.chan.Kill());
        }

        /// <summary>
        /// Readonly collection access.
        /// </summary>
        /// <param name="selected">Only selected items</param>
        public IEnumerable<ChannelControl?> GetChannelControls(bool selected = false)
        {
            var ctrls = _outputChannels.Where(ch => ch.ctrl is not null && (!selected || ch.ctrl.Selected)).Select(ch => ch.ctrl);
            return ctrls;
        }

        /// <summary>
        /// Readonly collection access.
        /// </summary>
        /// <param name="selected">Only selected items</param>
        public IEnumerable<OutputChannel> GetChannels(bool selected = false)
        {
            var chans = _outputChannels.Where(ch => ch.ctrl is not null && (!selected || ch.ctrl.Selected)).Select(ch => ch.chan);
            return chans;
        }

        /// <summary>
        /// Get channel object from control object.
        /// </summary>
        /// <param name="ctrl"></param>
        /// <returns>The channel or null if invalid control.</returns>
        public OutputChannel? GetChannel(ChannelControl ctrl)
        {
            foreach (var b in _outputChannels) { if (b.ctrl == ctrl) return b.chan; }
            return null;
        }

        /// <summary>
        /// Get channel object from channel number.
        /// </summary>
        /// <param name="channelNum"></param>
        /// <returns>The channel or null if invalid number.</returns>
        public OutputChannel? GetChannel(int channelNum)
        {
            foreach (var (chan, _) in _outputChannels) { if (chan.ChannelNumber == channelNum) return chan; }
            return null;
        }

        /// <summary>
        /// Get channel control object from channel object.
        /// </summary>
        /// <param name="chan"></param>
        /// <returns>The channel or null if invalid.</returns>
        public ChannelControl? GetControl(OutputChannel chan)
        {
            foreach (var b in _outputChannels) { if (b.chan == chan) return b.ctrl; }
            return null;
        }

        /// <summary>
        /// Get channel object from channel handle.
        /// </summary>
        /// <param name="chnd">Channel handle</param>
        /// <returns>The channel or null if invalid handle.</returns>
        public OutputChannel? GetChannel(ChannelHandle chnd)
        {
            foreach (var (chan, _) in _outputChannels) { if (chan.Handle == chnd) return chan; }
            return null;
        }

        ///// <summary>
        ///// Get channel object from channel name.
        ///// </summary>
        ///// <param name="chname"></param>
        ///// <returns>The channel or null if invalid handle.</returns>
        //public OutputChannel? GetChannel(string chname)
        //{
        //    foreach (var b in _outputChannels) { if (b.chan.ChannelName == chname) return b.chan; }
        //    return null;
        //}
        #endregion
    }
}
