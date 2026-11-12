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

    [Serializable]
    public class InputConfig //-- persisted stuff
    {
        public string ChannelName { get; set; } = "???";

        [Editor(typeof(ChannelNumberTypeEditor), typeof(UITypeEditor))]
        public int ChannelNumber { get; init; } = 0;

        [Editor(typeof(MidiInputDeviceTypeEditor), typeof(UITypeEditor))]
        public string DeviceName { get; set; } = "";
    }




    /// <summary>Describes one midi input.</summary>
    public class MidiInput
    {
        #region Fields -- runtime
        /// <summary>Associated device. Required.</summary>
        readonly IInputDevice _device;
        #endregion


        // Binding
        //public InputConfig Config { get; init; }
        InputConfig _config;


        #region Properties -- runtime
        // /// <summary>Actual 1-based midi channel number. Required.</summary>
        // public int ChannelNumber { get; init; }
        public int ChannelNumber { get { return _config.ChannelNumber; } }

        // /// <summary>Actual 1-based midi channel number. Required.</summary>
        // public string ChannelName { get; init; }

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
        public MidiInput(IInputDevice device, InputConfig config)// int channelNumber, string channelName)
        {
            if (config.ChannelNumber is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"channelNumber:{(config.ChannelNumber)}"); }

            _device = device;
            _config = config;
            //ChannelNumber = channelNumber;
            //ChannelName = channelName;
            Handle = new(device.Id, _config.ChannelNumber, false);
        }

        public override string ToString()
        {
            return $"In Ch:{_config.ChannelNumber} Dev:{_device.DeviceName}";
        }
    }
}
