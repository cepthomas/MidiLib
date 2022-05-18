using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using NBagOfTricks;


namespace MidiLib
{
    /// <summary>The control.</summary>
    public partial class BarBar : UserControl
    {
        #region Enums
        /// <summary>User selection options.</summary>
        public enum SnapType { Subdiv, Beat, Bar }
        #endregion

        #region Fields
        /// <summary>Total length.</summary>
        BarSpan _length;

        /// <summary>First valid point.</summary>
        BarSpan _start;

        /// <summary>Last valid point.</summary>
        BarSpan _end;

        /// <summary>Current time.</summary>
        BarSpan _current;

        /// <summary>For tracking mouse moves.</summary>
        int _lastXPos = 0;

        /// <summary>Tooltip for mousing.</summary>
        readonly ToolTip _toolTip = new();

        /// <summary>The brush.</summary>
        readonly SolidBrush _brush = new(Color.White);

        /// <summary>The pen.</summary>
        readonly Pen _penMarker = new(Color.Black, 1);

        /// <summary>For drawing text.</summary>
        readonly StringFormat _format = new() { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };
        #endregion

        #region Properties
        /// <summary>Option for engineers instead of musicians.</summary>
        public bool ZeroBased { get; set; } = false;

        /// <summary>Our signature. Only tested with 4.</summary>
        public int BeatsPerBar { get { return BarSpan._beatsPerBar; } set { BarSpan._beatsPerBar = value; } }

        /// <summary>Our resolution.</summary>
        public int SubdivsPerBeat { get { return BarSpan._subdivsPerBeat; } set { BarSpan._subdivsPerBeat = value; } }

        /// <summary>How to snap.</summary>
        public SnapType Snap { get { return BarSpan._snapType; } set { BarSpan._snapType = value; } }

        /// <summary>Total length.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public BarSpan Length { get { return _length; } set { _length = value; Invalidate(); } }

        /// <summary>First valid point.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public BarSpan Start { get { return _start; } set { _start = value; Invalidate(); } }

        /// <summary>Last valid point.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public BarSpan End { get { return _end; } set { _end = value; Invalidate(); } }

        /// <summary>Where we be now.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public BarSpan Current { get { return _current; } set { _current = value; Invalidate(); } }

        /// <summary>For styling.</summary>
        public Color ProgressColor { get { return _brush.Color; } set { _brush.Color = value; } }

        /// <summary>For styling.</summary>
        public Color MarkerColor { get { return _penMarker.Color; } set { _penMarker.Color = value; } }

        /// <summary>Big font.</summary>
        public Font FontLarge { get; set; } = new("Microsoft Sans Serif", 20, FontStyle.Regular, GraphicsUnit.Point, 0);

        /// <summary>Baby font.</summary>
        public Font FontSmall { get; set; } = new("Microsoft Sans Serif", 10, FontStyle.Regular, GraphicsUnit.Point, 0);
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
            _start.Constrain(BarSpan.Zero, _length);
            _start.Constrain(BarSpan.Zero, _end);
            _end.Constrain(BarSpan.Zero, _length);
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
            if (_start != BarSpan.Zero || _end != _length)
            {
                int mstart = Scale(_start);
                int mend = Scale(_end);
                pe.Graphics.DrawLine(_penMarker, mstart, 0, mstart, Height);
                pe.Graphics.DrawLine(_penMarker, mend, 0, mend, Height);
            }

            // Text.
            _format.Alignment = StringAlignment.Center;
            pe.Graphics.DrawString(_current.Format(ZeroBased), FontLarge, Brushes.Black, ClientRectangle, _format);
            _format.Alignment = StringAlignment.Near;
            pe.Graphics.DrawString(_start.Format(ZeroBased), FontSmall, Brushes.Black, ClientRectangle, _format);
            _format.Alignment = StringAlignment.Far;
            pe.Graphics.DrawString(_end.Format(ZeroBased), FontSmall, Brushes.Black, ClientRectangle, _format);
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
        }

