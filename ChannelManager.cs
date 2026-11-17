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
    /// <summary>Hides all the messy logic with midi devices and channels, and the NAudio versions of each.</summary>
    public class ChannelManager : IDisposable
    {
        #region Fields
        /// <summary>All midi devices to use for send. Index is the IOutputDevice.Id.</summary>
        readonly List<IOutputDevice> _outputDevices = [];

        /// <summary>All the outputs with associated controls.</summary>
        readonly List<(MidiOutput mout, ChannelControl? ctrl)> _midiOutputs = [];

        /// <summary>Need to add/remove dynamically generated controls.</summary>
        readonly Control.ControlCollection? _controls;

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

            _inputDevices.ForEach(d => d.Dispose());
            _inputDevices.Clear();

            _outputDevices.ForEach(d => d.Dispose());
            _outputDevices.Clear();
        }
        #endregion

        #region UI event handlers
        /// <summary>
        /// User clicked solo/mute. Slow-time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_StateChange(object? sender, ChannelControl.StateChangeEventArgs e)
        {
            //var cc = sender as ChannelControl;
            //var _ = GetMidiOutput(cc!) ?? throw new MidiLibException($"Invalid midiChannel");

            UpdateState();
        }

        /// <summary>
        /// User clicked something. Slow-time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_SelectionChange(object? sender, ChannelControl.SelectionChangeEventArgs e)
        {
            //var cc = sender as ChannelControl;
            //var _ = GetMidiOutput(cc!) ?? throw new MidiLibException($"Invalid midiChannel");

            // Nothing to do?
        }
        #endregion

        #region Outputs
        /// <summary>
        /// Open a normal output midiChannel. Lazy inits the device. Throws if anything is invalid. Slow-time.
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="midiChannel"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public MidiOutput OpenMidiOutput(string deviceName, int midiChannel, string name)
        {
            // Check args.
            if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException("Invalid deviceName"); }
            if ((midiChannel is not MidiDefs.ALL_CHANNEL) && (midiChannel is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL))
                { throw new ArgumentOutOfRangeException($"midiChannel:{(midiChannel)}"); }

            var outdev = GetOutputDevice(deviceName) ?? throw new MidiLibException($"Invalid output device [{deviceName}]");

            MidiOutput mout = new(outdev, midiChannel, name);
            _midiOutputs.Add((mout, null));
            return mout;
        }
        #endregion

        #region Inputs
        /// <summary>
        /// Open a midi input. Lazy inits the device. Throws if anything is invalid. Slow-time.
        /// </summary>
        /// <param name="deviceName"></param>
        /// <param name="midiChannel"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public MidiInput OpenMidiInput(string deviceName, int midiChannel, string name)
        {
            // Check args.
            if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException("Invalid deviceName"); }
            if (midiChannel is < 1 or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"midiChannel:{midiChannel}"); }

            var indev = GetInputDevice(deviceName) ?? throw new MidiLibException($"Invalid input device [{deviceName}]");

            // Add the midiChannel.
            MidiInput mi = new(indev, midiChannel, name);
            _midiInputs.Add(mi);

            return mi;
        }

        /// <summary>
        /// Something arrived from a midi device. Filter and maybe pass on to registered clients.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Dev_MessageReceived(object? sender, BaseEvent e)
        {
            var dev = (sender as MidiInputDevice)!;
            ChannelHandle chnd = new(dev.Id, e.MidiChannel, false);
            var mins = _midiInputs.Where(mi => mi.Handle == chnd && mi.Enable);
            mins.ForEach(i => MessageReceived?.Invoke(this, e));
        }
        #endregion

        #region Devices
        /// <summary>
        /// Get output device. Lazy creation. Slow-time.
        /// </summary>
        /// <param name="deviceName"></param>
        /// <returns>The device or null if invalid.</returns>
        IOutputDevice? GetOutputDevice(string deviceName)
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

        /// <summary>
        /// Get input device. Lazy creation. Slow-time.
        /// </summary>
        /// <param name="deviceName"></param>
        /// <returns>The device or null if invalid.</returns>
        IInputDevice? GetInputDevice(string deviceName)
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
                        dev.MessageReceived += Dev_MessageReceived;
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

        #endregion

        #region User functions
        /// <summary>
        /// Sends patch message. Throws if anything is invalid.
        /// </summary>
        /// <param name="mout"></param>
        /// <param name="patch">Explicit</param>
        /// <returns></returns>
        public void SetPatch(MidiOutput mout, Enum patch)
        {
            // Check args.
            ArgumentNullException.ThrowIfNull(mout);

            var pe = new Patch(mout.MidiChannel, patch);
            mout.Send(pe);
        }

        /// <summary>
        /// Bind a UI control to a midi output.
        /// </summary>
        /// <param name="mout"></param>
        /// <param name="ctrl"></param>
        public void Bind(MidiOutput mout, ChannelControl ctrl)
        {
            if (_controls is null) { throw new MidiLibException($"This configuration does not support UI controls"); }

            for (int i = 0; i < _midiOutputs.Count; i++)
            {
                if (_midiOutputs[i].mout == mout)
                {
                    if (_midiOutputs[i].ctrl is not null) { throw new MidiLibException($"Channel [{mout}] already bound to [{ctrl}]"); }

                    if (!_controls.Contains(ctrl))
                    {
                        _controls.Add(ctrl);
                    }

                    ctrl.StateChange += ChannelControl_StateChange;
                    ctrl.SelectionChange += ChannelControl_SelectionChange;
                    _midiOutputs[i] = (mout, ctrl);
                    return;
                }
            }
            throw new MidiLibException($"Unknown midiChannel [{mout}]");
        }

        /// <summary>
        /// Handles solo/mute across all channels.
        /// </summary>
        public void UpdateState()
        {
            // Update all channels.
            bool anySolo = _midiOutputs.Where(c => c.ctrl is not null && c.ctrl.State == ChannelState.Solo).Any();

            foreach (var (mout, ctrl) in _midiOutputs)
            {
                if (ctrl is null)
                {
                    mout.EnableSound = true;
                    continue;
                }

                switch ((ctrl.State, anySolo))
                {
                    case (ChannelState.Solo, true):
                        mout.EnableSound = true;
                        break;

                    case (ChannelState.Mute, true):
                    case (ChannelState.Normal, true):
                        mout.EnableSound = false;
                        mout.Kill(); // just in case
                        break;

                    case (ChannelState.Solo, false): // never
                        mout.EnableSound = true;
                        break;

                    case (ChannelState.Mute, false):
                        mout.EnableSound = false;
                        mout.Kill(); // just in case
                        break;

                    case (ChannelState.Normal, false):
                        mout.EnableSound = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Die die die.
        /// </summary>
        public void KillAll()
        {
            _midiOutputs.ForEach(b => b.mout.Kill());
        }

        /// <summary>
        /// Remove current channels and controls - but not devices.
        /// </summary>
        public void Reset()
        {
            KillAll();

            if (_controls is not null)
            {
                // Clean out our current elements.
                _midiOutputs.ForEach(b =>
                {
                    _controls.Remove(b.ctrl);
                    b.ctrl?.Dispose();
                });
            }

            _midiInputs.Clear();
            _midiOutputs.Clear();
        }
        #endregion

        #region Collection accessors
        /// <summary>
        /// Iterate through the contents.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<(MidiOutput mout, ChannelControl? ctrl)> GetOutputs() // TODO1 do better?
        {
            foreach (var op in _midiOutputs)
            {
                yield return op;
            }
        }

        ///// <summary>
        ///// Get midiChannel object from control object.
        ///// </summary>
        ///// <param name="chCtrl"></param>
        ///// <returns>The midiChannel or null if invalid control.</returns>
        //public MidiOutput? GetMidiOutput(ChannelControl chCtrl)
        //{
        //    foreach (var (mout, ctrl) in _midiOutputs) { if (ctrl == chCtrl) return mout; }
        //    return null;
        //}

        ///// <summary>
        ///// Get midiChannel object from midiChannel number.
        ///// </summary>
        ///// <param name="channelNum"></param>
        ///// <returns>The midiChannel or null if invalid number.</returns>
        //public MidiOutput? GetMidiOutput(int channelNum)
        //{
        //    foreach (var (mout, _) in _midiOutputs) { if (mout.MidiChannel == channelNum) return mout; }
        //    return null;
        //}

        ///// <summary>
        ///// Get midiChannel object from midiChannel number.
        ///// </summary>
        ///// <param name="name"></param>
        ///// <returns>The midiChannel or null if invalid number.</returns>
        //public MidiOutput? GetMidiOutput(string name)
        //{
        //    foreach (var (mout, _) in _midiOutputs) { if (mout.name == name) return mout; }
        //    return null;
        //}

        ///// <summary>
        ///// Get midiChannel object from midiChannel handle.
        ///// </summary>
        ///// <param name="chnd">Channel handle</param>
        ///// <returns>The midiChannel or null if invalid handle.</returns>
        //public MidiOutput? GetMidiOutput(ChannelHandle chnd)
        //{
        //    foreach (var (mout, _) in _midiOutputs) { if (mout.Handle == chnd) return mout; }
        //    return null;
        //}
        #endregion
    }
}
