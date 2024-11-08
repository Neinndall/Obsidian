using CommunityToolkit.HighPerformance;
using LeagueToolkit.Core.Wad;
using LeagueToolkit.Utils;
using Obsidian.Data;
using Serilog;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Obsidian.Services;

public class HashtableService {
    public Config Config { get; }

    // Declaración de propiedades
    public Dictionary<ulong, string> Hashes { get; private set; } = new();
    public Dictionary<uint, string> BinTypes { get; private set; } = new();
    public Dictionary<uint, string> BinFields { get; private set; } = new();
    public Dictionary<uint, string> BinHashes { get; private set; } = new();
    public Dictionary<uint, string> BinEntries { get; private set; } = new();

    private const string HASHES_BASE_URL = "https://raw.communitydragon.org/data/hashes/lol/";
    private const string HASHES_DIRECTORY = "hashes";
    private const string GAME_HASHES_FILENAME = "hashes.game.txt";
    private const string LCU_HASHES_FILENAME = "hashes.lcu.txt";
    private const string GAME_HASHES_PATH = $"{HASHES_DIRECTORY}/hashes.game.txt";
    private const string LCU_HASHES_PATH = $"{HASHES_DIRECTORY}/hashes.lcu.txt";

    private const string BIN_FIELDS_FILENAME = "hashes.binfields.txt";
    private const string BIN_TYPES_FILENAME = "hashes.bintypes.txt";
    private const string BIN_HASHES_FILENAME = "hashes.binhashes.txt";
    private const string BIN_ENTRIES_FILENAME = "hashes.binentries.txt";
    
    private const string BIN_FIELDS_PATH = $"{HASHES_DIRECTORY}/hashes.binfields.txt";
    private const string BIN_TYPES_PATH = $"{HASHES_DIRECTORY}/hashes.bintypes.txt";
    private const string BIN_HASHES_PATH = $"{HASHES_DIRECTORY}/hashes.binhashes.txt";
    private const string BIN_ENTRIES_PATH = $"{HASHES_DIRECTORY}/hashes.binentries.txt";

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

            // Proporcionar valores para los tamaños de hash locales
            long localGameHashesSize = this.Config.GameHashesFileSize;
            long localLcuHashesSize = this.Config.LcuHashesFileSize;
            
            // Proporcionar valores para los tamaños de hash locales de bin
            long localBinFieldsSize = this.Config.BinFieldsFileSize; 
            long localBinTypesSize = this.Config.BinTypesFileSize;
            long localBinHashesSize = this.Config.BinHashesFileSize;
            long localBinEntriesSize = this.Config.BinEntriesFileSize;

            await SyncHashtables(client, hashFilesHtml, localGameHashesSize, localLcuHashesSize);
            await SyncBinHashtables(client, hashFilesHtml, localBinFieldsSize, localBinTypesSize, localBinHashesSize, localBinEntriesSize);

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
        File.Open(BIN_TYPES_PATH, FileMode.OpenOrCreate).Dispose();
        File.Open(BIN_HASHES_PATH, FileMode.OpenOrCreate).Dispose();
        File.Open(BIN_ENTRIES_PATH, FileMode.OpenOrCreate).Dispose();

