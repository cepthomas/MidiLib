#define _NEW_ML

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using System.Drawing.Drawing2D;
using Ephemera.NBagOfTricks;


// TODO zoom, drag, shift


namespace Ephemera.MidiLib
{
    /// <summary>The control.</summary>
    [DesignTimeVisible(true)]
    [Browsable(false)]
    public class TimeBar : UserControl
    {
        #region Fields
        /// <summary>For tracking mouse moves.</summary>
        int _lastXPos = 0;

        /// <summary>Metadata.</summary>
        readonly List<(int tick, string name)> _sectionInfo = [];

        /// <summary>Tooltip for mousing.</summary>
        readonly ToolTip _toolTip = new();

        /// <summary>For drawing text.</summary>
        readonly StringFormat _format = new();

        /// <summary>For drawing lines etc.</summary>
        readonly Pen _penMarker = new(Color.Red, 1);

        /// <summary>For drawing lines etc.</summary>
        readonly Pen _penSel = new(Color.Blue, 1);

        /// <summary>Total length.</summary>
        readonly MusicTime _length = new();

        ///// <summary>Start of selected region.</summary>
        readonly MusicTime _selStart = new();

        ///// <summary>End of selected region.</summary>
        readonly MusicTime _selEnd = new();

        /// <summary>Where we be now.</summary>
        readonly MusicTime _current = new();

        /// <summary>Convenience.</summary>
        readonly MusicTime ZERO = new();

        /// <summary>Avoid creating many transient objects.</summary>
        readonly MusicTime TEMP = new();
        #endregion

