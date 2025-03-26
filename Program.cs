using System;
using System.Data;
using System.Xml.Linq;
using OfficeOpenXml;
using System.Text;
using System.Security.Cryptography;
using static Package_Generator_Service.Program;
using ConsoleApp1;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace Package_Generator_Service
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        private static Mutex mutex = new Mutex(true, "YourUniqueAppMutex");

        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                MessageBox.Show("Application is already running!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());

            mutex.ReleaseMutex();
        }
    }
}