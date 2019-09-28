namespace OblivionModManager
{
    partial class TextEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TextEditor));
            this.ToolBar = new System.Windows.Forms.ToolStrip();
            this.bOpen = new System.Windows.Forms.ToolStripButton();
            this.bSave = new System.Windows.Forms.ToolStripButton();
            this.bRTF = new System.Windows.Forms.ToolStripButton();
            this.bBold = new System.Windows.Forms.ToolStripButton();
            this.bItalic = new System.Windows.Forms.ToolStripButton();
            this.bUnderline = new System.Windows.Forms.ToolStripButton();
            this.cmbFontSize = new System.Windows.Forms.ToolStripComboBox();
            this.bFind = new System.Windows.Forms.ToolStripButton();
            this.bFindNext = new System.Windows.Forms.ToolStripButton();
            this.rtbEdit = new System.Windows.Forms.RichTextBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.cutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.OpenDialog = new System.Windows.Forms.OpenFileDialog();
            this.ToolBar.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // ToolBar
            // 
            this.ToolBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.bOpen,
            this.bSave,
            this.bRTF,
            this.bBold,
            this.bItalic,
            this.bUnderline,
            this.cmbFontSize,
            this.bFind,
            this.bFindNext});
            this.ToolBar.Location = new System.Drawing.Point(0, 0);
            this.ToolBar.Name = "ToolBar";
            this.ToolBar.Size = new System.Drawing.Size(484, 25);
            this.ToolBar.TabIndex = 2;
            // 
            // bOpen
            // 
            this.bOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bOpen.Image = ((System.Drawing.Image)(resources.GetObject("bOpen.Image")));
            this.bOpen.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bOpen.Name = "bOpen";
            this.bOpen.Size = new System.Drawing.Size(23, 22);
            this.bOpen.Text = "Open file";
            // 
            // bSave
            // 
            this.bSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bSave.Image = ((System.Drawing.Image)(resources.GetObject("bSave.Image")));
            this.bSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bSave.Name = "bSave";
            this.bSave.Size = new System.Drawing.Size(23, 22);
            this.bSave.Text = "Save file";
            // 
            // bRTF
            // 
            this.bRTF.CheckOnClick = true;
            this.bRTF.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bRTF.Image = ((System.Drawing.Image)(resources.GetObject("bRTF.Image")));
            this.bRTF.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bRTF.Name = "bRTF";
            this.bRTF.Size = new System.Drawing.Size(23, 22);
            this.bRTF.Text = "Toggle RTF";
            // 
            // bBold
            // 
            this.bBold.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bBold.Enabled = false;
            this.bBold.Image = ((System.Drawing.Image)(resources.GetObject("bBold.Image")));
            this.bBold.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bBold.Name = "bBold";
            this.bBold.Size = new System.Drawing.Size(23, 22);
            this.bBold.Text = "Bold";
            // 
            // bItalic
            // 
            this.bItalic.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bItalic.Enabled = false;
            this.bItalic.Image = ((System.Drawing.Image)(resources.GetObject("bItalic.Image")));
            this.bItalic.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bItalic.Name = "bItalic";
            this.bItalic.Size = new System.Drawing.Size(23, 22);
            this.bItalic.Text = "Italic";
            // 
            // bUnderline
            // 
            this.bUnderline.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bUnderline.Enabled = false;
            this.bUnderline.Image = ((System.Drawing.Image)(resources.GetObject("bUnderline.Image")));
            this.bUnderline.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bUnderline.Name = "bUnderline";
            this.bUnderline.Size = new System.Drawing.Size(23, 22);
            this.bUnderline.Text = "Underline";
            // 
            // cmbFontSize
            // 
            this.cmbFontSize.Enabled = false;
            this.cmbFontSize.Items.AddRange(new object[] {
            "8",
            "10",
            "12",
            "14",
            "18",
            "22",
            "28",
            "32",
            "48",
            "78"});
            this.cmbFontSize.Name = "cmbFontSize";
            this.cmbFontSize.Size = new System.Drawing.Size(121, 25);
            this.cmbFontSize.Text = "10";
            // 
            // bFind
            // 
            this.bFind.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.bFind.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bFind.Name = "bFind";
            this.bFind.Size = new System.Drawing.Size(46, 22);
            this.bFind.Text = "Search";
            // 
            // bFindNext
            // 
            this.bFindNext.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.bFindNext.Enabled = false;
            this.bFindNext.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bFindNext.Name = "bFindNext";
            this.bFindNext.Size = new System.Drawing.Size(60, 22);
            this.bFindNext.Text = "Find next";
            // 
            // rtbEdit
            // 
            this.rtbEdit.AcceptsTab = true;
            this.rtbEdit.ContextMenuStrip = this.contextMenuStrip1;
            this.rtbEdit.Location = new System.Drawing.Point(0, 28);
            this.rtbEdit.Name = "rtbEdit";
            this.rtbEdit.Size = new System.Drawing.Size(484, 333);
            this.rtbEdit.TabIndex = 3;
            this.rtbEdit.Text = "";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cutToolStripMenuItem,
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(103, 70);
            // 
            // cutToolStripMenuItem
            // 
            this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
            this.cutToolStripMenuItem.Size = new System.Drawing.Size(102, 22);
            this.cutToolStripMenuItem.Text = "Cut";
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(102, 22);
            this.copyToolStripMenuItem.Text = "Copy";
            // 
            // pasteToolStripMenuItem
            // 
            this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
            this.pasteToolStripMenuItem.Size = new System.Drawing.Size(102, 22);
            this.pasteToolStripMenuItem.Text = "Paste";
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.Filter = "Rich text file (*.rtf)|*.rtf|Plain text (*.txt)|*.txt";
            this.saveFileDialog1.RestoreDirectory = true;
            this.saveFileDialog1.Title = "Save file as";
            // 
            // OpenDialog
            // 
            this.OpenDialog.Filter = "text files (*.txt, *.rtf)|*.txt;*.rtf|All files|*.*";
            this.OpenDialog.RestoreDirectory = true;
            this.OpenDialog.Title = "Choose file to import";
            // 
            // TextEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 361);
            this.Controls.Add(this.rtbEdit);
            this.Controls.Add(this.ToolBar);
            this.Name = "TextEditor";
            this.Text = "Text Editor";
            this.ToolBar.ResumeLayout(false);
            this.ToolBar.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip ToolBar;
        private System.Windows.Forms.ToolStripButton bOpen;
        private System.Windows.Forms.ToolStripButton bSave;
        private System.Windows.Forms.ToolStripButton bRTF;
        private System.Windows.Forms.ToolStripButton bBold;
        private System.Windows.Forms.ToolStripButton bItalic;
        private System.Windows.Forms.ToolStripButton bUnderline;
        private System.Windows.Forms.ToolStripComboBox cmbFontSize;
        private System.Windows.Forms.ToolStripButton bFind;
        private System.Windows.Forms.ToolStripButton bFindNext;
        private System.Windows.Forms.RichTextBox rtbEdit;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem cutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.OpenFileDialog OpenDialog;
    }
}