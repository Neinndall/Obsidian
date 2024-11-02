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

    // Clase para el Tamaño del Archivo
    public class ServerFileSizeWrapper {
        public long Size { get; set; }
    }
        
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
        Log.Information("Syncing WAD Hashtables...");
        if (this.Config.SyncHashtables) {
            
            var gameFileSizeWrapper = new ServerFileSizeWrapper();
            var lcuFileSizeWrapper = new ServerFileSizeWrapper();
            
            // Definir y sincronizar Game Hashes
            Log.Information("Syncing Game Hashes...");
            this.Config.ServerGameHashesLastUpdate = await SyncFile(
                client, 
                hashFilesHtml, 
                HASHES_BASE_URL + GAME_HASHES_FILENAME, 
                GAME_HASHES_PATH, 
                this.Config.ServerGameHashesLastUpdate, 
                gameFileSizeWrapper
            );
            Log.Information($"Game Hashes Last Update: {this.Config.ServerGameHashesLastUpdate}, File Size: {gameFileSizeWrapper.Size}");
            
            // Solo guardar el tamaño si ha cambiado
            if (gameFileSizeWrapper.Size > 0) {
                this.Config.ServerGameHashesFileSize = gameFileSizeWrapper.Size; // Guardar tamaño de archivo
                Log.Information($"Updated Game Hashes File Size: {this.Config.ServerGameHashesFileSize}");
            } else {
                Log.Information("No update for Game Hashes. Size remains unchanged.");
            }
            
            // Definir y sincronizar LCU Hashes
            Log.Information("Syncing LCU Hashes...");
            this.Config.ServerLcuHashesLastUpdate = await SyncFile(
                client, 
                hashFilesHtml, 
                HASHES_BASE_URL + LCU_HASHES_FILENAME, 
                LCU_HASHES_PATH, 
                this.Config.ServerLcuHashesLastUpdate, 
                lcuFileSizeWrapper
            );
            Log.Information($"LCU Hashes Last Update: {this.Config.ServerLcuHashesLastUpdate}, File Size: {lcuFileSizeWrapper.Size}");
            
            // Solo guardar el tamaño si ha cambiado
            if (lcuFileSizeWrapper.Size > 0) {
                this.Config.ServerLcuHashesFileSize = lcuFileSizeWrapper.Size; // Guardar tamaño de archivo
                Log.Information($"Updated LCU Hashes File Size: {this.Config.ServerLcuHashesFileSize}");
            } else {
                Log.Information("No update for LCU Hashes. Size remains unchanged.");
            }
        } else {
            Log.Information("Synchronization for hashtables is disabled.");
        }
    }

    private async Task SyncBinHashtables(HttpClient client, string hashFilesHtml) {
        Log.Information("Syncing BIN hashtables...");

        if (this.Config.SyncHashtables) {
            var binFieldsFileSizeWrapper = new ServerFileSizeWrapper();
            var binTypesFileSizeWrapper = new ServerFileSizeWrapper();
            var binHashesFileSizeWrapper = new ServerFileSizeWrapper();
            var binEntriesFileSizeWrapper = new ServerFileSizeWrapper();
            
            // Definir y sincronizar Bin Fields
            Log.Information("Syncing Bin Fields...");
            this.Config.ServerBinFieldsHashesLastUpdate = await SyncFile(
                client, 
                hashFilesHtml, 
                HASHES_BASE_URL + BIN_FIELDS_FILENAME, 
                BIN_FIELDS_PATH, 
                this.Config.ServerBinFieldsHashesLastUpdate, 
                binFieldsFileSizeWrapper
            );
            Log.Information($"Bin Fields Last Update: {this.Config.ServerBinFieldsHashesLastUpdate}, File Size: {binFieldsFileSizeWrapper.Size}");
            
            // Solo guardar el tamaño si ha cambiado
            if (binFieldsFileSizeWrapper.Size > 0) {
                this.Config.ServerBinFieldsFileSize = binFieldsFileSizeWrapper.Size; // Guardar tamaño de archivo
            }
            
            // Definir y sincronizar Bin Classes
            Log.Information("Syncing Bin Classes...");
            this.Config.ServerBinTypesHashesLastUpdate = await SyncFile(
                client, 
                hashFilesHtml, 
                HASHES_BASE_URL + BIN_CLASSES_FILENAME, 
                BIN_CLASSES_PATH, 
                this.Config.ServerBinTypesHashesLastUpdate, 
                binTypesFileSizeWrapper
            );
            Log.Information($"Bin Classes Last Update: {this.Config.ServerBinTypesHashesLastUpdate}, File Size: {binTypesFileSizeWrapper.Size}");
            
            // Solo guardar el tamaño si ha cambiado
            if (binTypesFileSizeWrapper.Size > 0) {
                this.Config.ServerBinClassesFileSize = binTypesFileSizeWrapper.Size; // Guardar tamaño de archivo
            }
            
            // Definir y sincronizar Bin Hashes
            Log.Information("Syncing Bin Hashes...");
            this.Config.ServerBinHashesHashesLastUpdate = await SyncFile(
                client, 
                hashFilesHtml, 
                HASHES_BASE_URL + BIN_HASHES_FILENAME, 
                BIN_HASHES_PATH, 
                this.Config.ServerBinHashesHashesLastUpdate, 
                binHashesFileSizeWrapper
            );
            Log.Information($"Bin Hashes Last Update: {this.Config.ServerBinHashesHashesLastUpdate}, File Size: {binHashesFileSizeWrapper.Size}");
            
            // Solo guardar el tamaño si ha cambiado
            if (binHashesFileSizeWrapper.Size > 0) {
                this.Config.ServerBinHashesFileSize = binHashesFileSizeWrapper.Size; // Guardar tamaño de archivo
            }
            
            // Definir y sincronizar Bin Objects
            Log.Information("Syncing Bin Objects...");
            this.Config.ServerBinEntriesHashesLastUpdate = await SyncFile(
                client, 
                hashFilesHtml, 
                HASHES_BASE_URL + BIN_OBJECTS_FILENAME, 
                BIN_OBJECTS_PATH, 
                this.Config.ServerBinEntriesHashesLastUpdate, 
                binEntriesFileSizeWrapper
            );
            Log.Information($"Bin Objects Last Update: {this.Config.ServerBinEntriesHashesLastUpdate}, File Size: {binEntriesFileSizeWrapper.Size}");
            
            // Solo guardar el tamaño si ha cambiado
            if (binEntriesFileSizeWrapper.Size > 0) {
                this.Config.ServerBinObjectsFileSize = binEntriesFileSizeWrapper.Size; // Guardar tamaño de archivo
            }
        }
    }
    
    private async Task<bool> ShouldSyncFile(string filePath, long serverConfigFileSize, DateTime localConfigLastUpdateTime, HttpClient client, string url, string hashFilesHtml) {
        if (!File.Exists(filePath)) {
            Log.Information($"File {filePath} does not exist. Need to download from {url}.");
            return true; 
        }

        DateTime serverTime = ParseServerUpdateTime(hashFilesHtml, Path.GetFileName(url));
        Log.Information($"Checking file: {filePath}. Last Update Time: {localConfigLastUpdateTime}, Server Time: {serverTime}");

        // Obtén el tamaño de los hashes locales
        long localFileSize = new FileInfo(filePath).Length;

        // Obtén el tamaño de los hashes del servidor
        long currentServerFileSize = await GetServerFileSize(client, url);
        
        // Solo si el servidor ha sido actualizado procedemos a ...
        if (serverTime > localConfigLastUpdateTime) {
            Log.Information($"Local File Size: {localFileSize}, Server File Size: {currentServerFileSize}");
            
            // Actualiza el tiempo de última actualización del servidor
            this.Config.ServerGameHashesLastUpdate = serverTime;
            this.Config.ServerLcuHashesLastUpdate = serverTime;
            this.Config.ServerBinFieldsHashesLastUpdate = serverTime;
            this.Config.ServerBinTypesHashesLastUpdate = serverTime;
            this.Config.ServerBinHashesHashesLastUpdate = serverTime;
            this.Config.ServerBinEntriesHashesLastUpdate = serverTime;

            if (localFileSize == currentServerFileSize) {
                Log.Information($"File {filePath} is already up to date. No need to download.");
                return false; // No hay necesidad de sincronizar
                
            } else if (localFileSize < serverConfigFileSize) {
                Log.Information($"File size mismatch or outdated for {filePath}. Need to download.");
                return true; // Sincroniza para obtenerlos actualizados
            }

        } else {
            // Si el servidor no ha sido actualizado procedemos a ...
            Log.Information($"Local File Size: {localFileSize}, Server Config File Size: {serverConfigFileSize}");
            
            // Comparar el tamaño de los hashes locales vs 
            if (localFileSize != serverConfigFileSize) {
                Log.Information($"File size mismatch or outdated for {filePath}. Need to download.");
                return true; // Sincroniza para obtenerlos actualizados
            }
        }

        // Este es el camino que faltaba para devolver un valor en caso de que no se cumplan las condiciones anteriores
        Log.Information($"File {filePath} is up to date. No need to download.");
        return false; // Si ninguna condición se cumple, se considera que está actualizado
    }

    private async Task<DateTime> SyncFile(HttpClient client, string hashFilesHtml, string url, string filePath, DateTime localConfigLastUpdateTime, ServerFileSizeWrapper serverFileSizeWrapper) {
        
        // Obtener la fecha de modificación del archivo del servidor
        DateTime serverTime = ParseServerUpdateTime(hashFilesHtml, Path.GetFileName(url));
        
        // Obtiene el tamaño del archivo del servidor
        long currentServerFileSize = await GetServerFileSize(client, url);
        
        // Sincronizar si es necesario
        if (await ShouldSyncFile(filePath, currentServerFileSize, localConfigLastUpdateTime, client, url, hashFilesHtml)) {
            localConfigLastUpdateTime = await SyncHashtable(client, hashFilesHtml, url, filePath, serverTime, serverFileSizeWrapper);
        } else if (localConfigLastUpdateTime == DateTime.MinValue) {
            localConfigLastUpdateTime = serverTime;
        }

        // Actualizar el tamaño del archivo después de la sincronización
        serverFileSizeWrapper.Size = new FileInfo(filePath).Length; // Actualiza el tamaño del archivo local

        return localConfigLastUpdateTime;
    }

    private static async Task<DateTime> SyncHashtable(HttpClient client, string hashFilesHtml, string url, string filePath, DateTime serverTime, ServerFileSizeWrapper serverFileSizeWrapper) {
        Log.Information($"Syncing hashtable from {url}...");
       
        // Tamaño de hashes del servidor
        long currentServerFileSize = await GetServerFileSize(client, url);

        using var fileContentStream = await client.GetStreamAsync(url);
        using var fileStream = File.Create(filePath);
        await fileContentStream.CopyToAsync(fileStream);
        serverFileSizeWrapper.Size = currentServerFileSize; // Guardar el tamaño del archivo del servidor

        Log.Information($"Successfully synced hashtable: {filePath}");
        string formattedDate = serverTime.ToString("dd-MMM-yyyy HH:mm");
        Log.Information($"Last update time: {formattedDate}");

        return serverTime;
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
    
            // Copiar el contenido del stream al MemoryStream para calcular su tamaño
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