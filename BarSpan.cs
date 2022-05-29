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
    /// <summary>Sort of like DateTime but for musical terminlogy.</summary>
    public class BarSpan : IComparable
    {
        #region Fields
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
        public BarSpan(int bar, int beat, int subdiv)
        {
            TotalSubdivs = (bar * InternalDefs.SUBDIVS_PER_BAR) + (beat * InternalDefs.SUBDIVS_PER_BEAT) + subdiv;
            _id = _all_ids++;
        }

        /// <summary>
        /// Constructor from args.
        /// </summary>
        /// <param name="subdivs">Number of subdivs.</param>
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
            if(snapType != SnapType.Subdiv)
            {
                // res:32   in:27  floor=(in%aim)*aim  ceiling=floor+aim
                int res = snapType == SnapType.Bar ? InternalDefs.SUBDIVS_PER_BAR : InternalDefs.SUBDIVS_PER_BEAT;
                int floor = (subdiv / res) * res;
                int ceiling = floor + res;

                if (up || (ceiling - subdiv) >= res / 2)
                {
                    subdiv = ceiling;
                }
                else
                {
                    subdiv = floor;
                }
            }

            TotalSubdivs = subdiv;
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
            return obj is not null && obj is BarSpan tm && tm.TotalSubdivs == TotalSubdivs;
        }

        public override int GetHashCode()
        {
            return _id;
        }

        public int CompareTo(object? obj)
        {
            if (obj is null)
            {
                throw new ArgumentException("Object is null");
            }

            BarSpan? other = obj as BarSpan;
            if (other is not null)
            {
                return TotalSubdivs.CompareTo(other.TotalSubdivs);
            }
            else
            {
                throw new ArgumentException("Object is not a BarTime");
            }
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

        // TODOX neb time stuff >>>

        //public Time() - BarTime(0)
        //public Time(int subdivs) - BarTime(subdivs)
        //public Time(long subdivs) - BarTime((int)subdivs)
        //public Time(Time other) - implement?
        //{
        //    TotalSubdivs = other.TotalSubdivs;
        //}
        //public Time(int beat, int subdiv) - use BarTime(int bar, int beat, int subdiv)

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public BarSpan(BarSpan other)
        {
            TotalSubdivs = other.TotalSubdivs;
        }

        /// <summary>
        /// Constructor from Beat.Subdiv representation as a double. TODOX this is broken for two digits after the dp.
        /// </summary>
        /// <param name="tts"></param>
        public BarSpan(double tts)
        {
            if (tts < 0)
            {
                throw new Exception($"Negative value is invalid: {tts}");
            }
            var (integral, fractional) = MathUtils.SplitDouble(tts);

            // 1.9 1.10 1.11 ... 1.30 1.31 2.0 2.1
            var subdivs = (int)Math.Round(fractional * 10.0);
            var Beat = (int)integral + subdivs / InternalDefs.SUBDIVS_PER_BEAT;
            var Subdiv = subdivs % InternalDefs.SUBDIVS_PER_BEAT;

            if (Subdiv >= InternalDefs.SUBDIVS_PER_BEAT)
            {
                throw new Exception($"Invalid subdiv value: {tts}");
            }
        }

        // Public functions
        // <returns>True if it's a new beat.</returns>
        //public bool Advance() - probably Increment but return rollover.

        //public void Reset() - use new(0)

        //public void RoundUp() - use SetRounded()
    }
}
