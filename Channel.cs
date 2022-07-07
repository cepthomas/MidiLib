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
        double _volume = VolumeDefs.DEFAULT;
        #endregion

        #region Properties
        /// <summary>Actual 1-based midi channel number - required.</summary>
        public int ChannelNumber { get; set; } = -1;

        /// <summary>For muting/soloing.</summary>
        public ChannelState State { get; set; } = ChannelState.Normal;

        /// <summary>Current patch.</summary>
        public int Patch { get; set; } = -1;

        /// <summary>Current volume constrained to legal values.</summary>
        public double Volume
        {
            get { return _volume; }
            set { _volume = MathUtils.Constrain(value, VolumeDefs.MIN, VolumeDefs.MAX); }
        }

        /// <summary>Associated device.</summary>
        public IMidiOutputDevice? Device { get; set; } = null;

        /// <summary>Optional UI label/reference.</summary>
        public string ChannelName { get; set; } = "";

        /// <summary>Drums may be handled differently.</summary>
        public bool IsDrums { get; set; } = false;

        /// <summary>The device used by this channel. Used to find and bind the device at runtime.</summary>
        public string DeviceId { get; set; } = "";

        /// <summary>For UI user selection.</summary>
        public bool Selected { get; set; } = false;

        ///<summary>The duration of the whole channel - calculated.</summary>
        public int MaxSubdiv { get; private set; } = 0;

        /// <summary>Get the number of events - calculated.</summary>
        public int NumEvents { get { return _events.Count; } }
        #endregion

        #region Functions
        /// <summary>
        /// Set the time-ordered events for the channel.
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
        public IEnumerable<MidiEvent> GetEvents(int subdiv)
        {
            return _events.ContainsKey(subdiv) ? _events[subdiv] : new List<MidiEvent>();
        }

        /// <summary>
        /// Get all events.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<List<MidiEvent>> GetAllEvents()
        {
            return _events.Values;
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

        /// <summary>
        /// General patch sender.
        /// </summary>
        public void SendPatch()
        {
            PatchChangeEvent evt = new(0, ChannelNumber, Patch);
            SendEvent(evt);
        }

        /// <summary>
        /// Send midi all notes off.
        /// </summary>
        public void Kill()
        {
            ControlChangeEvent nevt = new(0, ChannelNumber, MidiController.AllNotesOff, 0);
            SendEvent(nevt);
        }

        /// <summary>
        /// Generic event sender.
        /// </summary>
        /// <param name="evt"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void SendEvent(MidiEvent evt)
        {
            if(Device is null)
            {
                throw new InvalidOperationException("Device not set");
            }

            Device.SendEvent(evt);
        }
        #endregion

    }

    /// <summary>Helper extension methods.</summary>
    public static class ChannelUtils
    {
        /// <summary>
        /// Any solo in collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channels"></param>
        /// <returns></returns>
        public static bool AnySolo<T>(this Dictionary<string, T> channels) where T : Channel
        {
            var solo = channels.Values.Where(c => c.State == ChannelState.Solo).Any();
            return solo;
        }

        /// <summary>
        /// Get subdivs for the collection, rounded to beat.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channels"></param>
        /// <returns></returns>
        public static int TotalSubdivs<T>(this Dictionary<string, T> channels) where T : Channel
        {
            var chmax = channels.Values.Max(ch => ch.MaxSubdiv);
            // Round total up to next beat.
            BarTime bs = new();
            bs.SetRounded(chmax, SnapType.Beat, true);
            return bs.TotalSubdivs;
        }
    }
}
