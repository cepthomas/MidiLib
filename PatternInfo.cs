using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using NAudio.Midi;
using NBagOfTricks;


namespace MidiLib
{
    /// <summary>Represents the contents of a midi file pattern. If it is a plain midi file (not style) there will be one only.</summary>
    public class PatternInfo
    {
        #region Properties
        /// <summary>Pattern name. Empty indicates single pattern aka plain midi file.</summary>
        public string PatternName { get; init; } = "";

        /// <summary>Tempo, if supplied by file. Default indicates invalid which will be filled in during read.</summary>
        public int Tempo { get; set; } = 0;

        /// <summary>Time signature, if supplied by file.</summary>
        public string TimeSig { get; set; } = "";

        /// <summary>Key signature, if supplied by file.</summary>
        public string KeySig { get; set; } = "";

        /// <summary>All the channel patches. Index is 0-based, not channel number.</summary>
        public int[] Patches { get; } = new int[MidiDefs.NUM_CHANNELS];

        /// <summary>All channel numbers in the pattern.</summary>
        public HashSet<int> ChannelNumbers { get; } = new();
        #endregion

        #region Fields
        /// <summary>All the pattern midi events.</summary>
        readonly List<MidiEventDesc> _events = new();

        /// <summary>All the pattern midi events, key is when to play (scaled time).</summary>
        readonly Dictionary<int, List<MidiEventDesc>> _eventsByTime = new();

        /// <summary>For scaling subdivs to internal.</summary>
        readonly MidiTimeConverter? _mt = null;
        #endregion

        /// <summary>
        /// Default constructor. Use only for initialization!
        /// </summary>
        public PatternInfo()
        {
            // Init fixed length arrays.
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                Patches[i] = -1;
            }
        }

        /// <summary>
        /// Normal constructor.
        /// </summary>
        public PatternInfo(string name, int ppq) : this()
        {
            PatternName = name;
            _mt = new(ppq, MidiSettings.LibSettings.DefaultTempo);
        }

        /// <summary>
        /// Constructor from existing data.
        /// </summary>
        public PatternInfo(string name, int ppq, IEnumerable<MidiEventDesc> events, IEnumerable<Channel> channels, int tempo) : this(name, ppq)
        {
            events.ForEach(e => AddEvent(e));
            Tempo = tempo;
            channels.ForEach(ch => Patches[ch.ChannelNumber] = ch.Patch);
        }

        /// <summary>
        /// Add an event to the collection. This function does the scaling.
        /// </summary>
        /// <param name="evt">The event to add.</param>
        public void AddEvent(MidiEventDesc evt)
        {
            ChannelNumbers.Add(evt.ChannelNumber);

            // Scale time.
            evt.ScaledTime = _mt!.MidiToInternal(evt.AbsoluteTime);
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
        /// <param name="channels">Specific channnels.</param>
        /// <returns>Enumerator sorted by scaled time.</returns>
        public IEnumerable<MidiEventDesc> GetFilteredEvents(IEnumerable<int> channels)
        {
            IEnumerable<MidiEventDesc> descs = _events.Where(e => channels.Contains(e.ChannelNumber)) ?? Enumerable.Empty<MidiEventDesc>();
            return descs.OrderBy(e => e.ScaledTime);
        }

        /// <summary>
        /// Get all events at a specific scaled time.
        /// </summary>
        /// <param name="when"></param>
        /// <returns></returns>
        public IEnumerable<MidiEventDesc> GetEventsWhen(int when)
        {
            var evts = _eventsByTime.ContainsKey(when) ? _eventsByTime[when] : new();
            return evts;
        }

        /// <summary>
        /// Helper function.
        /// </summary>
        public Dictionary<int, int> ValidPatches
        {
            get
            {
                Dictionary<int, int> ps = new();
                for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
                {
                    int chnum = i + 1;
                    if (Patches[i] >= 0)
                    {
                        ps.Add(chnum, Patches[i]);
                    }
                }
                return ps;
            }
        }

        /// <summary>
        /// Readable version.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            List<string> content = new()
            {
                $"Name:{(PatternName == "" ? "None" : PatternName)}",
                $"Tempo:{Tempo}",
                $"TimeSig:{TimeSig}",
                $"KeySig:{KeySig}"
            };

            ValidPatches.ForEach(p => content.Add($"Ch:{p.Key} Patch:{MidiDefs.GetInstrumentName(p.Value)}"));

            return string.Join(' ', content);
        }
    }
}