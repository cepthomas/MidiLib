using System;
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
    public enum RunState { Stopped = 0, Playing = 1, Complete = 2 }
    #endregion

    /// <summary>
    /// A "good enough" midi player.
    /// </summary>
    public class MidiPlayer
    {
        #region Fields
        /// <summary>Midi output device.</summary>
        MidiOut? _midiOut = null;

        /// <summary>The internal channel objects.</summary>
        ChannelCollection _allChannels = new();

        /// <summary>Backing.</summary>
        int _currentSubdiv = 0;

        /// <summary>Where to log to.</summary>
        readonly string _midiLogFile = "";
        #endregion

        #region Properties
        /// <summary>What are we doing right now.</summary>
        public RunState State { get; set; } = RunState.Stopped;

        /// <summary>Current master volume.</summary>
        public double Volume { get; set; } = Channel.DEFAULT_VOLUME;

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
        /// <param name="channels">The actual channels.</param>
        /// <param name="midiLogPath">Where to log wire events (optional).</param>
        public MidiPlayer(string midiDevice, ChannelCollection channels, string midiLogPath = "")
        {
            _allChannels = channels;

            if (midiLogPath != "")
            {
                _midiLogFile = Path.Combine(midiLogPath, "midi_out.txt");
                File.Delete(_midiLogFile);
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
        }

        /// <summary>
        /// Empty constructor to satisfy nullability.
        /// </summary>
        public MidiPlayer()
        {
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
            _currentSubdiv = MathUtils.Constrain(newval, 0, _allChannels.TotalSubdivs);
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
                bool anySolo = _allChannels.AnySolo;
                int numSelected = _allChannels.NumSelected;

                // Process each channel.
                foreach (var ch in _allChannels)
                {
                    // Look for events to send. Any explicit solos?
                    if ((numSelected == 0 || ch.Selected) && (ch.State == ChannelState.Solo || (!anySolo && ch.State == ChannelState.Normal)))
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

                                case NoteEvent evt: // aka NoteOff
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
                if (_currentSubdiv >= _allChannels.TotalSubdivs)
                {
                    State = RunState.Complete;
                    _currentSubdiv = 0;
                }
            }
        }
        #endregion

        #region Public functions - midi
        public void SendPatch(int channelNumber, int patch)
        {
            if(patch >= 0)
            {
                PatchChangeEvent evt = new(0, channelNumber, patch);
                MidiSend(evt);
                _allChannels.SetPatch(channelNumber, patch);
            }
        }

        /// <summary>
        /// Send all notes off.
        /// </summary>
        /// <param name="channelNumber">1-based channel</param>
        public void Kill(int channelNumber)
        {
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

                if (_midiLogFile != "" && LogMidi)
                {
                    File.AppendAllText(_midiLogFile, $"{DateTime.Now:mm\\:ss\\.fff} {evt}{Environment.NewLine}");
                }
            }
        }
        #endregion
    }
}
