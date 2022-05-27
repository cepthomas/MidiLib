using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using NBagOfTricks;
using NBagOfUis;

// TODO Snipping, editing, etc.


namespace MidiLib
{
    /// <summary>
    /// A "good enough" midi player.
    /// </summary>
    public class MidiPlayer : IDisposable
    {
        #region Fields
        /// <summary>Midi output device.</summary>
        readonly MidiOut? _midiOut = null;

        /// <summary>The internal channel objects.</summary>
        readonly ChannelCollection _allChannels = new();

        /// <summary>Backing.</summary>
        int _currentSubdiv = 0;

        /// <summary>Where to log to.</summary>
        readonly string _midiLogFile = "";
        #endregion

        #region Properties
        /// <summary>Are we ok?</summary>
        public bool Valid { get { return _midiOut is not null; } }

        /// <summary>What are we doing right now.</summary>
        public MidiState State { get; set; } = MidiState.Stopped;

        /// <summary>Current master volume.</summary>
        public double Volume { get; set; } = InternalDefs.VOLUME_DEFAULT;

        /// <summary>Current position in subdivs.</summary>
        public int CurrentSubdiv
        {
            get { return _currentSubdiv; }
            set { _currentSubdiv = MathUtils.Constrain(value, 0, _allChannels.TotalSubdivs); }
        }

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
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                if (midiDevice == MidiOut.DeviceInfo(i).ProductName)
                {
                    _midiOut = new MidiOut(i);
                    break;
                }
            }
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        public void Dispose()
        {
            State = MidiState.Stopped;

            // Resources.
            _midiOut?.Dispose();
        }

        /// <summary>
        /// Hard reset before loading a pattern.
        /// </summary>
        public void Reset()
        {
            State = MidiState.Stopped;
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
                State = MidiState.Playing;
            }
            else
            {
                KillAll();
                State = MidiState.Stopped;
            }
        }

        /// <summary>
        /// Synchronously outputs the next midi events. Does solo/mute.
        /// This is running on the background thread.
        /// </summary>
        /// <returns></returns>
        public void DoNextStep()
        {
            if (State == MidiState.Playing)
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
                    State = MidiState.Complete;
                    _currentSubdiv = 0;
                }
            }
        }
        #endregion

        #region Public functions - midi
        public void SendPatch(int channelNumber, int patch)
        {
            if (patch >= 0)
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
