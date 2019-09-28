using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OblivionModManager
{
    internal partial class TextEditor : Form
    {
        internal string Result;
        private bool AllowRTF;
        private bool Edited = false;
        private FindForm ff = null;

        internal TextEditor(string Title, string InitialContents, bool allowRTF, bool AllowEdit)
        {
            Result = InitialContents;
            AllowRTF = allowRTF;
            InitializeComponent();
            Text = Title;

            bBold.Enabled = false;
            bItalic.Enabled = false;
            bUnderline.Enabled = false;
            cmbFontSize.Enabled = false;

            if (!AllowRTF)
            {
                bRTF.Enabled = false;
                rtbEdit.Text = InitialContents;
            }
            else
            {
                bRTF.Checked = false;
                if(InitialContents != "")
                {
                    try
                    {
                        rtbEdit.Rtf = InitialContents;
                        bRTF.Checked = true;
                    }
                    catch
                    {
                        rtbEdit.Text = InitialContents;
                    }
                }
            }

            if (!AllowEdit)
            {
                rtbEdit.ReadOnly = true;
                bRTF.Enabled = false;
                bOpen.Enabled = false;
            }
            else rtbEdit.TextChanged += new EventHandler(rtbEdit_TextChanged);
            rtbEdit.Select(0, 0);
        }

        // Conflict report, ignore
        internal TextEditor(string p)
        {
            InitializeComponent();

        }

        private void cmbFontSize_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\b' && !char.IsDigit(e.KeyChar)) e.Handled = true;
        }

        private void bBold_Click(object sender, EventArgs e)
        {
            rtbEdit.SelectionFont = new Font(rtbEdit.SelectionFont, rtbEdit.SelectionFont.Style ^ FontStyle.Bold);
        }

        private void bItalic_Click(object sender, EventArgs e)
        {
            rtbEdit.SelectionFont = new Font(rtbEdit.SelectionFont, rtbEdit.SelectionFont.Style ^ FontStyle.Italic);
        }

        private void bUnderline_Click(object sender, EventArgs e)
        {
            rtbEdit.SelectionFont = new Font(rtbEdit.SelectionFont, rtbEdit.SelectionFont.Style ^ FontStyle.Underline);
        }

        private void cmbFontSize_Leave(object sender, EventArgs e)
        {
            try
            {
                float f = Convert.ToInt32(cmbFontSize.Text);
                rtbEdit.SelectionFont = new Font(rtbEdit.SelectionFont.FontFamily, f, rtbEdit.SelectionFont.Style);
            }
            catch
            {
                cmbFontSize.Text = rtbEdit.SelectionFont.SizeInPoints.ToString();
            }
        }

        private void cmbFontSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            rtbEdit.Focus();
        }

        private void bOpen_Click(object sender, EventArgs e)
        {
            if (OpenDialog.ShowDialog() == DialogResult.OK)
            {
                if (AllowRTF)
                {
                    try
                    {
                        rtbEdit.Rtf = Program.ReadAllText(OpenDialog.FileName);
                        bRTF.Checked = true;
                    }
                    catch
                    {
                        rtbEdit.Text = Program.ReadAllText(OpenDialog.FileName);
                    }
                }
                else
                {
                    rtbEdit.Text = Program.ReadAllText(OpenDialog.FileName);
                }
            }
        }

        private void rtbEdit_TextChanged(object sender, EventArgs e)
        {
            Edited = true;
            rtbEdit.TextChanged -= new EventHandler(rtbEdit_TextChanged);
        }

        private void bRTF_CheckedChanged(object sender, EventArgs e)
        {
            if (bRTF.Checked)
            {
                bBold.Enabled = true;
                bItalic.Enabled = true;
                bUnderline.Enabled = true;
                cmbFontSize.Enabled = true;
            }
            else
            {
                if (MessageBox.Show("Warning: This will clear any formatting from the current document",
                    "warning", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    string s = rtbEdit.Text;
                    rtbEdit.Clear();
                    rtbEdit.Text = s;
                    bBold.Enabled = false;
                    bItalic.Enabled = false;
                    bUnderline.Enabled = false;
                    cmbFontSize.Enabled = false;
                }
            }
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbEdit.Cut();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbEdit.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbEdit.Paste();
        }

        private void bSave_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;
            try
            {
                if (saveFileDialog1.FilterIndex == 1)
                {
                    System.IO.File.WriteAllText(saveFileDialog1.FileName, rtbEdit.Rtf);
                }
                else
                {
                    System.IO.File.WriteAllText(saveFileDialog1.FileName, rtbEdit.Text, System.Text.Encoding.Default);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured while trying to save the file\n" + ex.Message, "Error");
            }
        }

        private void bFind_Click(object sender, EventArgs e)
        {
            //rtbEdit.HideSelection=false;
            if (ff != null) return;
            ff = new FindForm();
            ff.bFind.Click += new EventHandler(ffbFind_Click);
            ff.FormClosed += new FormClosedEventHandler(ff_FormClosed);
            ff.Show();
            bFindNext.Enabled = true;
        }

        internal void ff_FormClosed(object sender, FormClosedEventArgs e)
        {
            //rtbEdit.HideSelection=true;
            ff = null;
            bFindNext.Enabled = false;
        }

        internal void ffbFind_Click(object sender, EventArgs e)
        {
            //Focus();
            int start = rtbEdit.SelectionStart + rtbEdit.SelectionLength;
            if (start > rtbEdit.Text.Length) start = 0;
            if (rtbEdit.Find(ff.tbFind.Text, start, RichTextBoxFinds.None) == -1)
            {
                if (start == 0 || rtbEdit.Find(ff.tbFind.Text, 0, RichTextBoxFinds.None) == -1)
                {
                    MessageBox.Show("Search string not found", "Message");
                }
            }
        }

        private void bFindNext_Click(object sender, EventArgs e)
        {
            if (ff == null) return;
            ffbFind_Click(null, null);
        }

        private void TextEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Edited)
            {
                switch (MessageBox.Show("Save changes?", "question", MessageBoxButtons.YesNoCancel))
                {
                    case DialogResult.Yes:
                        DialogResult = DialogResult.Yes;
                        if (AllowRTF && bRTF.Checked)
                        {
                            if (rtbEdit.Text == "") Result = "";
                            else Result = rtbEdit.Rtf;
                        }
                        else
                        {
                            Result = rtbEdit.Text;
                        }
                        break;
                    case DialogResult.No:
                        DialogResult = DialogResult.No;
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        return;
                }
            }
            if (ff != null) ff.Close();
        }

        private void rtbEdit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                e.Handled = true;
                switch (e.KeyCode)
                {
                    case Keys.S:
                        bSave_Click(null, null);
                        break;
                    case Keys.O:
                        bOpen_Click(null, null);
                        break;
                    default:
                        e.Handled = false;
                        break;
                }
            }
        }
    }
}
