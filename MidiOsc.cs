using System;
using System.Collections.Generic;
using NAudio.Midi;
using NBagOfTricks;
using NBagOfTricks.Slog;
//using NebOsc;
using MidiLib;


namespace MidiLib
{
    /// <summary>Provides midi over OSC. Server side.</summary>
    public sealed class OscInput : IInputDevice
    {
        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("OscInput");

        /// <summary>OSC input device.</summary>
        NebOsc.Input? _oscInput = null;
        #endregion

        #region Events
        /// <inheritdoc />
        public event EventHandler<InputEventArgs>? InputEvent;
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; private set; } = "";

        /// <inheritdoc />
        public bool CaptureEnable { get; set; }

        /// <inheritdoc />
        public bool Valid { get { return _oscInput is not null; } }

        /// <inheritdoc />
        public bool LogEnable { get; set; }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// <param name="deviceName">Client must supply name of device.</param>
        /// </summary>
        public OscInput(string deviceName)
        {
            //bool inited = false;

            DeviceName = "Invalid"; // default

            try
            {
                Dispose();

                // Check for properly formed port.
                List<string> parts = deviceName.SplitByToken(":");
                if (parts.Count == 2)
                {
                    if (int.TryParse(parts[1], out int port))
                    {
                        _oscInput = new NebOsc.Input(port);
                        DeviceName = _oscInput.DeviceName;
                        _oscInput.InputEvent += OscInput_InputEvent;
                        _oscInput.LogEvent += OscInput_LogEvent;
                    }
                }
            }
            catch (Exception ex)
            {
                Dispose();
                _logger.Error($"Init OSC input failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            _oscInput?.Dispose();
            _oscInput = null;
        }
        #endregion

        #region Private functions
        /// <summary>
        /// OSC has something to say.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OscInput_LogEvent(object? sender, NebOsc.LogEventArgs e)
        {
            if(e.IsError)
            {
                _logger.Error(e.Message);
            }
            else
            {
                _logger.Debug(e.Message);
            }
        }

        /// <summary>
        /// Handle a received message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OscInput_InputEvent(object? sender, NebOsc.InputEventArgs e)
        {
            // message could be:
            // /note/ channel notenum vel
            // /controller/ channel ctlnum val

            e.Messages.ForEach(m =>
            {
                InputEventArgs args = new();

                switch (m.Address)
                {
                    case "/note/":
                        if (m.Data.Count == 3)
                        {
                            args.Channel = (int)m.Data[0];
                            args.Note = (int)m.Data[1];
                            args.Value = (int)m.Data[2]; // velocity
                        }
                        break;

                    case "/controller/":
                        if (m.Data.Count == 3)
                        {
                            args.Channel = (int)m.Data[0];
                            args.Controller = (int)m.Data[1];
                            args.Value = (int)m.Data[2]; // ctl value
                        }
                        break;

                    default:
                        args.ErrorInfo = $"Invalid address: {m.Address}";
                        break;
                }

                InputEvent?.Invoke(this, args);

                if (LogEnable)
                {
                    _logger.Trace($"Input:{args}");
                }
            });
        }
        #endregion
    }

    /// <summary>Provides midi over OSC. Client side.</summary>
    public sealed class OscOutput : IOutputDevice
    {
        #region Fields
        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("OscOutput");

        /// <summary>OSC output device.</summary>
        NebOsc.Output? _oscOutput;

        /// <summary>Access synchronizer.</summary>
        readonly object _lock = new();
        #endregion

        #region Properties
        /// <inheritdoc />
        public string DeviceName { get; private set; } = "";

        /// <inheritdoc />
        public bool Valid { get { return _oscOutput is not null; } }

        /// <inheritdoc />
        public bool LogEnable { get; set; }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        public OscOutput(string deviceName)
        {
            Dispose();

            try
            {
                // Check for properly formed url:port.
                List<string> parts = deviceName.SplitByToken(":");
                if (parts.Count == 3)
                {
                    if (int.TryParse(parts[2], out int port))
                    {
                        string ip = parts[1];
                        _oscOutput = new NebOsc.Output(ip, port);
                        DeviceName = _oscOutput.DeviceName;
                        _oscOutput.LogEvent += OscOutput_LogEvent;
                    }
                }
            }
            catch
            {
                _logger.Error($"Init OSC out failed");
                Dispose();
            }
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            _oscOutput?.Dispose();
            _oscOutput = null;
        }
        #endregion

        #region Public functions
        /// <inheritdoc />
        public void SendEvent(MidiEvent mevt)
        {
            // Critical code section.
            if (_oscOutput is not null)
            {
                lock (_lock)
                {
                    NebOsc.Message? msg = null;

                    switch (mevt)
                    {
                        case NoteOnEvent evt:
                            // /noteon/ channel notenum
                            msg = new NebOsc.Message() { Address = "/noteon" };
                            msg.Data.Add(evt.Channel);
                            msg.Data.Add(evt.NoteNumber);
                            msg.Data.Add(evt.Velocity);
                            break;

                        case NoteEvent evt when evt.Velocity == 0: // aka NoteOff
                            // /noteoff/ channel notenum
                            msg = new NebOsc.Message() { Address = "/noteoff" };
                            msg.Data.Add(evt.Channel);
                            msg.Data.Add(evt.NoteNumber);
                            break;

                        case ControlChangeEvent evt:
                            // /controller/ channel ctlnum val
                            msg = new NebOsc.Message() { Address = "/controller" };
                            msg.Data.Add(evt.Channel);
                            msg.Data.Add(evt.Controller);
                            msg.Data.Add(evt.ControllerValue);
                            break;

                        case PatchChangeEvent evt:
                            // /patch/ channel patchnum
                            msg = new NebOsc.Message() { Address = "/patch" };
                            msg.Data.Add(evt.Channel);
                            msg.Data.Add(evt.Patch);
                            break;

                        default:
                            // Unknown!
                            _logger.Error($"Unknown event: {mevt}");
                            break;
                    }

                    if (msg is not null)
                    {
                        if (_oscOutput.Send(msg))
                        {
                            if (LogEnable)
                            {
                                _logger.Trace($"Output:{msg}");
                            }
                        }
                        else
                        {
                            _logger.Error($"Send failed");
                        }
                    }
                }
            }
        }
        #endregion

        #region Private functions
        /// <summary>
        /// OSC has something to say.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OscOutput_LogEvent(object? sender, NebOsc.LogEventArgs e)
        {
            if (e.IsError)
            {
                _logger.Error(e.Message);
            }
            else
            {
                _logger.Debug(e.Message);
            }
        }
        #endregion
    }
}
