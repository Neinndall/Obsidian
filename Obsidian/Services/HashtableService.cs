using CommunityToolkit.HighPerformance;
using LeagueToolkit.Core.Wad;
using LeagueToolkit.Utils;
using Obsidian.Data;
using Serilog;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using FileMode = System.IO.FileMode;
using System.IO;
using System.Threading.Tasks;

namespace Obsidian.Services;

public class HashtableService {
    public Config Config { get; }

    // Declaración de propiedades
    public Dictionary<ulong, string> Hashes { get; private set; } = new();
    public Dictionary<uint, string> BinClasses { get; private set; } = new();
    public Dictionary<uint, string> BinProperties { get; private set; } = new();
    public Dictionary<uint, string> BinHashes { get; private set; } = new();
    public Dictionary<uint, string> BinObjects { get; private set; } = new();

    private const string HASHES_BASE_URL = "https://raw.communitydragon.org/data/hashes/lol/";
    private const string HASHES_DIRECTORY = "hashes";
    private const string GAME_HASHES_FILENAME = "hashes.game.txt";
    private const string LCU_HASHES_FILENAME = "hashes.lcu.txt";
    private const string GAME_HASHES_PATH = $"{HASHES_DIRECTORY}/hashes.game.txt";
    private const string LCU_HASHES_PATH = $"{HASHES_DIRECTORY}/hashes.lcu.txt";

    private const string BIN_FIELDS_FILENAME = "hashes.binfields.txt";
    private const string BIN_CLASSES_FILENAME = "hashes.bintypes.txt";
    private const string BIN_HASHES_FILENAME = "hashes.binhashes.txt";
    private const string BIN_OBJECTS_FILENAME = "hashes.binentries.txt";
    private const string BIN_FIELDS_PATH = $"{HASHES_DIRECTORY}/hashes.binfields.txt";
    private const string BIN_CLASSES_PATH = $"{HASHES_DIRECTORY}/hashes.bintypes.txt";
    private const string BIN_HASHES_PATH = $"{HASHES_DIRECTORY}/hashes.binhashes.txt";
    private const string BIN_OBJECTS_PATH = $"{HASHES_DIRECTORY}/hashes.binentries.txt";

    // Constructor
    public HashtableService(Config config) {
        this.Config = config;
    }

    // Métodos
    public async Task Initialize() {
        using HttpClient client = new();

        Directory.CreateDirectory(HASHES_DIRECTORY);

        if (this.Config.SyncHashtables) {
            string hashFilesHtml = await client.GetStringAsync(HASHES_BASE_URL);
            await SyncHashtables(client, hashFilesHtml);
            await SyncBinHashtables(client, hashFilesHtml);
        }

        await InitializeHashtables();
        await InitializeBinHashtables();
    }

    private async Task InitializeHashtables() {
        Log.Information("Initializing hashtables");

        File.Open(GAME_HASHES_PATH, FileMode.OpenOrCreate).Dispose();
        File.Open(LCU_HASHES_PATH, FileMode.OpenOrCreate).Dispose();

        await LoadHashtable(GAME_HASHES_PATH);
        await LoadHashtable(LCU_HASHES_PATH);
    }

    private async Task InitializeBinHashtables() {
        Log.Information("Initializing BIN hashtables");

        File.Open(BIN_FIELDS_PATH, FileMode.OpenOrCreate).Dispose();
        File.Open(BIN_CLASSES_PATH, FileMode.OpenOrCreate).Dispose();
        File.Open(BIN_HASHES_PATH, FileMode.OpenOrCreate).Dispose();
        File.Open(BIN_OBJECTS_PATH, FileMode.OpenOrCreate).Dispose();

        await LoadBinHashtable(BIN_FIELDS_PATH, this.BinProperties);
        await LoadBinHashtable(BIN_CLASSES_PATH, this.BinClasses);
        await LoadBinHashtable(BIN_HASHES_PATH, this.BinHashes);
        await LoadBinHashtable(BIN_OBJECTS_PATH, this.BinObjects);
    }

