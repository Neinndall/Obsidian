using LeagueToolkit.Hashing;
using LeagueToolkit.Utils;
using MudBlazor;
using System.Diagnostics;
using System.Collections.Generic;
using PathIO = System.IO.Path;

namespace Obsidian.Data.Wad;

[DebuggerDisplay("{Name}")]
public class WadTreeItemModel : IWadTreePathable, IWadTreeParent, IComparable<WadTreeItemModel>, IEquatable<WadTreeItemModel> {

    public WadTreeItemType Type => this.Items.Count switch {
        0 => WadTreeItemType.File,
        _ => WadTreeItemType.Directory,
    };

    public IWadTreeParent Parent { get; protected set; }
    public int Depth => this.GetDepth();

    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; }
    public string Path { get; private set; }
    public ulong NameHash { get; }
    public ulong PathHash { get; }

    public string Icon => GetIcon();

    public bool IsHighlighted => this.IsWadArchive;

    public bool IsSelected { get; set; }
    public bool IsChecked { get; set; }
    private bool _isExpanded;

    public bool IsExpanded {
        get => _isExpanded;
        set {
            if (_isExpanded != value) {
                _isExpanded = value;
                if (_isExpanded) {
                    LoadItems(); // Cargar elementos al expandir
                }
            }
        }
    }

    public bool IsWadArchive { get; }
    private readonly List<WadTreeItemModel> _children = new();
    public ICollection<WadTreeItemModel> Items => _children; // Propiedad pública que expone los hijos

    public WadTreeItemModel(IWadTreeParent parent, string name) {
        this.Parent = parent;

        this.Name = name;
        this.Path = parent switch {
            null or WadTreeModel or { IsWadArchive: true } => name,
            _ => string.Join('/', parent.Path, name)
        };
        this.NameHash = XxHash64Ext.Hash(this.Name);
        this.PathHash = XxHash64Ext.Hash(this.Path);

        this.IsWadArchive = this.Name.EndsWith(".wad", StringComparison.OrdinalIgnoreCase)
            || this.Name.EndsWith(".wad.client", StringComparison.OrdinalIgnoreCase)
            || this.Name.EndsWith(".wad.mobile", StringComparison.OrdinalIgnoreCase);
    }

    public void SortItems() {
        if (this.Items is null)
            return;

        var itemsList = (List<WadTreeItemModel>)this.Items; // Casting a List para poder usar Sort
        itemsList.Sort();

        foreach (WadTreeItemModel item in itemsList.Where(item => item.Type == WadTreeItemType.Directory)) {
            item.SortItems();
        }
    }

    public void CheckItemTree(bool value) {
        if (this.Items != null) {
            foreach (WadTreeItemModel item in this.TraverseFlattenedItems()) {
                item.IsChecked = value;
            }
        }
    }

    public string GetIcon() {
        // Comprobamos si hay un icono personalizado
        string customIcon = GetCustomIcon(this);
        if (customIcon != null) {
            return customIcon; // Retorna el icono personalizado si existe
        }

        if (this.Type == WadTreeItemType.Directory) {
            // Asigna íconos según el estado expandido de la carpeta
            return this.IsExpanded ? Icons.Material.Outlined.FolderOpen : Icons.Material.Outlined.Folder;
        }

        string extension = PathIO.GetExtension(this.Name);
        if (this.IsWadArchive) {
            return Icons.Material.Outlined.Archive;
        }

        // Utilizando un diccionario para simplificar la asignación de íconos
        var iconMapping = new Dictionary<LeagueFileType, string> {
            { LeagueFileType.Animation, Icons.Material.Outlined.Animation },
            { LeagueFileType.Jpeg, Icons.Material.Outlined.Image },
            { LeagueFileType.MapGeometry, CustomIcons.Material.ImageFilterHdr },
            { LeagueFileType.Png, Icons.Material.Outlined.Image },
            { LeagueFileType.PropertyBin, CustomIcons.Material.CodeBracesBox },
            { LeagueFileType.PropertyBinOverride, CustomIcons.Material.CodeBracesBox },
            { LeagueFileType.RiotStringTable, Icons.Material.Outlined.Translate },
            { LeagueFileType.SimpleSkin, CustomIcons.Material.Cube },
            { LeagueFileType.Skeleton, CustomIcons.Material.Bone },
            { LeagueFileType.StaticMeshAscii, CustomIcons.Material.Cube },
            { LeagueFileType.StaticMeshBinary, CustomIcons.Material.Cube },
            { LeagueFileType.Texture, Icons.Material.Outlined.Image },
            { LeagueFileType.TextureDds, Icons.Material.Outlined.Image },
            { LeagueFileType.WorldGeometry, CustomIcons.Material.ImageFilterHdr },
            { LeagueFileType.WadArchive, Icons.Material.Outlined.Archive },
            { LeagueFileType.WwiseBank, CustomIcons.Material.VolumeHigh },
            { LeagueFileType.WwisePackage, CustomIcons.Material.AccountVoice },
        };

        return iconMapping.TryGetValue(LeagueFile.GetFileType(extension), out var icon) ? icon : Icons.Custom.FileFormats.FileDocument;
    }

    // Método para cargar elementos en Items
    private void LoadItems() {
        if (Items != null && Items.Count == 0) { // Solo carga si la lista está vacía
            try {
                // Suponiendo que 'Path' es una ruta válida donde se buscan los archivos
                var files = Directory.GetFiles(Path);
                var directories = Directory.GetDirectories(Path);
                
                foreach (var file in files) {
                    Items.Add(new WadTreeItemModel(this, PathIO.GetFileName(file)));
                }

                foreach (var directory in directories) {
                    var directoryModel = new WadTreeItemModel(this, PathIO.GetFileName(directory));
                    directoryModel.LoadItems(); // Cargar elementos dentro del subdirectorio
                    Items.Add(directoryModel);
                }
            } catch (Exception ex) {
                // Manejo de excepciones en caso de errores al acceder a los archivos
                Debug.WriteLine($"Error al cargar elementos: {ex.Message}");
            }
        }
    }

    // Método para obtener el icono personalizado
    private string GetCustomIcon(WadTreeItemModel item) {
        return item.Type switch {
            WadTreeItemType.File when item.Name.EndsWith(".json") => Icons.Material.Outlined.TextSnippet,
            WadTreeItemType.File when item.Name.EndsWith(".ogg") => Icons.Material.Outlined.VolumeUp,
            WadTreeItemType.File when item.Name.EndsWith(".js") => Icons.Material.Outlined.Javascript,
            WadTreeItemType.File when item.Name.EndsWith(".html") => Icons.Material.Outlined.Html,
            _ => null // Retorna null si no hay icono personalizado
        };
    }

    public int CompareTo(WadTreeItemModel other) =>
        (this.IsWadArchive, other.IsWadArchive) switch {
            (true, true) => this.Name.CompareTo(other?.Name),
            (true, false) => 1,
            (false, true) => -1,
            (false, false) => (this.Type, other?.Type) switch {
                (WadTreeItemType.Directory, WadTreeItemType.File) => -1,
                (WadTreeItemType.File, WadTreeItemType.Directory) => 1,
                _ => this.Name.CompareTo(other?.Name)
            }
        };

    public bool Equals(WadTreeItemModel other) => this.Id == other?.Id;

    public override bool Equals(object obj) =>
        obj switch {
            WadTreeItemModel item => Equals(item),
            _ => false
        };

    public override int GetHashCode() => this.Id.GetHashCode();

    #region Operator overloads
    public static bool operator ==(WadTreeItemModel left, WadTreeItemModel right) => left.Equals(right);
    public static bool operator !=(WadTreeItemModel left, WadTreeItemModel right) => !left.Equals(right);
    public static bool operator <(WadTreeItemModel left, WadTreeItemModel right) => left.CompareTo(right) < 0;
    public static bool operator <=(WadTreeItemModel left, WadTreeItemModel right) => left.CompareTo(right) <= 0;
    public static bool operator >(WadTreeItemModel left, WadTreeItemModel right) => left.CompareTo(right) > 0;
    public static bool operator >=(WadTreeItemModel left, WadTreeItemModel right) => left.CompareTo(right) >= 0;
    #endregion
}

public enum WadTreeItemType {
    File,
    Directory
}
