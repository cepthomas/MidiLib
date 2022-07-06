using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using NBagOfTricks;
using NBagOfUis;
using NBagOfTricks.Slog;


// TODO Snipping, editing, etc.


namespace MidiLib
{
    /// <summary>
    /// A "good enough" midi player. Single output device.
    /// </summary>
    public sealed class MidiPlayer : IDisposable
    {
        #region Fields
        /// <summary>Midi output device.</summary>
        readonly MidiSender? _midiOut = null;

        /// <summary>The internal channel objects.</summary>
        readonly ChannelManager _channelManager = new();

        /// <summary>Backing.</summary>
        int _currentSubdiv = 0;

        /// <summary>Midi send logging.</summary>
        readonly Logger _logger = LogManager.CreateLogger("MidiPlayer");
        #endregion

        #region Properties
        /// <summary>Are we ok?</summary>
        public bool Valid { get { return _midiOut is not null; } }

        /// <summary>What are we doing right now.</summary>
        public bool Playing { get; private set; }

        /// <summary>Current master volume.</summary>
        public double Volume { get; set; } = VolumeDefs.DEFAULT;

        /// <summary>Current position in subdivs.</summary>
        public int CurrentSubdiv
        {
            get { return _currentSubdiv; }
            set { _currentSubdiv = MathUtils.Constrain(value, 0, _channelManager.TotalSubdivs); }
        }

        /// <summary>Log outbound traffic at Trace level. Warning - can get busy.</summary>
        public bool LogMidi { get { return _logger.Enable; } set { _logger.Enable = value; } }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="midiDevice">Client supplies name of device.</param>
        /// <param name="channelManager">The actual channels.</param>
        public MidiPlayer(string midiDevice, ChannelManager channelManager)
        {
            _channelManager = channelManager;
            LogMidi = false;
            _midiOut = new MidiSender(midiDevice);

            if(!_midiOut.Valid)
            {
                throw new ArgumentException($"Invalid midi out device: {midiDevice}");
            }
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        public void Dispose()
        {
            // Resources.
            _midiOut?.Dispose();
        }

        /// <summary>
        /// Hard reset before loading a pattern.
        /// </summary>
        public void Reset()
        {
            Playing = false;
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
            Playing = go;

            if (go)
            {
                Playing = true;
            }
            else
            {
                KillAll();
                Playing = false;
            }
        }

        /// <summary>
        /// Synchronously outputs the next midi events. Does solo/mute.
        /// This is running on the background thread.
        /// </summary>
        /// <returns>True if sequence completed.</returns>
        public bool DoNextStep()
        {
            bool done = false;

            if (Playing)
            {
                // Any soloes?
                bool anySolo = _channelManager.AnySolo;
                int numSelected = _channelManager.NumSelected;

                // Process each channel.
                foreach (var ch in _channelManager)
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

                                        SendMidi(ne);
                                    }
                                    break;

                                case NoteEvent evt: // aka NoteOff
                                    if (ch.IsDrums)
                                    {
                                        // Skip drum noteoffs as windows GM doesn't like them.
                                    }
                                    else
                                    {
                                        SendMidi(evt);
                                    }
                                    break;

                                default:
                                    // Everything else as is.
                                    SendMidi(mevt);
                                    break;
                            }
                        }
                    }
                }

                // Bump time. Check for end of play. Client must handle next action.
                _currentSubdiv++;
                if (_currentSubdiv >= _channelManager.TotalSubdivs)
                {
                    done = true;
                    _currentSubdiv = 0;
                }
            }

            return done;
        }
        #endregion

        #region Public functions - midi TODOX1 - this is messed up, send using Channel: breaks ChannelManager
        public void SendPatch(int channelNumber, int patch)
        {
            if (patch >= 0)
            {
                PatchChangeEvent evt = new(0, channelNumber, patch);
                SendMidi(evt);
                _channelManager.SetPatch(channelNumber, patch);
            }
        }

        /// <summary>
        /// Send all notes off.
        /// </summary>
        /// <param name="channelNumber">1-based channel</param>
        public void Kill(int channelNumber)
        {
            ControlChangeEvent nevt = new(0, channelNumber, MidiController.AllNotesOff, 0);
            SendMidi(nevt);
        }

        /// <summary>
        /// Send all notes off.
        /// </summary>
        public void KillAll()
        {
            //Playing = false; // TODOX2 also stop playing

            // Send midi stop all notes just in case.
            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                int chnum = i + 1;
                Kill(chnum);
            }
        }

        /// <summary>
        /// Send midi event.
        /// </summary>
        /// <param name="evt"></param>
        public void SendMidi(MidiEvent evt)
        {
            if(_midiOut is not null)
            {
                _midiOut.SendEvent(evt);
            }
            if(LogMidi)
            {
                _logger.Trace(evt.ToString());
            }
        }
        #endregion
    }
}