    private async Task SyncHashtables(HttpClient client, string hashFilesHtml) {
        Log.Information("Syncing WAD hashtables");

        // Solo sincroniza el archivo que falta
        if (this.Config.SyncHashtables) {
            this.Config.GameHashesLastUpdate = await SyncHashtable(
                client,
                hashFilesHtml,
                HASHES_BASE_URL + GAME_HASHES_FILENAME,
                GAME_HASHES_PATH,
                this.Config.GameHashesLastUpdate
            );
            this.Config.LcuHashesLastUpdate = await SyncHashtable(
                client,
                hashFilesHtml,
                HASHES_BASE_URL + LCU_HASHES_FILENAME,
                LCU_HASHES_PATH,
                this.Config.LcuHashesLastUpdate
            );
        }
    }

    private async Task SyncBinHashtables(HttpClient client, string hashFilesHtml) {
        Log.Information("Syncing BIN hashtables");

        // Solo sincroniza el archivo que falta
        if (this.Config.SyncHashtables) {
            this.Config.BinFieldsHashesLastUpdate = await SyncHashtable(
                client,
                hashFilesHtml,
                HASHES_BASE_URL + BIN_FIELDS_FILENAME,
                BIN_FIELDS_PATH,
                this.Config.BinFieldsHashesLastUpdate
            );
            
            this.Config.BinTypesHashesLastUpdate = await SyncHashtable(
                client,
                hashFilesHtml,
                HASHES_BASE_URL + BIN_CLASSES_FILENAME,
                BIN_CLASSES_PATH,
                this.Config.BinTypesHashesLastUpdate
            );

            this.Config.BinHashesHashesLastUpdate = await SyncHashtable(
                client,
                hashFilesHtml,
                HASHES_BASE_URL + BIN_HASHES_FILENAME,
                BIN_HASHES_PATH,
                this.Config.BinHashesHashesLastUpdate
            );

            this.Config.BinEntriesHashesLastUpdate = await SyncHashtable(
                client,
                hashFilesHtml,
                HASHES_BASE_URL + BIN_OBJECTS_FILENAME,
                BIN_OBJECTS_PATH,
                this.Config.BinEntriesHashesLastUpdate
            );
        }
    }

    private static async Task<DateTime> SyncHashtable(
        HttpClient client,
        string hashFilesHtml,
        string url,
        string path,
        DateTime lastUpdateTime
    ) {
        // Obtener la fecha de modificación del servidor
        DateTime serverTime = ParseServerUpdateTime(hashFilesHtml, Path.GetFileName(url));
        
        // Verificar si el archivo existe
        if (!File.Exists(path)) {
            Log.Information($"File {path} does not exist. Downloading from {url}.");
            using Stream remoteFileContentStream = await client.GetStreamAsync(url);
            using FileStream localFileStream = File.Create(path);
            await remoteFileContentStream.CopyToAsync(localFileStream);
            return serverTime; // Establecer la fecha del servidor como última actualización
        }

        // Obtener el tamaño del archivo local
        long localFileSize = new FileInfo(path).Length;

        // Obtener el tamaño del archivo del servidor
        long serverFileSize = await GetServerFileSize(client, url);
        
        // Log para mostrar los tamaños de los archivos
        Log.Information($"Local file size of {path}: {localFileSize} bytes");
        Log.Information($"Server file size of {url}: {serverFileSize} bytes");

        // Comparar el tamaño de los archivos
        if (localFileSize >= serverFileSize) {
            Log.Information($"{path} is already up to date with the server based on file size.");
            // No se necesita descargar nada, no se hace nada aquí
        } else {
            // Si el archivo local es más pequeño, descargarlo
            Log.Information($"Local file {path} is smaller than the server version. Downloading hashtable: {path} from {url}");

            using Stream fileContentStream = await client.GetStreamAsync(url);
            using FileStream fileStream = File.Create(path);
            await fileContentStream.CopyToAsync(fileStream);
        }

        return serverTime; // Después de descargar, actualizamos la fecha
    }

