
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
using static MidiLib.ChannelCollection;


namespace MidiLib.Test
{
    public partial class MainForm : Form
    {
        #region Fields - internal
        /// <summary>Midi player.</summary>
        readonly Player _player;

        /// <summary>The fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Midi events from the input file.</summary>
        MidiData _mdata = new();

        /// <summary>All the channel controls.</summary>
        readonly List<ChannelControl> _channelControls = new();

        /// <summary>Prevent button press recursion.</summary>
        bool _guard = false;

        /// <summary>Our internal midi send timer resolution.</summary>
        readonly int _sendPPQ = 32;

        /// <summary>Current file.</summary>
        string _fn = "";

        /// <summary>Main drum channel.</summary>
        int _drumChannel1 = 0;

        /// <summary>Secondary (optional) drum channel.</summary>
        int _drumChannel2 = 0;
        #endregion

        #region Fields - user custom
        /// <summary>Cosmetics.</summary>
        readonly Color _controlColor = Color.Aquamarine;

        /// <summary>My midi out.</summary>
        readonly string _midiDevice = "VirtualMIDISynth #1";

        /// <summary>Adjust to taste.</summary>
        readonly string _exportPath = @"C:\Dev\repos\MidiLib\out";

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
            _player = new(_midiDevice) { MidiTraceFile = @"C:\Dev\repos\MidiLib\out\midi_out.txt" };
        }

