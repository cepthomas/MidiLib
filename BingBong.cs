using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NBagOfTricks;
using NBagOfUis;


namespace MidiLib
{
    /// <summary>
    /// Experimental midi controller.
    /// </summary>
    public partial class BingBong : UserControl
    {
        #region Fields
        /// <summary>Underlying image data.</summary>
        PixelBitmap? _bmp;

        /// <summary>Tool tip.</summary>
        readonly ToolTip toolTip1 = new();

        /// <summary>Lowest piano key.</summary>
        readonly int _minNote = 21;

        /// <summary>Highest piano key.</summary>
        readonly int _maxNote = 108;

        /// <summary>Off.</summary>
        readonly int _minVelocity = 0;

        /// <summary>Loudest.</summary>
        readonly int _maxVelocity = 127;

        /// <summary>Visibility.</summary>
        bool _drawGrid = true;

        /// <summary>Last key down.</summary>
        int _lastNote = -1;
        #endregion

        #region Events
        /// <summary>Click press info.</summary>
        public event EventHandler<DeviceEventArgs>? DeviceEvent;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor. Creates the color gradations.
        /// </summary>
        public BingBong()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            Text = "Bing Bong";
        }

        /// <summary>
        /// Init after properties set.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            DrawBitmap();

            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            _bmp?.Dispose();
            base.Dispose(disposing);
        }
        #endregion






        #region Event handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if(_bmp is not null)
            {
                e.Graphics.DrawImage(_bmp.Bitmap, 0, 0, _bmp.Bitmap.Width, _bmp.Bitmap.Height);
            }

            // Draw grid

            base.OnPaint(e);
        }

        /// <summary>
        /// Show the pixel info.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
            }
            else
            {
                var mp = PointToClient(MousePosition);

                //toolTip1.SetToolTip(this, $"X:{mp.X} Y:{mp.Y}");

                //Color clr = _result.GetPixel(mp.X, mp.Y);
                //toolTip1.SetToolTip(this, $"X:{mp.X} Y:{mp.Y} C:{clr}");

                //var note = MidiDefs.NoteNumberToName(mp.X);
                //toolTip1.SetToolTip(this, $"{note}({mp.Y})");

                var nv = XyToMidi(mp.X, mp.Y);
                var note = MidiDefs.NoteNumberToName(nv.note);
                toolTip1.SetToolTip(this, $"{note} {nv.note} {nv.velocity}");
            }

            base.OnMouseMove(e);
        }

        /// <summary>
        /// Send info to client.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            var mp = PointToClient(MousePosition);
            var (note, velocity) = XyToMidi(mp.X, mp.Y);
            _lastNote = note;

            DeviceEvent?.Invoke(this, new() { NoteId = note, Velocity = velocity });

            base.OnMouseDown(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if(_lastNote != -1)
            {
                DeviceEvent?.Invoke(this, new() { NoteId = _lastNote, Velocity = 0 });
            }

            _lastNote = -1;

            base.OnMouseUp(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnResize(EventArgs e)
        {
            DrawBitmap();

            Invalidate();
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Render.
        /// </summary>
        void DrawBitmap()
        {
            // Clean up old.
            _bmp?.Dispose();

            _bmp = new(Width, Height);

            foreach (var y in Enumerable.Range(0, Height))
            {
                foreach (var x in Enumerable.Range(0, Width))
                {
                    _bmp!.SetPixel(x, y, 255, x * 256 / Width, y * 256 / Height, 150);
                }
            }
        }

        /// <summary>
        /// Map function.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        (int note, int velocity) XyToMidi(int x, int y)
        {
            int note = MathUtils.Map(x, 0, Width, _minNote, _maxNote);
            int velocity = MathUtils.Map(y, Height, 0, _minVelocity, _maxVelocity);

            return (note, velocity);
        }
        #endregion
    }
}
