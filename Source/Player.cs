using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using NBagOfTricks;
using NBagOfUis;
using static MidiLib.ChannelCollection;


namespace MidiLib
{
    #region Types
    /// <summary>Player state.</summary>
    public enum RunState { Stopped, Playing, Complete }
    #endregion

    /// <summary>
    /// A "good enough" midi player.
    /// </summary>
    public class Player
    {
        #region Fields
        /// <summary>Midi output device.</summary>
        MidiOut? _midiOut = null;

        ///// <summary>All the channels. Index is 0-based, not channel number.</summary>
        //readonly Channel[] _channels = new Channel[MidiDefs.NUM_CHANNELS];

        /// <summary>Backing.</summary>
        int _currentSubdiv = 0;
        #endregion

        #region Properties
        /// <summary>What are we doing right now.</summary>
        public RunState State { get; set; } = RunState.Stopped;

        /// <summary>Current master volume.</summary>
        public double Volume { get; set; } = Channel.DEFAULT_VOLUME;

        ///// <summary>Total length in subdivs.</summary>
        //public int TotalSubdivs { get; private set; }

        /// <summary>Current position in subdivs.</summary>
        public int CurrentSubdiv { get { return _currentSubdiv; } set { UpdateCurrent(value); } }

        /// <summary>Log outbound traffic. Warning - can get busy.</summary>
        public bool LogMidi { get; set; } = false;

        /// <summary>Adjust to taste.</summary>
        public string MidiTraceFile { get; set; } = "";
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="midiDevice">Client supplies name of device.</param>
        public Player(string midiDevice)
        {
            if(MidiTraceFile != "")
            {
                File.Delete(MidiTraceFile);
            }

            // Figure out which midi output device.
            int devIndex = -1;
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                if (midiDevice == MidiOut.DeviceInfo(i).ProductName)
                {
                    _midiOut = new MidiOut(devIndex);
                    break;
                }
            }

            if (_midiOut is null)
            {
                throw new ArgumentException($"Invalid midi device: {midiDevice}");
            }

            // Init the channels.
            TheChannels.Init();

            //for (int i = 0; i < _channels.Length; i++)
            //{
            //    int chnum = i + 1;
            //    var ch = new Channel { ChannelNumber = chnum };
            //    //ch.Events.Clear();
            //    _channels[i] = ch;
            //}
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        public void Dispose()
        {
            State = RunState.Stopped;

            // Resources.
            _midiOut?.Dispose();
            _midiOut = null;
        }
        /// <summary>
        /// Hard reset before loading a pattern.
        /// </summary>
        public void Reset()
        {
            State = RunState.Stopped;
            CurrentSubdiv = 0;
        }
        #endregion

        #region Public functions - runtime
        /// <summary>
        /// Start/stop everything.
        /// </summary>
        /// <param name="go"></param>
        public void Run(bool go)
        {
            if (go)
            {
                State = RunState.Playing;
            }
            else
            {
                KillAll();
                State = RunState.Stopped;
            }
        }

        /// <summary>
        /// Set position.
        /// </summary>
        /// <param name="newval"></param>
        void UpdateCurrent(int newval)
        {
            _currentSubdiv = MathUtils.Constrain(newval, 0, TheChannels.TotalSubdivs);
        }

        /// <summary>
        /// Synchronously outputs the next midi events. Does solo/mute.
        /// This is running on the background thread.
        /// </summary>
        /// <returns></returns>
        public void DoNextStep()
        {
            if (State == RunState.Playing)
            {
                // Any soloes?
                //bool solo = _channels.Where(c => c.State == ChannelState.Solo).Any();

                // Process each channel.
                foreach(var ch in TheChannels)
                {
                    // Look for events to send.
                    if (ch.State == ChannelState.Solo || (!TheChannels.AnySolo && ch.State == ChannelState.Normal))
                    {
                        // Process any sequence steps.
                        var playEvents = ch.GetEvents(_currentSubdiv);
                        foreach (var mevt in playEvents)
                        {
                            switch (mevt)
                            {
                                case NoteOnEvent evt:
                                    if (ch.IsDrums && evt.Velocity == 0)
                                    {
                                        // Skip drum noteoffs as windows GM doesn't like them.
                                    }
                                    else
                                    {
                                        // Adjust volume. Redirect drum channel to default.
                                        NoteOnEvent ne = new(
                                            evt.AbsoluteTime,
                                            ch.IsDrums ? MidiDefs.DEFAULT_DRUM_CHANNEL : evt.Channel,
                                            evt.NoteNumber,
                                            Math.Min((int)(evt.Velocity * Volume * ch.Volume), MidiDefs.MAX_MIDI),
                                            evt.OffEvent is null ? 0 : evt.NoteLength); // Fix NAudio NoteLength bug.

                                        MidiSend(ne);
                                    }
                                    break;

                                case NoteEvent evt:
                                    if (ch.IsDrums)
                                    {
                                        // Skip drum noteoffs as windows GM doesn't like them.
                                    }
                                    else
                                    {
                                        MidiSend(evt);
                                    }
                                    break;

                                default:
                                    // Everything else as is.
                                    MidiSend(mevt);
                                    break;
                            }
                        }
                    }
                }

                // Bump time. Check for end of play.
                _currentSubdiv++;
                if (_currentSubdiv >= TheChannels.TotalSubdivs)
                {
                    State = RunState.Complete;
                    _currentSubdiv = 0;
                }
            }
        }
        #endregion

