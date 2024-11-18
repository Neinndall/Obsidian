using CommunityToolkit.HighPerformance;
using LeagueToolkit.Core.Mesh;
using LeagueToolkit.Core.Meta;
using LeagueToolkit.Core.Wad;
using LeagueToolkit.Hashing;
using LeagueToolkit.Meta;
using LeagueToolkit.Meta.Classes;
using LeagueToolkit.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Text.RegularExpressions;
using MudBlazor;
using MudExtensions;
using Obsidian.BabylonJs;
using Obsidian.Data;
using Obsidian.Data.Wad;
using Obsidian.Services;
using Obsidian.Utils;
using PhotinoNET;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Concurrent;
using Toolbelt.Blazor.HotKeys2;
using RigResource = LeagueToolkit.Core.Animation.RigResource;

namespace Obsidian.Shared;

public partial class WadExplorer : IDisposable {
    #region Injection
    [Inject]
    public Config Config { get; set; }

    [Inject]
    public DiscordRichPresence RichPresence { get; set; }

    [Inject]
    public HashtableService Hashtable { get; set; }

    [Inject]
    public PhotinoWindow Window { get; set; }

    [Inject]
    public ISnackbar Snackbar { get; set; }

    [Inject]
    public HotKeys HotKeys { get; set; }

    [Inject]
    public IJSRuntime JsRuntime { get; set; }
    #endregion

    [Parameter]
    public WadTreeModel WadTree { get; set; }

    [Parameter]
    public EventCallback RebuildWadTree { get; set; }

    private WadFilter _wadFilterComponent;
    private WadFileTextPreview _textPreview;
    private WadAudioPreview _audioPreview;
    
    private MudSplitter _splitter;
    
    private readonly ConcurrentQueue<Task> _previewQueue = new();

    private readonly System.Timers.Timer _previewTimer = new(250);

    private HotKeysContext _hotKeysContext;

    private bool _isLoadingWadFile = false;
    private bool _isExportingFiles = false;

    // TODO: Asset loading should be moved into a new component
    private bool _isLoadingPreview = false;

    #region Toolbar Events
    public async Task OpenWad() {
        CommonOpenFileDialog dialog = FileDialogUtils.CreateOpenWadDialog(
            this.Config.GameDataDirectory
        );
        if (dialog.ShowDialog(this.Window.WindowHandle) is CommonFileDialogResult.Cancel)
            return;

        Log.Information("Opening WAD files: {FileNames}", dialog.FileNames);
        this._isLoadingWadFile = true;
        StateHasChanged();
        try {
            await Task.Run(() => {
                foreach (string wadPath in dialog.FileNames) {
                    FileStream wadFileStream = File.OpenRead(wadPath);
                    WadFile wad = new(wadFileStream);

                    this.WadTree.CreateTreeForWadFile(wad, Path.GetFileName(wadPath), allowDuplicate: true);
                }

                this.WadTree.SortItems();
            });

            this.Snackbar.Add("Successfully opened Wad archives!", Severity.Success);
        } catch (Exception exception) {
            SnackbarUtils.ShowHardError(this.Snackbar, exception);
        } finally {
            this._isLoadingWadFile = false;
            StateHasChanged();
        }
    }

    public async Task ExtractAll() {
        CommonOpenFileDialog dialog = FileDialogUtils.CreateExtractWadDialog(
            this.Config.DefaultExtractDirectory
        );
        if (dialog.ShowDialog(this.Window.WindowHandle) is not CommonFileDialogResult.Ok)
            return;

        IEnumerable<WadTreeFileModel> fileItems = this.WadTree
            .TraverseFlattenedItems()
            .Where(x => x is WadTreeFileModel)
            .Select(x => x as WadTreeFileModel);

        Log.Information($"Extracting all chunks");
        ToggleExporting(true);
        try {
            await Task.Run(() => ExtractFiles(fileItems, dialog.FileName));

            this.Snackbar.Add(
                $"Successfully exported {fileItems.Count()} files!",
                Severity.Success
            );
        } catch (Exception exception) {
            SnackbarUtils.ShowHardError(this.Snackbar, exception);
        } finally {
            ToggleExporting(false);
        }
    }

