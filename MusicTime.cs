using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Ephemera.NBagOfTricks;


namespace Ephemera.MidiLib
{
    /// <summary>Sort of like DateTime but for musical terminology.</summary>
    public class MusicTime : IEquatable<MusicTime>
    {
        #region Fields
        /// <summary>For hashing comparable.</summary>
        readonly int _id;

        /// <summary>Increment for unique value.</summary>
        static int _nextId = 1;
        #endregion

        #region Properties
        /// <summary>Only 4/4 time supported currently.</summary>
        public static int BeatsPerBar { get { return 4; } }

        /// <summary>Our resolution => 32nd note.</summary>
        public static int TicksPerBeat { get { return 32; } }

        /// <summary>Convenience.</summary>
        public static int TicksPerBar { get { return TicksPerBeat * BeatsPerBar; } }

        /// <summary>The total time in ticks. Zero-based.</summary>
        public int Tick { get; private set; }

        /// <summary>Accessor.</summary>
        public int Id { get { return _id; } }

        /// <summary>The time in music form.</summary>
        public (int bar, int beat, int tick) Parts
        {
            get { return (Tick / TicksPerBar, Tick / TicksPerBeat % BeatsPerBar, Tick % TicksPerBeat); }
        }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Default constructor.
        /// </summary>
        public MusicTime()
        {
            Tick = 0;
            _id = _nextId++;
        }

        /// <summary>
        /// Constructor from tick.
        /// </summary>
        /// <param name="tick">Number of ticks.</param>
        public MusicTime(int tick)
        {
            if (tick < 0)
            {
                throw new ArgumentException("Negative value is invalid");
            }

            Tick = tick;
            _id = _nextId++;
        }

        /// <summary>
        /// Constructor from bar/beat/tick.
        /// </summary>
        /// <param name="bar"></param>
        /// <param name="beat"></param>
        /// <param name="tick"></param>
        public MusicTime(int bar, int beat, int tick)
        {
            Tick = (bar * TicksPerBar) + (beat * TicksPerBeat) + tick;
            _id = _nextId++;
        }

        /// <summary>
        /// Construct a MusicTime from a string repr.
        /// </summary>
        /// <param name="s">time string can be "1.2.3" or "1.2" or "1".</param>
        public MusicTime(string s)
        {
            var parts = StringUtils.SplitByToken(s, ".");

            bool ok = true;
            int bars = 0;
            int beats = 0;
            int ticks = 0;

            if (ok && parts.Count > 0) ok = int.TryParse(parts[0], out bars);

            if (ok && parts.Count > 1) ok = int.TryParse(parts[1], out beats);

            if (ok && parts.Count > 2) ok = int.TryParse(parts[2], out ticks);

            if (ok &&
                bars >= 0 && bars <= 9999 &&
                beats >= 0 && beats < BeatsPerBar &&
                ticks >= 0 && ticks <= TicksPerBeat)
            {
                Tick = bars * TicksPerBar + beats * TicksPerBeat + ticks;
            }
            else
            {
                throw new ArgumentException($"Invalid MusicTime format [{s}]");
            }
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Utility helper function.
        /// </summary>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        public void Constrain(MusicTime lower, MusicTime upper)
        {
            Tick = MathUtils.Constrain(Tick, lower.Tick, upper.Tick);
        }

        /// <summary>
        /// Update current value.
        /// </summary>
        /// <param name="ticks">By this number of ticks. Can be negative = decrement.</param>
        public void Update(int ticks)
        {
            Tick += ticks;
            if (Tick < 0)
            {
                Tick = 0;
            }
        }

        /// <summary>
        /// Set the value using specified rounding.
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="snapType"></param>
        public void Set(int tick, SnapType snapType = SnapType.Tick)
        {
            if (tick > 0 && snapType != SnapType.Tick)
            {
                // res:32 in:27 floor=(in%aim)*aim  ceiling=floor+aim
                int res = snapType == SnapType.Bar ? TicksPerBar : TicksPerBeat;

                int floor = tick / res;
                int delta = tick % res;
                if (delta > res / 2)
                {
                    floor++;
                }

                Tick = floor * res;
            }
            else
            {
                Tick = tick;
            }
        }

        /// <summary>
        /// Format a readable string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var p = Parts;
            return $"{p.bar}.{p.beat}.{p.tick:00}";
        }
        #endregion

        #region Operator overloads
        public override int GetHashCode() { return _id; }

        public static bool operator ==(MusicTime a, MusicTime b) { return a.Tick == b.Tick; }

        public static bool operator !=(MusicTime a, MusicTime b) { return !(a == b); }

        public static MusicTime operator +(MusicTime a, MusicTime b) { return new MusicTime(a.Tick + b.Tick); }

        public static MusicTime operator -(MusicTime a, MusicTime b) { return new MusicTime(a.Tick - b.Tick); }

        public static bool operator <(MusicTime a, MusicTime b) { return a.Tick < b.Tick; }

        public static bool operator >(MusicTime a, MusicTime b) { return a.Tick > b.Tick; }

        public static bool operator <=(MusicTime a, MusicTime b) { return a.Tick <= b.Tick; }

        public static bool operator >=(MusicTime a, MusicTime b) { return a.Tick >= b.Tick; }
        #endregion

        #region IEquatable
        public bool Equals(MusicTime? other) { return other is MusicTime tm && tm.Tick == Tick; }

        public override bool Equals(object? obj) { return obj is MusicTime time && Equals(time); }
        #endregion
    }
}