        /// <summary>
        /// Initialize form controls.
        /// </summary>
        void MainForm_Load(object? sender, EventArgs e)
        {
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
            sldVolume.Value = Channel.DEFAULT_VOLUME;
            sldTempo.DrawColor = _controlColor;
            sldTempo.Value = _defaultTempo;
            sldPosition.DrawColor = _controlColor;

            // Hook up some simple UI handlers.
            btnPlay.CheckedChanged += (_, __) => { UpdateState(); };
            btnRewind.Click += (_, __) => { Rewind(); };
            btnKillMidi.Click += (_, __) => { _player.KillAll(); };
            btnLogMidi.Click += (_, __) => { _player.LogMidi = btnLogMidi.Checked; };
            sldTempo.ValueChanged += (_, __) => { SetTimer(); };
            sldPosition.ValueChanged += (_, __) => { GetPosition(); };

            // Set up timer.
            sldTempo.Value = _defaultTempo;
            SetTimer();

            // Make sure out path exists.
            DirectoryInfo di = new(_exportPath);
            di.Create();

            UpdateDrumChannels();

            // Look for filename passed in.
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                OpenFile(args[1]);
            }
            else
            {
                OpenFile(@"C:\Dev\repos\MidiStyleExplorer\test\_LoveSong.S474.sty");
                //OpenFile(@"C:\Users\cepth\OneDrive\Audio\Midi\styles\2kPopRock\60'sRock&Roll.S605.sty");
                //OpenFile(@"C:\Dev\repos\ClipExplorer\_files\_drums_ch1.mid");
                //OpenFile(@"C:\Dev\repos\ClipExplorer\_files\25jazz.mid");

                // TODO?? see 2 non-std drum channels in:
                //OpenFile(@"C:\Users\cepth\OneDrive\Audio\Midi\styles\Gary USB\g-70 styles\G-70 #1\ContempBeat_G70.S423.STY");
            }
        }

        /// <summary>
        /// Clean up on shutdown. Dispose() will get the rest.
        /// </summary>
        void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
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
                case RunState.Complete:
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

                case RunState.Playing:
                    if (!btnPlay.Checked)
                    {
                        Stop();
                    }
                    break;

                case RunState.Stopped:
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
            sldPosition.Value = 0;
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
                // Reset drums. Or maybe not?
                txtDrumChannel1.Text = MidiDefs.DEFAULT_DRUM_CHANNEL.ToString();
                txtDrumChannel2.Text = "";
                UpdateDrumChannels();

                // Process the file. Set the default tempo from preferences.
                _mdata = new();
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
            int x = sldTempo.Right + 5;
            int y = sldTempo.Top;

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

                if (chEvents.Any())
                {
                    TheChannels.SetEvents(chnum, chEvents, mt);
                    //PatchInfo patch = pinfo.Patches[i];

                    // Make new controls. Bind to internal channel object.
                    ChannelControl control = new()
                    {
                        Channel = TheChannels.GetChannel(chnum), // TODO2 find a better way to bind?
                        Location = new(x, y),
                        Patch = pinfo.Patches[i],
                        IsDrums = chnum == _drumChannel1 || chnum == _drumChannel2
                    };

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
        int _report = 0;

        /// <summary>
        /// Multimedia timer callback. Synchronously outputs the next midi events.
        /// This is running on the background thread.
        /// </summary>
        void MmTimerCallback(double totalElapsed, double periodElapsed)
        {
            // TODOF This sometimes blows up on shutdown with ObjectDisposedException. I am probably doing bad things with threads.
            try
            {
                if(--_report <= 0)
                {
                    this.InvokeIfRequired(_ => LogMessage($"DBG CurrentSubdiv:{_player.CurrentSubdiv}"));
                    _report = 100;
                }

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
        /// Check for legal entries for channel number.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DrumChannels_Validating(object? sender, CancelEventArgs e)
        {
            // Check for valid value.
            var tb = (ToolStripTextBox)sender!;
            if (tb.Text.Length == 0)
            {
                // Valid condition.
            }
            else
            {
                bool ok = int.TryParse(tb.Text, out int val);
                if (ok)
                {
                    ok = val > 0 && val <= MidiDefs.NUM_CHANNELS;
                }
                if (!ok)
                {
                    e.Cancel = true;
                }
            }
        }

        /// <summary>
        /// Get both selections and update UI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DrumChannels_Validated(object? sender, EventArgs e)
        {
            UpdateDrumChannels();
            _channelControls.ForEach(ctl => ctl.IsDrums = ctl.ChannelNumber == _drumChannel1 || ctl.ChannelNumber == _drumChannel2);
        }

        /// <summary>
        /// Convert UI entries for internal usage.
        /// </summary>
        void UpdateDrumChannels()
        {
            _drumChannel1 = txtDrumChannel1.Text.Length > 0 ? int.Parse(txtDrumChannel1.Text) : 0;
            _drumChannel2 = txtDrumChannel2.Text.Length > 0 ? int.Parse(txtDrumChannel2.Text) : 0;
        }
        #endregion

        #region Position bar
        /// <summary>
        /// Set UI slider value.
        /// </summary>
        void SetPosition()
        {
            int pos = _player.CurrentSubdiv * (int)sldPosition.Maximum / TheChannels.TotalSubdivs;
            sldPosition.Value = pos;
        }

        /// <summary>
        /// Get UI slider value.
        /// </summary>
        void GetPosition()
        {
            int pos = TheChannels.TotalSubdivs * (int)sldPosition.Value / (int)sldPosition.Maximum;
            _player.CurrentSubdiv = pos;
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
                Tempo = sldTempo.Value
            };

            double period = mt.RoundedInternalPeriod();
            _mmTimer.SetTimer((int)Math.Round(period), MmTimerCallback);
        }

        /// <summary>
        /// Export current file to human readable or midi.
        /// </summary>
        void Export_Click(object? sender, EventArgs e)
        {
            _mdata.ExportPath = _exportPath;

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
                    var s = _mdata.ExportAllEvents(channels);
                    LogMessage($"INF Exported to {s}");
                }
                else if (sender == btnExportPattern)
                {
                    if(_mdata.AllPatterns.Count == 1)
                    {
                        var s = _mdata.ExportGroupedEvents("", channels, true);
                        LogMessage($"INF Exported default to {s}");
                    }
                    else
                    {
                        foreach (var patternName in patternNames)
                        {
                            var s = _mdata.ExportGroupedEvents(patternName, channels, true);
                            LogMessage($"INF Exported pattern {patternName} to {s}");
                        }
                    }
                }
                else if (sender == btnExportMidi)
                {
                    if (_mdata.AllPatterns.Count == 1)
                    {
                        // Use original ppq.
                        var s = _mdata.ExportMidi("", channels, _mdata.DeltaTicksPerQuarterNote, false);
                        LogMessage($"INF Export midi to {s}");
                    }
                    else
                    {
                        foreach (var patternName in patternNames)
                        {
                            // Use original ppq.
                            var s = _mdata.ExportMidi(patternName, channels, _mdata.DeltaTicksPerQuarterNote, false);
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
    }
}
