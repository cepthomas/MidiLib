using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    /// <summary>The control.</summary>
    public class BarBar : UserControl
    {
        #region Fields
        /// <summary>For tracking mouse moves.</summary>
        int _lastXPos = 0;

        /// <summary>Tooltip for mousing.</summary>
        readonly ToolTip _toolTip = new();

        /// <summary>For drawing text.</summary>
        readonly StringFormat _format = new() { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };
        #endregion

        #region Backing fields
        readonly SolidBrush _brush = new(Color.White);
        readonly Pen _penMarker = new(Color.Black, 1);
        BarTime _length = new();
        BarTime _start = new();
        BarTime _end = new();
        BarTime _current = new();
        #endregion

        #region Properties
        /// <summary>Total length of the bar.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public BarTime Length { get { return _length; } set { _length = value; Invalidate(); } }

        /// <summary>Start of marked region.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public BarTime Start { get { return _start; } set { _start = value; Invalidate(); } }

        /// <summary>End of marked region.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public BarTime End { get { return _end; } set { _end = value; Invalidate(); } }

        /// <summary>Where we be now.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public BarTime Current { get { return _current; } set { _current = value; Invalidate(); } }

        /// <summary>For styling.</summary>
        public Color ProgressColor { get { return _brush.Color; } set { _brush.Color = value; } }

        /// <summary>For styling.</summary>
        public Color MarkerColor { get { return _penMarker.Color; } set { _penMarker.Color = value; } }

        /// <summary>Big font.</summary>
        public Font FontLarge { get; set; } = new("Microsoft Sans Serif", 20, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>Baby font.</summary>
        public Font FontSmall { get; set; } = new("Microsoft Sans Serif", 10, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>All the important beat points with their names. Used also by tooltip.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public Dictionary<int, string> TimeDefs { get; set; } = new Dictionary<int, string>();
        #endregion

        #region Events
        /// <summary>Value changed by user.</summary>
        public event EventHandler? CurrentTimeChanged;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        public BarBar()
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
                _brush.Dispose();
                _penMarker.Dispose();
                _format.Dispose();
            }
            base.Dispose(disposing);
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

            // Validate times.
            BarTime zero = new();
            _start.Constrain(zero, _length);
            _start.Constrain(zero, _end);
            _end.Constrain(zero, _length);
            _end.Constrain(_start, _length);
            _current.Constrain(_start, _end);

            // Draw the bar.
            if (_current < _length)
            {
                int dstart = Scale(_start);
                int dend = _current > _end ? Scale(_end) : Scale(_current);
                pe.Graphics.FillRectangle(_brush, dstart, 0, dend - dstart, Height);
            }

            // Draw start/end markers.
            if (_start != zero || _end != _length)
            {
                int mstart = Scale(_start);
                int mend = Scale(_end);
                pe.Graphics.DrawLine(_penMarker, mstart, 0, mstart, Height);
                pe.Graphics.DrawLine(_penMarker, mend, 0, mend, Height);
            }

            // Text.
            if(DesignMode) // Can't access LibSettings yet.
            {
                _format.Alignment = StringAlignment.Center;
                pe.Graphics.DrawString("CENTER", FontLarge, Brushes.Black, ClientRectangle, _format);
                _format.Alignment = StringAlignment.Near;
                pe.Graphics.DrawString("NEAR", FontSmall, Brushes.Black, ClientRectangle, _format);
                _format.Alignment = StringAlignment.Far;
                pe.Graphics.DrawString("FAR", FontSmall, Brushes.Black, ClientRectangle, _format);
            }
            else
            {
                _format.Alignment = StringAlignment.Center;
                pe.Graphics.DrawString(_current.Format(), FontLarge, Brushes.Black, ClientRectangle, _format);
                _format.Alignment = StringAlignment.Near;
                pe.Graphics.DrawString(_start.Format(), FontSmall, Brushes.Black, ClientRectangle, _format);
                _format.Alignment = StringAlignment.Far;
                pe.Graphics.DrawString(_end.Format(), FontSmall, Brushes.Black, ClientRectangle, _format);
            }
        }
        #endregion

        #region UI handlers
        /// <summary>
        /// Handle selection operations.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if(e.KeyData == Keys.Escape)
            {
                // Reset.
                _start.Reset();
                _end.Reset();
                Invalidate();
            }
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Handle mouse position changes.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _current.SetRounded(GetSubFromMouse(e.X), MidiSettings.LibSettings.Snap);
                CurrentTimeChanged?.Invoke(this, new EventArgs());
            }
            else if (e.X != _lastXPos)
            {
                BarTime bs = new();
                var vv = MidiSettings.LibSettings;

                var sub = GetSubFromMouse(e.X);
                bs.SetRounded(sub, MidiSettings.LibSettings.Snap);
                string sdef = GetTimeDefString(bs.TotalBeats);
                _toolTip.SetToolTip(this, $"{bs.Format()} {sdef}");
                _lastXPos = e.X;
            }

            Invalidate();
            base.OnMouseMove(e);
        }

        /// <summary>
        /// Handle dragging.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                _start.SetRounded(GetSubFromMouse(e.X), MidiSettings.LibSettings.Snap);
            }
            else if (ModifierKeys.HasFlag(Keys.Alt))
            {
                _end.SetRounded(GetSubFromMouse(e.X), MidiSettings.LibSettings.Snap);
            }
            else
            {
                _current.SetRounded(GetSubFromMouse(e.X), MidiSettings.LibSettings.Snap);
            }

            CurrentTimeChanged?.Invoke(this, new EventArgs());
            Invalidate();
            base.OnMouseDown(e);
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Change current time. 
        /// </summary>
        /// <param name="num">Subs/ticks.</param>
        /// <returns>True if at the end of the sequence.</returns>
        public bool IncrementCurrent(int num)
        {
            bool done = false;

            _current.Increment(num);

            if (_current < new BarTime(0))
            {
                _current.Reset();
            }
            else if (_current < _start)
            {
                _current.SetRounded(_start.TotalSubs, SnapType.Sub);
                done = true;
            }
            else if (_current > _end)
            {
                _current.SetRounded(_end.TotalSubs, SnapType.Sub);
                done = true;
            }

            Invalidate();

            return done;
        }

        /// <summary>
        /// Clear everything.
        /// </summary>
        public void Reset()
        {
            _lastXPos = 0;
            _length.Reset();
            _current.Reset();
            _start.Reset();
            _end.Reset();

            Invalidate();
        }
        /// <summary>
        /// Gets the time def string associated with val.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private string GetTimeDefString(int val)
        {
            string s = "";

            foreach (KeyValuePair<int, string> kv in TimeDefs)
            {
                if (kv.Key > val)
                {
                    break;
                }
                else
                {
                    s = kv.Value;
                }
            }

            return s;
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Convert x pos to sub.
        /// </summary>
        /// <param name="x"></param>
        int GetSubFromMouse(int x)
        {
            int sub = 0;

            if(_current < _length)
            {
                sub = x * _length.TotalSubs / Width;
                sub = MathUtils.Constrain(sub, 0, _length.TotalSubs);
            }

            return sub;
        }

        /// <summary>
        /// Map from time to UI pixels.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public int Scale(BarTime val)
        {
            return val.TotalSubs * Width / _length.TotalSubs;
        }
        #endregion
    }
}
