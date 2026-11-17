using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows.Forms.Design;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace Ephemera.MidiLib
{
    /// <summary>Describes one output runtime.</summary>
    public class MidiOutput
    {
        #region Fields  
        /// <summary>Associated device.</summary>
        readonly IOutputDevice _device;
        #endregion

        #region Properties
        /// <summary>1-based midi channel number. Default is invalid/unknown.</summary>
        public int MidiChannel { get; init; } = 0;

        /// <summary>Host supplied name.</summary>
        public string Name { get; init; }

        /// <summary>True if not muted.</summary>
        public bool EnableSound { get; set; } = true;

        /// <summary>Handle for use by scripts.</summary>
        public ChannelHandle Handle { get; init; }

        /// <summary>Associated events - optional depending on implementation.</summary>
        public EventCollection Events { get; set; } = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor for output.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="midiChannel"></param>
        /// <param name="name"></param>
        public MidiOutput(IOutputDevice device, int midiChannel, string name)
        {
            if ((midiChannel is not MidiDefs.ALL_CHANNEL) && (midiChannel is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL))
                { throw new ArgumentOutOfRangeException($"midiChannel:{(midiChannel)}"); }

            _device = device;
            MidiChannel = midiChannel;
            Name = name;
            Handle = new(device.Id, MidiChannel, true);
        }

        /// <summary>
        /// Human readable.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Ch:{MidiChannel} Dev:{_device.DeviceName}";
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Send a midi message. Honors muting. Assumes host has fixed up volume etc.
        /// </summary>
        /// <param name="evt"></param>
        public void Send(BaseEvent evt)
        {
            if ((evt is NoteOn && !EnableSound) ||
                (evt is NoteOff && !EnableSound) ||
                 evt is Function)
            {
                // ignore
            }
            else
            {
                // Maybe fix midi channel number.
                if (MidiChannel is not MidiDefs.ALL_CHANNEL)
                {
                    evt.MidiChannel = MidiChannel;
                }
                _device.Send(evt);
            }
        }

        /// <summary>
        /// Hard stop sending.
        /// </summary>
        public void Kill()
        {
            if (MidiChannel is MidiDefs.ALL_CHANNEL)
            {
                for (int i = MidiDefs.MIN_CHANNEL; i < MidiDefs.MAX_CHANNEL; i++)
                {
                    Send(new Controller(i, MidiDefs.ControllerId.AllNotesOff, 0));
                }
            }
            else
            {
                Send(new Controller(MidiChannel, MidiDefs.ControllerId.AllNotesOff, 0));
            }
        }
        #endregion
    }
}
