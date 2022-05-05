﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using NBagOfTricks;
using NBagOfUis;


namespace MidiLib
{
    #region Types
    /// <summary>Player state.</summary>
    public enum RunState { Stopped, Playing, Complete }
    #endregion

    /// <summary>
    /// A "good enough" midi player.
    /// There are some limitations: Windows multimedia timer has 1 msec resolution at best. This causes a trade-off between
    /// ppq resolution and accuracy. The timer is also inherently wobbly.
    /// </summary>
    public class Player
    {
        #region Fields
        /// <summary>Midi output device.</summary>
        MidiOut? _midiOut = null;

        /// <summary>All the channels. Index is 0-based channel number.</summary>
        readonly Channel[] _channels = new Channel[MidiDefs.NUM_CHANNELS];

        ///<summary>Backing.</summary>
        int _currentSubdiv = 0;

        ///<summary>Adjust to taste.</summary>
        string _dumpFile = @"C:\Dev\repos\MidiLib\out\midi_out.txt";
        #endregion

        #region Properties
        /// <summary>What are we doing right now.</summary>
        public RunState State { get; set; } = RunState.Stopped;

        /// <summary>Current master volume.</summary>
        public double Volume { get; set; } = 0.8;

        /// <summary>Total length in subdivs.</summary>
        public int TotalSubdivs { get; private set; }

        /// <summary>Current position in subdivs.</summary>
        public int CurrentSubdiv { get { return _currentSubdiv; } set { UpdateCurrent(value); } }

        /// <summary>Log outbound traffic. Warning - can get busy.</summary>
        public bool LogMidi { get; set; } = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="midiDevice">Client supplies name of device.</param>
        public Player(string midiDevice)
        {
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
            for (int i = 0; i < _channels.Length; i++)
            {
                var ch = new Channel { ChannelNumber = i + 1 }; // midi is 1-based
                //ch.Events.Clear();
                _channels[i] = ch;
            }
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
            TotalSubdivs = 0;
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
            _currentSubdiv = MathUtils.Constrain(newval, 0, TotalSubdivs);
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
                bool solo = _channels.Where(c => c.State == ChannelState.Solo).Any();

                // Process each channel.
                for (int i = 0; i < _channels.Length; i++)
                {
                    var ch = _channels[i];
                    // Look for events to send.
                    if (ch.State == ChannelState.Solo || (!solo && ch.State == ChannelState.Normal))
                    {
                        // Process any sequence steps.
                        var playEvents = ch.GetEvents(_currentSubdiv);
                        foreach (var mevt in playEvents)
                        {
                            switch (mevt)
                            {
                                case NoteOnEvent evt:
                                    if (ch.Patch.Modifier == PatchInfo.PatchModifier.IsDrums && evt.Velocity == 0)
                                    {
                                        // Skip drum noteoffs as windows GM doesn't like them.
                                    }
                                    else
                                    {
                                        // Adjust volume and maybe drum channel.
                                        NoteOnEvent ne = new(
                                            evt.AbsoluteTime,
                                            ch.Patch.Modifier == PatchInfo.PatchModifier.IsDrums ? MidiDefs.DEFAULT_DRUM_CHANNEL : evt.Channel,
                                            evt.NoteNumber,
                                            Math.Min((int)(evt.Velocity * Volume * ch.Volume), MidiDefs.MAX_MIDI),
                                            evt.OffEvent is null ? 0 : evt.NoteLength); // Fix NAudio NoteLength bug.

                                        MidiSend(ne);
                                    }
                                    break;

                                case NoteEvent evt:
                                    if (ch.Patch.Modifier == PatchInfo.PatchModifier.IsDrums)
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
                if (_currentSubdiv >= TotalSubdivs)
                {
                    State = RunState.Complete;
                    _currentSubdiv = 0;
                }
            }
        }
        #endregion

        #region Public functions - channel access
        /// <summary>
        /// Get channel object for channelNumber. Throws an exception for invalid values.
        /// </summary>
        /// <param name="channelNumber"></param>
        /// <returns></returns>
        public Channel GetChannel(int channelNumber)
        {
            if (channelNumber < 1 || channelNumber > MidiDefs.NUM_CHANNELS)
            {
                throw new ArgumentOutOfRangeException(nameof(channelNumber));
            }

            return _channels[channelNumber - 1];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channelNumber"></param>
        /// <param name="events"></param>
        /// <param name="mt"></param>
        public void SetEvents(int channelNumber, IEnumerable<EventDesc> events, MidiTime mt)
        {
            var ch = GetChannel(channelNumber);
            ch.ResetEvents();

            // First scale time.
            events.ForEach(e => e.ScaledTime = mt.MidiToInternal(e.AbsoluteTime));

            ch.SetEvents(events);

            TotalSubdivs = Math.Max(TotalSubdivs, ch.MaxSubdiv);
        }

        /// <summary>
        /// Client is changing the state. Assumes client updates any UI.
        /// </summary>
        /// <param name="channelNumber"></param>
        /// <param name="state"></param>
        public void SetChannelState(int channelNumber, ChannelState state)
        {
            var ch = GetChannel(channelNumber);
            ch.State = state;
        }

        /// <summary>
        /// Client is changing the patch. Assumes client updates any UI.
        /// </summary>
        /// <param name="channelNumber">Substitute patch for this channel.</param>
        /// <param name="patch">Use this patch for Patch Channel.</param>
        public void SetPatch(int channelNumber, PatchInfo patch)
        {
            var ch = GetChannel(channelNumber);
            ch.Patch = patch;

            if (patch.Modifier == PatchInfo.PatchModifier.None)
            {
                PatchChangeEvent evt = new(0, channelNumber, patch.Patch);
                MidiSend(evt);
            }
        }

        /// <summary>
        /// Send all notes off.
        /// </summary>
        /// <param name="channelNumber">1-based channel</param>
        public void Kill(int channelNumber)
        {
            GetChannel(channelNumber);
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
                Kill(i + 1);
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

                if (LogMidi)
                {
                    File.AppendAllText(_dumpFile, $"SND {evt}");
                }
            }
        }
        #endregion
    }
}