        await LoadBinHashtable(BIN_FIELDS_PATH, this.BinFields);
        await LoadBinHashtable(BIN_TYPES_PATH, this.BinTypes);
        await LoadBinHashtable(BIN_HASHES_PATH, this.BinHashes);
        await LoadBinHashtable(BIN_ENTRIES_PATH, this.BinEntries);
    }

    // Clase para el Tamaño del Archivo
    public class ServerHashSize {
        public long Size { get; set; }
    }

    private bool ShouldSyncHash(string filePath, long localHashesSize, DateTime configLastUpdate, DateTime serverUpdate, long serverSize) {
        // Log los valores que se están comparando
        Log.Information($"Checking sync for {filePath}:");
        Log.Information($"Local Hashes Size: {localHashesSize}, Server Size: {serverSize}");
        Log.Information($"Config Last Update: {configLastUpdate}, Server Update: {serverUpdate}");
    
        // Comprobamos si el archivo existe, si su tamaño coincide, o si su fecha es antigua.
        return !File.Exists(filePath) || localHashesSize < serverSize;
    }
    
       // configLastUpdate != serverUpdate && localHashesSize != serverSize;


    private async Task SyncHashtables(HttpClient client, string hashFilesHtml, long localGameHashesSize, long localLcuHashesSize) {
        await SyncHashIfNeeded(client, hashFilesHtml, GAME_HASHES_FILENAME, GAME_HASHES_PATH, 
                                this.Config.GameHashesLastUpdate, localGameHashesSize, 
                                UpdateConfigForHashes, UpdateConfigForBinFile);
        await SyncHashIfNeeded(client, hashFilesHtml, LCU_HASHES_FILENAME, LCU_HASHES_PATH, 
                                this.Config.LcuHashesLastUpdate, localLcuHashesSize, 
                                UpdateConfigForHashes, UpdateConfigForBinFile);
    }


    private async Task SyncBinHashtables(HttpClient client, string hashFilesHtml, long localBinFieldsSize, long localBinTypesSize, long localBinHashesSize, long localBinEntriesSize) {
        await SyncHashIfNeeded(client, hashFilesHtml, BIN_FIELDS_FILENAME, BIN_FIELDS_PATH, 
                                this.Config.BinFieldsHashesLastUpdate, localBinFieldsSize, 
                                UpdateConfigForHashes, UpdateConfigForBinFile);
        await SyncHashIfNeeded(client, hashFilesHtml, BIN_TYPES_FILENAME, BIN_TYPES_PATH, 
                                this.Config.BinTypesHashesLastUpdate, localBinTypesSize, 
                                UpdateConfigForHashes, UpdateConfigForBinFile);
        await SyncHashIfNeeded(client, hashFilesHtml, BIN_HASHES_FILENAME, BIN_HASHES_PATH, 
                                this.Config.BinHashesLastUpdate, localBinHashesSize, 
                                UpdateConfigForHashes, UpdateConfigForBinFile);
        await SyncHashIfNeeded(client, hashFilesHtml, BIN_ENTRIES_FILENAME, BIN_ENTRIES_PATH, 
                                this.Config.BinEntriesHashesLastUpdate, localBinEntriesSize, 
                                UpdateConfigForHashes, UpdateConfigForBinFile);
    }

   
    private async Task SyncHashIfNeeded(HttpClient client, string hashFilesHtml, string filename, string filePath, 
                                        DateTime configLastUpdate, long localHashesSize, 
                                        Action<string, DateTime, long> UpdateConfigforHashes,
                                        Action<string, DateTime, long> updateConfigForBinFile) {

        DateTime serverUpdate = ParseServerUpdateTime(hashFilesHtml, filename);
        long serverSize = await GetServerFileSize(client, HASHES_BASE_URL + filename);

        // Log los resultados de la consulta al servidor
        Log.Information($"Server update time for {filename}: {serverUpdate}");
        Log.Information($"Server file size for {filename}: {serverSize}");

        if (ShouldSyncHash(filePath, localHashesSize, configLastUpdate, serverUpdate, serverSize)) {
            Log.Information($"{filename} needs to be synced...");
            var result = await SyncHash(client, hashFilesHtml, filename, filePath, configLastUpdate, localHashesSize);

            // Usamos la función adecuada según el tipo de archivo
            if (filename.Contains("bin")) { 
                updateConfigForBinFile(filename, result.configLastUpdate, result.configHashSize);
            } else {
                UpdateConfigforHashes(filename, result.configLastUpdate, result.configHashSize);
            }

            Log.Information($"Updated {filename}. New configLastUpdate: {result.configLastUpdate}, New configHashSize: {result.configHashSize}");
        } else {
            // Comprobar si solo se necesita actualizar la configuración
            if (configLastUpdate != serverUpdate || localHashesSize != serverSize) {
                Log.Information($"{filename} config needs to be updated.");
                if (filename.Contains("bin")) {
                    updateConfigForBinFile(filename, serverUpdate, serverSize);
                } else {
                    UpdateConfigforHashes(filename, serverUpdate, serverSize);
                }
            } else {
                Log.Information($"{filename} is up to date.");
            }
        }
    }

    // Method to update configuration for hash files
    private void UpdateConfigForHashes(string filename, DateTime configLastUpdate, long configHashSize) {
        switch (filename) {
            case GAME_HASHES_FILENAME:
                this.Config.GameHashesLastUpdate = configLastUpdate;
                this.Config.GameHashesFileSize = configHashSize;
                break;
            case LCU_HASHES_FILENAME:
                this.Config.LcuHashesLastUpdate = configLastUpdate;
                this.Config.LcuHashesFileSize = configHashSize;
                break;
        }
    }
    
    // Method to update configuration for bin files
    private void UpdateConfigForBinFile(string filename, DateTime configLastUpdate, long configHashSize) {
        switch (filename) {
            case BIN_FIELDS_FILENAME:
                this.Config.BinFieldsHashesLastUpdate = configLastUpdate;
                this.Config.BinFieldsFileSize = configHashSize;
                break;
            case BIN_TYPES_FILENAME:
                this.Config.BinTypesHashesLastUpdate = configLastUpdate;
                this.Config.BinTypesFileSize = configHashSize;
                break;
            case BIN_HASHES_FILENAME:
                this.Config.BinHashesLastUpdate = configLastUpdate;
                this.Config.BinHashesFileSize = configHashSize;
                break;
            case BIN_ENTRIES_FILENAME:
                this.Config.BinEntriesHashesLastUpdate = configLastUpdate;
                this.Config.BinEntriesFileSize = configHashSize;
                break;
        }
    }

    private async Task<(DateTime configLastUpdate, long configHashSize)> SyncHash(HttpClient client, string hashFilesHtml, string filename, string filePath, 
        DateTime configLastUpdate, long localHashesSize) {
            
        var hashSizeWrapper = new ServerHashSize();
        configLastUpdate = await SyncFile(client, hashFilesHtml, HASHES_BASE_URL + filename, filePath, configLastUpdate, hashSizeWrapper);
        Log.Information($"{filename} configLastUpdate: {configLastUpdate}, configHashSize from Server: {hashSizeWrapper.Size}");
        long configHashSize = hashSizeWrapper.Size;
        return (configLastUpdate, configHashSize);
    }
    
    
    private async Task<DateTime> SyncFile(HttpClient client, string hashFilesHtml, string url, string filePath, DateTime configLastUpdate, ServerHashSize serverSize) {
        
        DateTime serverLastUpdate = ParseServerUpdateTime(hashFilesHtml, Path.GetFileName(url));
        serverSize.Size = await GetServerFileSize(client, url);

        if (!File.Exists(filePath) || serverLastUpdate > configLastUpdate || serverSize.Size != new FileInfo(filePath).Length) {
            await DownloadFile(client, url, filePath);
            return serverLastUpdate;
        }

        return configLastUpdate;
    }

    private async Task DownloadFile(HttpClient client, string url, string filePath) {
        using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        using Stream stream = await response.Content.ReadAsStreamAsync();
        using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream);
    }

    private static DateTime ParseServerUpdateTime(string html, string fileName) {
        var match = Regex.Match(html, $"""<a href="{fileName}\">{fileName}</a> *([^ ]+) *([^ ]+)""");
        if (!match.Success) throw new Exception($"Failed to find entry for file {fileName}");

        var date = DateOnly.Parse(match.Groups[1].Value, DateTimeFormatInfo.InvariantInfo);
        var time = TimeOnly.Parse(match.Groups[2].Value, DateTimeFormatInfo.InvariantInfo);
        return date.ToDateTime(time);
    }
    
    private static async Task<long> GetServerFileSize(HttpClient client, string url) {
        try {
            using var stream = await client.GetStreamAsync(url);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            return memoryStream.Length; // Obtener el tamaño del MemoryStream
        } catch (Exception e) {
            Log.Error(e, "Error retrieving file size from server.");
            return 0; // O un valor adecuado para indicar un error
        }
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