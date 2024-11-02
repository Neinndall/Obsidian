using LeagueToolkit.Core.Wad;
using LeagueToolkit.Hashing;
using System.Text.RegularExpressions;

namespace Obsidian.Data.Wad;

public interface IWadTreeParent : IWadTreePathable {
    ICollection<WadTreeItemModel> Items { get; }
}

public static class IWadTreeParentExtensions {
    // Mejora en la forma de agregar archivos y manejo de excepciones
    public static void AddWadFile(
        this IWadTreeParent parent,
        IEnumerable<string> pathComponents,
        WadFile wad,
        WadChunk chunk
    ) {
        if (pathComponents == null || !pathComponents.Any()) return;
        
        // El archivo pertenece a esta carpeta
        if (pathComponents.Count() == 1) {
            // Agregar archivo en la carpeta
            parent.Items.Add(new WadTreeFileModel(parent, pathComponents.First(), wad, chunk));
            return;
        }
        
        string folderName = pathComponents.First();
        ulong folderNameHash = XxHash64Ext.Hash(folderName);

        WadTreeItemModel directory;
        
        lock (parent) {
            // Mejora: Uso de un diccionario para una búsqueda más rápida
            directory = parent.Items.FirstOrDefault(item => item.Type != WadTreeItemType.File && item.NameHash == folderNameHash)
                ?? new WadTreeItemModel(parent, folderName);
            
            if (!parent.Items.Contains(directory)) {
                parent.Items.Add(directory);
            }
        }

        // Recursivamente agregar el archivo en el directorio correspondiente
        directory.AddWadFile(pathComponents.Skip(1), wad, chunk);
    }

    public static IEnumerable<WadTreeItemModel> TraverseFlattenedItems(this IWadTreeParent parent) {
        if (parent?.Items == null)
            yield break;

        foreach (var item in parent.Items) {
            yield return item;

            foreach (WadTreeItemModel itemItem in item.TraverseFlattenedItems())
                yield return itemItem;
        }
    }

    public static IEnumerable<WadTreeItemModel> TraverseFlattenedCheckedItems(this IWadTreeParent parent) {
        if (parent?.Items == null)
            yield break;

        foreach (var item in parent.Items) {
            if (item.IsChecked)
                yield return item;

            foreach (WadTreeItemModel itemItem in item.TraverseFlattenedCheckedItems())
                yield return itemItem;
        }
    }

    public static IEnumerable<WadTreeItemModel> TraverseFlattenedVisibleItems(this IWadTreeParent parent, string filter, bool useRegex = false) {
        if (parent?.Items == null)
            yield break;

        foreach (var item in parent.Items) {
            // Si hay un filtro, comprueba si el elemento coincide
            if (!string.IsNullOrEmpty(filter)) {
                // Si el elemento actual es un archivo, se verifica el filtro
                if (item is WadTreeFileModel && DoesMatchFilter(item, filter, useRegex)) {
                    yield return item;
                    continue;
                }

                // Si el elemento es una carpeta, se obtiene la lista de elementos filtrados
                var filteredItems = item.TraverseFlattenedVisibleItems(filter, useRegex).ToList();
                if (!filteredItems.Any())
                    continue;

                // Devuelve el directorio solo si sus hijos coinciden con el filtro
                yield return item;

                if (item.IsExpanded) {
                    foreach (WadTreeItemModel itemItem in filteredItems)
                        yield return itemItem;
                }
            } else {
                // Los elementos raíz siempre son visibles
                yield return item;

                if (item.Type == WadTreeItemType.Directory && item.IsExpanded) {
                    foreach (WadTreeItemModel itemItem in item.TraverseFlattenedVisibleItems(null))
                        yield return itemItem;
                }
            }
        }
    }

    public static bool DoesMatchFilter(WadTreeItemModel item, string filter, bool useRegex) {
        return useRegex 
            ? Regex.IsMatch(item.Path, filter, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) 
            : item.Path.Contains(filter, StringComparison.InvariantCultureIgnoreCase);
    }
}
