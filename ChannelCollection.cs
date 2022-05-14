﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using NBagOfTricks;

namespace MidiLib
{
    /// <summary>Contains all the midi channel descriptors and properties related to the full set.</summary>
    public class ChannelCollection : IEnumerable<Channel>
    {
        #region Fields
        /// <summary>All the channels. Index is 0-based, not channel number.</summary>
        readonly Channel[] _channels = new Channel[MidiDefs.NUM_CHANNELS];
        #endregion

        #region Properties
        /// <summary>The global collection.</summary>
        public static ChannelCollection TheChannels { get; } = new();

        /// <summary>Longest length of channels in subdivs.</summary>
        public int TotalSubdivs { get; private set; }

        /// <summary>Has at least one solo channel.</summary>
        public bool AnySolo { get { return _channels.Where(c => c.State == ChannelState.Solo).Any(); } }
        #endregion

        #region Public functions
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public ChannelCollection()
        {
            TotalSubdivs = 0;

            // Init the channels.
            for (int i = 0; i < _channels.Length; i++)
            {
                int chnum = i + 1;
                var ch = new Channel { ChannelNumber = chnum };
                _channels[i] = ch;
            }
        }


        /// <summary>
        /// Opaque binder.
        /// </summary>
        /// <param name="chnum">This channel to..</param>
        /// <param name="control">This control.</param>
        public void Bind(int chnum, ChannelControl control)
        {
            control.Channel = GetChannel(chnum);
        }

        /// <summary>
        /// Set events for channel.
        /// </summary>
        /// <param name="channelNumber"></param>
        /// <param name="events"></param>
        /// <param name="mt"></param>
        public void SetEvents(int channelNumber, IEnumerable<EventDesc> events, MidiTime mt)
        {
            var ch = GetChannel(channelNumber);

            // First scale time.
            events.ForEach(e => e.ScaledTime = mt.MidiToInternal(e.AbsoluteTime));

            ch.SetEvents(events);

            TotalSubdivs = Math.Max(TotalSubdivs, ch.MaxSubdiv);
        }

        /// <summary>
        /// Client is changing the state.
        /// </summary>
        /// <param name="channelNumber"></param>
        /// <param name="state"></param>
        public void SetChannelState(int channelNumber, ChannelState state)
        {
            var ch = GetChannel(channelNumber);
            ch.State = state;
        }

        /// <summary>
        /// Is the channel drums?
        /// </summary>
        /// <param name="channelNumber"></param>
        /// <returns>T/F</returns>
        public bool IsDrums(int channelNumber)
        {
            var ch = GetChannel(channelNumber);
            return ch.IsDrums;
        }

        /// <summary>
        /// Client wants a new patch.
        /// </summary>
        /// <param name="channelNumber"></param>
        /// <param name="patch"></param>
        public void SetPatch(int channelNumber, int patch)
        {
            // Use this call just to test for valid range.
            MidiDefs.GetInstrumentName(patch);
            var ch = GetChannel(channelNumber);
            ch.Patch = patch;
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Get channel object for channelNumber. Throws an exception for invalid values.
        /// </summary>
        /// <param name="channelNumber"></param>
        /// <returns>The channel</returns>
        //public
        Channel GetChannel(int channelNumber)
        {
            if (channelNumber < 1 || channelNumber > MidiDefs.NUM_CHANNELS)
            {
                throw new ArgumentOutOfRangeException(nameof(channelNumber));
            }

            return _channels[channelNumber - 1];
        }
        #endregion

        #region IEnumerable implementation
        /// <summary>
        /// Enumerator.
        /// </summary>
        /// <returns>Enumerator</returns>
        public IEnumerator<Channel> GetEnumerator()
        {
            for (int i = 0; i < _channels.Length; i ++)
            {
                yield return _channels[i];
            }
        }

        /// <summary>
        /// Enumerator.
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator(); // Just return the generic version
        }
        #endregion
    }
}