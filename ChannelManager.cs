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
    public class ChannelManager : IDisposable
    {
        #region Fields
        /// <summary>All midi devices to use for send. Index is the id.</summary>
        readonly List<IOutputDevice> _outputDevices = [];

        /// <summary>All the output channels with associated controls.</summary>
        readonly List<(Channel chan, ChannelControl? ctrl)> _channels = [];

        /// <summary>Need to add/remove dynamically generated controls.</summary>
        readonly Control.ControlCollection? _controls;
        #endregion

        #region Properties

        #endregion

        #region Events

        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="controls">Required for using UI controls. Headless doesn't.</param>
        public ChannelManager(Control.ControlCollection? controls = null)
        {
            _controls = controls;
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void Dispose()
        {
            Reset();

            _outputDevices.ForEach(d => d.Dispose());
            _outputDevices.Clear();
        }
        #endregion

        #region UI event handlers
        /// <summary>
        /// User clicked solo/mute.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_StateChange(object? sender, ChannelControl.StateChangeEventArgs e)
        {
            var cc = sender as ChannelControl;
            var _ = GetChannel(cc!) ?? throw new MidiLibException($"Invalid channel");
            UpdateState();
        }

        /// <summary>
        /// User clicked something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_SelectionChange(object? sender, ChannelControl.SelectionChangeEventArgs e)
        {
            var cc = sender as ChannelControl;
            var _ = GetChannel(cc!) ?? throw new MidiLibException($"Invalid channel");

            // Nothing to do?
        }
        #endregion

        #region Channels
        /// <summary>
        /// Open a normal output channel with known patch name. Lazy inits the device. Sends initial patch message. Throws if anything is invalid.
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="channelNumber"></param>
        /// <param name="channelName"></param>
        /// <param name="patchName"></param>
        /// <param name="aliasFile">Alternate patch names from an ini file.</param>
        /// <returns></returns>
        public Channel OpenChannel(string deviceName, int channelNumber, string channelName, string patchName, string? aliasFile = null)
        {
            // Check args.
            if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException("Invalid deviceName"); }
            if (channelNumber is < 1 or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"channelNumber:{channelNumber}"); }

            var outdev = GetOutputDevice(deviceName) ?? throw new MidiLibException($"Invalid output device [{deviceName}]");

            // Add the channel.
            ChannelConfig config = new()
            {
                ChannelNumber = channelNumber,
                ChannelName = channelName,
                PatchName = patchName,
                AliasFile = aliasFile,
                DeviceName = deviceName,
                Volume = MidiLibDefs.DEFAULT_VOLUME,
                Pan = 0.0
            };

            Channel ch = new(outdev, config);// channelNumber, channelName);
            ch.InitInstruments(patchName, aliasFile);
            _channels.Add((ch, null));
            return ch;
        }

        /// <summary>
        /// Like above but opens a channel without known patch name.
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="channelNumber"></param>
        /// <param name="channelName"></param>
        /// <param name="patchNum">Explicit</param>
        /// <param name="isDrums">Because I can't determine this myself</param>
        /// <param name="aliasFile">Alternate patch names from an ini file.</param>
        /// <returns></returns>
        public Channel OpenChannel(string deviceName, int channelNumber, string channelName, int patchNum, bool isDrums, string? aliasFile = null)
        {
            // Check args.
            if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException("Invalid deviceName"); }
            if (channelNumber is < 1 or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"channelNumber:{channelNumber}"); }
            if (patchNum is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"patchNum:{patchNum}"); }

            var outdev = GetOutputDevice(deviceName) ?? throw new MidiLibException($"Invalid output device [{deviceName}]");

            // Add the channel.
            ChannelConfig config = new()
            {
                ChannelNumber = channelNumber,
                ChannelName = channelName,
//???                PatchName = patchName,
                AliasFile = aliasFile,
                DeviceName = deviceName,
                Volume = MidiLibDefs.DEFAULT_VOLUME,
                Pan = 0.0
            };

            Channel ch = new(outdev, config);// channelNumber, channelName);
            ch.InitInstruments(patchNum, isDrums, aliasFile);
            _channels.Add((ch, null));
            return ch;
        }
        #endregion

        #region Devices
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
                        //dev.MessageSent += (sender, e) => MessageSent?.Invoke((IOutputDevice)sender!, e);
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
        #endregion

        #region User functions
        /// <summary>
        /// Bind a control to a channel.
        /// </summary>
        /// <param name="chan"></param>
        /// <param name="ctrl"></param>
        public void Bind(Channel chan, ChannelControl ctrl)
        {
            if (_controls is null) { throw new MidiLibException($"This configuration does not support UI controls"); }

            for (int i = 0; i < _channels.Count; i++)
            {
                if (_channels[i].chan == chan)
                {
                    if (_channels[i].ctrl is not null) { throw new MidiLibException($"Channel [{chan}] already bound to [{ctrl}]"); }

                    if (!_controls.Contains(ctrl))
                    {
                        _controls.Add(ctrl);
                    }

                    ctrl.StateChange += ChannelControl_StateChange;
                    ctrl.SelectionChange += ChannelControl_SelectionChange;
                    _channels[i] = (chan, ctrl);
                    return;
                }
            }
            throw new MidiLibException($"Unknown channel [{chan}]");
        }

        /// <summary>
        /// Handles solo/mute.
        /// </summary>
        public void UpdateState()
        {
            // Update all channels.
            bool anySolo = _channels.Where(c => c.ctrl is not null && c.ctrl.State == ChannelState.Solo).Any();

            foreach (var (chan, ctrl) in _channels)
            {
                if (ctrl is null)
                {
                    chan.EnableSound = true;
                    continue;
                }

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
            _channels.ForEach(b => b.chan.Kill());
        }

        /// <summary>
        /// Remove current channels and controls. Not devices
        /// </summary>
        public void Reset()
        {
            KillAll();

            if (_controls is not null)
            {
                // Clean out our current elements.
                _channels.ForEach(b =>
                {
                    _controls.Remove(b.ctrl);
                    b.ctrl?.Dispose();
                });
            }

            _channels.Clear();
        }
        #endregion

        #region Collection accessors
        /// <summary>
        /// Iterate through the contents.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<(Channel chan, ChannelControl? ctrl)> GetChannels()
        {
            foreach (var ch in _channels)
            {
                yield return ch;
            }
        }

        /// <summary>
        /// Get channel object from control object.
        /// </summary>
        /// <param name="chCtrl"></param>
        /// <returns>The channel or null if invalid control.</returns>
        public Channel? GetChannel(ChannelControl chCtrl)
        {
            foreach (var (chan, ctrl) in _channels) { if (ctrl == chCtrl) return chan; }
            return null;
        }

        /// <summary>
        /// Get channel object from channel number.
        /// </summary>
        /// <param name="channelNum"></param>
        /// <returns>The channel or null if invalid number.</returns>
        public Channel? GetChannel(int channelNum)
        {
            foreach (var (chan, _) in _channels) { if (chan.ChannelNumber == channelNum) return chan; }
            return null;
        }

        /// <summary>
        /// Get channel object from channel number.
        /// </summary>
        /// <param name="channelName"></param>
        /// <returns>The channel or null if invalid number.</returns>
        public Channel? GetChannel(string channelName)
        {
            foreach (var (chan, _) in _channels) { if (chan.ChannelName == channelName) return chan; }
            return null;
        }

        /// <summary>
        /// Get channel object from channel handle.
        /// </summary>
        /// <param name="chnd">Channel handle</param>
        /// <returns>The channel or null if invalid handle.</returns>
        public Channel? GetChannel(ChannelHandle chnd)
        {
            foreach (var (chan, _) in _channels) { if (chan.Handle == chnd) return chan; }
            return null;
        }
        #endregion

        #region Dev stuff
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<string> DumpState()
        {
            List<string> res = [];
            res.Add("Output Channels");

            foreach (var (chan, ctrl) in _channels)
            {
                if (ctrl is not null)
                {
                    res.Add($"    Ch:{chan.ChannelNumber} St:{ctrl.State} Sel:{ctrl.Selected} Enb:{chan.EnableSound} Pt:{chan.Patch}/{chan.PatchName}");
                }
                else
                {
                    res.Add($"    Ch:{chan.ChannelNumber} ctrl:null Enb:{chan.EnableSound} Pt:{chan.Patch}/{chan.PatchName}");
                }
            }
            return res;

            // Channel:
            // bool IsDrums { get; private set; } = false;
            // string AliasFile { get; set; } = "";
            // ChannelHandle Handle { get; init; }
            // EventCollection Events { get; set; } = new();
            //
            // ChannelControl
            // (string ui, string tt) Info  set only
            // Color DrawColor
            // Color SelectedColor { get; set; } = Color.LightPink;
            // double Volume
        }
        #endregion
    }
}
