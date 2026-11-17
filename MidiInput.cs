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
        public int MidiChannel { get; init; }

        /// <summary>Host supplied name.</summary>
        public string Name { get; init; }

        /// <summary>True if listening.</summary>
        public bool Enable { get; set; } = true;

        /// <summary>Handle for use by scripts.</summary>
        public ChannelHandle Handle { get; init; }
        #endregion

        /// <summary>
        /// Constructor with required args.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="midiChannel"></param>
        /// <param name="name"></param>
        public MidiInput(IInputDevice device, int midiChannel, string name)
        {
            if (midiChannel is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"midiChannel:{(midiChannel)}"); }

            _device = device;
            MidiChannel = midiChannel;
            Name = name;
            Handle = new(device.Id, MidiChannel, false);
        }

        public override string ToString()
        {
            return $"In Ch:{MidiChannel} Dev:{_device.DeviceName}";
        }
    }
}
