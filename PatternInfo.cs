using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using NAudio.Midi;


namespace MidiLib
{
    /// <summary>Represents the contents of a midi file pattern. If it is a plain midi file (not style) there will be one only.</summary>
    public class PatternInfo
    {
        /// <summary>Pattern name. Empty indicates single pattern aka plain midi file.</summary>
        public string PatternName { get; init; } = "";

        /// <summary>Tempo, if supplied by file. Default indicates invalid which will be filled in during read.</summary>
        public int Tempo { get; set; } = 0;

        /// <summary>Time signature, if supplied by file.</summary>
        public string TimeSig { get; set; } = "";

        /// <summary>Key signature, if supplied by file.</summary>
        public string KeySig { get; set; } = "";

        /// <summary>All the channel patches. Index is 0-based, not channel number.</summary>
        public int[] Patches { get; set; } = new int[MidiDefs.NUM_CHANNELS];

        /// <summary>All the pattern midi events.</summary>
        readonly List<MidiEventDesc> _events = new();

        /// <summary>All the pattern midi events, key is when to play (scaled time).</summary>
        readonly Dictionary<int, List<MidiEventDesc>> _eventsByTime = new();

        /// <summary>For scaling subdivs to internal.</summary>
        readonly MidiTimeConverter? _mt = null;

        /// <summary>
        /// Normal constructor.
        /// </summary>
        public PatternInfo(string name, int ppq)
        {
            PatternName = name;
            _mt = new(ppq, MidiSettings.LibSettings.DefaultTempo);

            // Init fixed length arrays.
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                Patches[i] = -1;
            }
        }

        /// <summary>
        /// Constructor from existing data.
        /// </summary>
        public PatternInfo(string name, int ppq, List<MidiEventDesc> events, List<Channel> channels, int tempo) : this(name, ppq)
        {
            _events.ForEach(e => AddEvent(e));
            Tempo = tempo;
            channels.ForEach(ch => Patches[ch.ChannelNumber] = ch.Patch);
        }

        /// <summary>
        /// Add an event to the collection.
        /// </summary>
        /// <param name="evt">The event with properly scaled time.</param>
        public void AddEvent(MidiEventDesc evt)
        {
            // First scale time.
            _events.ForEach(e => e.ScaledTime = _mt!.MidiToInternal(e.AbsoluteTime));

            _events.Add(evt);

            if(!_eventsByTime.ContainsKey(evt.ScaledTime))
            {
                _eventsByTime.Add(evt.ScaledTime, new());
            }

            _eventsByTime[evt.ScaledTime].Add(evt);
        }

        /// <summary>
        /// Get enumerator for events using supplied filters.
        /// </summary>
        /// <param name="channels">Specific channnels or all if empty.</param>
        /// <param name="sortByTime">Optional sort.</param>
        /// <returns>Enumerator.</returns>
        public IEnumerable<MidiEventDesc> GetFilteredEvents(List<int> channels, bool sortByTime)
        {
            IEnumerable<MidiEventDesc> descs = _events.Where(e => channels.Contains(e.ChannelNumber));

            if (descs is not null)
            {
                if (sortByTime)
                {
                    descs = descs.OrderBy(e => e.AbsoluteTime);
                }
            }
            else
            {
                descs = Enumerable.Empty<MidiEventDesc>();
            }

            return descs;
        }

        /// <summary>
        /// Get all events at a specific time.
        /// </summary>
        /// <param name="when"></param>
        /// <returns></returns>
        public List<MidiEventDesc> GetEvents(int when)
        {
            var evts = _eventsByTime.ContainsKey(when) ? _eventsByTime[when] : new();
            return evts;
        }

        /// <summary>Readable version.</summary>
        /// <returns></returns>
        public override string ToString()
        {
            List<string> content = new();
            content.Add($"Name:{(PatternName == "" ? "None" : PatternName)}");
            content.Add($"Tempo:{Tempo}");

            if (TimeSig != "")
            {
                content.Add($"TimeSig:{TimeSig}");
            }

            if (KeySig != "")
            {
                content.Add($"KeySig:{KeySig}");
            }

            for(int i = 0; i <MidiDefs.NUM_CHANNELS; i++)
            {
                int chnum = i + 1;
                if(Patches[i] >= 0)
                {
                    content.Add($"Ch:{chnum} Patch:{MidiDefs.GetInstrumentName(Patches[i])}");
                }
            }

            return string.Join(' ', content);
        }
    }
}