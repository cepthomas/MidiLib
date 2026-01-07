using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;

namespace Ephemera.MidiLib.Test
{
    static class Program
    {
        /// <summary>The main entry point for the application.</summary>
        [STAThread]
        static void Main()
        {
            bool ui = false;

            if (ui)
            {
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                var f = new MainForm();
                Application.Run(f);
            }
            else
            {
                TestRunner runner = new(OutputFormat.Readable);
                var cases = new[] { "MIDILIB" };
                runner.RunSuites(cases);
                File.WriteAllLines(Path.Join(MiscUtils.GetSourcePath(), "out", "test.txt"), runner.Context.OutputLines);
            }
        }
    }
}
