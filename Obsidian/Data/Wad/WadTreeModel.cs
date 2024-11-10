using CommunityToolkit.Diagnostics;
using System.Collections.Generic;
using LeagueToolkit.Core.Wad;
using Obsidian.Services;
using Serilog;
using PathIO = System.IO.Path;

namespace Obsidian.Data.Wad;

public class WadTreeModel : IWadTreeParent, IDisposable {
    public HashtableService Hashtable { get; }
    public Config Config { get; }
    public IWadTreeParent Parent => null;
    public int Depth => 0;
    public string Name => string.Empty;   
    public string Path { get; private set; }
    public ulong NameHash => 0;
    public ulong PathHash => 0;
    public bool IsWadArchive => false;
    public bool UseRegexFilter { get; set; }
    public string Filter { get; set; }
    
    // Propiedad para almacenar el flujo de audio
    public Stream CurrentAudioStream { get; set; }
    
    public WadFilePreviewType CurrentPreviewType { get; set; }
    
    public WadTreeModel(string path) {
        Path = path;
    }
    
    public void AddItem(WadTreeItemModel item) {
        _items.Add(item);
    }
    
    // public List<WadTreeItemModel> Items { get; set; } = new();
    private readonly List<WadTreeItemModel> _items = new();
    public ICollection<WadTreeItemModel> Items => _items;

    public IEnumerable<WadTreeFileModel> CheckedFiles =>
        this.TraverseFlattenedCheckedItems()
            .OfType<WadTreeFileModel>();

    public WadTreeFileModel SelectedFile => this.SelectedFiles.FirstOrDefault();

    public IEnumerable<WadTreeFileModel> SelectedFiles =>
        this.TraverseFlattenedItems()
            .Where(x => x.IsSelected)
            .OfType<WadTreeFileModel>();

    private readonly Dictionary<string, WadFile> _mountedWadFiles = new();
    public bool IsDisposed { get; private set; }

    public WadTreeModel(HashtableService hashtable, Config config, IEnumerable<string> wadFiles) {
        Guard.IsNotNull(wadFiles, nameof(wadFiles));
        this.Hashtable = hashtable;
        this.Config = config;

        foreach (string wadFilePath in wadFiles) {
            try {
                MountWadFile(wadFilePath);
            } catch (Exception exception) {
                Log.Error(exception, "Failed to mount Wad file: {WadFile}", wadFilePath);
            }
        }

        SortItems();
    }

    private void MountWadFile(string path) {
        WadFile wad = new(path);
        string relativeWadPath = PathIO
            .GetRelativePath(this.Config.GameDataDirectory, path)
            .Replace(PathIO.DirectorySeparatorChar, '/');

        this._mountedWadFiles.Add(relativeWadPath, wad);
        CreateTreeForWadFile(wad, relativeWadPath);
    }

    public void CreateTreeForWadFile(WadFile wad, string wadFilePath, bool allowDuplicate = false) {
        IEnumerable<string> wadFilePathComponents = wadFilePath.Split('/');
        IWadTreeParent wadParent = this;

        if (allowDuplicate) {
            var wadItem = new WadTreeItemModel(this, wadFilePathComponents.First());
            this.Items.Add(wadItem);
            wadParent = wadItem;
            wadFilePathComponents = wadFilePathComponents.Skip(1);
        }

        foreach (var (_, chunk) in wad.Chunks) {
            string path = this.Hashtable.TryGetChunkPath(chunk, out path) switch {
                true => path,
                false => HashtableService.GuessChunkPath(chunk, wad),
            };

            wadParent.AddWadFile(wadFilePathComponents.Concat(path.Split('/')), wad, chunk);
        }
    }

    public void SortItems() {
        Log.Information($"Sorting wad tree");
        var sortedItems = Items.OrderBy(item => item.Name).ToList();
        Items.Clear();
        foreach (var item in sortedItems) {
            Items.Add(item);
            if (item.Type is WadTreeItemType.Directory) {
                item.SortItems();
            }
        }
    }

    // Dispose pattern para liberar recursos
    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (IsDisposed) return;

        if (disposing) {
            // Liberar los archivos Wad montados
            foreach (var (_, wad) in _mountedWadFiles) {
                wad?.Dispose();
            }
        }

        IsDisposed = true;
    }
}

public enum WadFilePreviewType {
    None,
    Image,
    Viewport,
    Text,
    Audio
}
