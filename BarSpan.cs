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
        /// <summary>The time in subbeats. Always zero-based.</summary>
        public int TotalSubbeats { get; private set; }

        // /// <summary>The time in beats. Always zero-based.</summary>
        // public int TotalBeats { get { return TotalSubbeats / MidiSettings.LibSettings.SubbeatsPerBeat; } }

        /// <summary>The bar number.</summary>
        public int Bar { get { return TotalSubbeats / MidiSettings.LibSettings.SubeatsPerBar; } }

        /// <summary>The beat number in the bar.</summary>
        public int Beat { get { return TotalSubbeats / MidiSettings.LibSettings.SubbeatsPerBeat % MidiSettings.LibSettings.BeatsPerBar; } }

        /// <summary>The subbeat in the beat.</summary>
        public int Subbeat { get { return TotalSubbeats % MidiSettings.LibSettings.SubbeatsPerBeat; } }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Default constructor.
        /// </summary>
        public BarSpan()
        {
            TotalSubbeats = 0;
            _id = _all_ids++;
        }

        /// <summary>
        /// Constructor from bar/beat/subbeat.
        /// </summary>
        /// <param name="bar"></param>
        /// <param name="beat"></param>
        /// <param name="subbeat"></param>
        public BarSpan(int bar, int beat, int subbeat)
        {
            TotalSubbeats = (bar * MidiSettings.LibSettings.SubeatsPerBar) + (beat * MidiSettings.LibSettings.SubbeatsPerBeat) + subbeat;
            _id = _all_ids++;
        }

        /// <summary>
        /// Constructor from subbeats.
        /// </summary>
        /// <param name="subbeats">Number of subbeats.</param>
        public BarSpan(int subbeats)
        {
            if (subbeats < 0)
            {
                throw new ArgumentException("Negative value is invalid");
            }

            TotalSubbeats = subbeats;
            _id = _all_ids++;
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Hard reset.
        /// </summary>
        public void Reset()
        {
            TotalSubbeats = 0;
        }

        /// <summary>
        /// Utility helper function.
        /// </summary>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        public void Constrain(BarSpan lower, BarSpan upper)
        {
            TotalSubbeats = MathUtils.Constrain(TotalSubbeats, lower.TotalSubbeats, upper.TotalSubbeats);
        }

        /// <summary>
        /// Update current value.
        /// </summary>
        /// <param name="subbeats">By this number of subbeats.</param>
        public void Increment(int subbeats)
        {
            TotalSubbeats += subbeats;
            if (TotalSubbeats < 0)
            {
                TotalSubbeats = 0;
            }
        }

        /// <summary>
        /// Set to subbeat using specified rounding.
        /// </summary>
        /// <param name="subbeat"></param>
        /// <param name="snapType"></param>
        /// <param name="up">To ceiling otherwise closest.</param>
        public void SetRounded(int subbeat, SnapType snapType, bool up = false)
        {
            if(subbeat > 0 && snapType != SnapType.Subbeat)
            {
                // res:32 in:27 floor=(in%aim)*aim  ceiling=floor+aim
                int res = snapType == SnapType.Bar ? MidiSettings.LibSettings.SubeatsPerBar : MidiSettings.LibSettings.SubbeatsPerBeat;
                int floor = (subbeat / res) * res;
                int ceiling = floor + res;

                if (up || (ceiling - subbeat) >= res / 2)
                {
                    subbeat = ceiling;
                }
                else
                {
                    subbeat = floor;
                }
            }

            TotalSubbeats = subbeat;
        }

        /// <summary>
        /// Format a readable string.
        /// </summary>
        /// <returns></returns>
        public string Format()
        {
           return $"{Bar}.{Beat}.{Subbeat:00}";
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
            return obj is not null && obj is BarSpan tm && tm.TotalSubbeats == TotalSubbeats;
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
                return TotalSubbeats.CompareTo(other.TotalSubbeats);
            }
            else
            {
                throw new ArgumentException("Object is not a BarSpan");
            }
        }

        public static bool operator ==(BarSpan a, BarSpan b)
        {
            return a.TotalSubbeats == b.TotalSubbeats;
        }

        public static bool operator !=(BarSpan a, BarSpan b)
        {
            return !(a == b);
        }

        public static BarSpan operator +(BarSpan a, BarSpan b)
        {
            return new BarSpan(a.TotalSubbeats + b.TotalSubbeats);
        }

        public static BarSpan operator -(BarSpan a, BarSpan b)
        {
            return new BarSpan(a.TotalSubbeats - b.TotalSubbeats);
        }

        public static bool operator <(BarSpan a, BarSpan b)
        {
            return a.TotalSubbeats < b.TotalSubbeats;
        }

        public static bool operator >(BarSpan a, BarSpan b)
        {
            return a.TotalSubbeats > b.TotalSubbeats;
        }

        public static bool operator <=(BarSpan a, BarSpan b)
        {
            return a.TotalSubbeats <= b.TotalSubbeats;
        }

        public static bool operator >=(BarSpan a, BarSpan b)
        {
            return a.TotalSubbeats >= b.TotalSubbeats;
        }
        #endregion
    }
}
