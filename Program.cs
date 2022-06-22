using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JA
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Render3DForm());
        }
        public static void Log(Type type, object arg, [CallerMemberName] string name = null)
        {
            Debug.WriteLine($"{type.Name}.{name}({arg})");
        }
        public static void Log(Type type, [CallerMemberName] string name = null)
            => Log(type, string.Empty, name);
        public static void Log(Type type, object[] args, [CallerMemberName] string name = null)
            => Log(type, string.Join(",", args), name);
    }
}
