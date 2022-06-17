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
        /// <summary>Background image data.</summary>
        PixelBitmap? _bmp;

        /// <summary>Tool tip.</summary>
        readonly ToolTip _toolTip = new();

        /// <summary>Last key down.</summary>
        int _lastNote = -1;

        /// <summary>The pen.</summary>
        readonly Pen _pen = new(Color.LightGray, 1);
        #endregion

        #region Properties

        /// <summary>Lowest piano key.</summary>
        public int MinNote { get; set; } = 21;

        /// <summary>Highest piano key.</summary>
        public int MaxNote { get; set; } = 108;

        /// <summary>Min control value. For velocity = off.</summary>
        public int MinControl { get; set; } = 0;

        /// <summary>Max control value. For velocity = loudest.</summary>
        public int MaxControl { get; set; } = 127;

        /// <summary>Visibility.</summary>
        public bool DrawNoteGrid { get; set; } = true;
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
            _pen.Dispose();
            base.Dispose(disposing);
        }
        #endregion

        #region Event handlers
        /// <summary>
        /// Paint the surface.
        /// </summary>
        /// <param name="pe"></param>
        protected override void OnPaint(PaintEventArgs pe)
        {
            // Background?
            if(_bmp is not null)
            {
                pe.Graphics.DrawImage(_bmp.Bitmap, 0, 0, _bmp.Bitmap.Width, _bmp.Bitmap.Height);
            }

            // Draw grid? TODO
            if(DrawNoteGrid)
            {
                int num = MaxNote - MinNote;

                for (int i = 0; i < MaxNote - MinNote; i += 6)
                {
                    int px = i * Width / num;
                    pe.Graphics.DrawLine(_pen, px, 0, px, Height);
                }
            }

            base.OnPaint(pe);
        }

        /// <summary>
        /// Show the pixel info.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            var mp = PointToClient(MousePosition);
            var (note, control) = XyToMidi(mp.X, mp.Y);

            if (e.Button == MouseButtons.Left)
            {
                // Dragging. Did it change?
                if(_lastNote != note)
                {
                    _lastNote = note;
                    DeviceEvent?.Invoke(this, new() { Note = note, Control = control });
                }
            }

            //toolTip1.SetToolTip(this, $"X:{mp.X} Y:{mp.Y}");

            //Color clr = _result.GetPixel(mp.X, mp.Y);
            //toolTip1.SetToolTip(this, $"X:{mp.X} Y:{mp.Y} C:{clr}");

            //var note = MidiDefs.NoteNumberToName(mp.X);
            //toolTip1.SetToolTip(this, $"{note}({mp.Y})");

            var snote = MidiDefs.NoteNumberToName(note);
            _toolTip.SetToolTip(this, $"{snote} {note} {control}");

            base.OnMouseMove(e);
        }

        /// <summary>
        /// Send info to client.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            var mp = PointToClient(MousePosition);
            var (note, control) = XyToMidi(mp.X, mp.Y);
            _lastNote = note;

            DeviceEvent?.Invoke(this, new() { Note = note, Control = control });

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
                DeviceEvent?.Invoke(this, new() { Note = _lastNote, Control = 0 });
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
        /// <param name="x">UI location.</param>
        /// <param name="y">UI location.</param>
        /// <returns>Tuple of note num and control value.</returns>
        (int note, int control) XyToMidi(int x, int y)
        {
            int note = MathUtils.Map(x, 0, Width, MinNote, MaxNote);
            int control = MathUtils.Map(y, Height, 0, MinControl, MaxControl);

            return (note, control);
        }
        #endregion
    }
}
