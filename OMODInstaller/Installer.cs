﻿using OMODInstaller.Forms;
using System;
using System.Windows.Forms;

namespace OMODInstaller
{
    class Installer
    {
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}