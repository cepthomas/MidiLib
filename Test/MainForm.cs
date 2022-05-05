
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

        ///// <summary>Supported file types.</summary>
        //readonly string _fileTypes = "Style Files|*.sty;*.pcs;*.sst;*.prs|Midi Files|*.mid";

        /// <summary>Use this if not supplied.</summary>
        int _defaultTempo = 100;

        /// <summary>Only 4/4 time supported.</summary>
        int _beatsPerBar = 4;

        /// <summary>Our internal ppq aka resolution - used for sending realtime midi messages.</summary>
        int _ppq = 32;

        /// <summary>Prevent button press recursion.</summary>
        bool _guard = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            _player = new(_midiDevice);
            _player.StatusEvent += Player_StatusEvent;
        }

        /// <summary>
        /// Initialize form controls.
        /// </summary>
        void MainForm_Load(object? sender, EventArgs e)
        {
            //KeyPreview = true; // for routing kbd strokes through MainForm_KeyDown

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
            sldVolume.Value = 0.8;
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
                // @"C:\Users\cepth\OneDrive\Audio\Midi\styles\2kPopRock\60'sRock&Roll.S605.sty";
                // also in C:\Dev\repos\ClipExplorer\_files
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
        /// Player has something to say.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Player_StatusEvent(object? sender, StatusEventArgs e)
        {
            this.InvokeIfRequired(_ => { LogMessage(e.Message); });
        }

        /// <summary>
        /// General state management. Triggered by play button or the player via mm timer function.
        /// </summary>
        void UpdateState()
        {
            // Suppress recursive updates caused by manually changing the play button.
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
                _player.Run(true);
            }
            else
            {
                Rewind();
            }
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
                if(_mdata.Patterns.Count == 0)
                {
                    LogMessage($"ERR Something wrong with this file: {fn}");
                    ok = false;
                }
                else if(_mdata.Patterns.Count == 1) // plain midi
                {
                    var pinfo = _mdata.Patterns[0];
                    LoadPattern(pinfo);
                }
                else // style - multiple patterns.
                {
                    foreach (var p in _mdata.Patterns)
                    {
                        switch (p.Name)
                        {
                            // These don't contain a pattern
                            case "SFF1": // initial patches are in here
                            case "SFF2":
                            case "SInt":
                                break;

                            case "":
                                LogMessage("ERR Well, this should never happen!");
                                break;

                            default:
                                lbPatterns.Items.Add(p.Name);
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
            }
            catch (Exception ex)
            {
                LogMessage($"ERR Couldn't open the file: {fn} because: {ex.Message}");
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

            // First save current state to restore after loading.
            Dictionary<int, (ChannelState, double, bool, bool)> states = new();
            foreach (var control in _channelControls)
            {
                states.Add(control.ChannelNumber,
                    (control.State, control.Volume, control.Selected, control.Patch.Modifier == PatchInfo.PatchModifier.IsDrums));
                Controls.Remove(control);
            }

            // Clean out our collection.
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
                    Where(e => e.Pattern == pinfo.Name && e.ChannelNumber == chnum && (e.MidiEvent is NoteEvent || e.MidiEvent is NoteOnEvent)).
                    OrderBy(e => e.AbsoluteTime);

                if (chEvents.Any())
                {
                    _player.SetEvents(chind, chEvents, mt);

                    // Make new controls. Bind to internal channel object.
                    ChannelControl control = new()
                    {
                        Channel = _player.GetChannel(chind),
                        Location = new(x, y),
                    };

                    // Restore previous attributes if they match the new control.
                    if (states.ContainsKey(chnum))
                    {
                        control.State = states[chnum].Item1;
                        control.Volume = states[chnum].Item2;
                        control.Selected = states[chnum].Item3;
                        control.Patch.Modifier = states[chnum].Item4 ? PatchInfo.PatchModifier.IsDrums : PatchInfo.PatchModifier.None;
                    }

                    control.ChannelChange += Control_ChannelChange;
                    Controls.Add(control);

                    lastSubdiv = Math.Max(lastSubdiv, control.MaxSubdiv);

                    _channelControls.Add(control);

                    // Adjust positioning.
                    y += control.Height + 5;

                    // Send patch maybe. These can change per pattern.
                    _player.SetPatch(chnum, pinfo.Patches[chind]);
                }
            }

            // Figure out times. Round up to bar.
            int floor = lastSubdiv / (_ppq * 4); // 4/4 only.
            lastSubdiv = (floor + 1) * _ppq * 4;

            barBar.Length = new BarSpan(lastSubdiv);
            barBar.Start = BarSpan.Zero;
            barBar.End = barBar.Length - BarSpan.OneSubdiv;
            barBar.Current = BarSpan.Zero;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Patterns_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var pinfo = _mdata.Patterns.Where(p => p.Name == lbPatterns.SelectedItem.ToString()).First();

            LoadPattern(pinfo!);

            Rewind();

            if (btnAutoplay.Checked)
            {
                btnPlay.Checked = true; // ==> Start()
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
                // TODO2 Sometimes blows up on shutdown - ObjectDisposedException. I am probably doing bad things with threads.
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
        void Dump_Click(object sender, EventArgs e)
        {
            //var ds = _mdata.GetSequentialEvents();
            var ds = _mdata.GetGroupedEvents();

            if (ds.Count == 0)
            {
                ds.Add("No data");
            }

            // To clipboard.
            Clipboard.SetText(string.Join(Environment.NewLine, ds));
            LogMessage("INF Data dumped to clipboard");

            // or to file.
            // using SaveFileDialog dumpDlg = new() { Title = "Dump to file", FileName = "dump.csv" };
            // if (dumpDlg.ShowDialog() == DialogResult.OK)
            // {
            //     File.WriteAllLines(dumpDlg.FileName, ds.ToArray());
            //     LogMessage("INF Data dumped to file");
            // }
        }

        /// <summary>
        /// Export parts to midi.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Export_Click(object sender, EventArgs e)
        {
            // public void ExportMidi(List<PatternInfo> patterns, string name, string exportPath, int ppq)  TODO1
            // export only selected channels or all if none selected.
            // use original ppq, ptob.

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
