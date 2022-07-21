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
        public int TimeSigNumerator { get; set; } = -1;

        /// <summary>Time signature, if supplied by file.</summary>
        public int TimeSigDenominator { get; set; } = -1;

        /// <summary>Key signature, if supplied by file.</summary>
        public int KeySigSharpsFlats { get; set; } = -1;

        /// <summary>Key signature, if supplied by file.</summary>
        public int KeySigMajorMinor { get; set; } = -1;
        #endregion

        #region Fields
        /// <summary>All the pattern midi events.</summary>
        readonly List<MidiEventDesc> _events = new();

        /// <summary>All the pattern midi events, key is when to play (scaled time).</summary>
        readonly Dictionary<int, List<MidiEventDesc>> _eventsByTime = new();

        /// <summary>For scaling subdivs to internal.</summary>
        readonly MidiTimeConverter? _mt = null;

        /// <summary>Collection of all channels in this pattern. Key is number, value is associated patch.</summary>
        readonly Dictionary<int, int> _channelPatches = new();
        #endregion

        /// <summary>
        /// Default constructor. Use only for initialization!
        /// </summary>
        public PatternInfo()
        {
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
            channels.ForEach(ch => SetChannelPatch(ch.ChannelNumber, ch.Patch));
        }

        /// <summary>
        /// Add an event to the collection. This function does the scaling.
        /// </summary>
        /// <param name="evt">The event to add.</param>
        public void AddEvent(MidiEventDesc evt)
        {
            SetChannelPatch(evt.ChannelNumber);

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
        /// Get an ordered list of valid channel numbers with their patches.
        /// </summary>
        /// <returns></returns>
        public List<(int number, int patch)> GetValidChannels()
        {
            List<(int number, int patch)> ps = new();

            if (_events.Any())
            {
                _channelPatches.OrderBy(n => n.Key).ForEach(n => { ps.Add((n.Key, n.Value)); });
            }

            return ps;
        }

        /// <summary>
        /// Get the patch associated with the arg.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public int GetPatch(int channel)
        {
            return _channelPatches.ContainsKey(channel) ? _channelPatches[channel] : -1;
        }

        /// <summary>
        /// Safely add/update info.
        /// </summary>
        /// <param name="number">The channel number</param>
        /// <param name="patch">The patch. If this is a number update, don't overwrite patch with default.</param>
        public void SetChannelPatch(int number, int patch = -1)
        {
            if (!_channelPatches.ContainsKey(number))
            {
                _channelPatches.Add(number, patch);
            }
            else if (patch != -1)
            {
                _channelPatches[number] = patch;
            }
        }

        /// <summary>
        /// Readable version.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            //List<string> content = new()
            //{
            //    $"Name:{(PatternName == "" ? "None" : PatternName)}",
            //    $"Tempo:{Tempo}",
            //    $"TimeSig:{TimeSig}",
            //    $"KeySig:{KeySig}"
            //};
            //ValidPatches.ForEach(p => content.Add($"Ch:{p.Key} Patch:{MidiDefs.GetInstrumentName(p.Value)}"));
            //return string.Join(' ', content);

            return PatternName;
        }
    }
}