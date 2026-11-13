using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms.Design;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    /// <summary>Channel handle class for interacting with scripts.</summary>
    public class ChannelHandle
    {
        /// <summary>Differentiate inputs and outputs.</summary>
        static readonly int OUTPUT_FLAG = 0x0800;

        /// <summary>Script-side handle.</summary>
        public int Raw { get { return _raw; } }
        readonly int _raw = 0;

        /// <summary>App-side id.</summary>
        public int DeviceId { get { return (_raw >> 4) & 0x0F; } }

        /// <summary>App-side value.</summary>
        public int ChannelNumber { get { return _raw & 0x0F; } }

        /// <summary>App-side value.</summary>
        public bool Output { get { return (_raw & OUTPUT_FLAG) > 0; } }

        /// <summary>App-side constructor.</summary>
        public ChannelHandle(int deviceId, int channelNumber, bool output)
        {
            _raw = (deviceId << 4) | channelNumber | (output ? OUTPUT_FLAG : OUTPUT_FLAG);
        }

        /// <summary>Script-side constructor.</summary>
        public ChannelHandle(int handle)
        {
            _raw = handle;
        }

        /// <summary>Human readable.</summary>
        public override string ToString()
        {
            return $"{(Output ? "out" : "in")} {ChannelNumber} on D{DeviceId}";
        }
    }
}