        #region Properties
        /// <summary>Big font.</summary>
        public Font FontLarge { get { return _fontLarge; } set { _fontLarge = value; Invalidate(); } }
        Font _fontLarge = new("Microsoft Sans Serif", 16, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>Baby font.</summary>
        public Font FontSmall { get; set; } = new("Microsoft Sans Serif", 10, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>Drawing the active elements of a control.</summary>
        public Color ControlColor { get { return _controlColor; } set { _controlColor = value; Invalidate(); } }
        Color _controlColor = Color.Red;

        /// <summary>Drawing the control when selected.</summary>
        public Color SelectedColor { get { return _selectedColor; } set { _selectedColor = value; Invalidate(); } }
        Color _selectedColor = Color.Blue;

        /// <summary>How to select times.</summary>
        public SnapType Snap { get; set; } = SnapType.Beat;

        /// <summary>Keep going at end.</summary>
        public bool DoLoop { get { return _doLoop; } set { _doLoop = value; Invalidate(); } }
        bool _doLoop = false;

        /// <summary>Convenience for readability.</summary>
        public bool Valid { get { return _length.Tick > 0; } }

        /// <summary>Convenience for readability.</summary>
        public MusicTime Length { get { return _length; } }

        /// <summary>Convenience for readability.</summary>
        public MusicTime Current { get { return _current; } }
        #endregion

        #region Events
        /// <summary>Something happened.</summary>
        public event EventHandler<StateChangeEventArgs>? StateChange;
        public class StateChangeEventArgs : EventArgs
        {
            public bool CurrentTimeChange { get; set; } = false;
        }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public TimeBar()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _toolTip.Dispose();
                _penMarker.Dispose();
                _penSel.Dispose();
                _format.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Something to say.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var s = $"C:{_current.Tick} L:{_length.Tick} {_selStart.Tick}=>{_selEnd.Tick}";
            return s;
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Client supplies metadata about sections in the time range.
        /// </summary>
        /// <param name="sectInfo"></param>
        public void InitSectionInfo(Dictionary<int, string> sectInfo)
        {
            _sectionInfo.Clear();
            _length.Set(0);
            _selStart.Set(0);
            _selEnd.Set(0);
            _current.Set(0);

            if (sectInfo.Count > 0)
            {
                List<(int tick, string name)> sinfo = [];
                var spos = sectInfo.Keys.OrderBy(k => k).ToList();
                spos.ForEach(sp => _sectionInfo.Add((sp, sectInfo[sp])));
                _length.Set(_sectionInfo.Last().tick);
                _selEnd.Set(_length.Tick);

                ValidateTimes();
                Invalidate();
            }
        }

        /// <summary>
        ///
        /// </summary>
        public void ResetSelection()
        {
            _selStart.Set(0);
            _selEnd.Set(_length.Tick);

            ValidateTimes();
            Invalidate();
        }

        /// <summary>
        /// Back up dude.
        /// </summary>
        public void Rewind()
        {
            _current.Set(_doLoop ? _selStart.Tick : 0);

            ValidateTimes();
            Invalidate();
        }

        /// <summary>
        /// Step current time.
        /// </summary>
        /// <returns>True if still running.</returns>
        public bool Increment()
        {
            bool running = Valid;

            if (running)
            {
                if (_current >= _selEnd) // at end
                {
                    if (_doLoop) // continue from start
                    {
                        _current.Set(_selStart.Tick);
                    }
                    else // stop
                    {
                        running = false;
                    }
                }
                else // continue
                {
                    _current.Update(1);
                }
            }

            ValidateTimes();
            Invalidate();

            return running;
        }
        #endregion

        #region Drawing
        /// <summary>
        /// Draw the control.
        /// </summary>
        protected override void OnPaint(PaintEventArgs pe)
        {
            // Setup.
            pe.Graphics.Clear(BackColor);

            if (!Valid)
            {
                _format.Alignment = StringAlignment.Center;
                _format.LineAlignment = StringAlignment.Center;
                pe.Graphics.DrawString("Invalid", FontLarge, Brushes.Black, ClientRectangle, _format);
                return;
            }

            _penMarker.Color = ControlColor;
            _penSel.Color = SelectedColor;

            ///// Loop area.
            // box:
            int lstart = GetClientFromTick(_selStart.Tick);
            int lend = GetClientFromTick(_selEnd.Tick);
            PointF[] ploc = [new(lstart, 0), new(lend, 0), new(lend, Height), new(lstart, Height)];
            pe.Graphics.FillPolygon(_penSel.Brush, ploc);

            ///// Sections.
            var fsize = pe.Graphics.MeasureString("X", FontSmall).Height;
            foreach (var (tick, name) in _sectionInfo)
            {
                int sect = GetClientFromTick(tick);
                pe.Graphics.DrawLine(_penMarker, sect, 0, sect, Height);
                _format.Alignment = StringAlignment.Center;
                _format.LineAlignment = StringAlignment.Center;
                pe.Graphics.DrawString(name, FontSmall, Brushes.Black, sect + 2, Height - fsize - 2);
            }

            ///// Some vertical lines.
            var incr = MusicTime.TicksPerBeat; // TODO or bar if dense.
            _penMarker.DashStyle = DashStyle.Custom;
            _penMarker.DashPattern = [5, 5]; 
            for (int i = 0; i < _length.Tick; i += incr)
            {
                int x = GetClientFromTick(i);
                pe.Graphics.DrawLine(_penMarker, x, 0, x, Height);
            }
            _penMarker.DashStyle = DashStyle.Solid;

            ///// Current pos.
            int markSize = 7;
            int cpos = GetClientFromTick(_current.Tick);
            pe.Graphics.DrawLine(_penMarker, cpos, 0, cpos, Height);
            ploc = [new(cpos - markSize, 0), new(cpos + markSize, 0), new(cpos, 2 * markSize)];
            pe.Graphics.FillPolygon(_penMarker.Brush, ploc);

            ///// Text.
            _format.Alignment = StringAlignment.Center;
            _format.LineAlignment = StringAlignment.Near; // Center;
            pe.Graphics.DrawString(_current.ToString(), FontLarge, Brushes.Black, ClientRectangle, _format);

            _format.Alignment = StringAlignment.Near;
            _format.LineAlignment = StringAlignment.Near;
            pe.Graphics.DrawString(_selStart.ToString(), FontSmall, Brushes.Black, ClientRectangle, _format);

            _format.Alignment = StringAlignment.Far;
            _format.LineAlignment = StringAlignment.Near;
            pe.Graphics.DrawString(_selEnd.ToString(), FontSmall, Brushes.Black, ClientRectangle, _format);
        }
        #endregion

        #region UI handlers
        /// <summary>
        /// Mouse position changes.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            // SetToolTip triggers mouse move event - infernal loop.
            if (Valid && e.X != _lastXPos)
            {
                _lastXPos = e.X;

                // Assemble info.
                var actualTick = GetTickFromClient(e.X);
                var roundedTick = GetRounded(actualTick);

                string sectionName = "???";
                _sectionInfo.TakeWhile(si => si.tick <= roundedTick).ForEach(si => sectionName = si.name);

                TEMP.Set(actualTick);
                _toolTip.SetToolTip(this, $"{TEMP} {sectionName}");

                // var debugInfo1 = $"ID:{TEMP.Id} T:{actualTick}";
                // var debugInfo2 = $"L:{_length.Tick} {_selStart.Tick}=>{_selEnd.Tick}";
                // _toolTip.SetToolTip(this, $"{TEMP} {sectionName}{Environment.NewLine}{debugInfo1}{Environment.NewLine}{debugInfo2}");
            }

            base.OnMouseMove(e);
        }

        /// <summary>
        /// Selection of time and loop points.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (Valid)
            {
                int newval = GetTickFromClient(e.X);

                if (ModifierKeys.HasFlag(Keys.Control))
                {
                    _selStart.Set(newval, Snap);
                }
                else if (ModifierKeys.HasFlag(Keys.Alt))
                {
                    _selEnd.Set(newval, Snap);
                }
                else
                {
                    _current.Set(newval, Snap);

                    StateChange?.Invoke(this, new() { CurrentTimeChange = true });
                }

                ValidateTimes();
                Invalidate();
            }

            base.OnMouseDown(e);
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Validate and correct all important times.
        /// </summary>
        void ValidateTimes()
        {
            // Maybe fix loop points.
            _selEnd.Constrain(ZERO, _length);
            _selStart.Constrain(ZERO, _selEnd);
            _current.Constrain(_selStart, _selEnd);
        }

        /// <summary>
        /// Convert x pos to tick.
        /// </summary>
        /// <param name="x"></param>
        int GetTickFromClient(int x)
        {
            int tick = 0;

            if (_current < _length)
            {
                tick = x * _length.Tick / Width;
                tick = MathUtils.Constrain(tick, 0, _length.Tick);
            }

            return tick;
        }

        /// <summary>
        /// Map from time to UI pixels.
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        int GetClientFromTick(int tick)
        {
            return _length.Tick > 0 ? tick * Width / _length.Tick : 0;
        }

        /// <summary>
        /// Set to sub using specified rounding.
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="up">To ceiling otherwise closest.</param>
        int GetRounded(int tick, bool up = false)
        {
            if (tick > 0 && Snap != SnapType.Tick)
            {
                int res = Snap == SnapType.Bar ? MusicTime.TicksPerBar : MusicTime.TicksPerBeat;

                double dtick = Math.Floor((double)tick);
                int floor = (int)(dtick / res) * res;
                int ceiling = floor + res;

                tick = (up || (ceiling - tick) < res / 2) ? ceiling : floor;
            }

            return tick;
        }
        #endregion
    }
}
