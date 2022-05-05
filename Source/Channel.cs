using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using NBagOfTricks;


namespace MidiLib
{
    #region Types
    /// <summary>Channel state.</summary>
    public enum ChannelState { Normal = 0, Solo = 1, Mute = 2 }
    #endregion

    /// <summary>Describes one midi channel.</summary>
    public class Channel
    {
        #region Constants
        public const double MIN_VOLUME = 0.0;
        public const double MAX_VOLUME = 2.0;
        #endregion

        #region Fields
        ///<summary>The main collection of playable events for a channel/pattern. The key is the internal subdiv/time.</summary>
        readonly Dictionary<int, List<MidiEvent>> _events = new();

        ///<summary>Backing.</summary>
        double _volume = 1.0;
        #endregion

        #region Properties
        /// <summary>Actual 1-based midi channel number.</summary>
        public int ChannelNumber { get; set; } = -1;

        /// <summary>For muting/soloing.</summary>
        public ChannelState State { get; set; } = ChannelState.Normal;

        ///<summary>The duration of the whole channel.</summary>
        public int MaxSubdiv { get; private set; } = 0;

        /// <summary>Current patch.</summary>
        public PatchInfo Patch { get; set; } = new();

        /// <summary>Current volume.</summary>
        public double Volume
        {
            get { return _volume; }
            set { _volume = MathUtils.Constrain(value, MIN_VOLUME, MAX_VOLUME, 0.05); }
        }
        #endregion

        #region Functions
        /// <summary>
        /// Set the events for the channel.
        /// </summary>
        /// <param name="events"></param>
        public void SetEvents(IEnumerable<EventDesc> events)
        {
            // Bin by subdiv.
            foreach (var te in events)
            {
                // Add to our collection.
                if (!_events.ContainsKey(te.ScaledTime))
                {
                    _events.Add(te.ScaledTime, new List<MidiEvent>());
                }

                _events[te.ScaledTime].Add(te.MidiEvent);
                MaxSubdiv = Math.Max(MaxSubdiv, te.ScaledTime);
            }
        }

        /// <summary>
        /// Get the events for a specific subdiv.
        /// </summary>
        /// <param name="subdiv"></param>
        /// <returns></returns>
        public List<MidiEvent> GetEvents(int subdiv)
        {
            return _events.ContainsKey(subdiv) ? _events[subdiv] : new List<MidiEvent>();
        }

        /// <summary>
        /// Clear events.
        /// </summary>
        public void ResetEvents()
        {
            _events.Clear();
        }
        #endregion
    }
}
