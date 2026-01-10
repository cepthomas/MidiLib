using System;
using System.Collections.Generic;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    //----------------------------------------------------------------
    public class NullInputDevice : IInputDevice
    {
        /// <inheritdoc />
        /// Uses 'nullin:{name}' for DeviceName.
        public string DeviceName { get; } = "Invalid";

        /// <inheritdoc />
        public bool Valid { get; set; } = false;

        /// <inheritdoc />
        public bool CaptureEnable { get; set; } = true;

        /// <inheritdoc />
        public int Id { get; init; }

        /// <inheritdoc />
        public event EventHandler<BaseEvent>? MessageReceive;

        /// <summary>For test use.</summary>
        public List<BaseEvent> EventsToSend = [];

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        public NullInputDevice(string deviceName)
        {
            var parts = deviceName.SplitByToken(":");
            if (parts.Count == 2)
            {
                if (parts[0] == "nullin" && !string.IsNullOrEmpty(parts[1]))
                {
                    DeviceName = deviceName;
                    Valid = true;
                }
            }

            if (!Valid)
            {
                throw new ArgumentException($"Invalid device name [{deviceName}]");
            }
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
        /// Uses 'nullout:{name}' for DeviceName.
        public string DeviceName { get; } = "Invalid";

        /// <inheritdoc />
        public bool Valid { get; set; } = false;

        /// <inheritdoc />
        public int Id { get; init; }

        /// <inheritdoc />
        public event EventHandler<BaseEvent>? MessageSend;

        /// <summary>For test use.</summary>
        public List<BaseEvent> CollectedEvents = [];

        #region Lifecycle
        /// <summary>
        /// Normal constructor. OK to throw in here.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        public NullOutputDevice(string deviceName)
        {
            var parts = deviceName.SplitByToken(":");
            if (parts.Count == 2)
            {
                if (parts[0] == "nullout" && !string.IsNullOrEmpty(parts[1]))
                {
                    DeviceName = deviceName;
                    Valid = true;
                }
            }

            if (!Valid)
            {
                throw new ArgumentException($"Invalid device name [{deviceName}]");
            }
        }

        public void Dispose()
        {
        }
        #endregion

        /// <inheritdoc />
        public void Send(BaseEvent evt)
        {
            MessageSend?.Invoke(this, evt);

            CollectedEvents.Add(evt);        
        }
    }
}
