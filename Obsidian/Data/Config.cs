using System.Text.Json;

namespace Obsidian.Data;

public class Config {
    #region Wad Hashtable Checksums
    public DateTime ServerGameHashesLastUpdate {
        get => this._serverGameHashesLastUpdate;
        set {
            this._serverGameHashesLastUpdate = value;
            Save();
        }
    }
    private DateTime _serverGameHashesLastUpdate;

    public DateTime ServerLcuHashesLastUpdate {
        get => this._serverLcuHashesLastUpdate;
        set {
            this._serverLcuHashesLastUpdate = value;
            Save();
        }
    }
    private DateTime _serverLcuHashesLastUpdate;
    #endregion

    #region Bin Hashtable Checksums
    public DateTime ServerBinFieldsHashesLastUpdate {
        get => this._serverBinFieldsHashesLastUpdate;
        set {
            this._serverBinFieldsHashesLastUpdate = value;
            Save();
        }
    }
    private DateTime _serverBinFieldsHashesLastUpdate;

    public DateTime ServerBinTypesHashesLastUpdate {
        get => this._serverBinTypesHashesLastUpdate;
        set {
            this._serverBinTypesHashesLastUpdate = value;
            Save();
        }
    }
    private DateTime _serverBinTypesHashesLastUpdate;

    public DateTime ServerBinHashesHashesLastUpdate {
        get => this._serverBinHashesHashesLastUpdate;
        set {
            this._serverBinHashesHashesLastUpdate = value;
            Save();
        }
    }
    private DateTime _serverBinHashesHashesLastUpdate;

    public DateTime ServerBinEntriesHashesLastUpdate {
        get => this._serverBinEntriesHashesLastUpdate;
        set {
            this._serverBinEntriesHashesLastUpdate = value;
            Save();
        }
    }
    private DateTime _serverBinEntriesHashesLastUpdate;
    #endregion

    // Tamaño del archivo de hashes del servidor
    public long ServerGameHashesFileSize {
        get => this._serverGameHashesFileSize;
        set {
            this._serverGameHashesFileSize = value;
            Save();
        }
    }
    private long _serverGameHashesFileSize;

    public long ServerLcuHashesFileSize {
        get => this._serverLcuHashesFileSize;
        set {
            this._serverLcuHashesFileSize = value;
            Save();
        }
    }
    private long _serverLcuHashesFileSize;

    public long ServerBinFieldsFileSize {
        get => this._serverBinFieldsFileSize;
        set {
            this._serverBinFieldsFileSize = value;
            Save();
        }
    }
    private long _serverBinFieldsFileSize;

    public long ServerBinClassesFileSize {
        get => this._serverBinClassesFileSize;
        set {
            this._serverBinClassesFileSize = value;
            Save();
        }
    }
    private long _serverBinClassesFileSize;

    public long ServerBinHashesFileSize {
        get => this._serverBinHashesFileSize;
        set {
            this._serverBinHashesFileSize = value;
            Save();
        }
    }
    private long _serverBinHashesFileSize;

    public long ServerBinObjectsFileSize {
        get => this._serverBinObjectsFileSize;
        set {
            this._serverBinObjectsFileSize = value;
            Save();
        }
    }
    private long _serverBinObjectsFileSize;

    public bool DoNotRequireGameDirectory {
        get => this._doNotRequireGameDirectory;
        set {
            this._doNotRequireGameDirectory = value;
            Save();
        }
    }
    private bool _doNotRequireGameDirectory;

    public string GameDataDirectory {
        get => this._gameDataDirectory;
        set {
            this._gameDataDirectory = value;
            Save();
        }
    }
    private string _gameDataDirectory;

    public string DefaultExtractDirectory {
        get => this._defaultExportDirectory;
        set {
            this._defaultExportDirectory = value;
            Save();
        }
    }
    private string _defaultExportDirectory;

    public bool SyncHashtables {
        get => this._syncHashtables;
        set {
            this._syncHashtables = value;
            Save();
        }
    }
    private bool _syncHashtables = true;

    public bool IsRichPresenceEnabled {
        get => this._isRichPresenceEnabled;
        set {
            this._isRichPresenceEnabled = value;
            Save();
        }
    }
    private bool _isRichPresenceEnabled = true;

    public bool LoadSkinnedMeshAnimations {
        get => this._loadSkinnedMeshAnimations;
        set {
            this._loadSkinnedMeshAnimations = value;
            Save();
        }
    }
    private bool _loadSkinnedMeshAnimations = false;

    public bool ShouldPreviewSelectedItems {
        get => this._shouldPreviewSelectedItems;
        set {
            this._shouldPreviewSelectedItems = value;
            Save();
        }
    }
    private bool _shouldPreviewSelectedItems = true;

    private const string CONFIG_FILE = "config.json";

    public Config() { }

    public static Config Load() {
        if (!File.Exists(CONFIG_FILE))
            return new();

        using FileStream configStream = File.OpenRead(CONFIG_FILE);
        return JsonSerializer.Deserialize<Config>(configStream);
    }

    public void Save() {
        try {
            File.WriteAllText(
                CONFIG_FILE,
                JsonSerializer.Serialize(
                    this,
                    new JsonSerializerOptions() { AllowTrailingCommas = true, WriteIndented = true }
                )
            );
        } catch (Exception) { }
    }
}
