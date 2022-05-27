using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using NBagOfTricks;


// public const int BEATS_PER_BAR = 4;
// public const int SUBDIVS_PER_BEAT = 32;
//
// bar.beat.subdiv()
//
// one-based
// 1.1.1
// ...
// 1.1.32
// 1.2.1
// ...
// 1.2.32
// ...
// 4.4.32
// ...
// 5.1.1
// ...
//
// zero-based
// 0.0.0
// ...
// 0.0.31
// 0.1.0
// ...
// 0.1.31
// ...
// 3.3.31
// ...
// 4.0.0
// ...


namespace MidiLib
{
    /// <summary>Sort of like DateTime but for musical terminlogy.</summary>
    public class BarTime // TODO some uses should be a BarSpan.
    {
        #region Fields
        /// <summary>A useful constant.</summary>
        public static readonly BarTime Zero = new(0);

        /// <summary>A useful constant.</summary>
        public static readonly BarTime OneSubdiv = new(1);

        /// <summary>For hashing.</summary>
        readonly int _id;

        /// <summary>Increment for unique value.</summary>
        static int _all_ids = 1;
        #endregion

        #region Properties
        /// <summary>The length.</summary>
        public int TotalSubdivs { get; private set; }

        /// <summary>The bar number.</summary>
        public int Bar { get { return TotalSubdivs / InternalDefs.SUBDIVS_PER_BAR; } }

        /// <summary>The beat number in the bar.</summary>
        public int Beat { get { return TotalSubdivs / InternalDefs.SUBDIVS_PER_BEAT % InternalDefs.BEATS_PER_BAR; } }

        /// <summary>The subdiv in the beat.</summary>
        public int Subdiv { get { return TotalSubdivs % InternalDefs.SUBDIVS_PER_BEAT; } }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor from args.
        /// </summary>
        /// <param name="bar"></param>
        /// <param name="beat"></param>
        /// <param name="subdiv"></param>
        public BarTime(int bar, int beat, int subdiv)
        {
            TotalSubdivs = (bar * InternalDefs.SUBDIVS_PER_BAR) + (beat * InternalDefs.SUBDIVS_PER_BEAT) + subdiv;
            _id = _all_ids++;
        }

        /// <summary>
        /// Constructor from args.
        /// </summary>
        /// <param name="subdivs">Number of subdivs.</param>
        public BarTime(int subdivs)
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
        public void Constrain(BarTime lower, BarTime upper)
        {
            TotalSubdivs = MathUtils.Constrain(TotalSubdivs, lower.TotalSubdivs, upper.TotalSubdivs);
        }

        /// <summary>
        /// Update current value.
        /// </summary>
        /// <param name="subdivs">By this number of subdivs.</param>
        public void Increment(int subdivs)
        {
            TotalSubdivs += subdivs;
            if (TotalSubdivs < 0)
            {
                TotalSubdivs = 0;
            }
        }

        /// <summary>
        /// Set to subdiv using specified rounding.
        /// </summary>
        /// <param name="subdiv"></param>
        /// <param name="snapType"></param>
        /// <param name="up">To ceiling otherwise closest.</param>
        public void SetRounded(int subdiv, SnapType snapType, bool up = false)
        {
            BarTime newtime = new(subdiv);
            int newbar = newtime.Bar;
            int newbeat = newtime.Beat;

            switch (snapType)
            {
                case SnapType.Bar:
                    {
                        if (up || newbeat >= InternalDefs.BEATS_PER_BAR / 2)
                        {
                            newbar++;
                        }
                    }
                    TotalSubdivs = newbar * InternalDefs.SUBDIVS_PER_BAR;
                    break;

                case SnapType.Beat:
                    {
                        if (up || newtime.Subdiv >= InternalDefs.SUBDIVS_PER_BEAT / 2)
                        {
                            newbeat++;
                            if (newbeat >= InternalDefs.BEATS_PER_BAR)
                            {
                                newbar++;
                                newbeat = 0;
                            }
                        }
                        TotalSubdivs = (newbar * InternalDefs.SUBDIVS_PER_BAR) + (newbeat * InternalDefs.SUBDIVS_PER_BEAT);
                    }
                    break;

                case SnapType.Subdiv:
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
        public string Format(bool zeroBased = true)
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
            return obj is not null && obj is BarTime tm && tm.TotalSubdivs == TotalSubdivs;
        }

        public override int GetHashCode()
        {
            return _id;
        }

        public static bool operator ==(BarTime a, BarTime b)
        {
            return a.TotalSubdivs == b.TotalSubdivs;
        }

        public static bool operator !=(BarTime a, BarTime b)
        {
            return !(a == b);
        }

        public static BarTime operator +(BarTime a, BarTime b)
        {
            return new BarTime(a.TotalSubdivs + b.TotalSubdivs);
        }

        public static BarTime operator -(BarTime a, BarTime b)
        {
            return new BarTime(a.TotalSubdivs - b.TotalSubdivs);
        }

        public static bool operator <(BarTime a, BarTime b)
        {
            return a.TotalSubdivs < b.TotalSubdivs;
        }

        public static bool operator >(BarTime a, BarTime b)
        {
            return a.TotalSubdivs > b.TotalSubdivs;
        }

        public static bool operator <=(BarTime a, BarTime b)
        {
            return a.TotalSubdivs <= b.TotalSubdivs;
        }

        public static bool operator >=(BarTime a, BarTime b)
        {
            return a.TotalSubdivs >= b.TotalSubdivs;
        }
        #endregion
    }
}
