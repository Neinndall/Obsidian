using LeagueToolkit.Core.Wad;
using LeagueToolkit.Hashing;
using System.Text.RegularExpressions;

namespace Obsidian.Data.Wad;

public interface IWadTreeParent : IWadTreePathable {
    List<WadTreeItemModel> Items { get; }
}

public static class IWadTreeParentExtensions {
    public static void AddWadFile(
        this IWadTreeParent parent,
        IEnumerable<string> pathComponents,
        WadFile wad,
        WadChunk chunk
    ) {
        // File belongs to this folder
        if (pathComponents.Count() is 1) {
            parent.Items.Add(new WadTreeFileModel(parent, pathComponents.First(), wad, chunk));
            return;
        }

        string folderName = pathComponents.First();
        ulong folderNameHash = XxHash64Ext.Hash(folderName);

        WadTreeItemModel directory = null;
        lock (parent) {
            directory = parent.Items.FirstOrDefault(item => item.Type is not WadTreeItemType.File && item.NameHash == folderNameHash);
            if (directory is null) {
                directory = new(parent, folderName);
                parent.Items.Add(directory);
            }
        }

        directory.AddWadFile(pathComponents.Skip(1), wad, chunk);
    }

    public static IEnumerable<WadTreeItemModel> TraverseFlattenedItems(this IWadTreeParent parent) {
        if (parent.Items is null)
            yield break;

        foreach (var item in parent.Items) {
            yield return item;

            foreach (WadTreeItemModel itemItem in item.TraverseFlattenedItems())
                yield return itemItem;
        }
    }

    public static IEnumerable<WadTreeItemModel> TraverseFlattenedCheckedItems(
        this IWadTreeParent parent
    ) {
        if (parent.Items is null)
            yield break;

        foreach (var item in parent.Items) {
            if (item.IsChecked)
                yield return item;

            foreach (WadTreeItemModel itemItem in item.TraverseFlattenedCheckedItems())
                yield return itemItem;
        }
    }

    public static IEnumerable<WadTreeItemModel> TraverseFlattenedVisibleItems(
        this IWadTreeParent parent,
        string filter,
        bool useRegex = false
    ) {
        if (parent.Items == null)
            yield break;

        foreach (var item in parent.Items) {
            if (!string.IsNullOrEmpty(filter)) {
                // If the current item is a file, we check if it matches the filter
                if (item is WadTreeFileModel && DoesMatchFilter(item, filter, useRegex)) {
                    yield return item;
                    continue;
                }

                // If the current item is a folder, get filtered items and if there are none, skip it
                var filteredItems = item.TraverseFlattenedVisibleItems(filter, useRegex);
                if (!filteredItems.Any()) // Check if there are no filtered items before continuing
                    continue;

                // Return parent only if its children are included in the filter
                yield return item;

                // Recursively yield the filtered child items if expanded
                if (item.IsExpanded)
                    foreach (WadTreeItemModel childItem in filteredItems)
                        yield return childItem;
            } else {
                // Root items are always visible
                yield return item;

                // Expand directories if they are expanded
                if (item.Type == WadTreeItemType.Directory && item.IsExpanded)
                    foreach (WadTreeItemModel childItem in item.TraverseFlattenedVisibleItems(null))
                        yield return childItem;
            }
        }
    }



    public static bool DoesMatchFilter(WadTreeItemModel item, string filter, bool useRegex) =>
        useRegex switch {
            true
                => Regex.IsMatch(
                    item.Path,
                    filter,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
                ),
            false => item.Path.Contains(filter, StringComparison.InvariantCultureIgnoreCase)
        };
}