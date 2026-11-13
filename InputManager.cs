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
using System.ComponentModel;
using System.Drawing.Design;


namespace Ephemera.MidiLib
{
    // /// <summary>In charge of all midi inputs.</summary>
    // public class InputManager : IDisposable
    // {
    //     #region Fields
    //     /// <summary>All midi devices to use for receive. Index is the id.</summary>
    //     readonly List<IInputDevice> _inputDevices = [];

    //     /// <summary>All the midi inputs.</summary>
    //     readonly List<MidiInput> _midiInputs = [];
    //     #endregion

    //     #region Properties

    //     #endregion

    //     #region Events
    //     /// <summary>Handler for midi message arrived.</summary>
    //     public event EventHandler<BaseEvent>? MessageReceived;
    //     #endregion

    //     #region Lifecycle
    //     /// <summary>
    //     /// Normal constructor.
    //     /// </summary>
    //     public InputManager()
    //     {
    //     }

    //     /// <summary>
    //     /// Clean up.
    //     /// </summary>
    //     /// <exception cref="NotImplementedException"></exception>
    //     public void Dispose()
    //     {
    //         _inputDevices.ForEach(d => d.Dispose());
    //         _inputDevices.Clear();
    //     }
    //     #endregion

    //     #region API
    //     /// <summary>
    //     /// Open a midi input. Lazy inits the device. Throws if anything is invalid.
    //     /// </summary>
    //     /// <param name="deviceName"></param>
    //     /// <param name="channelNumber"></param>
    //     /// <param name="channelName"></param>
    //     /// <returns></returns>
    //     public MidiInput OpenMidiInput(string deviceName, int channelNumber, string channelName)
    //     {
    //         // Check args.
    //         if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException("Invalid deviceName"); }
    //         if (channelNumber is < 1 or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"channelNumber:{channelNumber}"); }

    //         var indev = GetInputDevice(deviceName) ?? throw new MidiLibException($"Invalid input device [{deviceName}]");

    //         // Add the channel.
    //         MidiInput mi = new(indev, channelNumber, channelName);
    //         _midiInputs.Add(mi);

    //         return mi;
    //     }

    //     /// <summary>
    //     /// Get I/O device. Lazy creation.
    //     /// </summary>
    //     /// <param name="deviceName"></param>
    //     /// <returns>The device or null if invalid.</returns>
    //     public IInputDevice? GetInputDevice(string deviceName)
    //     {
    //         IInputDevice? dev = null;

    //         // Check for known.
    //         var indevs = _inputDevices.Where(o => o.DeviceName == deviceName);

    //         if (!indevs.Any())
    //         {
    //             // Is it a new device? Try to create specific flavor based on name.
    //             try
    //             {
    //                 if (MidiInputDevice.GetAvailableDevices().Contains(deviceName))
    //                 {
    //                     dev = new MidiInputDevice(deviceName) { Id = _inputDevices.Count + 1 };
    //                 }
    //                 else if (deviceName.Contains("oscin"))
    //                 {
    //                     dev = new OscInputDevice(deviceName) { Id = _inputDevices.Count + 1 };
    //                 }
    //                 else if (deviceName.Contains("nullin"))
    //                 {
    //                     dev = new NullInputDevice(deviceName) { Id = _inputDevices.Count + 1 };
    //                 }

    //                 if (dev is not null)
    //                 {
    //                     _inputDevices.Add(dev);
    //                     dev.CaptureEnable = true;
    //                     // Just pass inputs up and let client make sense of them.
    //                     dev.MessageReceived += (sender, e) => MessageReceived?.Invoke((MidiInputDevice)sender!, e);
    //                 }
    //             }
    //             catch (Exception)
    //             {
    //                 dev = null;
    //             }
    //         }
    //         else
    //         {
    //             dev = indevs.ElementAt(0);
    //         }

    //         return dev;
    //     }

    //     /// <summary>
    //     /// Remove current channels. Not devices
    //     /// </summary>
    //     public void Reset()
    //     {
    //         _midiInputs.Clear();
    //     }
    //     #endregion
    // }
}
