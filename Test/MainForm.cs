
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
        #region Fields
        /// <summary>Midi player.</summary>
        readonly Player _player;

        /// <summary>The fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Midi events from the input file.</summary>
        MidiData _mdata = new();

        /// <summary>All the channel controls.</summary>
        readonly List<ChannelControl> _channelControls = new();

        /// <summary>Cosmetics.</summary>
        readonly Color _controlColor = Color.Aquamarine;

        /// <summary>My midi out.</summary>
        readonly string _midiDevice = "VirtualMIDISynth #1";

        /// <summary>Supported file types. Can be used for open file dialog.</summary>
        readonly string _fileTypes = "Style Files|*.sty;*.pcs;*.sst;*.prs|Midi Files|*.mid";

        /// <summary>Use this if not supplied.</summary>
        readonly int _defaultTempo = 100;

        /// <summary>Only 4/4 time supported.</summary>
        readonly int _beatsPerBar = 4;

        /// <summary>Our internal ppq/resolution - used for sending realtime midi messages.</summary>
        readonly int _ppq = 32;

        /// <summary>Prevent button press recursion.</summary>
        bool _guard = false;

        /// <summary>Current loaded file.</summary>
        string _fn = "";

        /// <summary>Adjust to taste.</summary>
        readonly string _exportPath = @"C:\Dev\repos\MidiLib\out";
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            _player = new(_midiDevice);
        }

        /// <summary>
        /// Initialize form controls.
        /// </summary>
        void MainForm_Load(object? sender, EventArgs e)
        {
            // The text output.
            txtViewer.Font = Font;
            txtViewer.WordWrap = true;
            txtViewer.Colors.Add("ERR", Color.LightPink);
            txtViewer.Colors.Add("WRN", Color.Plum);

            // Toolbar configs.
            btnAutoplay.Checked = false;
            btnLoop.Checked = true;

            // UI configs.
            sldVolume.DrawColor = _controlColor;
            sldVolume.Value = Channel.DEFAULT_VOLUME;
            sldVolume.Resolution = 0.05;
            sldTempo.DrawColor = _controlColor;
            sldTempo.Value = 100;
            sldTempo.Resolution = 5;

            // Time controller.
            barBar.ZeroBased = true;
            barBar.BeatsPerBar = _beatsPerBar;
            barBar.SubdivsPerBeat = _ppq;
            barBar.Snap = BarBar.SnapType.Beat;
            barBar.ProgressColor = _controlColor;

            // Hook up some simple UI handlers.
            btnPlay.CheckedChanged += (_, __) => { UpdateState(); };
            btnRewind.Click += (_, __) => { Rewind(); };
            btnKillMidi.Click += (_, __) => { _player.KillAll(); };
            sldTempo.ValueChanged += (_, __) => { SetTimer(); };
            lbPatterns.SelectedIndexChanged += (_, __) => { _player.CurrentSubdiv = barBar.Current.Subdiv; };
            barBar.CurrentTimeChanged += (_, __) => { _player.CurrentSubdiv = barBar.Current.Subdiv; };

            // Set up timer.
            sldTempo.Value = _defaultTempo;
            SetTimer();

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
            }

            LogMessage("INF Hello. C to clear text, W to toggle word wrap");
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
            barBar.Current = new(_player.CurrentSubdiv);

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
                            if (i != chc.ChannelNumber && chc.State != ChannelState.Solo)
                            {
                                _player.Kill(i);
                            }
                        }
                        break;

                    case ChannelState.Mute:
                        _player.Kill(chc.ChannelNumber);
                        break;
                }
            }

            if (e.PatchChange && chc.Patch.Modifier == PatchInfo.PatchModifier.None)
            {
                _player.SetPatch(chc.ChannelNumber, chc.Patch);
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
            barBar.Current = BarSpan.Zero;
            _player.CurrentSubdiv = 0;
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

            // Clean out our collection.
            _channelControls.ForEach(c => Controls.Remove(c));
            _channelControls.Clear();

            // Create the new controls.
            int lastSubdiv = 0;
            int x = sldTempo.Right + 5;
            int y = sldTempo.Top;

            // For scaling subdivs to internal.
            MidiTime mt = new()
            {
                InternalPpq = _ppq,
                MidiPpq = _mdata.DeltaTicksPerQuarterNote,
                Tempo = _defaultTempo
            };

            for (int chind = 0; chind < MidiDefs.NUM_CHANNELS; chind++)
            {
                int chnum = chind + 1;

                var chEvents = _mdata.AllEvents.
                    Where(e => e.PatternName == pinfo.PatternName && e.ChannelNumber == chnum && (e.MidiEvent is NoteEvent || e.MidiEvent is NoteOnEvent)).
                    OrderBy(e => e.AbsoluteTime);

                if (chEvents.Any())
                {
                    _player.SetEvents(chind, chEvents, mt);
                    PatchInfo patch = pinfo.Patches[chind];

                    // Make new controls. Bind to internal channel object.
                    ChannelControl control = new()
                    {
                        Channel = _player.GetChannel(chind),
                        Location = new(x, y),
                        Patch = patch,
                        //// default state
                        //State = ChannelState.Normal,
                        //Volume = Channel.DEFAULT_VOLUME,
                        //Selected = false
                    };

                    control.ChannelChange += Control_ChannelChange;
                    Controls.Add(control);
                    _channelControls.Add(control);

                    lastSubdiv = Math.Max(lastSubdiv, control.MaxSubdiv);

                    // Adjust positioning.
                    y += control.Height + 5;

                    // Send patch maybe. These can change per pattern.
                    _player.SetPatch(chnum, patch);
                }
            }

            //// Figure out times. Round up to bar.
            //int floor = lastSubdiv / (_ppq * 4); // 4/4 only.
            //lastSubdiv = (floor + 1) * _ppq * 4;

            // Figure out times.
            barBar.Length = new BarSpan(lastSubdiv);
            barBar.Start = BarSpan.Zero;
            barBar.End = barBar.Length - BarSpan.OneSubdiv;
            barBar.Current = BarSpan.Zero;
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
            // TODO This sometimes blows up on shutdown with ObjectDisposedException. I am probably doing bad things with threads.
            try
            {
                _player.DoNextStep();
                // Bump over to main thread.
                this.InvokeIfRequired(_ => UpdateState());
            }
            catch
            {
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
                InternalPpq = _ppq,
                MidiPpq = _mdata.DeltaTicksPerQuarterNote,
                Tempo = sldTempo.Value
            };

            double period = mt.RoundedInternalPeriod();
            _mmTimer.SetTimer((int)Math.Round(period), MmTimerCallback);
        }

        /// <summary>
        /// Dump current file to human readable aand/or midi
        /// </summary>
        void Dump_Click(object? sender, EventArgs e)
        {
            _mdata.ExportPath = _exportPath;

            try
            {
                // Collect filters.
                List<string> patterns = new();
                foreach(var p in lbPatterns.CheckedItems)
                {
                    patterns.Add(p.ToString()!);
                }

                List<int> channels = new();
                foreach (var cc in _channelControls.Where(c => c.Selected))
                {
                    channels.Add(cc.ChannelNumber);
                }

                if (sender == btnDumpSeq)
                {
                    var s = _mdata.DumpSequentialEvents(patterns, channels);
                    LogMessage($"INF Dumped to {s}");
                }
                else if (sender == btnDumpSeq)
                {
                    var s = _mdata.DumpGroupedEvents(patterns, channels);
                    LogMessage($"INF Dumped to {s}");
                }
                else if (sender == btnExport)
                {
                    // Use original ppq.
                    var s = _mdata.ExportMidi(patterns, channels, _mdata.DeltaTicksPerQuarterNote, false);
                    LogMessage($"INF Export midi to {s}");
                }
                else
                {
                    LogMessage($"ERR Ooops");
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
