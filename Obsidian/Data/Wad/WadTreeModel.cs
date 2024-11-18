using CommunityToolkit.Diagnostics;
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
       
    public WadFilePreviewType CurrentPreviewType { get; set; }
    
    public List<WadTreeItemModel> Items { get; set; } = new();
    
    //public IEnumerable<WadTreeFileModel> CheckedFiles =>
    //    this.TraverseFlattenedCheckedItems()
    //        .OfType<WadTreeFileModel>();
    
    // Test CheckedFiles for Improvemt Perfomance
    public IEnumerable<WadTreeFileModel> CheckedFiles {
        get {
            foreach (var item in this.TraverseFlattenedCheckedItems()) {
                if (item is WadTreeFileModel fileItem) {
                    yield return fileItem;
                }
            }
        }
    }

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
        var wadFilePathComponents = wadFilePath.Split('/');
        IWadTreeParent wadParent = this;

        if (allowDuplicate) {
            var wadItem = new WadTreeItemModel(this, wadFilePathComponents[0]);
            this.Items.Add(wadItem);
            wadParent = wadItem;
            wadFilePathComponents = wadFilePathComponents.Skip(1).ToArray(); // Modificado aquí para evitar mutación innecesaria
        }

        foreach (var (_, chunk) in wad.Chunks) {
            string path = this.Hashtable.TryGetChunkPath(chunk, out path) 
                ? path 
                : HashtableService.GuessChunkPath(chunk, wad);

            wadParent.AddWadFile(wadFilePathComponents.Concat(path.Split('/')), wad, chunk);
        }
    }


    public void SortItems() {
        Log.Information($"Sorting wad tree");
        
        this.Items.Sort();
        foreach (WadTreeItemModel item in this.Items.Where(item => item.Type is WadTreeItemType.Directory)) {
            item.SortItems();
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
