
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
        readonly List<ChannelControl> _channelControls = new();

        /// <summary>Prevent button press recursion.</summary>
        bool _guard = false;

        /// <summary>Our internal midi send timer resolution.</summary>
        readonly int _sendPPQ = 32;

        /// <summary>Current file.</summary>
        string _fn = "";
        #endregion

        #region Fields - user custom
        /// <summary>Cosmetics.</summary>
        readonly Color _controlColor = Color.Aquamarine;

        /// <summary>My midi out.</summary>
        readonly string _midiOutDevice = "VirtualMIDISynth #1";

        /// <summary>My midi in.</summary>
        readonly string _midiInDevice = "????";

        /// <summary>Adjust to taste.</summary>
        readonly string _exportPath = @"C:\Dev\repos\MidiLib\out";

        /// <summary>Use this if not supplied.</summary>
        readonly int _defaultTempo = 100;

        ///// <summary>Reporting interval.</summary>
        //int _report = 0;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            DirectoryInfo di = new(_exportPath);
            di.Create();

            toolStrip1.Renderer = new NBagOfUis.CheckBoxRenderer() { SelectedColor = _controlColor };

            // The text output.
            txtViewer.Font = Font;
            txtViewer.WordWrap = true;
            txtViewer.Colors.Add("ERR", Color.LightPink);
            txtViewer.Colors.Add("WRN", Color.Plum);

            // Toolbar configs. Adjust to taste.
            btnAutoplay.Checked = false;
            btnLoop.Checked = true;

            // UI configs.
            sldVolume.DrawColor = _controlColor;
            sldVolume.Resolution = VolumeDefs.RESOLUTION;
            sldVolume.Minimum = VolumeDefs.MIN;
            sldVolume.Maximum = VolumeDefs.MAX;
            sldVolume.Value = VolumeDefs.DEFAULT;
            sldVolume.Label = "volume";

            // Init channel selectors.
            cmbDrumChannel1.Items.Add("NA");
            cmbDrumChannel2.Items.Add("NA");
            for (int i = 1; i <= MidiDefs.NUM_CHANNELS; i ++)
            {
                cmbDrumChannel1.Items.Add(i);
                cmbDrumChannel2.Items.Add(i);
            }

            // Hook up some simple UI handlers.
            btnPlay.CheckedChanged += (_, __) => { UpdateState(); };
            btnRewind.Click += (_, __) => { Rewind(); };
            btnKillMidi.Click += (_, __) => { _player.KillAll(); };
            btnLogMidi.Click += (_, __) => { _player.LogMidi = btnLogMidi.Checked; };
            nudTempo.ValueChanged += (_, __) => { SetTimer(); };
            sldVolume.ValueChanged += (_, __) => { _player.Volume = sldVolume.Value; };

            // Set up timer.
            nudTempo.Value = _defaultTempo;
            SetTimer();

            // MidiTimeTest();

            // Make sure out path exists.
            DirectoryInfo di = new(_exportPath);
            di.Create();

            // Set up midi.
            _player = new(_midiOutDevice, _allChannels, _exportPath);
            //TODOX test _listener = new(_midiInDevice, _exportPath);
            //_listener.InputEvent += (object? sender, MidiEventArgs e) => { LogMessage($"RCV {e}"); };
            //_listener.Enable = true;

            vkey.ShowNoteNames = true;

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

            _player?.Dispose();
            _listener?.Dispose();

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
        void UpdateState()
        {
            // Suppress recursive updates caused by manually pressing the play button.
            if (_guard)
            {
                return;
            }

            _guard = true;

            //LogMessage($"DBG State:{_player.State}  btnLoop{btnLoop.Checked}  TotalSubdivs:{_player.TotalSubdivs}");

            switch (_player.State)
            {
                case MidiState.Complete:
                    Rewind();

                    if (btnLoop.Checked)
                    {
                        btnPlay.Checked = true;
                        Play();
                    }
                    else
                    {
                        btnPlay.Checked = false;
                        Stop();
                    }
                    break;

                case MidiState.Playing:
                    if (!btnPlay.Checked)
                    {
                        Stop();
                    }
                    break;

                case MidiState.Stopped:
                    if (btnPlay.Checked)
                    {
                        Play();
                    }
                    break;
            }

            // Update UI.
            SetPosition();

            _guard = false;
        }

        /// <summary>
        /// The user clicked something in one of the channel controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Control_ChannelChange(object? sender, ChannelControl.ChannelChangeEventArgs e)
        {
            ChannelControl chc = (ChannelControl)sender!;

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
            // Start or restart?
            if (!_mmTimer.Running)
            {
                _mmTimer.Start();
            }
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
            progPosition.Value = 0;
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
                btnPlay.Checked = false; // ==> Stop()
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

                if (ok && btnAutoplay.Checked)
                {
                    btnPlay.Checked = true; // ==> Start()
                }

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
            _channelControls.ForEach(c => Controls.Remove(c));
            _channelControls.Clear();

            // Create the new controls.
            int lastSubdiv = 0;
            int x = lbPatterns.Right + 5;
            int y = lbPatterns.Top;

            // For scaling subdivs to internal.
            MidiTime mt = new()
            {
                InternalPpq = _sendPPQ,
                MidiPpq = _mdata.DeltaTicksPerQuarterNote,
                Tempo = _defaultTempo
            };

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
                    ChannelControl control = new() { Location = new(x, y) };

                    // Bind to internal channel object.
                    _allChannels.Bind(chnum, control);

                    // Now init the control - after binding!
                    control.Patch = pinfo.Patches[i];
                    //control.IsDrums = GetDrumChannels().Contains(chnum);

                    control.ChannelChange += Control_ChannelChange;
                    Controls.Add(control);
                    _channelControls.Add(control);

                    lastSubdiv = Math.Max(lastSubdiv, control.MaxSubdiv);

                    // Adjust positioning.
                    y += control.Height + 5;

                    // Send patch maybe. These can change per pattern.
                    _player.SendPatch(chnum, pinfo.Patches[i]);
                }
            }

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

            if (btnAutoplay.Checked)
            {
                btnPlay.Checked = true; // ==> Start()
            }
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
                // if(--_report <= 0)
                // {
                //     this.InvokeIfRequired(_ => LogMessage($"DBG CurrentSubdiv:{_player.CurrentSubdiv}"));
                //     _report = 100;
                // }

                _player.DoNextStep();

                // Bump over to main thread.
                this.InvokeIfRequired(_ => UpdateState());

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
            _channelControls.ForEach(ctl => ctl.IsDrums =
                (ctl.ChannelNumber == cmbDrumChannel1.SelectedIndex) ||
                (ctl.ChannelNumber == cmbDrumChannel2.SelectedIndex));
        }
        #endregion

        #region Position bar
        /// <summary>
        /// Set UI slider value.
        /// </summary>
        void SetPosition()
        {
            int pos = _player.CurrentSubdiv * progPosition.Maximum / _allChannels.TotalSubdivs;
            progPosition.Value = pos;
        }

        /// <summary>
        /// Get UI slider value.
        /// </summary>
        void GetPosition()
        {
            int pos = _allChannels.TotalSubdivs * progPosition.Value / progPosition.Maximum;
            _player.CurrentSubdiv = pos;
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

            MidiTime mt = new()
            {
                InternalPpq = 8,
                MidiPpq = 0,
                Tempo = 100
            };

            TestClose(mt.InternalPeriod(), 75.0, 0.001);

            mt.Tempo = 99;
            TestClose(mt.InternalPeriod(), 75.757, 0.001);

            mt.MidiPpq = 384;
            mt.Tempo = 100;
            TestClose(mt.MidiToSec(144000) / 60.0, 3.75, 0.001);

            mt.MidiPpq = 96;
            mt.Tempo = 100;
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
            MidiTime mt = new()
            {
                InternalPpq = _sendPPQ,
                MidiPpq = _mdata.DeltaTicksPerQuarterNote,
                Tempo = (double)nudTempo.Value
            };

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
                foreach (var cc in _channelControls.Where(c => c.Selected))
                {
                    channels.Add(cc.ChannelNumber);
                }

                if (sender == btnExportAll)
                {
                    var s = _mdata.ExportAllEvents(_exportPath, channels);
                    LogMessage($"INF Exported to {s}");
                }
                else if (sender == btnExportPattern)
                {
                    if(_mdata.AllPatterns.Count == 1)
                    {
                        var s = _mdata.ExportGroupedEvents(_exportPath, "", channels, true);
                        LogMessage($"INF Exported default to {s}");
                    }
                    else
                    {
                        foreach (var patternName in patternNames)
                        {
                            var s = _mdata.ExportGroupedEvents(_exportPath, patternName, channels, true);
                            LogMessage($"INF Exported pattern {patternName} to {s}");
                        }
                    }
                }
                else if (sender == btnExportMidi)
                {
                    if (_mdata.AllPatterns.Count == 1)
                    {
                        // Use original ppq.
                        var s = _mdata.ExportMidi(_exportPath, "", channels, _mdata.DeltaTicksPerQuarterNote);
                        LogMessage($"INF Export midi to {s}");
                    }
                    else
                    {
                        foreach (var patternName in patternNames)
                        {
                            // Use original ppq.
                            var s = _mdata.ExportMidi(_exportPath, patternName, channels, _mdata.DeltaTicksPerQuarterNote);
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
        #endregion

        #region Piano keyboard
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Vkey_KeyboardEvent(object? sender, VirtualKeyboard.KeyboardEventArgs e)
        {
            LogMessage($"INF Vkey C:{e.ChannelNumber} N:{e.NoteId} V:{e.Velocity}");
        }
        #endregion
    }
}
