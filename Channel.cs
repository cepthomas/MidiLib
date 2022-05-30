using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using NBagOfTricks;


namespace MidiLib
{
    /// <summary>Describes one midi channel.</summary>
    public class Channel
    {
        #region Fields
        ///<summary>The collection of playable events for this channel and pattern. The key is the internal subdiv/time.</summary>
        readonly Dictionary<int, List<MidiEvent>> _events = new();

        ///<summary>Backing.</summary>
        double _volume = InternalDefs.VOLUME_DEFAULT;
        #endregion

        ///// added TODOX ///////
        /// <summary>UI label and script reference.</summary>
        public string ChannelName { get; set; } = "???"; // Definitions.UNKNOWN_STRING;



        #region Properties
        /// <summary>Actual 1-based midi channel number.</summary>
        public int ChannelNumber { get; set; } = -1;

        /// <summary>For muting/soloing.</summary>
        public ChannelState State { get; set; } = ChannelState.Normal;

        /// <summary>For user selection.</summary>
        public bool Selected { get; set; } = false;

        /// <summary>Drums are handled differently.</summary>
        public bool IsDrums { get; set; } = false;

        /// <summary>Current patch.</summary>
        public int Patch { get; set; } = -1;

        /// <summary>Current volume constrained to legal values.</summary>
        public double Volume
        {
            get { return _volume; }
            set { _volume = MathUtils.Constrain(value, InternalDefs.VOLUME_MIN, InternalDefs.VOLUME_MAX, InternalDefs.VOLUME_RESOLUTION); }
        }

        ///<summary>The duration of the whole channel.</summary>
        public int MaxSubdiv { get; private set; } = 0;
        #endregion

        #region Functions
        /// <summary>
        /// Set the events for the channel.
        /// </summary>
        /// <param name="events"></param>
        public void SetEvents(IEnumerable<EventDesc> events)
        {
            // Reset.
            _events.Clear();
            MaxSubdiv = 0;

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
        /// Clean the events for the channel.
        /// </summary>
        public void Reset()
        {
            // Reset.
            _events.Clear();
            MaxSubdiv = 0;

            State = ChannelState.Normal;
            Selected = false;
            IsDrums = false;
            Patch = -1;
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
        #endregion
    }
}
