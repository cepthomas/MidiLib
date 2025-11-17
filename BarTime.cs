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
    /// <summary>Sort of like DateTime but for musical terminology.</summary>
    public class BarTime : IComparable
    {
        #region Fields
        /// <summary>For hashing.</summary>
        readonly int _id;

        /// <summary>Increment for unique value.</summary>
        static int _all_ids = 1;

        /// <summary>Some features are at a lower resolution.</summary>
        public const int LOW_RES_PPQ = 8;
        #endregion

        #region Properties
        /// <summary>The time in subs. Always zero-based.</summary>
        public int TotalSubs { get; private set; }

        /// <summary>The time in beats. Always zero-based.</summary>
        public int TotalBeats { get { return TotalSubs / MidiSettings.LibSettings.SubsPerBeat; } }

        /// <summary>The bar number.</summary>
        public int Bar { get { return TotalSubs / MidiSettings.LibSettings.SubsPerBar; } }

        /// <summary>The beat number in the bar.</summary>
        public int Beat { get { return TotalSubs / MidiSettings.LibSettings.SubsPerBeat % MidiSettings.LibSettings.BeatsPerBar; } }

        /// <summary>The sub in the beat.</summary>
        public int Sub { get { return TotalSubs % MidiSettings.LibSettings.SubsPerBeat; } }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Default constructor.
        /// </summary>
        public BarTime()
        {
            TotalSubs = 0;
            _id = _all_ids++;
        }

        /// <summary>
        /// Constructor from bar/beat/sub.
        /// </summary>
        /// <param name="bar"></param>
        /// <param name="beat"></param>
        /// <param name="sub"></param>
        public BarTime(int bar, int beat, int sub)
        {
            TotalSubs = (bar * MidiSettings.LibSettings.SubsPerBar) + (beat * MidiSettings.LibSettings.SubsPerBeat) + sub;
            _id = _all_ids++;
        }

        /// <summary>
        /// Constructor from subs.
        /// </summary>
        /// <param name="subs">Number of subs.</param>
        public BarTime(int subs)
        {
            if (subs < 0)
            {
                throw new ArgumentException("Negative value is invalid");
            }

            TotalSubs = subs;
            _id = _all_ids++;
        }

        /// <summary>
        /// Construct a BarTime from Beat.Sub representation as a double. Sub is LOW_RES_PPQ.
        /// </summary>
        /// <param name="beat"></param>
        /// <returns>New BarTime.</returns>
        public BarTime(double beat)
        {
            var (integral, fractional) = MathUtils.SplitDouble(beat);
            var beats = (int)integral;
            var subs = (int)Math.Round(fractional * 10.0);

            if (subs >= LOW_RES_PPQ)
            {
                throw new ArgumentException($"Invalid sub value: {beat}");
            }

            // Scale subs to native.
            subs = subs * MidiSettings.LibSettings.InternalPPQ / LOW_RES_PPQ;
            TotalSubs = beats * MidiSettings.LibSettings.SubsPerBeat + subs;
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Hard reset.
        /// </summary>
        public void Reset()
        {
            TotalSubs = 0;
        }

        /// <summary>
        /// Utility helper function.
        /// </summary>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        public void Constrain(BarTime lower, BarTime upper)
        {
            TotalSubs = MathUtils.Constrain(TotalSubs, lower.TotalSubs, upper.TotalSubs);
        }

        /// <summary>
        /// Update current value.
        /// </summary>
        /// <param name="subs">By this number of subs.</param>
        public void Increment(int subs)
        {
            TotalSubs += subs;
            if (TotalSubs < 0)
            {
                TotalSubs = 0;
            }
        }

        /// <summary>
        /// Set to sub using specified rounding.
        /// </summary>
        /// <param name="sub"></param>
        /// <param name="snapType"></param>
        /// <param name="up">To ceiling otherwise closest.</param>
        public void SetRounded(int sub, SnapType snapType, bool up = false)
        {
            if(sub > 0 && snapType != SnapType.Sub)
            {
                // res:32 in:27 floor=(in%aim)*aim  ceiling=floor+aim
                int res = snapType == SnapType.Bar ? MidiSettings.LibSettings.SubsPerBar : MidiSettings.LibSettings.SubsPerBeat;
                int floor = (sub / res) * res;
                int ceiling = floor + res;

                if (up || (ceiling - sub) >= res / 2)
                {
                    sub = ceiling;
                }
                else
                {
                    sub = floor;
                }
            }

            TotalSubs = sub;
        }

        /// <summary>
        /// Format a readable string.
        /// </summary>
        /// <returns></returns>
        public string Format()
        {
           return $"{Bar}.{Beat}.{Sub:00}";
        }

        /// <summary>
        /// Format a readable string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Format();
        }
        #endregion

        #region Standard IComparable stuff
        public override bool Equals(object? obj)
        {
            return obj is not null && obj is BarTime tm && tm.TotalSubs == TotalSubs;
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

            BarTime? other = obj as BarTime;
            if (other is not null)
            {
                return TotalSubs.CompareTo(other.TotalSubs);
            }
            else
            {
                throw new ArgumentException("Object is not a BarTime");
            }
        }

        public static bool operator ==(BarTime a, BarTime b)
        {
            return a.TotalSubs == b.TotalSubs;
        }

        public static bool operator !=(BarTime a, BarTime b)
        {
            return !(a == b);
        }

        public static BarTime operator +(BarTime a, BarTime b)
        {
            return new BarTime(a.TotalSubs + b.TotalSubs);
        }

        public static BarTime operator -(BarTime a, BarTime b)
        {
            return new BarTime(a.TotalSubs - b.TotalSubs);
        }

        public static bool operator <(BarTime a, BarTime b)
        {
            return a.TotalSubs < b.TotalSubs;
        }

        public static bool operator >(BarTime a, BarTime b)
        {
            return a.TotalSubs > b.TotalSubs;
        }

        public static bool operator <=(BarTime a, BarTime b)
        {
            return a.TotalSubs <= b.TotalSubs;
        }

        public static bool operator >=(BarTime a, BarTime b)
        {
            return a.TotalSubs >= b.TotalSubs;
        }
        #endregion
    }
}