    public async Task ExtractSelected() {
        // Crea un diálogo para elegir el directorio de extracción.
        CommonOpenFileDialog dialog = FileDialogUtils.CreateExtractWadDialog(
            this.Config.DefaultExtractDirectory
        );

        // Si el diálogo se cierra sin seleccionar un directorio, salimos.
        if (dialog.ShowDialog(this.Window.WindowHandle) is not CommonFileDialogResult.Ok)
            return;

        // Obtenemos los archivos seleccionados en el árbol.
        IEnumerable<WadTreeFileModel> fileItems = this.WadTree.CheckedFiles;

        Log.Information($"Extracting selected chunks");
        
        // Iniciamos el estado de exportación.
        ToggleExporting(true);

        try {
            // Ejecutamos la extracción de archivos en un hilo separado.
            await Task.Run(() => ExtractFiles(fileItems, dialog.FileName));

            // Mostramos un mensaje de éxito en la snackbar.
            this.Snackbar.Add(
                $"Successfully exported {fileItems.Count()} files!",
                Severity.Success
            );
        } catch (Exception exception) {
            // Manejamos errores mostrando un mensaje.
            SnackbarUtils.ShowHardError(this.Snackbar, exception);
        } finally {
            // Finalizamos el estado de exportación.
            ToggleExporting(false);
        }
    }

    public async Task LoadHashtable() {
        CommonOpenFileDialog dialog = new("Select hashtables") { Multiselect = true };
        if (dialog.ShowDialog(this.Window.WindowHandle) is not CommonFileDialogResult.Ok)
            return;

        Log.Information("Loading external hashtables: {Hashtables}", dialog.FileNames);
        try {
            // Load hashtables
            await InvokeAsync(async () => { // Cambiado a async
                foreach (string hashtableFile in dialog.FileNames) {
                    await this.Hashtable.LoadHashtable(hashtableFile); // Añadido await aquí
                }
            });

            // Re-build tree
            await this.RebuildWadTree.InvokeAsync();

            this.Snackbar.Add("Successfully loaded hashtables!", Severity.Success);
        } catch (Exception exception) {
            SnackbarUtils.ShowHardError(this.Snackbar, exception);
        }
    }
    #endregion

    private void ExtractFiles(IEnumerable<WadTreeFileModel> fileItems, string extractionDirectory) {
        foreach (WadTreeFileModel fileItem in fileItems) {
            string finalFileName = fileItem.Path; // Nombre final del archivo

            // Comprueba si el archivo no tiene extensión
            if (string.IsNullOrEmpty(Path.GetExtension(fileItem.Path))) {
                // Si el archivo tiene un nombre legible (no es hexadecimal), añade un punto al inicio
                if (!Regex.IsMatch(Path.GetFileNameWithoutExtension(fileItem.Path), @"^[a-fA-F0-9]+$")) {
                    finalFileName = Path.Combine(Path.GetDirectoryName(fileItem.Path), 
                        $".{Path.GetFileName(fileItem.Path)}");
                }
            }

            // Ruta completa del archivo a extraer
            string filePath = Path.Combine(extractionDirectory, finalFileName);

            try {
                // Log de depuración
                Log.Information($"Extrayendo archivo: {fileItem.Path}, Chunk: {fileItem.Chunk}");

                // Guarda el chunk
                Utils.WadUtils.SaveChunk(
                    fileItem.Wad,
                    fileItem.Chunk,
                    finalFileName,
                    extractionDirectory
                );
            } catch (Exception ex) {
                Log.Error($"Error al extraer el archivo {fileItem.Path}: {ex.Message}");
            }
        }
    }

    private ICollection<WadTreeItemModel> GetVisibleItemsForWadTree() {
        try {
            var visibleItems = this.WadTree
                .TraverseFlattenedVisibleItems(this.WadTree.Filter, this.WadTree.UseRegexFilter)
                .ToArray();  // Materializamos la colección aquí
            return visibleItems;
        } catch (Exception exception) {
            SnackbarUtils.ShowSoftError(this.Snackbar, exception);
            return Array.Empty<WadTreeItemModel>();
        }
    }

    // TODO: Refactor this to be an event from the tree view itself
    public async Task UpdateSelectedFile(WadTreeItemModel item) {
        // Hide the preview if the selected file is null
        if (item is not WadTreeFileModel fileItem) {
            await SetCurrentPreviewType(WadFilePreviewType.None);
            return;
        }

        if (this.Config.ShouldPreviewSelectedItems)
            this._previewQueue.Enqueue(PreviewSelectedFile(fileItem));
    }

