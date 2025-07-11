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
                MessageBox.Show($"Fail to load Cache: {ex.Message}", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                MessageBox.Show($"Fail to save Cache: {ex.Message}", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                MessageBox.Show($"Error Scanning: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show($"Error Scanning: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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



        // 修改列表视图事件处理，确保总是使用Tag中的AudioFile对象
        private void listViewFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count == 0)
            {
                btnDelete.Enabled = false;
                return;
            }

            var selectedItem = listViewFiles.SelectedItems[0];
            var audioFile = selectedItem.Tag as AudioFile;

            // 更新删除按钮状态
            btnDelete.Enabled = (audioFile != null && audioFile.IsReplaced);
            DisplayAudioInfo(audioFile.FullPath);

            // 播放选中的文件
            if (audioFile != null)
            {
                try
                {
                    soundPlayer.Stop();
                    soundPlayer.SoundLocation = audioFile.FullPath;
                    soundPlayer.Play();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fail to play: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadFileSystem();

            // 重新应用当前筛选条件
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

        // 修改FilterFiles方法，确保保留完整的文件信息
        private void FilterFiles(string searchText)
        {
            listViewFiles.Items.Clear();

            var filteredFiles = string.IsNullOrWhiteSpace(searchText) ?
                audioFiles :
                audioFiles.Where(f =>
                    f.FileName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    f.ParentFolder.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    f.RelativePath.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var audioFile in filteredFiles)
            {
                var item = new ListViewItem(audioFile.ParentFolder);
                item.SubItems.Add(audioFile.FileName);
                item.SubItems.Add(audioFile.RelativePath);

                // 保留完整的AudioFile对象引用
                item.Tag = audioFile;

                // 根据替换状态设置颜色
                if (audioFile.IsReplaced)
                {
                    item.ForeColor = replacedColor;
                }

                listViewFiles.Items.Add(item);
            }
        }


        private void InitializeAudioInfoLabel()
        {
            lblAudioInfo = new System.Windows.Forms.Label();
            lblAudioInfo.AutoSize = true;
            lblAudioInfo.Location = new Point(12, 600);
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

                    lblAudioInfo.Text = $"Sample Rate: {info.SampleRate}Hz | " +
                                      $"Bit Depth: {info.BitsPerSample}bit | " +
                                      $"Channel Count: {info.Channels} | " +
                                      $"Duration: {info.Duration:mm\\:ss\\.ff}";
                }
            }
            catch (Exception ex)
            {
                lblAudioInfo.Text = "Fail to read audio info";
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

            var selectedItem = listViewFiles.SelectedItems[0];
            var audioFile = selectedItem.Tag as AudioFile;

            if (audioFile == null)
                return;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "WAV file (*.wav)|*.wav";
            openFileDialog.Title = "Choose your sound file";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string destinationPath = Path.Combine(modLoadPath, ContentFolderName, audioFile.RelativePath);
                    string destinationDir = Path.GetDirectoryName(destinationPath);

                    Directory.CreateDirectory(destinationDir);
                    File.Copy(openFileDialog.FileName, destinationPath, true);

                    // 更新文件状态
                    audioFile.FullPath = destinationPath;
                    audioFile.IsReplaced = true;
                    audioFile.SourceType = FileSourceType.Mod;

                    // 更新列表显示
                    selectedItem.ForeColor = replacedColor;
                    btnDelete.Enabled = true;

                    MessageBox.Show($"Replacement done:\n{destinationPath}", "成功",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Replacement failed:\n{ex.Message}", "错误",
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
                $"Are you sure to remove this replacement？\n{audioFile.FullPath}",
                "Yes",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // 1. 删除mod路径下的文件
                    File.Delete(audioFile.FullPath);

                    // 2. 恢复原始文件路径
                    string originalPath = Path.Combine(Application.StartupPath, ContentFolderName, audioFile.RelativePath);

                    // 3. 更新文件状态
                    audioFile.FullPath = originalPath;
                    audioFile.IsReplaced = false;
                    audioFile.SourceType = FileSourceType.Original;

                    // 4. 更新列表显示
                    selectedItem.ForeColor = Color.Black;
                    selectedItem.SubItems[2].Text = audioFile.RelativePath; // 更新路径显示
                    btnDelete.Enabled = false;

                    MessageBox.Show("Replacement removed", "成功",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to remove replacement:\n{ex.Message}", "错误",
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