
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using NAudio.Midi;
using NBagOfTricks;
using NBagOfUis;


namespace MidiLib.Test
{
    public partial class MainForm : Form
    {
        #region Types
        public enum PlayState { Stop, Play, Rewind, Complete }
        #endregion

        #region Fields - internal
        /// <summary>The internal channel objects.</summary>
        ChannelCollection _allChannels = new();

        /// <summary>Midi player.</summary>
        MidiPlayer _player;

        /// <summary>Midi input.</summary>
        readonly MidiListener _listener;

        /// <summary>The fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Midi events from the input file.</summary>
        readonly MidiData _mdata = new();

        /// <summary>All the channel controls.</summary>
        readonly List<PlayerControl> _playerControls = new();

        /// <summary>Prevent button press recursion.</summary>
        bool _guard = false;

        /// <summary>Our internal midi send timer resolution.</summary>
        const int PPQ = 32;

        /// <summary>4/4 time only.</summary>
        const int BEATS_PER_BAR = 4;

        /// <summary>Current file.</summary>
        string _fn = "";
        #endregion

        #region Fields - user custom
        /// <summary>Cosmetics.</summary>
        readonly Color _controlColor = Color.Aquamarine;

        /// <summary>My midi out.</summary>
        readonly string _midiOutDevice = "VirtualMIDISynth #1";

        /// <summary>My midi in.</summary>
        readonly string _midiInDevice = "MPK mini";

        /// <summary>Where to put things.</summary>
        readonly string _outPath = @"C:\Dev\repos\MidiLib\out";

        /// <summary>Use this if not supplied.</summary>
        readonly int _defaultTempo = 100;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            toolStrip1.Renderer = new NBagOfUis.CheckBoxRenderer() { SelectedColor = _controlColor };

            // Make sure out path exists.
            DirectoryInfo di = new(_outPath);
            di.Create();

            // The text output.
            txtViewer.Font = Font;
            txtViewer.WordWrap = true;
            txtViewer.Colors.Add("ERR", Color.LightPink);
            txtViewer.Colors.Add("WRN", Color.Plum);

            // UI configs.
            sldVolume.DrawColor = _controlColor;
            sldVolume.Resolution = InternalDefs.VOLUME_RESOLUTION;
            sldVolume.Minimum = InternalDefs.VOLUME_MIN;
            sldVolume.Maximum = InternalDefs.VOLUME_MAX;
            sldVolume.Value = InternalDefs.VOLUME_DEFAULT;
            sldVolume.Label = "volume";

            // Time controller.
            MidiSettings.Snap = SnapType.Beat;
            MidiSettings.ZeroBased = true;
            barBar.ProgressColor = _controlColor;
            barBar.CurrentTimeChanged += BarBar_CurrentTimeChanged;

            // Init channel selectors.
            cmbDrumChannel1.Items.Add("NA");
            cmbDrumChannel2.Items.Add("NA");
            for (int i = 1; i <= MidiDefs.NUM_CHANNELS; i ++)
            {
                cmbDrumChannel1.Items.Add(i);
                cmbDrumChannel2.Items.Add(i);
            }

            // Set up midi.
            DumpMidiDevices();

            _player = new(_midiOutDevice, _allChannels, _outPath);
            if (_player.Valid)
            {
            }
            else
            {
                LogMessage($"ERR Something wrong with your midi output device:{_midiOutDevice}");
            }

            _listener = new(_midiInDevice, _outPath);
            if(_listener.Valid)
            {
                _listener.InputEvent += (object? sender, MidiEventArgs e) => { this.InvokeIfRequired(_ => { LogMessage($"RCV {e}"); }); };
                _listener.Enable = true;
            }
            else
            {
                LogMessage($"ERR Something wrong with your midi input device:{_midiInDevice}");
            }

            // Hook up some simple UI handlers.
            btnPlay.CheckedChanged += (_, __) => { UpdateState(btnPlay.Checked ? PlayState.Play : PlayState.Stop); };
            btnRewind.Click += (_, __) => { UpdateState(PlayState.Rewind); };
            btnKillMidi.Click += (_, __) => { _player.KillAll(); };
            btnLogMidi.Click += (_, __) => { _player.LogMidi = btnLogMidi.Checked; };
            nudTempo.ValueChanged += (_, __) => { SetTimer(); };
            sldVolume.ValueChanged += (_, __) => { _player.Volume = sldVolume.Value; };