        #region Public functions - channel access
        ///// <summary>
        ///// Get channel object for channelNumber. Throws an exception for invalid values.
        ///// </summary>
        ///// <param name="channelNumber"></param>
        ///// <returns></returns>
        //public Channel GetChannel(int channelNumber)
        //{
        //    if (channelNumber < 1 || channelNumber > MidiDefs.NUM_CHANNELS)
        //    {
        //        throw new ArgumentOutOfRangeException(nameof(channelNumber));
        //    }

        //    return _channels[channelNumber - 1];
        //}

        ///// <summary>
        ///// Set events for channel.
        ///// </summary>
        ///// <param name="channelNumber"></param>
        ///// <param name="events"></param>
        ///// <param name="mt"></param>
        //public void SetEvents(int channelNumber, IEnumerable<EventDesc> events, MidiTime mt)
        //{
        //    var ch = GetChannel(channelNumber);

        //    // First scale time.
        //    events.ForEach(e => e.ScaledTime = mt.MidiToInternal(e.AbsoluteTime));

        //    ch.SetEvents(events);

        //    TotalSubdivs = Math.Max(TotalSubdivs, ch.MaxSubdiv);
        //}

        ///// <summary>
        ///// Client is changing the state.
        ///// </summary>
        ///// <param name="channelNumber"></param>
        ///// <param name="state"></param>
        //public void SetChannelState(int channelNumber, ChannelState state)
        //{
        //    var ch = GetChannel(channelNumber);
        //    ch.State = state;
        //}

        ///// <summary>
        ///// Client stipulates drums override.
        ///// </summary>
        ///// <param name="channelNumber"></param>
        ///// <param name="isDrums"></param>
        //public void SetChannelDrums(int channelNumber, bool isDrums)
        //{
        //    var ch = GetChannel(channelNumber);
        //    ch.IsDrums = isDrums;
        //}

        /// <summary>
        /// Client is changing the patch.
        /// </summary>
        /// <param name="channelNumber">Substitute patch for this channel.</param>
        /// <param name="patch">Use this patch for Patch Channel.</param>
        public void SendPatch(int channelNumber, int patch)
        {
            if(patch >= 0)
            {
                PatchChangeEvent evt = new(0, channelNumber, patch);
                MidiSend(evt);
                TheChannels.SetPatch(channelNumber, patch);
            }
        }

        /// <summary>
        /// Send all notes off.
        /// </summary>
        /// <param name="channelNumber">1-based channel</param>
        public void Kill(int channelNumber)
        {
//            GetChannel(channelNumber);
            ControlChangeEvent nevt = new(0, channelNumber, MidiController.AllNotesOff, 0);
            MidiSend(nevt);
        }

        /// <summary>
        /// Send all notes off.
        /// </summary>
        public void KillAll()
        {
            // Send midi stop all notes just in case.
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                int chnum = i + 1;
                Kill(chnum);
            }
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Send midi.
        /// </summary>
        /// <param name="evt"></param>
        void MidiSend(MidiEvent evt)
        {
            if(_midiOut is not null)
            {
                _midiOut.Send(evt.GetAsShortMessage());

                if (LogMidi && MidiTraceFile != "")
                {
                    File.AppendAllText(MidiTraceFile, $"{DateTime.Now:mm\\:ss\\.fff} {evt}{Environment.NewLine}");
                }
            }
        }
        #endregion
    }
}