    private static async Task<long> GetServerFileSize(HttpClient client, string url) {
        try {
            using var stream = await client.GetStreamAsync(url);
            using var memoryStream = new MemoryStream();

            // Copiar el contenido del stream al MemoryStream para calcular su tamaño
            await stream.CopyToAsync(memoryStream);
            return memoryStream.Length; // Obtener el tamaño del MemoryStream
        } catch (Exception e) {
            Log.Error(e, "Error retrieving file size from server.");
            return 0; // O un valor adecuado para indicar un error
        }
    }

    private static DateTime ParseServerUpdateTime(string html, string fileName) {
        var match = Regex.Match(html, $"""<a href="{fileName}\">{fileName}</a> *([^ ]+) *([^ ]+)""");
        if (!match.Success) throw new Exception($"Failed to find entry for file {fileName}");

        var date = DateOnly.Parse(match.Groups[1].Value, DateTimeFormatInfo.InvariantInfo);
        var time = TimeOnly.Parse(match.Groups[2].Value, DateTimeFormatInfo.InvariantInfo);
        return date.ToDateTime(time);
    }

    public async Task LoadHashtable(string hashtablePath) {
        using StreamReader reader = new(hashtablePath);
        string line;

        // Leer líneas de forma asincrónica
        while ((line = await reader.ReadLineAsync()) != null) { 
            var separatorIndex = line.IndexOf(' ');

            // Verificar si se encontró el separador
            if (separatorIndex == -1) continue; // O manejar el error de manera adecuada

            // Extraer la parte de la cadena que necesitas
            string hashString = line.Substring(0, separatorIndex);
            
            // Cambiar la forma en que se parsea el ulong
            if (ulong.TryParse(hashString, System.Globalization.NumberStyles.HexNumber, null, out ulong pathHash)) {
                this.Hashes.TryAdd(pathHash, line.Substring(separatorIndex + 1));
            }
        }
    }

    private async Task LoadBinHashtable(string hashtablePath, Dictionary<uint, string> hashtable) {
        using StreamReader reader = new(hashtablePath);
        string line;

        // Leer líneas de forma asincrónica
        while ((line = await reader.ReadLineAsync()) != null) { 
            string[] split = line.Split(' ', 2);

            // Asegurarse de que la línea tiene al menos dos partes
            if (split.Length < 2) continue; // O manejar el error de manera adecuada

            // Cambiar la forma en que se parsea el uint
            if (uint.TryParse(split[0], System.Globalization.NumberStyles.HexNumber, null, out uint hash)) {
                hashtable.TryAdd(hash, split[1]);
            }
        }   
    }


    public string GetChunkPath(WadChunk chunk) {
        if (this.Hashes.TryGetValue(chunk.PathHash, out string existingPath))
            return existingPath;

        return string.Format("{0:x16}", chunk.PathHash);
    }

    public bool TryGetChunkPath(WadChunk chunk, out string path) =>
        this.Hashes.TryGetValue(chunk.PathHash, out path);

    public static string GuessChunkPath(WadChunk chunk, WadFile wad) {
        string extension = chunk.Compression switch {
            WadChunkCompression.Satellite => null,
            _ => GuessChunkExtension(chunk, wad)
        };

        return string.IsNullOrEmpty(extension) switch {
            true => string.Format("{0:x16}", chunk.PathHash),
            false => string.Format("{0:x16}.{1}", chunk.PathHash, extension),
        };

        static string GuessChunkExtension(WadChunk chunk, WadFile wad) {
            using Stream stream = wad.LoadChunkDecompressed(chunk).AsStream();
            return LeagueFile.GetExtension(LeagueFile.GetFileType(stream));
        }
    }
}

