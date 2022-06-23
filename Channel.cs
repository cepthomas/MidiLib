using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using NBagOfTricks;


namespace MidiLib
{
    /// <summary>Describes one midi output channel.</summary>
    public class Channel
    {
        #region Fields
        ///<summary>The collection of playable events for this channel and pattern. The key is the internal subdiv/time.</summary>
        readonly Dictionary<int, List<MidiEvent>> _events = new();

        ///<summary>Backing.</summary>
        double _volume = InternalDefs.VOLUME_DEFAULT;
        #endregion

        // TODOX from neb:
        /// <summary>Optional label/reference.</summary>
        public string ChannelName { get; set; } = "";
        ///// <summary>The device type for this channel. Used to find and bind the device at runtime.</summary>
        /// <summary>How wobbly. 0 to disable.</summary>
        public double VolumeWobbleRange { get; set; } = 0.0;
        /// <summary>Wobbler for volume (optional).</summary>
        public Wobbler? VolWobbler { get; set; } = null;




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


        /// <summary>Get the next volume.</summary>
        /// <param name="def"></param>
        /// <returns></returns>
        public double NextVol(double def)
        {
            var vel = VolWobbler is null ? def : VolWobbler.Next(def);
            vel *= _volume;
            return vel;
        }


        #region Functions
        /// <summary>
        /// Set the events for the channel.
        /// </summary>
        /// <param name="events"></param>
        public void SetEvents(IEnumerable<MidiEventDesc> events)
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
