using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using NBagOfTricks;


namespace MidiLib
{
    /// <summary>Describes one midi output channel. Some properties are optional.</summary>
    public class Channel
    {
        #region Fields
        ///<summary>The collection of playable events for this channel and pattern. The key is the internal subdiv/time.</summary>
        readonly Dictionary<int, List<MidiEvent>> _events = new();

        ///<summary>Backing.</summary>
        double _volume = InternalDefs.VOLUME_DEFAULT;
        #endregion

        #region Properties - required
        /// <summary>Actual 1-based midi channel number.</summary>
        public int ChannelNumber { get; set; } = -1;

        /// <summary>For muting/soloing.</summary>
        public ChannelState State { get; set; } = ChannelState.Normal;

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

        #region Properties - optional
        /// <summary>Optional label/reference.</summary>
        public string ChannelName { get; set; } = "";

        /// <summary>The device used by this channel. Used to find and bind the device at runtime.</summary>
        public string DeviceId { get; set; } = "";

        /// <summary>For user selection.</summary>
        public bool Selected { get; set; } = false;

        /// <summary>For nefarious purposes.</summary>
        public object? Tag { get; set; } = null;
        #endregion

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

        /// <summary>Get the next volume.</summary>
        /// <param name="def"></param>
        /// <returns></returns>
        public double NextVol(double def)
        {
            //var vel = _volWobbler is null ? def : _volWobbler.Next(def);
            //vel *= _volume;
            //return vel;
            return _volume;
        }
        #endregion
    }
}