    #region Preview
    // TODO: This function shouldn't be here
    private async Task PreviewSelectedFile(WadTreeFileModel file) {
        using Stream fileStream = file.Wad.LoadChunkDecompressed(file.Chunk).AsStream();
        LeagueFileType fileType = LeagueFile.GetFileType(fileStream);
        string extension = Path.GetExtension(file.Name);

        if (BinUtils.IsSkinPackage(file.Path)) {
            await PreviewSkinPackage(file.Wad, fileStream);
        } else if (fileType is LeagueFileType.StaticMeshBinary or LeagueFileType.StaticMeshAscii) {
            await PreviewStaticMesh(
                fileStream,
                isAscii: fileType is LeagueFileType.StaticMeshAscii
            );
        } else if (fileType is (LeagueFileType.TextureDds or LeagueFileType.Texture)) {
            await PreviewImage(ImageUtils.GetImageFromTextureStream(fileStream));
        } else if (fileType is LeagueFileType.Png or LeagueFileType.Jpeg) {
            await PreviewImage(fileStream);
        } else if (fileType is LeagueFileType.PropertyBin) {
            await PreviewPropertyBin(fileStream);
        } else if (extension is ".ogg") {
            await PreviewAudio(fileStream);
        } else if (extension is ".json") {
            await PreviewText(fileStream, "json");
        } else if (extension is ".js") {
            await PreviewText(fileStream, "javascript");
        } else if (extension is ".html") {
            await PreviewText(fileStream, "html");
        } else if (extension is ".txt") {
            await PreviewText(fileStream, "txt");
        } else if (extension is ".ini") {
            await PreviewText(fileStream, "ini");

        } else {
            await SetCurrentPreviewType(WadFilePreviewType.None);
        }
    }

    private async Task PreviewSkinPackage(WadFile wad, Stream stream) {
        Log.Information("Previewing skin package");

        BinTree skinPackage = new(stream);
        MetaEnvironment metaEnvironment = BinUtils.CreateMetaEnvironment();

        BinTreeObject skinDataObject = skinPackage.Objects.Values.FirstOrDefault(
            x => x.ClassHash == Fnv1a.HashLower(nameof(SkinCharacterDataProperties))
        );

        if (skinDataObject is null)
            throw new InvalidDataException(
                $"Skin package does not contain {nameof(SkinCharacterDataProperties)}"
            );

        var skinData = MetaSerializer.Deserialize<SkinCharacterDataProperties>(
            metaEnvironment,
            skinDataObject
        );
        SkinMeshDataProperties meshData = skinData.SkinMeshProperties;

        using Stream simpleSkinStream = wad.LoadChunkDecompressed(meshData.SimpleSkin).AsStream();
        using Stream skeletonStream = wad.LoadChunkDecompressed(meshData.Skeleton).AsStream();

        using SkinnedMesh skinnedMesh = SkinnedMesh.ReadFromSimpleSkin(simpleSkinStream);
        RigResource skeleton = new(skeletonStream);

        await SetCurrentPreviewType(WadFilePreviewType.Viewport);
        await Task.Delay(25);

        if (this.Config.LoadSkinnedMeshAnimations) {
            await Three.RenderSkinnedMeshFromGltf(
                this.JsRuntime,
                WadPreviewUtils.VIEWPORT_CONTAINER_ID,
                skinnedMesh,
                skeleton,
                SkinnedMeshUtils.CollectMaterialTextures(
                    skinnedMesh,
                    meshData,
                    skinPackage,
                    wad,
                    metaEnvironment
                ),
                SkinnedMeshUtils.LoadAnimationAssets(skinData, skinPackage, wad, metaEnvironment)
            );
        } else {
            await Three.RenderSkinnedMesh(
                this.JsRuntime,
                WadPreviewUtils.VIEWPORT_CONTAINER_ID,
                skinnedMesh,
                skeleton,
                await SkinnedMeshUtils.CreateTextureImages(
                    this.JsRuntime,
                    skinnedMesh,
                    meshData,
                    skinPackage,
                    wad,
                    metaEnvironment
                )
            );
        }
    }

