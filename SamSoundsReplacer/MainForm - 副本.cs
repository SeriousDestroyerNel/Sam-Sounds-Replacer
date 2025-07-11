using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Forms;
using NAudio.Wave; // 需要安装NAudio NuGet包
using System.Diagnostics;
using System.Drawing;
using System.Reflection.Emit;

namespace SamSoundsReplacer
{
    public partial class MainForm : Form
    {
        private List<AudioFile> audioFiles = new List<AudioFile>();
        private SoundPlayer soundPlayer;
        private const string RootFolderName = "Content"; // 最大父文件夹名称
                                                         
        private TextBox txtSearch;
        private Button btnSearch;
        private string currentSearchText = "";

        private System.Windows.Forms.Label lblAudioInfo; // 显示音频信息的标签
        private string configFilePath = "config.txt";
        string modLoadPath;
        string modFilePath;

        public MainForm()
        {
            InitializeComponent();
            soundPlayer = new SoundPlayer();
            InitializeAudioInfoLabel();
            CheckConfigFile();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadFileSystem();
            DisplayFiles();
        }

        private void LoadFileSystem()
        {
            audioFiles.Clear();

            // 获取应用程序所在目录
            string appDirectory = Application.StartupPath;
            string rootFolderPath = Path.Combine(appDirectory, RootFolderName);

            if (!Directory.Exists(rootFolderPath))
            {
                MessageBox.Show($"找不到根目录: {rootFolderPath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 递归遍历所有子目录
            ScanDirectory(rootFolderPath);
        }

        private void ScanDirectory(string currentDirectory)
        {
            try
            {
                // 获取应用程序所在目录
                string appDirectory = Application.StartupPath;
                string rootFolderPath = Path.Combine(appDirectory, RootFolderName);

                // 获取当前目录下的所有文件
                foreach (string filePath in Directory.GetFiles(currentDirectory))
                {
                    string fileName = Path.GetFileName(filePath);
                    string parentFolder = Path.GetFileName(Path.GetDirectoryName(filePath));

                    // 计算相对于Content目录的相对路径
                    string relativePath = filePath.Substring(rootFolderPath.Length).TrimStart(Path.DirectorySeparatorChar);

                    audioFiles.Add(new AudioFile
                    {
                        FullPath = filePath,
                        FileName = fileName,
                        ParentFolder = parentFolder,
                        RelativePath = relativePath  // 新增相对路径属性
                    });
                }

                // 递归处理子目录
                foreach (string subDirectory in Directory.GetDirectories(currentDirectory))
                {
                    ScanDirectory(subDirectory);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // 跳过没有权限访问的目录
            }
            catch (Exception ex)
            {
                MessageBox.Show($"扫描目录时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplayFiles()
        {
            listViewFiles.Items.Clear();

            foreach (var audioFile in audioFiles)
            {
                var item = new ListViewItem(audioFile.ParentFolder);
                item.SubItems.Add(audioFile.FileName);
                item.SubItems.Add(audioFile.RelativePath);  // 显示相对路径而不是完整路径
                listViewFiles.Items.Add(item);
            }
        }

        private void listViewFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count == 0)
                return;

            string filePath = listViewFiles.SelectedItems[0].SubItems[2].Text;
            string fullPath = Path.Combine(Application.StartupPath, "Content", filePath);

            try
            {
                // 播放音频
                soundPlayer.Stop();
                soundPlayer.SoundLocation = fullPath;
                soundPlayer.Play();

                // 显示音频信息
                DisplayAudioInfo(fullPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法播放文件: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadFileSystem();
            if (!string.IsNullOrEmpty(currentSearchText))
            {
                FilterFiles(currentSearchText);
            }
            else
            {
                DisplayFiles();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            soundPlayer.Stop();
            soundPlayer.Dispose();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            currentSearchText = txtSearch.Text.Trim();
            FilterFiles(currentSearchText);
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                currentSearchText = txtSearch.Text.Trim();
                FilterFiles(currentSearchText);
            }
        }

        private void FilterFiles(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                DisplayFiles(); // 显示所有文件
                return;
            }

            listViewFiles.Items.Clear();

            var filteredFiles = audioFiles.Where(f =>
                f.FileName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                f.ParentFolder.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                f.RelativePath.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var audioFile in filteredFiles)
            {
                var item = new ListViewItem(audioFile.ParentFolder);
                item.SubItems.Add(audioFile.FileName);
                item.SubItems.Add(audioFile.RelativePath);
                listViewFiles.Items.Add(item);
            }
        }
        private void InitializeAudioInfoLabel()
        {
            lblAudioInfo = new System.Windows.Forms.Label();
            lblAudioInfo.AutoSize = true;
            lblAudioInfo.Location = new Point(12, 450);
            lblAudioInfo.Size = new Size(760, 60);
            this.Controls.Add(lblAudioInfo);

            // 调整窗体大小以适应新控件
            this.ClientSize = new Size(784, 520);
        }



        private void DisplayAudioInfo(string filePath)
        {
            try
            {
                using (var waveStream = new WaveFileReader(filePath))
                {
                    var info = new AudioInfo
                    {
                        SampleRate = waveStream.WaveFormat.SampleRate,
                        BitsPerSample = waveStream.WaveFormat.BitsPerSample,
                        Channels = waveStream.WaveFormat.Channels,
                        Duration = waveStream.TotalTime
                    };

                    lblAudioInfo.Text = $"采样率: {info.SampleRate}Hz | " +
                                      $"位深度: {info.BitsPerSample}bit | " +
                                      $"声道: {info.Channels} | " +
                                      $"时长: {info.Duration:mm\\:ss}";
                }
            }
            catch (Exception ex)
            {
                lblAudioInfo.Text = "无法读取音频信息";
                Debug.WriteLine($"读取音频信息失败: {ex.Message}");
            }
        }

      

        private void CheckConfigFile()
        {
            if (File.Exists(configFilePath))
            {
                var lines = File.ReadAllLines(configFilePath);
                modFilePath = lines[0];
                modLoadPath = lines[1];
              

            }
            else
            {
                CreateConfigFile();
            }
        }

        private void CreateConfigFile()
        {
            MessageBox.Show("Choose your Sam game disk.", "", MessageBoxButtons.OK);
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedPath = folderBrowserDialog.SelectedPath;
                modFilePath = SamSoundsReplacer.GetPath.SteamPath(selectedPath);
                string steamPath = Path.GetFullPath(Path.Combine(modFilePath, @"..\..\..\"));
                modLoadPath = Path.Combine(steamPath, @"common\Serious Sam Fusion 2017\Content");

                if (modFilePath != null)
                {
                    File.WriteAllLines(configFilePath, new string[] { modFilePath, modLoadPath });
                }
                else
                {
                    MessageBox.Show("Error finding game files");
                }
            }

        }
        private void listViewFiles_DoubleClick(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count == 0)
                return;

            // 获取选中的文件信息
            string relativePath = listViewFiles.SelectedItems[0].SubItems[2].Text;
            string destinationPath = Path.Combine(modLoadPath, relativePath);
            string destinationDir = Path.GetDirectoryName(destinationPath);

            // 打开文件选择对话框
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "WAV音频文件 (*.wav)|*.wav";
            openFileDialog.Title = "选择替换的WAV文件";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 确保目标目录存在
                    Directory.CreateDirectory(destinationDir);

                    // 复制文件到目标位置（覆盖已存在的文件）
                    File.Copy(openFileDialog.FileName, destinationPath, true);

                    MessageBox.Show($"文件已成功替换到:\n{destinationPath}", "成功",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"替换文件时出错:\n{ex.Message}", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


    }

    public class AudioFile
    {
        public string FullPath { get; set; }
        public string FileName { get; set; }
        public string ParentFolder { get; set; }
        public string RelativePath { get; set; }  // 新增相对路径属性
    }
    // 添加音频信息类
    public class AudioInfo
    {
        public int SampleRate { get; set; }
        public int BitsPerSample { get; set; }
        public int Channels { get; set; }
        public TimeSpan Duration { get; set; }
    }




}