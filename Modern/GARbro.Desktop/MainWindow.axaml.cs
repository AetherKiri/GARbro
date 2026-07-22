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
        private string m_audioPreviewPath;
        private string m_audioPreviewText;
        private string m_archivePath;
        private string m_archiveDirectory;
        private bool m_isExtracting;
        private readonly AudioPreviewPlayer m_audioPlayer = new AudioPreviewPlayer();
        private string m_pathText = Directory.GetCurrentDirectory();
        private string m_status = "Open a folder or archive to begin.";
        private string m_openingPath;

        public MainWindow ()
        {
            InitializeComponent();
            DataContext = this;
            FormatCatalog.Instance.ParametersRequest += OnParametersRequest;
            Closed += (sender, args) => {
                FormatCatalog.Instance.ParametersRequest -= OnParametersRequest;
                ClearPreview();
                m_audioPlayer.Dispose();
            };
            LoadDirectory (PathText);
        }

        public ObservableCollection<ResourceItem> Entries { get; } = new ObservableCollection<ResourceItem>();

        public string PathText { get => m_pathText; set => SetField (ref m_pathText, value); }
        public string Status { get => m_status; private set => SetField (ref m_status, value); }
        public Bitmap PreviewBitmap { get => m_previewBitmap; private set { SetField (ref m_previewBitmap, value); OnPropertyChanged (nameof (HasPreviewImage)); } }
        public string PreviewText { get => m_previewText; private set { SetField (ref m_previewText, value); OnPropertyChanged (nameof (HasPreviewText)); } }
        public string AudioPreviewText { get => m_audioPreviewText; private set => SetField (ref m_audioPreviewText, value); }
        public string AudioPlayButtonText => m_audioPlayer.IsPlaying ? "Pause" : "Play";
        public bool HasPreviewImage => PreviewBitmap != null;
        public bool HasPreviewText => !string.IsNullOrEmpty (PreviewText);
        public bool HasPreviewAudio => !string.IsNullOrEmpty (m_audioPreviewPath);
        public bool HasArchive => m_archive != null;
        public string ArchiveLocation => string.IsNullOrEmpty (m_archiveDirectory) ? "/" : "/" + m_archiveDirectory;
        public bool HasSelectedArchiveEntry => m_archive != null && m_selectedItem?.Entry != null;
        public bool CanExtractAll => HasArchive && !m_isExtracting;
        public bool CanExtractSelected => HasSelectedArchiveEntry && !m_isExtracting;
        public ResourceItem SelectedItem
        {
            get => m_selectedItem;
            set
            {
                if (SetField (ref m_selectedItem, value))
                {
                    OnPropertyChanged (nameof (HasSelectedArchiveEntry));
                    OnPropertyChanged (nameof (CanExtractSelected));
                    PreviewSelectedItem();
                }
            }
        }

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
            if (m_isExtracting)
            {
                Status = "Extraction is in progress.";
                return;
            }
            if (m_archive != null)
            {
                if (!string.IsNullOrEmpty (m_archiveDirectory))
                {
                    LoadArchiveDirectory (GetArchiveParentDirectory (m_archiveDirectory));
                    return;
                }
                while (VFS.IsVirtual)
                    VFS.ChDir ("..");
                m_archive = null;
                m_archivePath = null;
                m_archiveDirectory = null;
                OnPropertyChanged (nameof (HasArchive));
                OnPropertyChanged (nameof (ArchiveLocation));
                OnPropertyChanged (nameof (HasSelectedArchiveEntry));
                OnPropertyChanged (nameof (CanExtractAll));
                OnPropertyChanged (nameof (CanExtractSelected));
                SelectedItem = null;
            }
            var parent = Directory.GetParent (PathText);
            if (parent != null)
                OpenPath (parent.FullName);
        }

        private void Entry_DoubleTapped (object sender, RoutedEventArgs e) => OpenSelected ();

        private async void OpenPath (string path)
        {
            if (m_isExtracting)
            {
                Status = "Extraction is in progress.";
                return;
            }
            if (Directory.Exists (path))
            {
                m_archive = null;
                m_archivePath = null;
                m_archiveDirectory = null;
                OnPropertyChanged (nameof (HasArchive));
                OnPropertyChanged (nameof (ArchiveLocation));
                OnPropertyChanged (nameof (HasSelectedArchiveEntry));
                OnPropertyChanged (nameof (CanExtractAll));
                OnPropertyChanged (nameof (CanExtractSelected));
                SelectedItem = null;
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
                m_archivePath = path;
                m_archiveDirectory = string.Empty;
                PathText = path;
                OnPropertyChanged (nameof (HasArchive));
                LoadArchiveDirectory (m_archiveDirectory);
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
                if (m_archive != null && SelectedItem.ArchiveDirectory != null)
                {
                    LoadArchiveDirectory (SelectedItem.ArchiveDirectory);
                    return;
                }
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

        private void PreviewSelectedItem ()
        {
            ClearPreview();
            if (SelectedItem == null || SelectedItem.IsDirectory)
                return;
            if (m_archive == null)
            {
                TryPreviewFile (SelectedItem.FullPath);
                return;
            }
            TryPreviewArchiveEntry (SelectedItem.Entry);
        }

        private void LoadArchiveDirectory (string directory)
        {
            m_archiveDirectory = NormalizeArchivePath (directory);
            var prefix = string.IsNullOrEmpty (m_archiveDirectory) ? string.Empty : m_archiveDirectory + "/";
            var folders = new System.Collections.Generic.SortedDictionary<string, string> (StringComparer.OrdinalIgnoreCase);
            var files = new System.Collections.Generic.List<Entry>();

            foreach (var entry in m_archive.Dir)
            {
                var path = NormalizeArchivePath (entry.Name);
                if (!path.StartsWith (prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                var child = path.Substring (prefix.Length);
                if (string.IsNullOrEmpty (child))
                    continue;
                var separator = child.IndexOf ('/');
                if (separator >= 0)
                {
                    var name = child.Substring (0, separator);
                    if (!folders.ContainsKey (name))
                        folders.Add (name, prefix + name);
                }
                else
                    files.Add (entry);
            }

            ClearPreview();
            Entries.Clear();
            foreach (var folder in folders)
                Entries.Add (ResourceItem.FromArchiveDirectory (folder.Key, folder.Value));
            foreach (var entry in files.OrderBy (entry => entry.Name, StringComparer.OrdinalIgnoreCase))
                Entries.Add (ResourceItem.FromArchiveEntry (entry));
            SelectedItem = null;
            OnPropertyChanged (nameof (ArchiveLocation));
            OnPropertyChanged (nameof (HasSelectedArchiveEntry));
            OnPropertyChanged (nameof (CanExtractAll));
            OnPropertyChanged (nameof (CanExtractSelected));
            Status = m_archive.Tag + " archive " + ArchiveLocation + ", " + Entries.Count + " items";
        }

        private static string NormalizeArchivePath (string path)
        {
            return string.IsNullOrEmpty (path) ? string.Empty : path.Replace ('\\', '/').Trim ('/');
        }

        private static string GetArchiveParentDirectory (string directory)
        {
            var separator = NormalizeArchivePath (directory).LastIndexOf ('/');
            return separator < 0 ? string.Empty : directory.Substring (0, separator);
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
                // XP3 streams are sequential. Decoders and preview retries require seeking.
                using (var input = m_archive.OpenBinaryEntry (entry))
                    if (!TryPreviewStream (input, entry.Name))
                        Status = "No preview for " + entry.Name;
            }
            catch (Exception error)
            {
                Status = "Preview failed: " + error.Message;
            }
        }

        private bool TryPreviewStream (IBinaryStream input, string name)
        {
            input.Position = 0;
            try
            {
                PreviewBitmap = new Bitmap (input.AsStream);
                PreviewText = null;
                DeleteAudioPreview();
                AudioPreviewText = null;
                OnPropertyChanged (nameof (HasPreviewAudio));
                Status = name;
                return true;
            }
            catch
            {
                input.Position = 0;
            }

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
                    DeleteAudioPreview();
                    AudioPreviewText = null;
                    OnPropertyChanged (nameof (HasPreviewAudio));
                    Status = name + " (" + image.Width + " x " + image.Height + ")";
                    return true;
                }
            }

            input.Position = 0;
            using (var sound = AudioFormat.Read (input))
            {
                if (sound != null)
                {
                    SetAudioPreview (sound, name);
                    PreviewBitmap = null;
                    PreviewText = null;
                    Status = name + " (" + sound.SourceFormat + ", " + sound.Format.SamplesPerSecond + " Hz)";
                    return true;
                }
            }

            input.Position = 0;
            if (IsNativeAudioFile (name))
            {
                SetRawAudioPreview (input.AsStream, name);
                PreviewBitmap = null;
                PreviewText = null;
                Status = name + " (audio)";
                return true;
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
                DeleteAudioPreview();
                AudioPreviewText = null;
                OnPropertyChanged (nameof (HasPreviewAudio));
                Status = name;
                return true;
            }
        }

        private void SetAudioPreview (SoundInput sound, string name)
        {
            var extension = sound.SourceFormat is "wav" or "ogg" or "mp3" ? sound.SourceFormat : "wav";
            var path = CreateAudioPreviewPath (extension);
            using (var output = File.Create (path))
            {
                if (extension == sound.SourceFormat && sound.Source.CanSeek)
                {
                    sound.Source.Position = 0;
                    sound.Source.CopyTo (output);
                }
                else
                    AudioFormat.Wav.Write (sound, output);
            }

            DeleteAudioPreview();
            m_audioPreviewPath = path;
            AudioPreviewText = name + " - " + sound.Format.SamplesPerSecond + " Hz, " + sound.Format.Channels + " ch";
            OnPropertyChanged (nameof (HasPreviewAudio));
        }

        private void SetRawAudioPreview (Stream input, string name)
        {
            var extension = Path.GetExtension (name).TrimStart ('.').ToLowerInvariant();
            var path = CreateAudioPreviewPath (extension);
            using (var output = File.Create (path))
                input.CopyTo (output);
            DeleteAudioPreview();
            m_audioPreviewPath = path;
            AudioPreviewText = name + " - audio";
            OnPropertyChanged (nameof (HasPreviewAudio));
        }

        private static string CreateAudioPreviewPath (string extension)
        {
            var directory = Path.Combine (Path.GetTempPath(), "GARbro", "audio-preview");
            Directory.CreateDirectory (directory);
            return Path.Combine (directory, Guid.NewGuid().ToString ("N") + "." + extension);
        }

        private static bool IsNativeAudioFile (string name)
        {
            var extension = Path.GetExtension (name);
            return extension.Equals (".wav", StringComparison.OrdinalIgnoreCase)
                || extension.Equals (".ogg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals (".mp3", StringComparison.OrdinalIgnoreCase)
                || extension.Equals (".flac", StringComparison.OrdinalIgnoreCase)
                || extension.Equals (".m4a", StringComparison.OrdinalIgnoreCase)
                || extension.Equals (".aac", StringComparison.OrdinalIgnoreCase);
        }

        private void PlayAudio_Click (object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty (m_audioPreviewPath) || !File.Exists (m_audioPreviewPath))
                return;
            try
            {
                m_audioPlayer.Toggle (m_audioPreviewPath);
                OnPropertyChanged (nameof (AudioPlayButtonText));
                Status = m_audioPlayer.IsPlaying ? "Playing audio." : "Audio paused.";
            }
            catch (Exception error)
            {
                Status = "Unable to play audio: " + error.Message;
            }
        }

        private void StopAudio_Click (object sender, RoutedEventArgs e)
        {
            m_audioPlayer.Stop();
            OnPropertyChanged (nameof (AudioPlayButtonText));
            Status = "Audio stopped.";
        }

        private async void ExtractSelected_Click (object sender, RoutedEventArgs e)
        {
            if (m_archive == null || SelectedItem?.Entry == null)
                return;
            await ExtractEntriesAsync (new[] { SelectedItem.Entry });
        }

        private async void ExtractAll_Click (object sender, RoutedEventArgs e)
        {
            if (m_archive == null)
                return;
            await ExtractEntriesAsync (m_archive.Dir);
        }

        private async Task ExtractEntriesAsync (System.Collections.Generic.IEnumerable<Entry> entries)
        {
            var selectedEntries = entries.Where (entry => entry.Offset >= 0).ToArray();
            if (selectedEntries.Length == 0)
            {
                Status = "No archive entries to extract.";
                return;
            }
            var folders = await StorageProvider.OpenFolderPickerAsync (new FolderPickerOpenOptions { AllowMultiple = false, Title = "Extract to" });
            var target = folders.FirstOrDefault()?.TryGetLocalPath();
            if (string.IsNullOrEmpty (target))
                return;
            var archive = m_archive;
            Status = "Extracting " + selectedEntries.Length + " files...";
            m_isExtracting = true;
            OnPropertyChanged (nameof (CanExtractAll));
            OnPropertyChanged (nameof (CanExtractSelected));
            try
            {
                var result = await Task.Run (() => ExtractEntries (archive, target, selectedEntries));
                Status = "Extracted " + result.Extracted + " files" + (result.Skipped > 0 ? "; " + result.Skipped + " skipped" : "")
                    + (result.Failed > 0 ? "; " + result.Failed + " failed" : "") + ".";
            }
            catch (Exception error)
            {
                Status = "Extraction failed: " + error.Message;
            }
            finally
            {
                m_isExtracting = false;
                OnPropertyChanged (nameof (CanExtractAll));
                OnPropertyChanged (nameof (CanExtractSelected));
            }
        }

        private static ExtractionResult ExtractEntries (ArcFile archive, string root, System.Collections.Generic.IEnumerable<Entry> entries)
        {
            var result = new ExtractionResult();
            foreach (var entry in entries)
            {
                var destination = GetSafeDestination (root, entry.Name);
                if (destination == null)
                {
                    ++result.Skipped;
                    continue;
                }
                try
                {
                    Directory.CreateDirectory (Path.GetDirectoryName (destination));
                    using (var input = archive.OpenEntry (entry))
                    using (var output = new FileStream (destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        input.CopyTo (output);
                    ++result.Extracted;
                }
                catch (IOException) when (File.Exists (destination))
                {
                    ++result.Skipped;
                }
                catch
                {
                    ++result.Failed;
                }
            }
            return result;
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
            m_audioPlayer.Stop();
            OnPropertyChanged (nameof (AudioPlayButtonText));
            PreviewBitmap?.Dispose();
            PreviewBitmap = null;
            PreviewText = null;
            DeleteAudioPreview();
            AudioPreviewText = null;
            OnPropertyChanged (nameof (HasPreviewAudio));
        }

        private void DeleteAudioPreview ()
        {
            if (!string.IsNullOrEmpty (m_audioPreviewPath))
            {
                try { File.Delete (m_audioPreviewPath); }
                catch (IOException) { }
                m_audioPreviewPath = null;
            }
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

    internal sealed class ExtractionResult
    {
        public int Extracted;
        public int Skipped;
        public int Failed;
    }

    internal sealed class AudioPreviewPlayer : IDisposable
    {
        SoundFlow.Backends.MiniAudio.MiniAudioEngine m_engine;
        SoundFlow.Abstracts.Devices.AudioPlaybackDevice m_device;
        SoundFlow.Components.SoundPlayer m_player;
        string m_path;

        public bool IsPlaying => m_player?.State == SoundFlow.Enums.PlaybackState.Playing;

        public void Toggle (string path)
        {
            if (m_player != null && string.Equals (path, m_path, StringComparison.Ordinal))
            {
                if (IsPlaying)
                    m_player.Pause();
                else
                    m_player.Play();
                return;
            }

            Stop();
            m_engine ??= new SoundFlow.Backends.MiniAudio.MiniAudioEngine();
            m_engine.UpdateAudioDevicesInfo();
            var device = m_engine.PlaybackDevices.FirstOrDefault (item => item.IsDefault);
            if (string.IsNullOrEmpty (device.Name))
                throw new InvalidOperationException ("No audio output device is available.");

            var format = SoundFlow.Structs.AudioFormat.DvdHq;
            m_device = m_engine.InitializePlaybackDevice (device, format);
            m_player = new SoundFlow.Components.SoundPlayer (m_engine, format,
                new SoundFlow.Providers.StreamDataProvider (m_engine, File.OpenRead (path)));
            m_device.MasterMixer.AddComponent (m_player);
            m_device.Start();
            m_player.Play();
            m_path = path;
        }

        public void Stop ()
        {
            if (m_player != null)
            {
                m_player.Stop();
                m_device?.MasterMixer.RemoveComponent (m_player);
                m_player.Dispose();
                m_player = null;
            }
            if (m_device != null)
            {
                m_device.Stop();
                m_device.Dispose();
                m_device = null;
            }
            m_path = null;
        }

        public void Dispose ()
        {
            Stop();
            m_engine?.Dispose();
            m_engine = null;
        }
    }

    public sealed class ResourceItem
    {
        public string Name { get; private set; }
        public string Type { get; private set; }
        public string SizeText { get; private set; }
        public string FullPath { get; private set; }
        public string ArchiveDirectory { get; private set; }
        public Entry Entry { get; private set; }
        public bool IsDirectory { get; private set; }

        public static ResourceItem FromDirectory (DirectoryInfo directory) => new ResourceItem { Name = directory.Name, Type = "Folder", FullPath = directory.FullName, IsDirectory = true };
        public static ResourceItem FromArchiveDirectory (string name, string path) => new ResourceItem { Name = name, Type = "Folder", ArchiveDirectory = path, IsDirectory = true };
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
