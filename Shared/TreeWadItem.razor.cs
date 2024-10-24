using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Microsoft.WindowsAPICodePack.Dialogs;
using MudBlazor;
using Obsidian.Data.Wad;
using Obsidian.Pages;
using Obsidian.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Obsidian.Shared;

public partial class TreeWadItem {
    [Inject]
    public IJSRuntime JsRuntime { get; set; }

    [Parameter]
    public WadExplorer Explorer { get; set; }

    [Parameter]
    public WadTreeModel WadTree { get; set; }

    [Parameter]
    public WadTreeItemModel Item { get; set; }

    [Parameter]
    public EventCallback<TreeWadItem> OnSelect { get; set; }

    public bool IsChecked {
        get => this.Item.IsChecked;
        set {
            if (this.Item.IsChecked == value)
                return;

            // Update children checked state
            this.Item.IsChecked = value;
            this.Item.CheckItemTree(value);
        }
    }

    private async Task OnRowClick(MouseEventArgs e) {
        if (this.Item.IsSelected)
            return;

        if (e.ShiftKey) {
            SelectMultiple();
        } else if (e.CtrlKey) {
            this.IsChecked = !this.IsChecked;
        } else {
            SelectItem();
            await this.Explorer.UpdateSelectedFile(this.Item);
        }

        await this.OnSelect.InvokeAsync(this);
    }

    private void OnRowDoubleClick(MouseEventArgs e) {
        if (this.Item.Type is WadTreeItemType.Directory)
            ToggleExpand();

        this.Explorer.RefreshState();
    }

    private void OnCheckedChanged(bool value) {
        this.IsChecked = value;
        this.Explorer.RefreshState();
    }

    private void OnToggleExpand(MouseEventArgs e) {
        ToggleExpand();

        if (e.ShiftKey) {
            foreach (WadTreeItemModel item in this.Item.TraverseFlattenedItems())
                item.IsExpanded = this.Item.IsExpanded;
        }

        this.Explorer.RefreshState();
    }

    private void ToggleExpand() {
        this.Item.IsExpanded = !this.Item.IsExpanded;
    }

    private async Task CopyNameToClipboard() {
        await this.JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", this.Item.Name);
    }

    private async Task CopyPathToClipboard() {
        await this.JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", this.Item.Path);
    }

    private void SelectItem() {
        foreach (WadTreeItemModel item in this.WadTree.TraverseFlattenedItems())
            item.IsSelected = false;

        this.Item.IsSelected = true;
    }

    private void SelectMultiple() {
        WadTreeItemModel selectedItem = this.WadTree
            .TraverseFlattenedItems()
            .Where(x => x.IsSelected)
            .FirstOrDefault();
        List<WadTreeItemModel> items = this.WadTree
            .TraverseFlattenedVisibleItems(this.WadTree.Filter, this.WadTree.UseRegexFilter)
            .ToList();
        int targetIndex = items.IndexOf(this.Item);
        int currentIndex = items.IndexOf(selectedItem);

        if (currentIndex == -1)
            return;

        // Get range of items to select
        (int startIndex, int endIndex) = (targetIndex) switch {
            _ when targetIndex > currentIndex => (currentIndex, targetIndex),
            _ when targetIndex < currentIndex => (targetIndex, currentIndex),
            _ => (0, 0)
        };

        // Traverse over items and select them
        IEnumerable<WadTreeItemModel> itemsToSelect = items
            .Skip(startIndex)
            .Take(endIndex - startIndex + 1);
        foreach (WadTreeItemModel itemToSelect in itemsToSelect) {
            itemToSelect.IsChecked = !itemToSelect.IsChecked;
            if (!itemToSelect.IsExpanded)
                itemToSelect.CheckItemTree(itemToSelect.IsChecked);
        }

        this.Explorer.RefreshState();
    }

    private void Save() {
        if (this.Item is not WadTreeFileModel fileItem)
            return;

        // Diálogo para seleccionar el archivo de guardado
        CommonSaveFileDialog dialog = new("Save") { DefaultFileName = fileItem.Name };
        if (dialog.ShowDialog(this.Explorer.Window.WindowHandle) != CommonFileDialogResult.Ok)
            return;

        // Log para la información de guardado
        Log.Information($"Saving {fileItem.Path} to {dialog.FileName}");
        this.Explorer.ToggleExporting(true);
        try {
            // Aquí necesitas obtener el directorio de guardado
            string saveDirectory = System.IO.Path.GetDirectoryName(dialog.FileName);
            WadUtils.SaveChunk(fileItem.Wad, fileItem.Chunk, dialog.FileName, saveDirectory); // Agregar saveDirectory aquí
            this.Explorer.Snackbar.Add($"Saved {fileItem.Name}", Severity.Success);
        } catch (Exception exception) {
            // Muestra los errores en el Snackbar
            this.Explorer.Snackbar.Add($"Error: {exception.Message}", Severity.Error);
        } finally {
            this.Explorer.ToggleExporting(false);
        }
    }

    private void Delete() {
        this.Item.Parent.Items.Remove(this.Item);
        this.Explorer.RefreshState();
    }
}
