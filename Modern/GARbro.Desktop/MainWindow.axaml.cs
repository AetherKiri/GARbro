using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GameRes;
using GameRes.Formats.KiriKiri;

namespace GARbro.Desktop
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ArcFile m_archive;
        private ResourceItem m_selectedItem;
        private Bitmap m_previewBitmap;
        private string m_previewText;
        private string m_pathText = Directory.GetCurrentDirectory();
        private string m_status = "Open a folder or archive to begin.";
        private string m_openingPath;

        public MainWindow ()
        {
            InitializeComponent();
            DataContext = this;
            FormatCatalog.Instance.ParametersRequest += OnParametersRequest;
            Closed += (sender, args) => FormatCatalog.Instance.ParametersRequest -= OnParametersRequest;
            LoadDirectory (PathText);
        }

        public ObservableCollection<ResourceItem> Entries { get; } = new ObservableCollection<ResourceItem>();

        public string PathText { get => m_pathText; set => SetField (ref m_pathText, value); }
        public string Status { get => m_status; private set => SetField (ref m_status, value); }
        public Bitmap PreviewBitmap { get => m_previewBitmap; private set { SetField (ref m_previewBitmap, value); OnPropertyChanged (nameof (HasPreviewImage)); } }
        public string PreviewText { get => m_previewText; private set { SetField (ref m_previewText, value); OnPropertyChanged (nameof (HasPreviewText)); } }
        public bool HasPreviewImage => PreviewBitmap != null;
        public bool HasPreviewText => !string.IsNullOrEmpty (PreviewText);
        public bool HasArchive => m_archive != null;
        public ResourceItem SelectedItem { get => m_selectedItem; set => SetField (ref m_selectedItem, value); }

        private void OpenPath_Click (object sender, RoutedEventArgs e) => OpenPath (PathText);
        private void Home_Click (object sender, RoutedEventArgs e) => OpenPath (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile));
        private void WorkingFolder_Click (object sender, RoutedEventArgs e) => OpenPath (Directory.GetCurrentDirectory());

        private async void LoadXp3Profiles_Click (object sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync (new FilePickerOpenOptions {
                AllowMultiple = false,
                Title = "Load XP3 profiles",
                FileTypeFilter = new[] { new FilePickerFileType ("JSON") { Patterns = new[] { "*.json" } } },
            });
            var path = files.FirstOrDefault()?.TryGetLocalPath();
            if (string.IsNullOrEmpty (path))
                return;
            try
            {
                Xp3Opener.LoadModernSchemeProfiles (path);
                Status = "Loaded XP3 profiles.";
            }
            catch (Exception error)
            {
                Status = "Unable to load XP3 profiles: " + error.Message;
            }
        }

        private void Back_Click (object sender, RoutedEventArgs e)
        {
            if (m_archive != null)
            {
                while (VFS.IsVirtual)
                    VFS.ChDir ("..");
                m_archive = null;
                OnPropertyChanged (nameof (HasArchive));
            }
            var parent = Directory.GetParent (PathText);
            if (parent != null)
                OpenPath (parent.FullName);
        }

        private void Entry_DoubleTapped (object sender, RoutedEventArgs e) => OpenSelected ();

        private async void OpenPath (string path)
        {
            if (Directory.Exists (path))
            {
                m_archive = null;
                OnPropertyChanged (nameof (HasArchive));
                LoadDirectory (Path.GetFullPath (path));
                return;
            }
            if (File.Exists (path))
            {
                await TryOpenArchiveAsync (Path.GetFullPath (path));
                return;
            }
            Status = "Path not found: " + path;
        }

        private void LoadDirectory (string path)
        {
            ClearPreview();
            Entries.Clear();
            foreach (var directory in new DirectoryInfo (path).EnumerateDirectories().OrderBy (item => item.Name, StringComparer.OrdinalIgnoreCase))
                Entries.Add (ResourceItem.FromDirectory (directory));
            foreach (var file in new DirectoryInfo (path).EnumerateFiles().OrderBy (item => item.Name, StringComparer.OrdinalIgnoreCase))
                Entries.Add (ResourceItem.FromFile (file));
            PathText = path;
            Status = Entries.Count + " items";
        }

        private async Task TryOpenArchiveAsync (string path)
        {
            Status = "Opening archive...";
            try
            {
                m_openingPath = path;
                m_archive = await Task.Run (() => OpenArchive (path));
                if (m_archive == null)
                {
                    Status = "Unsupported archive: " + path;
                    return;
                }
                ClearPreview();
                Entries.Clear();
                foreach (var entry in m_archive.Dir.OrderBy (entry => entry.Name, StringComparer.OrdinalIgnoreCase))
                    Entries.Add (ResourceItem.FromArchiveEntry (entry));
                PathText = path;
                OnPropertyChanged (nameof (HasArchive));
                Status = m_archive.Tag + " archive, " + Entries.Count + " entries";
            }
            catch (OperationCanceledException)
            {
                Status = "Archive opening canceled.";
            }
            catch (Exception error)
            {
                Status = "Unable to open archive: " + error.Message;
            }
            finally
            {
                m_openingPath = null;
            }
        }

        private static ArcFile OpenArchive (string path)
        {
            while (VFS.IsVirtual)
                VFS.ChDir ("..");
            VFS.ChDir (path);
            return VFS.CurrentArchive;
        }

        private void OnParametersRequest (object sender, ParametersRequestEventArgs args)
        {
            if (!(sender is Xp3Opener))
                return;

            var completion = new TaskCompletionSource<Xp3SchemeSelection> (TaskCreationOptions.RunContinuationsAsynchronously);
            Dispatcher.UIThread.Post (async () => {
                try
                {
                    var dialog = new Xp3SchemeDialog (Xp3Opener.ModernSchemeNames, args.Notice, GetGameTitle (m_openingPath));
                    completion.TrySetResult (await dialog.ShowDialog<Xp3SchemeSelection> (this));
                }
                catch (Exception error)
                {
                    completion.TrySetException (error);
                }
            });

            var selected = completion.Task.GetAwaiter().GetResult();
            if (selected == null)
                return;
            args.Options = new Xp3Options {
                AutoDetect = selected.AutoDetect,
                Scheme = selected.AutoDetect ? null : Xp3Opener.GetScheme (selected.Scheme),
            };
            args.InputResult = true;
        }

        private static string GetGameTitle (string archivePath)
        {
            if (string.IsNullOrEmpty (archivePath))
                return null;
            try
            {
                var directory = Path.GetDirectoryName (archivePath);
                if (string.IsNullOrEmpty (directory))
                    return null;
                var executable = Directory.EnumerateFiles (directory, "*.exe")
                    .OrderBy (path => path, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (string.IsNullOrEmpty (executable))
                    return null;
                var version = FileVersionInfo.GetVersionInfo (executable);
                if (!string.IsNullOrWhiteSpace (version.ProductName))
                    return version.ProductName;
                if (!string.IsNullOrWhiteSpace (version.FileDescription))
                    return version.FileDescription;
                return Path.GetFileNameWithoutExtension (executable);
            }
            catch
            {
                return null;
            }
        }

        private void OpenSelected ()
        {
            if (SelectedItem == null)
                return;
            if (SelectedItem.IsDirectory)
            {
                OpenPath (SelectedItem.FullPath);
                return;
            }
            if (m_archive == null)
            {
                if (TryPreviewFile (SelectedItem.FullPath))
                    return;
                OpenPath (SelectedItem.FullPath);
                return;
            }
            TryPreviewArchiveEntry (SelectedItem.Entry);
        }

        private bool TryPreviewFile (string path)
        {
            try
            {
                using (var input = new BinaryStream (File.OpenRead (path), path))
                    return TryPreviewStream (input, path);
            }
            catch
            {
                return false;
            }
        }

        private void TryPreviewArchiveEntry (Entry entry)
        {
            try
            {
                using (var input = m_archive.OpenEntry (entry))
                using (var binary = new BinaryStream (input, entry.Name))
                {
                    if (!TryPreviewStream (binary, entry.Name))
                        Status = "No preview for " + entry.Name;
                }
            }
            catch (Exception error)
            {
                Status = "Preview failed: " + error.Message;
            }
        }

        private bool TryPreviewStream (IBinaryStream input, string name)
        {
            input.Position = 0;
            var image = ImageFormat.Read (input);
            if (image != null)
            {
                using (var png = new MemoryStream())
                {
                    ImageFormat.Png.Write (png, image);
                    png.Position = 0;
                    PreviewBitmap = new Bitmap (png);
                    PreviewText = null;
                    Status = name + " (" + image.Width + " x " + image.Height + ")";
                    return true;
                }
            }

            input.Position = 0;
            using (var reader = new StreamReader (input.AsStream, Encoding.UTF8, true, 4096, true))
            {
                var buffer = new char[8192];
                var count = reader.Read (buffer, 0, buffer.Length);
                if (count == 0 || buffer.Take (count).Count (character => character == '\0') > 2)
                    return false;
                PreviewText = new string (buffer, 0, count);
                PreviewBitmap = null;
                Status = name;
                return true;
            }
        }

        private async void Extract_Click (object sender, RoutedEventArgs e)
        {
            if (m_archive == null || SelectedItem?.Entry == null)
                return;
            var folders = await StorageProvider.OpenFolderPickerAsync (new FolderPickerOpenOptions { AllowMultiple = false, Title = "Extract to" });
            var target = folders.FirstOrDefault()?.TryGetLocalPath();
            if (string.IsNullOrEmpty (target))
                return;
            var destination = GetSafeDestination (target, SelectedItem.Entry.Name);
            if (destination == null)
            {
                Status = "Unsafe archive entry path.";
                return;
            }
            Directory.CreateDirectory (Path.GetDirectoryName (destination));
            using (var input = m_archive.OpenEntry (SelectedItem.Entry))
            using (var output = File.Create (destination))
                await input.CopyToAsync (output);
            Status = "Extracted " + SelectedItem.Entry.Name;
        }

        private static string GetSafeDestination (string root, string entryName)
        {
            var fullRoot = Path.GetFullPath (root);
            var destination = Path.GetFullPath (Path.Combine (fullRoot, entryName.Replace ('/', Path.DirectorySeparatorChar).Replace ('\\', Path.DirectorySeparatorChar)));
            var prefix = fullRoot.EndsWith (Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ? fullRoot : fullRoot + Path.DirectorySeparatorChar;
            return destination.StartsWith (prefix, StringComparison.Ordinal) ? destination : null;
        }

        private void ClearPreview ()
        {
            PreviewBitmap?.Dispose();
            PreviewBitmap = null;
            PreviewText = null;
        }

        private event PropertyChangedEventHandler ViewModelPropertyChanged;
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add => ViewModelPropertyChanged += value;
            remove => ViewModelPropertyChanged -= value;
        }
        private void OnPropertyChanged ([CallerMemberName] string name = null) => ViewModelPropertyChanged?.Invoke (this, new PropertyChangedEventArgs (name));
        private bool SetField<T> (ref T field, T value, [CallerMemberName] string name = null)
        {
            if (Equals (field, value)) return false;
            field = value;
            OnPropertyChanged (name);
            return true;
        }
    }

    public sealed class ResourceItem
    {
        public string Name { get; private set; }
        public string Type { get; private set; }
        public string SizeText { get; private set; }
        public string FullPath { get; private set; }
        public Entry Entry { get; private set; }
        public bool IsDirectory { get; private set; }

        public static ResourceItem FromDirectory (DirectoryInfo directory) => new ResourceItem { Name = directory.Name, Type = "Folder", FullPath = directory.FullName, IsDirectory = true };
        public static ResourceItem FromFile (FileInfo file) => new ResourceItem { Name = file.Name, Type = "File", FullPath = file.FullName, SizeText = FormatSize (file.Length) };
        public static ResourceItem FromArchiveEntry (Entry entry) => new ResourceItem { Name = entry.Name, Type = entry.Type, Entry = entry, SizeText = FormatSize (entry.Size) };

        private static string FormatSize (long size) => size < 1024 ? size + " B" : (size / 1024d).ToString ("0.0") + " KB";
    }

    internal sealed class Xp3SchemeSelection
    {
        public bool AutoDetect { get; }
        public string Scheme { get; }

        public Xp3SchemeSelection (string scheme)
        {
            Scheme = scheme;
        }

        Xp3SchemeSelection ()
        {
            AutoDetect = true;
        }

        public static Xp3SchemeSelection Automatic { get; } = new Xp3SchemeSelection();
    }

    internal sealed class Xp3SchemeDialog : Window
    {
        readonly ComboBox m_schemes;

        public Xp3SchemeDialog (System.Collections.Generic.IEnumerable<string> schemes, string notice, string gameTitle)
        {
            Title = "XP3 encryption";
            Width = 520;
            MinWidth = 440;
            CanResize = false;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            SizeToContent = SizeToContent.Height;

            var items = schemes.Select (scheme => new Xp3SchemeChoice (scheme, Xp3Opener.GetModernSchemeDisplayName (scheme))).ToArray();
            var defaultIndex = Array.FindIndex (items, item => item.Scheme == "NoCrypt");
            m_schemes = new ComboBox {
                ItemsSource = items,
                SelectedIndex = defaultIndex >= 0 ? defaultIndex : 0,
                MinHeight = 36,
            };

            var content = new Grid {
                Margin = new Thickness (28, 24),
                RowDefinitions = new RowDefinitions ("Auto,18,Auto,24,Auto"),
            };

            var heading = new StackPanel { Spacing = 6 };
            heading.Children.Add (new TextBlock {
                Text = string.IsNullOrEmpty (gameTitle) ? "Encrypted XP3 archive" : gameTitle,
                FontSize = 20,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
            });
            if (!string.IsNullOrEmpty (gameTitle))
                heading.Children.Add (new TextBlock { Text = "Encrypted XP3 archive", Opacity = 0.7 });
            heading.Children.Add (new TextBlock {
                Text = notice,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Opacity = 0.7,
            });
            content.Children.Add (heading);

            var selection = new StackPanel { Spacing = 8 };
            selection.Children.Add (new TextBlock {
                Text = "Encryption scheme",
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
            });
            selection.Children.Add (m_schemes);
            Grid.SetRow (selection, 2);
            content.Children.Add (selection);

            var actions = new Grid {
                ColumnDefinitions = new ColumnDefinitions ("*,Auto,Auto,Auto"),
                ColumnSpacing = 8,
            };
            var cancel = new Button { Content = "Cancel", MinWidth = 80 };
            cancel.Click += (sender, args) => Close (null);
            Grid.SetColumn (cancel, 1);

            var detect = new Button { Content = "Detect automatically", MinWidth = 150 };
            detect.Click += (sender, args) => Close (Xp3SchemeSelection.Automatic);
            detect.IsVisible = !notice.StartsWith ("Automatic detection", StringComparison.Ordinal);
            Grid.SetColumn (detect, 2);

            var open = new Button { Content = "Open", MinWidth = 80, IsDefault = true };
            open.Click += (sender, args) => Close (new Xp3SchemeSelection ((m_schemes.SelectedItem as Xp3SchemeChoice)?.Scheme));
            Grid.SetColumn (open, 3);
            actions.Children.Add (cancel);
            actions.Children.Add (detect);
            actions.Children.Add (open);
            Grid.SetRow (actions, 4);
            content.Children.Add (actions);
            Content = content;
        }
    }

    internal sealed class Xp3SchemeChoice
    {
        public string Scheme { get; }
        public string DisplayName { get; }

        public Xp3SchemeChoice (string scheme, string displayName)
        {
            Scheme = scheme;
            DisplayName = displayName;
        }

        public override string ToString () => DisplayName;
    }
}