    private async Task PreviewStaticMesh(Stream stream, bool isAscii) {
        Log.Information("Previewing static mesh");

        await SetCurrentPreviewType(WadFilePreviewType.Viewport);
        await Task.Delay(25);

        StaticMesh staticMesh = isAscii switch {
            true => StaticMesh.ReadAscii(stream),
            false => StaticMesh.ReadBinary(stream),
        };

        await Three.RenderStaticMesh(
            this.JsRuntime,
            WadPreviewUtils.VIEWPORT_CONTAINER_ID,
            staticMesh
        );
        UpdateSplitterForPreviewType(); // Restablecer el tamaño del divisor y actualizar la interfaz
    }

    private async Task PreviewMapGeometry(Stream stream) {
        Log.Information("Previewing map geometry");

        await SetCurrentPreviewType(WadFilePreviewType.Viewport);
        await Task.Delay(25);

        await Three.RenderEnvironmentAsset(
            this.JsRuntime,
            WadPreviewUtils.VIEWPORT_CONTAINER_ID,
            new(stream)
        );
        UpdateSplitterForPreviewType(); // Restablecer el tamaño del divisor y actualizar la interfaz
    }

    private async Task PreviewAudio(Stream audioStream) {
        Log.Information("Previewing audio");
        
        await SetCurrentPreviewType(WadFilePreviewType.Audio);
        await Task.Delay(5); // Para que no se produzca System.NullReferenceException
    
        // Reproducir el audio usando la función de JavaScript
        await _audioPreview.SetAudioPreview(audioStream);
        UpdateSplitterForPreviewType(); // Restablecer el tamaño del divisor y actualizar la interfaz
    }

    private async Task PreviewImage(Image<Rgba32> image) {
        Log.Information("Previewing image");

        MemoryStream imageStream = new();

        await image.SaveAsPngAsync(imageStream);
        imageStream.Position = 0;

        await PreviewImage(imageStream);
        UpdateSplitterForPreviewType(); // Restablecer el tamaño del divisor y actualizar la interfaz
    }

    private async Task PreviewImage(Stream imageStream) {
        Log.Information("Previewing image from stream");

        await SetCurrentPreviewType(WadFilePreviewType.Image);

        await this.JsRuntime.InvokeVoidAsync(
            "setImage",
            WadPreviewUtils.IMAGE_PREVIEW_ID,
            new DotNetStreamReference(imageStream)
        );
        UpdateSplitterForPreviewType(); // Restablecer el tamaño del divisor y actualizar la interfaz
    }

    private async Task PreviewPropertyBin(Stream stream) {
        Log.Information("Previewing property bin");

        await SetCurrentPreviewType(WadFilePreviewType.Text);
        await this._textPreview.PreviewRitobin(stream);
        UpdateSplitterForPreviewType(); // Restablecer el tamaño del divisor y actualizar la interfaz
    }

    private async Task PreviewText(Stream stream, string language) {
        Log.Information($"Previewing {language} text");

        await SetCurrentPreviewType(WadFilePreviewType.Text);
        await this._textPreview.Preview(stream, language);
        UpdateSplitterForPreviewType(); // Restablecer el tamaño del divisor y actualizar la interfaz
    }

    private async Task SetCurrentPreviewType(WadFilePreviewType previewType) {
        if (this.WadTree.CurrentPreviewType is WadFilePreviewType.Viewport)
            await this.JsRuntime.InvokeVoidAsync(
                "destroyThreeJsRenderer",
                WadPreviewUtils.VIEWPORT_CONTAINER_ID
            );
            
        this.WadTree.CurrentPreviewType = previewType;
        UpdateSplitterForPreviewType(); // Restablecer el tamaño del divisor y actualizar la interfaz
    }
    #endregion

    private async Task HandlePreviewTaskAsync(Task previewTask) {
        // Hide the preview if the selected file is null
        if (this.WadTree.SelectedFile is null) {
            await SetCurrentPreviewType(WadFilePreviewType.None);
            return;
        }

        try {
            await previewTask;
        } catch (Exception exception) {
            SnackbarUtils.ShowSoftError(this.Snackbar, exception);
            await SetCurrentPreviewType(WadFilePreviewType.None);
        }
    }
    
    private double _splitterDimension = 100; // Valor inicial
    private bool _userAdjustedDimension = false; // Indica si el usuario ha ajustado el divisor
  
