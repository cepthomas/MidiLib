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
        public int ChannelNumber { get; init; } = 0;

        /// <summary>Host supplied channel name.</summary>
        public string ChannelName { get; init; }

        /// <summary>True if channel is not muted.</summary>
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
        /// <param name="channelNumber"></param>
        /// <param name="channelName"></param>
        public MidiOutput(IOutputDevice device, int channelNumber, string channelName)
        {
            if (channelNumber is < MidiDefs.MIN_CHANNEL or > MidiDefs.MAX_CHANNEL) { throw new ArgumentOutOfRangeException($"channelNumber:{(channelNumber)}"); }

            _device = device;
            ChannelNumber = channelNumber;
            ChannelName = channelName;
            Handle = new(device.Id, ChannelNumber, true);
        }

        /// <summary>
        /// Human readable.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Ch:{ChannelNumber} Dev:{_device.DeviceName}";
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Sends patch message. Throws if anything is invalid.
        /// </summary>
        /// <param name="patch">Explicit</param>
        /// <returns></returns>
        public void SetPatch(int patch)
        {
            // Check args.
            if (patch is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException($"patch:{patch}"); }

//TODO1            _device.Send(new Patch(ChannelNumber, patch));
        }

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
                evt.ChannelNumber = ChannelNumber;
                _device.Send(evt);
            }
        }

        /// <summary>
        /// Hard stop the channel.
        /// </summary>
        public void Kill()
        {
            Send(new Controller(ChannelNumber, MidiDefs.ControllerId.AllNotesOff, 0));
        }
        #endregion
    }
}
