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
    /// <summary>Properties associated with a pattern.</summary>
    public class PatternInfo
    {
        /// <summary>Pattern name. Empty indicates single pattern aka plain midi file.</summary>
        public string PatternName { get; set; } = "";

        /// <summary>Tempo, if supplied by file. Default indicates invalid which will be filled in during read.</summary>
        public int Tempo { get; set; } = 0;

        /// <summary>Time signature, if supplied by file.</summary>
        public string TimeSig { get; set; } = "";

        /// <summary>Key signature, if supplied by file.</summary>
        public string KeySig { get; set; } = "";

        /// <summary>All the channel patches. Index is 0-based, not channel number.</summary>
        public int[] Patches { get; set; } = new int[MidiDefs.NUM_CHANNELS];

        /// <summary>All the pattern midi events.</summary>
        List<MidiEventDesc> _events = new();

        /// <summary>All the pattern midi events, key is when to play (scaled time).</summary>
        Dictionary<int, List<MidiEventDesc>> _eventsByTime = new();

        /// <summary>Normal constructor.</summary>
        public PatternInfo()
        {
            // Init fixed length arrays.
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                Patches[i] = -1;
            }
        }

        /// <summary>
        /// Add an event to the collection.
        /// </summary>
        /// <param name="evt"></param>
        public void AddEvent(MidiEventDesc evt)
        {
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
        /// <param name="sortTime">Optional sort.</param>
        /// <returns>Enumerator or null if invalid.</returns>
        public IEnumerable<MidiEventDesc>? GetFilteredEvents(List<int> channels, bool sortTime)
        {
            IEnumerable<MidiEventDesc>? descs = _events.Where(e => channels.Contains(e.ChannelNumber));

            // Always time order.
            if (descs is not null && sortTime)
            {
                descs = descs.OrderBy(e => e.AbsoluteTime);
            }

            return descs;
        }

        /// <summary>
        /// 
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