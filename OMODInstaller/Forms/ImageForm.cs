using System;
using System.Drawing;
using System.Windows.Forms;

namespace OblivionModManager
{
    internal partial class ImageForm : Form
    {
        internal ImageForm(Image i)
        {
            InitializeComponent();
            pictureBox1.Image = i;
        }

        internal ImageForm(Image i, string text) : this(i) { Text = text; }

        private void PictureBox1_Click_1(object sender, EventArgs e)
        {
            Close();
        }
    }
}
