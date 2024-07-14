using System.IO;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

namespace SourceGit.Views
{
    public partial class WelcomeToolbar : UserControl
    {
        public WelcomeToolbar()
        {
            InitializeComponent();
        }

        private async void OpenLocalRepository(object _1, RoutedEventArgs e)
        {
            if (!ViewModels.PopupHost.CanCreatePopup())
                return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
                return;

            var options = new FolderPickerOpenOptions() { AllowMultiple = false };
            var selected = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
            if (selected.Count == 1)
                OpenOrInitRepository(selected[0].Path.LocalPath);

            e.Handled = true;
        }

        private void OpenOrInitRepository(string path, ViewModels.RepositoryNode parent = null)
        {
            if (!Directory.Exists(path))
            {
                if (File.Exists(path))
                    path = Path.GetDirectoryName(path);
                else
                    return;
            }

            var root = new Commands.QueryRepositoryRootPath(path).Result();
            if (string.IsNullOrEmpty(root))
            {
                (DataContext as ViewModels.Welcome)?.InitRepository(path, parent);
                return;
            }

            var normalizedPath = root.Replace("\\", "/");
            var node = ViewModels.Preference.FindOrAddNodeByRepositoryPath(normalizedPath, parent, true);
            var launcher = this.FindAncestorOfType<Launcher>()?.DataContext as ViewModels.Launcher;
            launcher?.OpenRepositoryInTab(node, launcher.ActivePage);
        }
    }
}