            channelControl1.ChannelNumber = 7;
            channelControl1.Volume = 1.2;
            channelControl1.Patch = 44;

            // Set up timer.
            nudTempo.Value = _defaultTempo;

            // MidiTimeTest();

            // Look for filename passed in.
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                OpenFile(args[1]);
            }
            else
            {
                OpenFile(@"C:\Dev\repos\TestAudioFiles\_LoveSong.S474.sty");
            }
        }

        /// <summary>
        /// Clean up on shutdown. Dispose() will get the rest.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Stop();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            // Resources.
            _mmTimer.Stop();
            _mmTimer.Dispose();

            _player.Dispose();
            _listener.Dispose();

            if (disposing && (components is not null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        #region State management
        /// <summary>
        /// General state management. Triggered by play button or the player via mm timer function.
        /// </summary>
        void UpdateState(PlayState state)
        {
            // Suppress recursive updates caused by manually pressing the play button.
            if (_guard)
            {
                return;
            }
            _guard = true;

            //LogMessage($"DBG State:{_player.State}  btnLoop{btnLoop.Checked}  TotalSubdivs:{_player.TotalSubdivs}");

            switch(state)
            {
                case PlayState.Complete:
                    btnPlay.Checked = false;
                    Rewind();
                    Stop();
                    break;

                case PlayState.Play:
                    btnPlay.Checked = true;
                    Play();
                    break;

                case PlayState.Stop:
                    btnPlay.Checked = false;
                    Stop();
                    break;

                case PlayState.Rewind:
                    Rewind();
                    break;
            }

            _guard = false;
        }

        /// <summary>
        /// The user clicked something in one of the channel controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Control_ChannelChange(object? sender, PlayerControl.ChannelChangeEventArgs e)
        {
            PlayerControl chc = (PlayerControl)sender!;

            if (e.StateChange)
            {
                switch (chc.State)
                {
                    case ChannelState.Normal:
                        break;

                    case ChannelState.Solo:
                        // Mute any other non-solo channels.
                        for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
                        {
                            int chnum = i + 1;
                            if (chnum != chc.ChannelNumber && chc.State != ChannelState.Solo)
                            {
                                _player.Kill(chnum);
                            }
                        }
                        break;

                    case ChannelState.Mute:
                        _player.Kill(chc.ChannelNumber);
                        break;
                }
            }

            if (e.PatchChange && chc.Patch >= 0)
            {
                _player.SendPatch(chc.ChannelNumber, chc.Patch);
            }
        }
        #endregion

        #region Transport control
        /// <summary>
        /// Internal handler.
        /// </summary>
        void Play()
        {
            _mmTimer.Start();
            _player.Run(true);
        }

        /// <summary>
        /// Internal handler.
        /// </summary>
        void Stop()
        {
            _mmTimer.Stop();
            _player.Run(false);
        }

        /// <summary>
        /// Go back Jack. Doesn't affect the run state.
        /// </summary>
        void Rewind()
        {
            _player.CurrentSubdiv = 0;
            barBar.Current = new(0);
        }

        /// <summary>
        /// User has changed the time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BarBar_CurrentTimeChanged(object? sender, EventArgs e)
        {
            _player.CurrentSubdiv = barBar.Current.TotalSubdivs;
        }
        #endregion

        #region File handling
        /// <summary>
        /// Common file opener. Initializes pattern list from contents.
        /// </summary>
        /// <param name="fn">The file to open.</param>
        public bool OpenFile(string fn)
        {
            bool ok = true;
            _fn = "";

            LogMessage($"INF Reading file: {fn}");

            if(btnPlay.Checked)
            {
                Stop();
            }

            try
            {
                // Reset stuff.
                cmbDrumChannel1.SelectedIndex = MidiDefs.DEFAULT_DRUM_CHANNEL;
                cmbDrumChannel2.SelectedIndex = 0;
                _allChannels.Reset();

                // Process the file. Set the default tempo from preferences.
                _mdata.Read(fn, _defaultTempo, false);

                // Init new stuff with contents of file/pattern.
                lbPatterns.Items.Clear();

                if(_mdata.AllPatterns.Count == 0)
                {
                    LogMessage($"ERR Something wrong with this file: {fn}");
                    ok = false;
                }
                else if(_mdata.AllPatterns.Count == 1) // plain midi
                {
                    var pinfo = _mdata.AllPatterns[0];
                    LoadPattern(pinfo);
                }
                else // style - multiple patterns.
                {
                    foreach (var p in _mdata.AllPatterns)
                    {
                        switch (p.PatternName)
                        {
                            // These don't contain a pattern.
                            case "SFF1": // initial patches are in here
                            case "SFF2":
                            case "SInt":
                                break;

                            case "":
                                LogMessage("ERR Well, this should never happen!");
                                break;

                            default:
                                lbPatterns.Items.Add(p.PatternName);
                                break;
                        }
                    }

                    if (lbPatterns.Items.Count > 0)
                    {
                        lbPatterns.SelectedIndex = 0;
                    }
                }

                Rewind();

                _fn = fn;
                Text = $"Midi Lib - {fn}";

            }
            catch (Exception ex)
            {
                LogMessage($"ERR Couldn't open the file: {fn} because: {ex.Message}");
                Text = "Midi Lib";
                ok = false;
            }

            return ok;
        }
        #endregion

        #region Process patterns
        /// <summary>
        /// Load the requested pattern and create controls.
        /// </summary>
        /// <param name="pinfo"></param>
        void LoadPattern(PatternInfo pinfo)
        {
            _player.Reset();

            // Clean out our controls collection.
            _playerControls.ForEach(c => Controls.Remove(c));
            _playerControls.Clear();

            // Create the new controls.
            int lastSubdiv = 0;
            int x = lbPatterns.Right + 5;
            int y = lbPatterns.Top;

            // For scaling subdivs to internal.
            MidiTimeConverter mt = new(_mdata.DeltaTicksPerQuarterNote, _defaultTempo);

            for (int i = 0; i < MidiDefs.NUM_CHANNELS; i++)
            {
                int chnum = i + 1;

                var chEvents = _mdata.AllEvents.
                    Where(e => e.PatternName == pinfo.PatternName && e.ChannelNumber == chnum && (e.MidiEvent is NoteEvent || e.MidiEvent is NoteOnEvent)).
                    OrderBy(e => e.AbsoluteTime);

                // Is this channel pertinent?
                if (chEvents.Any())
                {
                    _allChannels.SetEvents(chnum, chEvents, mt);

                    // Make new control.
                    PlayerControl control = new() { Location = new(x, y) };

                    // Bind to internal channel object.
                    _allChannels.Bind(chnum, control);

                    // Now init the control - after binding!
                    control.Patch = pinfo.Patches[i];
                    //control.IsDrums = GetDrumChannels().Contains(chnum);

                    control.ChannelChange += Control_ChannelChange;
                    Controls.Add(control);
                    _playerControls.Add(control);

                    lastSubdiv = Math.Max(lastSubdiv, control.MaxSubdiv);

                    // Adjust positioning.
                    y += control.Height + 5;

                    // Send patch maybe. These can change per pattern.
                    _player.SendPatch(chnum, pinfo.Patches[i]);
                }
            }

            // Update bar.
            barBar.Start = new(0);
            barBar.End = new(_allChannels.TotalSubdivs - 1);
            barBar.Length = new(_allChannels.TotalSubdivs);
            barBar.Current = new(0);

            UpdateDrumChannels();
        }

        /// <summary>
        /// Load pattern selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Patterns_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var pinfo = _mdata.AllPatterns.Where(p => p.PatternName == lbPatterns.SelectedItem.ToString()).First();

            LoadPattern(pinfo!);

            Rewind();
        }

        /// <summary>
        /// Pattern selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AllOrNone_Click(object? sender, EventArgs e)
        {
            bool check = sender == btnAll;
            for(int i = 0; i < lbPatterns.Items.Count; i++)
            {
                lbPatterns.SetItemChecked(i, check);                
            }
        }
        #endregion

        #region Process tick
        /// <summary>
        /// Multimedia timer callback. Synchronously outputs the next midi events.
        /// This is running on the background thread.
        /// </summary>
        void MmTimerCallback(double totalElapsed, double periodElapsed)
        {
            try
            {
                // Bump time. Check for end of play. Client will take care of transport control.
                barBar.IncrementCurrent(1);
                if (_player.DoNextStep())
                {
                    // Done playing.
                    // Bump over to main thread.
                    this.InvokeIfRequired(_ => UpdateState(PlayState.Complete));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region Drum channel
        /// <summary>
        /// User changed the drum channel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DrumChannel_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateDrumChannels();
        }

        /// <summary>
        /// Update all channels based on current UI.
        /// </summary>
        void UpdateDrumChannels()
        {
            _playerControls.ForEach(ctl => ctl.IsDrums =
                (ctl.ChannelNumber == cmbDrumChannel1.SelectedIndex) ||
                (ctl.ChannelNumber == cmbDrumChannel2.SelectedIndex));
        }
        #endregion

        #region MidiTime
        /// <summary>
        /// Unit test.
        /// </summary>
        void MidiTimeTest()
        {
            // If we use ppq of 8 (32nd notes):
            // 100 bpm = 800 ticks/min = 13.33 ticks/sec = 0.01333 ticks/msec = 75.0 msec/tick
            //  99 bpm = 792 ticks/min = 13.20 ticks/sec = 0.0132 ticks/msec  = 75.757 msec/tick

            MidiTimeConverter mt = new(0, 100);
            TestClose(mt.InternalPeriod(), 75.0, 0.001);

            mt = new(0, 90);
            TestClose(mt.InternalPeriod(), 75.757, 0.001);

            mt = new(384, 100);
            TestClose(mt.MidiToSec(144000) / 60.0, 3.75, 0.001);

            mt = new(96, 100);
            TestClose(mt.MidiPeriod(), 6.25, 0.001);

            void TestClose(double value1, double value2, double tolerance)
            {
                if (Math.Abs(value1 - value2) > tolerance)
                {
                    LogMessage($"ERR [{value1}] not close enough to [{value2}]");
                }
            }
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Convert tempo to period and set mm timer.
        /// </summary>
        void SetTimer()
        {
            MidiTimeConverter mt = new(_mdata.DeltaTicksPerQuarterNote, (double)nudTempo.Value);
            double period = mt.RoundedInternalPeriod();
            _mmTimer.SetTimer((int)Math.Round(period), MmTimerCallback);
        }

        /// <summary>
        /// Export current file to human readable or midi.
        /// </summary>
        void Export_Click(object? sender, EventArgs e)
        {
            try
            {
                // Collect filters.
                List<string> patternNames = new();
                foreach(var p in lbPatterns.CheckedItems)
                {
                    patternNames.Add(p.ToString()!);
                }

                List<int> channels = new();
                foreach (var cc in _playerControls.Where(c => c.Selected))
                {
                    channels.Add(cc.ChannelNumber);
                }

                if (sender == btnExportAll)
                {
                    var s = _mdata.ExportAllEvents(_outPath, channels);
                    LogMessage($"INF Exported to {s}");
                }
                else if (sender == btnExportPattern)
                {
                    if(_mdata.AllPatterns.Count == 1)
                    {
                        var s = _mdata.ExportGroupedEvents(_outPath, "", channels, true);
                        LogMessage($"INF Exported default to {s}");
                    }
                    else
                    {
                        foreach (var patternName in patternNames)
                        {
                            var s = _mdata.ExportGroupedEvents(_outPath, patternName, channels, true);
                            LogMessage($"INF Exported pattern {patternName} to {s}");
                        }
                    }
                }
                else if (sender == btnExportMidi)
                {
                    if (_mdata.AllPatterns.Count == 1)
                    {
                        // Use original ppq.
                        var s = _mdata.ExportMidi(_outPath, "", channels, _mdata.DeltaTicksPerQuarterNote);
                        LogMessage($"INF Export midi to {s}");
                    }
                    else
                    {
                        foreach (var patternName in patternNames)
                        {
                            // Use original ppq.
                            var s = _mdata.ExportMidi(_outPath, patternName, channels, _mdata.DeltaTicksPerQuarterNote);
                            LogMessage($"INF Export midi to {s}");
                        }
                    }
                }
                else
                {
                    LogMessage($"ERR Ooops: {sender}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERR {ex.Message}");
            }
        }

        /// <summary>
        /// Something you should know.
        /// </summary>
        /// <param name="msg"></param>
        void LogMessage(string msg)
        {
            string s = $"> {msg}";
            txtViewer.AppendLine(s);
        }

        /// <summary>
        /// Tell me what you have.
        /// </summary>
        void DumpMidiDevices()
        {
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                LogMessage($"Midi In {i} \"{MidiIn.DeviceInfo(i).ProductName}\"");
            }

            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                LogMessage($"Midi Out {i} \"{MidiOut.DeviceInfo(i).ProductName}\"");
            }
        }
        #endregion

        #region Piano keyboard
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Vkey_KeyboardEvent(object? sender, VirtualKeyboard.KeyboardEventArgs e)
        {
            LogMessage($"INF Vkey N:{e.NoteId} V:{e.Velocity}");
        }
        #endregion
    }
}
