using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using NBagOfTricks;

namespace MidiLib
{
    /// <summary>Manages all the current channels. Does status/state. Does not send midi messages.</summary>
    public class ChannelManager : IEnumerable<Channel> //TODOX need to be iterable?
    {
        #region Fields
        /// <summary>All the channels. Index is 0-based, not channel number. TODOX new model will be sparse ch number + device.</summary>
        readonly Channel[] _channels = new Channel[MidiDefs.NUM_CHANNELS];
        #endregion

        #region Properties
        /// <summary>Longest length of channels in subdivs.</summary>
        public int TotalSubdivs { get; private set; }

        /// <summary>Has at least one solo channel.</summary>
        public bool AnySolo { get { return _channels.Where(c => c.State == ChannelState.Solo).Any(); } }

        /// <summary>Has at least one muted channel.</summary>
        public bool AnyMute { get { return _channels.Where(c => c.State == ChannelState.Mute).Any(); } }

        /// <summary>How many selected.</summary>
        public int NumSelected { get { return _channels.Where(c => c.Selected).Count(); } }
        #endregion

        #region Public functions
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public ChannelManager()
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
        /// Clean the channels.
        /// </summary>
        public void Reset()
        {
            TotalSubdivs = 0;

            // Reset the channel events.
            _channels.ForEach(ch => ch.Reset());
        }

        /// <summary>
        /// Opaque binder.
        /// </summary>
        /// <param name="channelNumber">From this channel...</param>
        /// <param name="control">...to this control.</param>
        public void Bind(int channelNumber, PlayerControl control)
        {
            control.BoundChannel = GetChannel(channelNumber);
        }

        /// <summary>
        /// Set events for the channel.
        /// </summary>
        /// <param name="channelNumber"></param>
        /// <param name="events"></param>
        public void SetEvents(int channelNumber, IEnumerable<MidiEventDesc> events)
        {
            var ch = GetChannel(channelNumber);

            ch.SetEvents(events);

            // Round total up to next beat.
            BarTime bs = new();
            bs.SetRounded(ch.MaxSubdiv, SnapType.Beat, true);
            TotalSubdivs = Math.Max(TotalSubdivs, bs.TotalSubdivs);
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
        /// Get channel object for channelNumber. Does checking for invalid values.
        /// </summary>
        /// <param name="channelNumber"></param>
        /// <returns>The channel</returns>
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
            for (int i = 0; i < _channels.Length; i++)
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