    private void UpdateSplitterForPreviewType() {
        // Ajusta la dimensión del divisor dependiendo del tipo de vista previa
        if (this.WadTree?.CurrentPreviewType == WadFilePreviewType.Viewport) {
            
            if (!_userAdjustedDimension)
            {
                _splitterDimension = 30;
            }
        }
        else if (this.WadTree?.CurrentPreviewType == WadFilePreviewType.Image ||
            this.WadTree?.CurrentPreviewType == WadFilePreviewType.Text ||
            this.WadTree?.CurrentPreviewType == WadFilePreviewType.Audio) {
                
            if (!_userAdjustedDimension)
            {
                _splitterDimension = 50;
            }
        }
        else
        {
            _splitterDimension = 100; // Tamaño original o ajustado
            _userAdjustedDimension = false; // Reseteamos el ajuste de usuario
        }
        
        StateHasChanged(); // Actualiza la interfaz de usuario
    }

    // Método asíncrono para manejar el cambio de dimensión
    private async Task OnDimensionChanged(double dimension)
    {
        // Permitir el ajuste manual de la dimensión
        if (this.WadTree?.CurrentPreviewType == WadFilePreviewType.Viewport)
        {
            _splitterDimension = dimension;
            _userAdjustedDimension = true; // El usuario ha ajustado la dimensión
            
            await this.JsRuntime.InvokeVoidAsync(
                "resizeViewport",
                WadPreviewUtils.VIEWPORT_CONTAINER_ID
            );
        }
        else if (this.WadTree?.CurrentPreviewType == WadFilePreviewType.Text ||
            this.WadTree?.CurrentPreviewType == WadFilePreviewType.Image ||
            this.WadTree?.CurrentPreviewType == WadFilePreviewType.Audio)
        {
            _splitterDimension = dimension;
            _userAdjustedDimension = true;
        }
    }

    private void OnCollapseAll() {
        foreach (WadTreeItemModel item in this.WadTree.TraverseFlattenedItems())
            item.IsExpanded = false;
    }

    private void OnExpandAll() {
        foreach (
            WadTreeItemModel item in this.WadTree
                .TraverseFlattenedItems()
                .Where(x => x.Type is WadTreeItemType.Directory)
        )
            item.IsExpanded = true;
    }

    private Task OnRefreshTree() => this.RebuildWadTree.InvokeAsync();

    private async Task OnShouldPreviewSelectedItemsChanged(bool value) {
        this.Config.ShouldPreviewSelectedItems = value;

        await SetCurrentPreviewType(WadFilePreviewType.None);
    }
    
    public void DeselectAllItems()
    {
        foreach (var item in this.WadTree.TraverseFlattenedItems())
        {
            item.IsSelected = false;
            item.IsChecked = false;  // También desmarca si es necesario
        }
        this.RefreshState();
    }

    private void OnFilterChanged(string value) {
        this.WadTree.Filter = value;
        this.RefreshState();
    }

    private void OnUseRegexFilterChanged(bool value) {
        this.WadTree.UseRegexFilter = value;
        this.RefreshState();
    }

    private async ValueTask FocusWadFilter() =>
        await this._wadFilterComponent.InputField.FocusAsync();

    public void ToggleExporting(bool isExporting) {
        this._isExportingFiles = isExporting;
        StateHasChanged();
    }

    private void ToggleLoadingPreview(bool value) {
        this._isLoadingPreview = value;
        StateHasChanged();
    }

    public void RefreshState() => StateHasChanged();

    protected override void OnInitialized() {
        this._hotKeysContext = this.HotKeys
            .CreateContext()
            .Add(ModCode.Ctrl, Code.F, FocusWadFilter, "Focus Wad Filter");

        this._previewTimer.Elapsed += (sender, eventArgs) => OnPreviewCallback();
        this._previewTimer.Start();

        base.OnInitialized();
    }

    private void OnPreviewCallback() {
        _ = InvokeAsync(async () => {
            if (this._previewQueue.TryDequeue(out Task previewTask) is false)
                return;

            this._previewQueue.Clear();

            ToggleLoadingPreview(true);

            await HandlePreviewTaskAsync(previewTask);

            ToggleLoadingPreview(false);
        });
    }

    public void Dispose() {
        this._hotKeysContext?.Dispose();
        this._previewTimer?.Dispose();
    }
}