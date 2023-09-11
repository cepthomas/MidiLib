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
        /// <summary>The time in subbeats. Always zero-based.</summary>
        public int TotalSubbeats { get; private set; }

        /// <summary>The time in beats. Always zero-based.</summary>
        public int TotalBeats { get { return TotalSubbeats / MidiSettings.LibSettings.SubbeatsPerBeat; } }

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
        public BarTime()
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
        public BarTime(int bar, int beat, int subbeat)
        {
            TotalSubbeats = (bar * MidiSettings.LibSettings.SubeatsPerBar) + (beat * MidiSettings.LibSettings.SubbeatsPerBeat) + subbeat;
            _id = _all_ids++;
        }

        /// <summary>
        /// Constructor from subbeats.
        /// </summary>
        /// <param name="subbeats">Number of subbeats.</param>
        public BarTime(int subbeats)
        {
            if (subbeats < 0)
            {
                throw new ArgumentException("Negative value is invalid");
            }

            TotalSubbeats = subbeats;
            _id = _all_ids++;
        }

        /// <summary>
        /// Construct a BarTime from Beat.Subbeat representation as a double. Subbeat is LOW_RES_PPQ.
        /// </summary>
        /// <param name="beat"></param>
        /// <returns>New BarTime.</returns>
        public BarTime(double beat)
        {
            var (integral, fractional) = MathUtils.SplitDouble(beat);
            var beats = (int)integral;
            var subbeats = (int)Math.Round(fractional * 10.0);

            if (subbeats >= LOW_RES_PPQ)
            {
                throw new Exception($"Invalid subbeat value: {beat}");
            }

            // Scale subbeats to native.
            subbeats = subbeats * MidiSettings.LibSettings.InternalPPQ / LOW_RES_PPQ;
            TotalSubbeats = beats * MidiSettings.LibSettings.SubbeatsPerBeat + subbeats;
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
        public void Constrain(BarTime lower, BarTime upper)
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
            return obj is not null && obj is BarTime tm && tm.TotalSubbeats == TotalSubbeats;
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
                return TotalSubbeats.CompareTo(other.TotalSubbeats);
            }
            else
            {
                throw new ArgumentException("Object is not a BarSpan");
            }
        }

        public static bool operator ==(BarTime a, BarTime b)
        {
            return a.TotalSubbeats == b.TotalSubbeats;
        }

        public static bool operator !=(BarTime a, BarTime b)
        {
            return !(a == b);
        }

        public static BarTime operator +(BarTime a, BarTime b)
        {
            return new BarTime(a.TotalSubbeats + b.TotalSubbeats);
        }

        public static BarTime operator -(BarTime a, BarTime b)
        {
            return new BarTime(a.TotalSubbeats - b.TotalSubbeats);
        }

        public static bool operator <(BarTime a, BarTime b)
        {
            return a.TotalSubbeats < b.TotalSubbeats;
        }

        public static bool operator >(BarTime a, BarTime b)
        {
            return a.TotalSubbeats > b.TotalSubbeats;
        }

        public static bool operator <=(BarTime a, BarTime b)
        {
            return a.TotalSubbeats <= b.TotalSubbeats;
        }

        public static bool operator >=(BarTime a, BarTime b)
        {
            return a.TotalSubbeats >= b.TotalSubbeats;
        }
        #endregion
    }
}
