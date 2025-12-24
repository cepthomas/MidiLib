using System;
using System.Collections.Generic;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    //----------------------------------------------------------------
    public class NullInputDevice : IInputDevice
    {
        /// <inheritdoc />
        public string DeviceName { get; }

        /// <inheritdoc />
        public bool Valid { get; set; } = true;

        /// <inheritdoc />
        public bool CaptureEnable { get; set; } = true;

        /// <inheritdoc />
        public int Id { get; init; }

        /// <inheritdoc />
        public event EventHandler<BaseMidiEvent>? MessageReceive;

        /// <summary>For test use.</summary>
        public List<BaseMidiEvent> EventsToSend = [];

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        public NullInputDevice(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException("Invalid deviceName"); }

            DeviceName = deviceName;
        }

        public void Dispose()
        {
        }
        #endregion
    }

    //----------------------------------------------------------------
    public class NullOutputDevice : IOutputDevice
    {
        /// <inheritdoc />
        public string DeviceName { get; }

        /// <inheritdoc />
        public bool Valid { get; set; } = true;

        /// <inheritdoc />
        public int Id { get; init; }

        /// <inheritdoc />
        public event EventHandler<BaseMidiEvent>? MessageSend;

        /// <summary>For test use.</summary>
        public List<BaseMidiEvent> CollectedEvents = [];


        #region Lifecycle
        /// <summary>
        /// Normal constructor. OK to throw in here.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        public NullOutputDevice(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName)) { throw new ArgumentException("Invalid deviceName"); }

            DeviceName = deviceName;
        }

        public void Dispose()
        {
        }
        #endregion

        /// <inheritdoc />
        public void Send(BaseMidiEvent evt)
        {
            MessageSend?.Invoke(this, evt);

            CollectedEvents.Add(evt);        
        }
    }
}
