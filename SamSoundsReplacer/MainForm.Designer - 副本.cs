namespace SamSoundsReplacer
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.ListView listViewFiles;
        private System.Windows.Forms.ColumnHeader columnHeaderFolder;
        private System.Windows.Forms.ColumnHeader columnHeaderFile;
        private System.Windows.Forms.ColumnHeader columnHeaderPath;
        private System.Windows.Forms.Button btnRefresh;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.listViewFiles = new System.Windows.Forms.ListView();
            this.columnHeaderFolder = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderFile = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnRefresh = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listViewFiles
            // 
            this.listViewFiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
        this.columnHeaderFolder,
        this.columnHeaderFile,
        this.columnHeaderPath});
            this.listViewFiles.FullRowSelect = true;
            this.listViewFiles.GridLines = true;
            this.listViewFiles.HideSelection = false;
            this.listViewFiles.Location = new System.Drawing.Point(12, 12);
            this.listViewFiles.Name = "listViewFiles";
            this.listViewFiles.Size = new System.Drawing.Size(760, 400);
            this.listViewFiles.TabIndex = 0;
            this.listViewFiles.UseCompatibleStateImageBehavior = false;
            this.listViewFiles.View = System.Windows.Forms.View.Details;
            this.listViewFiles.SelectedIndexChanged += new System.EventHandler(this.listViewFiles_SelectedIndexChanged);
            // 
            // columnHeaderFolder
            // 
            this.columnHeaderFolder.Text = "文件夹";
            this.columnHeaderFolder.Width = 150;
            // 
            // columnHeaderFile
            // 
            this.columnHeaderFile.Text = "文件";
            this.columnHeaderFile.Width = 150;
            // 
            // columnHeaderPath
            // 
            this.columnHeaderPath.Text = "完整路径";
            this.columnHeaderPath.Width = 450;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(697, 418);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(75, 23);
            this.btnRefresh.TabIndex = 1;
            this.btnRefresh.Text = "刷新";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 451);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.listViewFiles);
            this.Name = "MainForm";
            this.Text = "音频文件播放器";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.ResumeLayout(false);

            // 添加搜索文本框
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.txtSearch.Location = new System.Drawing.Point(12, 418);
            this.txtSearch.Size = new System.Drawing.Size(200, 21);
            this.txtSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtSearch_KeyDown);

            // 添加搜索按钮
            this.btnSearch = new System.Windows.Forms.Button();
            this.btnSearch.Location = new System.Drawing.Point(218, 418);
            this.btnSearch.Size = new System.Drawing.Size(75, 23);
            this.btnSearch.Text = "搜索";
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);

            // 调整原有刷新按钮位置
            this.btnRefresh.Location = new System.Drawing.Point(697, 418);

            // 将新控件添加到窗体
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.btnSearch);

            // 添加双击事件
            this.listViewFiles.DoubleClick += new System.EventHandler(this.listViewFiles_DoubleClick);

            // 调整窗体大小
            this.ClientSize = new System.Drawing.Size(784, 520);
        }






    }
}

