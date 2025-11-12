using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    /// <summary>Describes one midi output channel. Some properties are optional.</summary>
    public class Channel
    {
        #region Fields
        ///<summary>The collection of playable events for this channel and pattern. Key is the internal sub/time.</summary>
        readonly Dictionary<int, List<MidiEvent>> _events = [];

        /// <summary>Things that are executed once and disappear: NoteOffs, script send now. Key is the internal sub/time.</summary>
        readonly Dictionary<int, List<MidiEvent>> _transients = [];

        ///<summary>Current gain.</summary>
        double _gain = MidiLibDefs.DEFAULT_GAIN;
        #endregion

        #region Properties
        /// <summary>Actual 1-based midi channel number - required.</summary>
        public int ChannelNumber { get; set; } = -1;

        /// <summary>For muting/soloing.</summary>
        public ChannelState State { get; set; } = ChannelState.Normal;

        /// <summary>Current patch.</summary>
        public int Patch { get; set; } = -1;

        /// <summary>Current gain constrained to legal values.</summary>
        public double Gain
        {
            get { return _gain; }
            set { _gain = MathUtils.Constrain(value, MidiLibDefs.MIN_GAIN, MidiLibDefs.MAX_GAIN); }
        }

        /// <summary>Associated device.</summary>
        public IOutputDevice? Device { get; set; } = null;

        /// <summary>Add a ghost note off for note on.</summary>
        public bool AddNoteOff { get; set; } = false;

        /// <summary>Optional UI label/reference.</summary>
        public string ChannelName { get; set; } = "";

        /// <summary>Drums may be handled differently.</summary>
        public bool IsDrums { get; set; } = false;

        /// <summary>The device used by this channel. Used to find and bind the device at runtime.</summary>
        public string DeviceId { get; set; } = "";

        /// <summary>For UI user selection.</summary>
        public bool Selected { get; set; } = false;

        ///<summary>The duration of the whole channel - calculated.</summary>
        public int MaxSub { get; private set; } = 0;

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
            MaxSub = 0;

            // Bin by sub.
            foreach (var te in events)
            {
                // Add to our collection.
                if (!_events.TryGetValue(te.ScaledTime, out List<MidiEvent>? value))
                {
                    value = [];
                    _events.Add(te.ScaledTime, value);
                }

                value.Add(te.RawEvent);
                MaxSub = Math.Max(MaxSub, te.ScaledTime);
            }
        }

        /// <summary>
        /// Clean the events for the channel.
        /// </summary>
        public void Reset()
        {
            // Reset.
            _events.Clear();
            _transients.Clear();
            MaxSub = 0;

            State = ChannelState.Normal;
            Selected = false;
            IsDrums = false;
            Patch = -1;
        }

        /// <summary>
        /// Get the events for a specific sub.
        /// </summary>
        /// <param name="sub"></param>
        /// <returns></returns>
        public IEnumerable<MidiEvent> GetEvents(int sub)
        {
            return _events.TryGetValue(sub, out List<MidiEvent>? value) ? value : [];
        }

        /// <summary>
        /// Get all events.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<List<MidiEvent>> GetAllEvents()
        {
            return _events.Values;
        }

        /// <summary>
        /// Process any events for this time.
        /// </summary>
        /// <param name="sub"></param>
        public void DoStep(int sub)
        {
            // Main events.
            if(_events.TryGetValue(sub, out List<MidiEvent>? value))
            {
                foreach (var evt in value)
                {
                    switch (evt)
                    {
                        case FunctionMidiEvent fe:
                            fe.ScriptFunction?.Invoke();
                            break;

                        default:
                            SendEvent(evt);
                            break;
                    }
                }
            }

            // Transient events.
            if (_transients.TryGetValue(sub, out List<MidiEvent>? tvalue))
            {
                foreach (var evt in tvalue)
                {
                    SendEvent(evt);
                }
                _transients.Remove(sub);
            }
        }

        /// <summary>
        /// Execute any lingering transients and clear the collection.
        /// </summary>
        /// <param name="sub">After this time.</param>
        public void Flush(int sub)
        {
            _transients.Where(t => t.Key >= sub).ForEach(t => t.Value.ForEach(evt => SendEvent(evt)));
            _transients.Clear();
        }

        /// <summary>
        /// General patch sender.
        /// </summary>
        public void SendPatch()
        {
            if(Patch >= MidiDefs.MIN_MIDI && Patch <= MidiDefs.MAX_MIDI)
            {
                PatchChangeEvent evt = new(0, ChannelNumber, Patch);
                SendEvent(evt);
            }
        }

        /// <summary>
        /// Send a controller now.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="val"></param>
        public void SendController(MidiController controller, int val)
        {
            ControlChangeEvent evt = new(0, ChannelNumber, controller, val);
            SendEvent(evt);
        }

        /// <summary>
        /// Send midi all notes off.
        /// </summary>
        public void Kill()
        {
            SendController(MidiController.AllNotesOff, 0);
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

            // If note on, add a transient note off for later.
            if(AddNoteOff && evt is NoteOnEvent)
            {
                var nevt = evt as NoteOnEvent;
                int offTime = (int)evt.AbsoluteTime + nevt!.NoteLength;
                if (!_transients.TryGetValue(offTime, out List<MidiEvent>? value))
                {
                    value = [];
                    _transients.Add(offTime, value);
                }

                value.Add(nevt.OffEvent);
            }

            // Now send it.
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
        /// Get subs for the collection, rounded to beat.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channels"></param>
        /// <returns></returns>
        public static int TotalSubs<T>(this Dictionary<string, T> channels) where T : Channel
        {
            var chmax = channels.Values.Max(ch => ch.MaxSub);
            // Round total up to next beat.
            BarTime bs = new();
            bs.SetRounded(chmax, SnapType.Beat, true);
            return bs.TotalSubs;
        }
    }
}