        /// <summary>
        /// Handle mouse position changes.
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _current.DoSnap(GetSubdivFromMouse(e.X));
                CurrentTimeChanged?.Invoke(this, new EventArgs());
            }
            else if (e.X != _lastXPos)
            {
                BarSpan bs = new();
                bs.DoSnap(GetSubdivFromMouse(e.X));
                _toolTip.SetToolTip(this, bs.Format(ZeroBased));
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
                _start.DoSnap(GetSubdivFromMouse(e.X));
            }
            else if (ModifierKeys.HasFlag(Keys.Alt))
            {
                _end.DoSnap(GetSubdivFromMouse(e.X));
            }
            else
            {
                _current.DoSnap(GetSubdivFromMouse(e.X));
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
        /// <param name="num">Subdivs/ticks.</param>
        public bool IncrementCurrent(int num)
        {
            bool done = false;

            _current.Increment(num);

            if(_current < BarSpan.Zero)
            {
                _current = BarSpan.Zero;
            }
            else if(_current >= _length)
            {
                _current = _length - BarSpan.OneSubdiv;
                done = true;
            }
            else if(_current >= _end)
            {
                _current = _end - BarSpan.OneSubdiv;
                done = true;
            }

            Invalidate();

            return done;
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Convert x pos to subdiv.
        /// </summary>
        /// <param name="x"></param>
        int GetSubdivFromMouse(int x)
        {
            int subdiv = 0;

            if(_current < _length)
            {
                subdiv = x * _length.TotalSubdivs / Width;
                subdiv = MathUtils.Constrain(subdiv, 0, _length.TotalSubdivs);
            }

            return subdiv;
        }

        /// <summary>
        /// Map from time to UI pixels.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public int Scale(BarSpan val)
        {
            return val.TotalSubdivs * Width / _length.TotalSubdivs;
        }
        #endregion
    }

    /// <summary>Sort of like TimeSpan.</summary>
    public struct BarSpan
    {
        #region Fields
        /// <summary>A useful constant.</summary>
        public static readonly BarSpan Zero = new BarSpan();

        /// <summary>A useful constant.</summary>
        public static readonly BarSpan OneSubdiv = new BarSpan() { TotalSubdivs = 1 };

        /// <summary>For hashing.</summary>
        readonly int _id;

        /// <summary>Increment for unique value.</summary>
        static int _all_ids = 1;

        /// <summary>Global - set before using. Only tested with 4.</summary>
        internal static int _beatsPerBar = 4;

        /// <summary>Global - set before using. Our resolution.</summary>
        internal static int _subdivsPerBeat = 8;

        /// <summary>Global - set before using.</summary>
        internal static BarBar.SnapType _snapType = BarBar.SnapType.Subdiv;
        #endregion

        #region Properties
        /// <summary>The core.</summary>
        public int TotalSubdivs { get; private set; }

        /// <summary>The bar.</summary>
        public int Bar { get { return TotalSubdivs / _beatsPerBar / _subdivsPerBeat; } }

        /// <summary>The beat.</summary>
        public int Beat { get { return TotalSubdivs / _subdivsPerBeat % _beatsPerBar; } }

        /// <summary>The subdiv.</summary>
        public int Subdiv { get { return TotalSubdivs % _subdivsPerBeat; } }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor from args.
        /// </summary>
        /// <param name="bar"></param>
        /// <param name="beat"></param>
        /// <param name="subdiv"></param>
        public BarSpan(int bar, int beat, int subdiv)
        {
            TotalSubdivs = (bar * _beatsPerBar * _subdivsPerBeat) + (beat * _subdivsPerBeat) + subdiv;
            _id = _all_ids++;
        }

        /// <summary>
        /// Constructor from args.
        /// </summary>
        /// <param name="subdivs"></param>
        public BarSpan(int subdivs)
        {
            TotalSubdivs = subdivs;
            _id = _all_ids++;
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Hard reset.
        /// </summary>
        public void Reset()
        {
            TotalSubdivs = 0;
        }

        /// <summary>
        /// Utility helper function.
        /// </summary>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        public void Constrain(BarSpan lower, BarSpan upper)
        {
            TotalSubdivs = MathUtils.Constrain(TotalSubdivs, lower.TotalSubdivs, upper.TotalSubdivs);
        }

        /// <summary>
        /// Update current value.
        /// </summary>
        /// <param name="subdivs">Number of subdivs.</param>
        public void Increment(int subdivs)
        {
            TotalSubdivs += subdivs;
            if (TotalSubdivs < 0)
            {
                TotalSubdivs = 0;
            }
        }

        /// <summary>
        /// Snap to closest boundary.
        /// </summary>
        /// <param name="subdiv"></param>
        public void DoSnap(int subdiv)
        {
            BarSpan bspan = new BarSpan { TotalSubdivs = subdiv };
            int newbar = bspan.Bar;
            int newbeat = bspan.Beat;

            switch (_snapType)
            {
                case BarBar.SnapType.Bar:
                    {
                        if (newbeat >= _beatsPerBar / 2)
                        {
                            newbar++;
                        }
                    }
                    TotalSubdivs = (newbar * _beatsPerBar * _subdivsPerBeat);
                    break;

                case BarBar.SnapType.Beat:
                    {
                        if (bspan.Subdiv >= _subdivsPerBeat / 2)
                        {
                            newbeat++;
                            if (newbeat >= _beatsPerBar)
                            {
                                newbar++;
                                newbeat = 0;
                            }
                        }
                        TotalSubdivs = (newbar * _beatsPerBar * _subdivsPerBeat) + (newbeat * _subdivsPerBeat);
                    }
                    break;

                case BarBar.SnapType.Subdiv:
                    // Don't change it.
                    TotalSubdivs = subdiv;
                    break;
            }
        }

        /// <summary>
        /// Format a readable string.
        /// </summary>
        /// <param name="zeroBased"></param>
        /// <returns></returns>
        public string Format(bool zeroBased)
        {
            int inc = zeroBased ? 0 : 1;
            return $"{Bar + inc}.{Beat + inc}.{Subdiv + inc:00}";
        }

        /// <summary>
        /// Format a readable string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Format(true);
        }
        #endregion

        #region Standard IComparable stuff
        public override bool Equals(object? obj)
        {
            return obj is not null && obj is BarSpan span && span.TotalSubdivs == TotalSubdivs;
        }

        public override int GetHashCode()
        {
            return _id;
        }

        public static bool operator ==(BarSpan a, BarSpan b)
        {
            return a.TotalSubdivs == b.TotalSubdivs;
        }

        public static bool operator !=(BarSpan a, BarSpan b)
        {
            return !(a == b);
        }

        public static BarSpan operator +(BarSpan a, BarSpan b)
        {
            return new BarSpan(a.TotalSubdivs + b.TotalSubdivs);
        }

        public static BarSpan operator -(BarSpan a, BarSpan b)
        {
            return new BarSpan(a.TotalSubdivs - b.TotalSubdivs);
        }

        public static bool operator <(BarSpan a, BarSpan b)
        {
            return a.TotalSubdivs < b.TotalSubdivs;
        }

        public static bool operator >(BarSpan a, BarSpan b)
        {
            return a.TotalSubdivs > b.TotalSubdivs;
        }

        public static bool operator <=(BarSpan a, BarSpan b)
        {
            return a.TotalSubdivs <= b.TotalSubdivs;
        }

        public static bool operator >=(BarSpan a, BarSpan b)
        {
            return a.TotalSubdivs >= b.TotalSubdivs;
        }
        #endregion
    }
}
