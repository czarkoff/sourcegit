using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace SourceGit.UI {

    /// <summary>
    ///     Preference window.
    /// </summary>
    public partial class SettingDialog : Window {

        /// <summary>
        ///     Git global user name.
        /// </summary>
        public string GlobalUser {
            get;
            set;
        }

        /// <summary>
        ///     Git global user email.
        /// </summary>
        public string GlobalUserEmail {
            get;
            set;
        }

        /// <summary>
        ///     Git core.autocrlf setting.
        /// </summary>
        public string AutoCRLF {
            get;
            set;
        }

        /// <summary>
        ///     Options for core.autocrlf
        /// </summary>
        public class AutoCRLFOption {
            public string Value { get; set; }
            public string Desc { get; set; }
            
            public AutoCRLFOption(string v, string d) {
                Value = v;
                Desc = d;
            }
        }

        /// <summary>
        ///     Locale setting.
        /// </summary>
        public class Locale {
            public string Value { get; set; }
            public string Desc { get; set; }

            public Locale(string v, string d) {
                Value = v;
                Desc = d;
            }
        }

        /// <summary>
        ///     Avatar server
        /// </summary>
        public class AvatarServer {
            public string Value { get; set; }
            public string Desc { get; set; }

            public AvatarServer(string v, string d) {
                Value = v;
                Desc = d;
            }
        }

        /// <summary>
        ///     Constructor.
        /// </summary>
        public SettingDialog() {
            GlobalUser = GetConfig("user.name");
            GlobalUserEmail = GetConfig("user.email");
            AutoCRLF = GetConfig("core.autocrlf");
            if (string.IsNullOrEmpty(AutoCRLF)) AutoCRLF = "false";

            InitializeComponent();

            var locales = new List<Locale>() {
                new Locale("en_US", "English"),
                new Locale("zh_CN", "简体中文"),
            };
            cmbLang.ItemsSource = locales;
            cmbLang.SelectedItem = locales.Find(o => o.Value == App.Setting.UI.Locale);

            var avatarServers = new List<AvatarServer>() {
                new AvatarServer("https://www.gravatar.com/avatar/", "Gravatar官网"),
                new AvatarServer("https://cdn.s.loli.top/avatar/", "Gravatar中国CDN"),
            };
            cmbAvatarServer.ItemsSource = avatarServers;
            cmbAvatarServer.SelectedItem = avatarServers.Find(o => o.Value == App.Setting.UI.AvatarServer);

            int mergeType = App.Setting.Tools.MergeTool;
            var merger = Git.MergeTool.Supported[mergeType];
            txtMergePath.IsReadOnly = !merger.IsConfigured;
            txtMergeParam.Text = merger.Parameter;

            var crlfOptions = new List<AutoCRLFOption>() {
                new AutoCRLFOption("true", "Commit as LF, checkout as CRLF"),
                new AutoCRLFOption("input", "Only convert for commit"),
                new AutoCRLFOption("false", "Do NOT convert"),
            };
            cmbAutoCRLF.ItemsSource = crlfOptions;
            cmbAutoCRLF.SelectedItem = crlfOptions.Find(o => o.Value == AutoCRLF);
        }

        /// <summary>
        ///     Close this dialog
        /// </summary>
        private void Close(object sender, RoutedEventArgs e) {
            var oldUser = GetConfig("user.name");
            if (oldUser != GlobalUser) SetConfig("user.name", GlobalUser);

            var oldEmail = GetConfig("user.email");
            if (oldEmail != GlobalUserEmail) SetConfig("user.email", GlobalUserEmail);

            var oldAutoCRLF = GetConfig("core.autocrlf");
            if (oldAutoCRLF != AutoCRLF) SetConfig("core.autocrlf", AutoCRLF);

            Close();
        }

        /// <summary>
        ///     Set locale
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeLanguage(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count != 1) return;

            var mode = e.AddedItems[0] as Locale;
            if (mode == null) return;

            App.Setting.UI.Locale = mode.Value;
            App.SaveSetting();
        }

        /// <summary>
        ///     Set avatar server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeAvatarServer(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count != 1) return;

            var s = e.AddedItems[0] as AvatarServer;
            if (s == null) return;

            App.Setting.UI.AvatarServer = s.Value;
            App.SaveSetting();
        }

        /// <summary>
        ///     Select git executable file path.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectGitPath(object sender, RoutedEventArgs e) {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Git Executable|git.exe";
            dialog.FileName = "git.exe";
            dialog.Title = App.Text("Preference.Dialog.GitExe");
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            dialog.CheckFileExists = true;

            if (dialog.ShowDialog() == true) {
                txtGitPath.Text = dialog.FileName;
                App.Setting.Tools.GitExecutable = dialog.FileName;
            }
        }

        /// <summary>
        ///     Set default clone path.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectDefaultClonePath(object sender, RoutedEventArgs e) {
            FolderDialog.Open(App.Text("Preference.Dialog.GitDir"), path => {
                txtGitCloneDir.Text = path;
                App.Setting.Tools.GitDefaultCloneDir = path;
            });
        }

        /// <summary>
        ///     Choose external merge tool.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeMergeTool(object sender, SelectionChangedEventArgs e) {
            if (IsLoaded) {
                var t = Git.MergeTool.Supported[App.Setting.Tools.MergeTool];

                App.Setting.Tools.MergeExecutable = t.Finder();

                txtMergePath.Text = App.Setting.Tools.MergeExecutable;
                txtMergeParam.Text = t.Parameter;
                txtMergePath.IsReadOnly = !t.IsConfigured;
            }
        }

        /// <summary>
        ///     Set merge tool executable file path.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectMergeToolPath(object sender, RoutedEventArgs e) {
            int mergeType = App.Setting.Tools.MergeTool;
            if (mergeType == 0) return;

            var merger = Git.MergeTool.Supported[mergeType];
            var dialog = new OpenFileDialog();
            dialog.Filter = $"{merger.Name} Executable|{merger.ExecutableName}";
            dialog.Title = App.Format("Preference.Dialog.Merger", merger.Name);
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            dialog.CheckFileExists = true;

            if (dialog.ShowDialog() == true) {
                txtMergePath.Text = dialog.FileName;
                App.Setting.Tools.MergeExecutable = dialog.FileName;
            }
        }

        /// <summary>
        ///     Set core.autocrlf
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoCRLFSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count != 1) return;

            var mode = e.AddedItems[0] as AutoCRLFOption;
            if (mode == null) return;

            AutoCRLF = mode.Value;
        }

        #region CONFIG
        private string GetConfig(string key) {
            if (!App.IsGitConfigured) return "";

            var startInfo = new ProcessStartInfo();
            startInfo.FileName = App.Setting.Tools.GitExecutable;
            startInfo.Arguments = $"config --global {key}";
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.StandardOutputEncoding = Encoding.UTF8;

            var proc = new Process() { StartInfo = startInfo };
            proc.Start();
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            proc.Close();

            return output.Trim();
        }

        private void SetConfig(string key, string val) {
            if (!App.IsGitConfigured) return;

            var startInfo = new ProcessStartInfo();
            startInfo.FileName = App.Setting.Tools.GitExecutable;
            startInfo.Arguments = $"config --global {key} \"{val}\"";
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            var proc = new Process() { StartInfo = startInfo };
            proc.Start();
            proc.WaitForExit();
            proc.Close();
        }
        #endregion
    }
}
