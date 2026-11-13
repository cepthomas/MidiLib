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
    /// <summary>Describes one midi input runtime.</summary>
    public class MidiInput
    {
        #region Fields
        /// <summary>Associated device.</summary>
        readonly IInputDevice _device;
        #endregion

        #region Properties
        /// <summary>1-based midi channel number. Required.</summary>
        public int ChannelNumber { get; init; }

        /// <summary>Host supplied channel name.</summary>
        public string ChannelName { get; init; }

        /// <summary>True if channel is active.</summary>
        public bool Enable { get; set; } = true;

        /// <summary>Handle for use by scripts.</summary>
        public ChannelHandle Handle { get; init; }
        #endregion

        /// <summary>
        /// Constructor with required args.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="channelNumber"></param>
        /// <param name="channelName"></param>
        public MidiInput(IInputDevice device, int channelNumber, string channelName)
        {
            if (channelNumber is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"channelNumber:{(channelNumber)}"); }

            _device = device;
            // _config = config;
            ChannelNumber = channelNumber;
            ChannelName = channelName;
            Handle = new(device.Id, ChannelNumber, false);
        }

        public override string ToString()
        {
            return $"In Ch:{ChannelNumber} Dev:{_device.DeviceName}";
        }
    }
}
