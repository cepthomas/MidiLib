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
    /// <summary>Sort of like TimeSpan but for musical terminology. TODO finish and integrate with BarTime.</summary>
    public class BarSpan : IComparable
    {
        #region Fields
        /// <summary>For hashing.</summary>
        readonly int _id;

        /// <summary>Increment for unique value.</summary>
        static int _all_ids = 1;
        #endregion

        #region Properties
        /// <summary>The time in subs. Always zero-based.</summary>
        public int TotalSubs { get; private set; }

        // /// <summary>The time in beats. Always zero-based.</summary>
        // public int TotalBeats { get { return TotalSubs / MidiSettings.LibSettings.SubsPerBeat; } }

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
        public BarSpan()
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
        public BarSpan(int bar, int beat, int sub)
        {
            TotalSubs = (bar * MidiSettings.LibSettings.SubsPerBar) + (beat * MidiSettings.LibSettings.SubsPerBeat) + sub;
            _id = _all_ids++;
        }

        /// <summary>
        /// Constructor from subs.
        /// </summary>
        /// <param name="subs">Number of subs.</param>
        public BarSpan(int subs)
        {
            if (subs < 0)
            {
                throw new ArgumentException("Negative value is invalid");
            }

            TotalSubs = subs;
            _id = _all_ids++;
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
        public void Constrain(BarSpan lower, BarSpan upper)
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
            return obj is not null && obj is BarSpan tm && tm.TotalSubs == TotalSubs;
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
                return TotalSubs.CompareTo(other.TotalSubs);
            }
            else
            {
                throw new ArgumentException("Object is not a BarSpan");
            }
        }

        public static bool operator ==(BarSpan a, BarSpan b)
        {
            return a.TotalSubs == b.TotalSubs;
        }

        public static bool operator !=(BarSpan a, BarSpan b)
        {
            return !(a == b);
        }

        public static BarSpan operator +(BarSpan a, BarSpan b)
        {
            return new BarSpan(a.TotalSubs + b.TotalSubs);
        }

        public static BarSpan operator -(BarSpan a, BarSpan b)
        {
            return new BarSpan(a.TotalSubs - b.TotalSubs);
        }

        public static bool operator <(BarSpan a, BarSpan b)
        {
            return a.TotalSubs < b.TotalSubs;
        }

        public static bool operator >(BarSpan a, BarSpan b)
        {
            return a.TotalSubs > b.TotalSubs;
        }

        public static bool operator <=(BarSpan a, BarSpan b)
        {
            return a.TotalSubs <= b.TotalSubs;
        }

        public static bool operator >=(BarSpan a, BarSpan b)
        {
            return a.TotalSubs >= b.TotalSubs;
        }
        #endregion
    }
}
