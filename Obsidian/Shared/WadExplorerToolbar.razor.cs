using Microsoft.AspNetCore.Components;
using Obsidian.Data.Wad;
using Obsidian.Pages;
using Toolbelt.Blazor.HotKeys2;
using MudBlazor; // Asegúrate de tener esta línea para usar el IDialogService
using Microsoft.JSInterop; // Asegúrate de tener esta línea

namespace Obsidian.Shared;

public partial class WadExplorerToolbar : IDisposable {
    [Inject]
    public HotKeys HotKeys { get; set; }
    
    [Inject]
    public IJSRuntime Js { get; set; }

    [CascadingParameter]
    public ExplorerPage ExplorerPage { get; set; }

    [Parameter]
    public WadTreeModel WadTree { get; set; }

    [Parameter]
    public EventCallback OnOpenWad { get; set; }

    [Parameter]
    public EventCallback OnExtractAll { get; set; }

    [Parameter]
    public EventCallback OnExtractSelected { get; set; }

    [Parameter]
    public EventCallback OnLoadHashtable { get; set; }

    // Inyectar el IDialogService aquí
    [Inject]
    public IDialogService DialogService { get; set; }

    private HotKeysContext _hotKeysContext;

    public AppTheme Theme { get; } = new();

    protected override void OnInitialized() {
        this._hotKeysContext = this.HotKeys
            .CreateContext()
            .Add(ModCode.Ctrl, Code.O, async () => await OnOpenWad.InvokeAsync(), "Open Wad");
    }

    public void Dispose() {
        this._hotKeysContext?.Dispose();
    }

    // Método para abrir el diálogo de configuración
    private void OpenSettings() {
        DialogService.Show<SettingsDialog>();
    }

    // Método para reportar un bug
    private async Task SubmitBugReport() =>
        await this.Js.InvokeVoidAsync(
            "useCmd",
            @"explorer ""https://github.com/Crauzer/Obsidian/issues/new?assignees=&labels=bug%2C+triage&template=bug_report.md&title=%5BBUG%5D+%2A%2ABug+report+title+here%2A%2A"""
        );
    
    // Método para ir a GitHub
    private async Task GoToGithub() =>
        await this.Js.InvokeVoidAsync(
            "useCmd",
            @"explorer ""https://github.com/Crauzer/Obsidian"""
        );
}
