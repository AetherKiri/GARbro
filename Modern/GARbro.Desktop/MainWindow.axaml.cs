using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using GameRes;

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

        public MainWindow ()
        {
            InitializeComponent();
            DataContext = this;
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

        private void OpenPath (string path)
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
                TryOpenArchive (Path.GetFullPath (path));
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

        private void TryOpenArchive (string path)
        {
            try
            {
                while (VFS.IsVirtual)
                    VFS.ChDir ("..");
                VFS.ChDir (path);
                m_archive = VFS.CurrentArchive;
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
            catch (Exception error)
            {
                Status = "Unable to open archive: " + error.Message;
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
                TryOpenArchive (SelectedItem.FullPath);
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
        private void SetField<T> (ref T field, T value, [CallerMemberName] string name = null)
        {
            if (Equals (field, value)) return;
            field = value;
            OnPropertyChanged (name);
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
}
