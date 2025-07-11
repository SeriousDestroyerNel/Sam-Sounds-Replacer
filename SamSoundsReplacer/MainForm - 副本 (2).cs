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
        private const string ContentFolderName = "Content"; // 最大父文件夹名称
                                                         
        private TextBox txtSearch;
        private Button btnSearch;
        private string currentSearchText = "";

        private System.Windows.Forms.Label lblAudioInfo; // 显示音频信息的标签
        private string configFilePath = "config.txt";
        string modLoadPath;
        string modFilePath;

        private Color replacedColor = Color.Red;
        private Button btnDelete;

        private Dictionary<string, AudioFile> originalFiles = new Dictionary<string, AudioFile>(StringComparer.OrdinalIgnoreCase);
        private const string CacheFileName = "data.txt";


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

        // 修改LoadFileSystem方法
        private void LoadFileSystem()
        {
            audioFiles.Clear();
            originalFiles.Clear();

            string appDirectory = Application.StartupPath;
            string originalRootPath = Path.Combine(appDirectory, ContentFolderName);
            string modRootPath = Path.Combine(modLoadPath, ContentFolderName);

            // 1. 首先加载缓存或扫描原始文件
            if (File.Exists(CacheFileName) && Directory.Exists(originalRootPath))
            {
                LoadFromCache(originalRootPath);
            }
            else if (Directory.Exists(originalRootPath))
            {
                ScanOriginalDirectory(originalRootPath);
                SaveToCache();
            }

            // 2. 只扫描mod目录中与缓存匹配的文件
            if (Directory.Exists(modRootPath))
            {
                ScanModDirectorySelectively(modRootPath);
            }
        }

        // 添加缓存加载方法
        private void LoadFromCache(string originalRootPath)
        {
            try
            {
                foreach (string line in File.ReadAllLines(CacheFileName))
                {
                    string[] parts = line.Split('|');
                    if (parts.Length == 3)
                    {
                        string relativePath = parts[0];
                        string fileName = parts[1];
                        string parentFolder = parts[2];
                        string fullPath = Path.Combine(originalRootPath, relativePath);

                        var audioFile = new AudioFile
                        {
                            FullPath = fullPath,
                            FileName = fileName,
                            ParentFolder = parentFolder,
                            RelativePath = relativePath,
                            IsReplaced = false,
                            SourceType = FileSourceType.Original
                        };

                        originalFiles[relativePath] = audioFile;
                        audioFiles.Add(audioFile);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载缓存失败: {ex.Message}", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                originalFiles.Clear();
                audioFiles.Clear();
            }
        }

        // 添加缓存保存方法
        private void SaveToCache()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(CacheFileName))
                {
                    foreach (var file in originalFiles.Values)
                    {
                        writer.WriteLine($"{file.RelativePath}|{file.FileName}|{file.ParentFolder}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存缓存失败: {ex.Message}", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }



        // 添加选择性扫描mod目录的方法
        private void ScanModDirectorySelectively(string modRootPath)
        {
            try
            {
                foreach (var originalFile in originalFiles.Values)
                {
                    string modFilePath = Path.Combine(modRootPath, originalFile.RelativePath);

                    if (File.Exists(modFilePath))
                    {
                        // 只处理在原始文件中存在的路径
                        var existingFile = audioFiles.FirstOrDefault(f =>
                            f.RelativePath.Equals(originalFile.RelativePath, StringComparison.OrdinalIgnoreCase));

                        if (existingFile != null)
                        {
                            existingFile.FullPath = modFilePath;
                            existingFile.IsReplaced = true;
                            existingFile.SourceType = FileSourceType.Mod;
                        }
                        else
                        {
                            audioFiles.Add(new AudioFile
                            {
                                FullPath = modFilePath,
                                FileName = originalFile.FileName,
                                ParentFolder = originalFile.ParentFolder,
                                RelativePath = originalFile.RelativePath,
                                IsReplaced = true,
                                SourceType = FileSourceType.Mod
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"扫描mod目录时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 修改原始目录扫描方法
        private void ScanOriginalDirectory(string originalRootPath)
        {
            try
            {
                foreach (string filePath in Directory.GetFiles(originalRootPath, "*", SearchOption.AllDirectories))
                {
                    string fileName = Path.GetFileName(filePath);
                    string parentFolder = Path.GetFileName(Path.GetDirectoryName(filePath));
                    string relativePath = filePath.Substring(originalRootPath.Length).TrimStart(Path.DirectorySeparatorChar);

                    var audioFile = new AudioFile
                    {
                        FullPath = filePath,
                        FileName = fileName,
                        ParentFolder = parentFolder,
                        RelativePath = relativePath,
                        IsReplaced = false,
                        SourceType = FileSourceType.Original
                    };

                    originalFiles[relativePath] = audioFile;
                    audioFiles.Add(audioFile);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"扫描原始目录时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                // 如果是替换文件，设置为红色
                if (audioFile.IsReplaced)
                {
                    item.ForeColor = replacedColor;
                }

                item.Tag = audioFile;
                listViewFiles.Items.Add(item);
            }
        }

        private void listViewFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count == 0)
            {
                btnDelete.Enabled = false;
                return;
            }

            var audioFile = listViewFiles.SelectedItems[0].Tag as AudioFile;
            if (audioFile == null)
                return;
            btnDelete.Enabled = (audioFile != null && audioFile.IsReplaced);
            try
            {
                // 播放音频
                soundPlayer.Stop();
                soundPlayer.SoundLocation = audioFile.FullPath;
                soundPlayer.Play();

                // 显示音频信息
                DisplayAudioInfo(audioFile.FullPath);
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
                modLoadPath = Path.Combine(steamPath, @"common\Serious Sam Fusion 2017");

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
            var selectedItem = listViewFiles.SelectedItems[0];
            var audioFile = selectedItem.Tag as AudioFile;
            if (audioFile == null)
                return;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "WAV音频文件 (*.wav)|*.wav";
            openFileDialog.Title = "选择替换的WAV文件";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string destinationPath = Path.Combine(modLoadPath, ContentFolderName, audioFile.RelativePath);
                    string destinationDir = Path.GetDirectoryName(destinationPath);

                    // 确保目标目录存在
                    Directory.CreateDirectory(destinationDir);

                    // 复制文件到目标位置（覆盖已存在的文件）
                    File.Copy(openFileDialog.FileName, destinationPath, true);

                    // 更新文件状态
                    audioFile.FullPath = destinationPath;
                    audioFile.IsReplaced = true;
                    audioFile.SourceType = FileSourceType.Mod;

                    // 更新列表显示
                    selectedItem.ForeColor = replacedColor;

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

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count == 0)
                return;

            var selectedItem = listViewFiles.SelectedItems[0];
            var audioFile = selectedItem.Tag as AudioFile;

            if (audioFile == null || !audioFile.IsReplaced)
                return;

            DialogResult result = MessageBox.Show(
                $"确定要删除替换文件吗？\n{audioFile.RelativePath}",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // 删除mod路径下的文件
                    File.Delete(audioFile.FullPath);

                    // 只更新当前文件状态，不重载整个列表
                    var originalFile = originalFiles[audioFile.RelativePath];
                    audioFile.FullPath = originalFile.FullPath;
                    audioFile.IsReplaced = false;
                    audioFile.SourceType = FileSourceType.Original;

                    // 更新列表显示
                    selectedItem.ForeColor = Color.Black;
                    btnDelete.Enabled = false;

                    MessageBox.Show("文件已成功删除", "成功",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除文件时出错:\n{ex.Message}", "错误",
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
        public string RelativePath { get; set; }
        public bool IsReplaced { get; set; }
        public FileSourceType SourceType { get; set; }
    }
    public enum FileSourceType
    {
        Original,
        Mod
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