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
    /// <summary>Sort of like DateTime but for musical terminology.</summary>
    public class BarTime : IComparable
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
        public int Bar { get { return TotalSubdivs / MidiSettings.LibSettings.SubdivsPerBar; } }

        /// <summary>The beat number in the bar.</summary>
        public int Beat { get { return TotalSubdivs / MidiSettings.LibSettings.SubdivsPerBeat % MidiSettings.LibSettings.SubdivsPerBar; } }

        /// <summary>The subdiv in the beat.</summary>
        public int Subdiv { get { return TotalSubdivs % MidiSettings.LibSettings.SubdivsPerBeat; } }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Default constructor.
        /// </summary>
        public BarTime()
        {
            TotalSubdivs = 0;
            _id = _all_ids++;
        }

        /// <summary>
        /// Constructor from bar/beat/subdiv.
        /// </summary>
        /// <param name="bar"></param>
        /// <param name="beat"></param>
        /// <param name="subdiv"></param>
        public BarTime(int bar, int beat, int subdiv)
        {
            TotalSubdivs = (bar * MidiSettings.LibSettings.SubdivsPerBar) + (beat * MidiSettings.LibSettings.SubdivsPerBeat) + subdiv;
            _id = _all_ids++;
        }

        /// <summary>
        /// Constructor from subdivs.
        /// </summary>
        /// <param name="subdivs">Number of subdivs.</param>
        public BarTime(int subdivs)
        {
            if (subdivs < 0)
            {
                throw new ArgumentException("Negative value is invalid");
            }

            TotalSubdivs = subdivs;
            _id = _all_ids++;
        }

        /// <summary>
        /// Constructor from Beat.Subdiv representation as a double. TODO1 a bit crude but other ways (e.g. string) clutter the syntax. !!
        /// </summary>
        /// <param name="tts"></param>
        public BarTime(double tts)
        {
            if (tts < 0)
            {
                throw new ArgumentException($"Negative value is invalid");
            }

            var (integral, fractional) = MathUtils.SplitDouble(tts);
            int subdivs = (int)Math.Round(fractional * 10.0);

            int Beat = (int)integral + subdivs / MidiSettings.LibSettings.SubdivsPerBeat;
            int Subdiv = subdivs % MidiSettings.LibSettings.SubdivsPerBeat;

            if (Subdiv >= MidiSettings.LibSettings.SubdivsPerBeat)
            {
                throw new ArgumentException($"Invalid subdiv value");
            }
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
            if(snapType != SnapType.Subdiv)
            {
                // res:32   in:27  floor=(in%aim)*aim  ceiling=floor+aim
                int res = snapType == SnapType.Bar ? MidiSettings.LibSettings.SubdivsPerBar : MidiSettings.LibSettings.SubdivsPerBeat;
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
            return obj is not null && obj is BarTime tm && tm.TotalSubdivs == TotalSubdivs;
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
                return TotalSubdivs.CompareTo(other.TotalSubdivs);
            }
            else
            {
                throw new ArgumentException("Object is not a BarSpan");
            }
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
