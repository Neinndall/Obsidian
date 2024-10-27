using System.Text.Json;

namespace Obsidian.Data;

public class Config {
    #region Wad Hashtable Checksums
    public DateTime GameHashesLastUpdate {
        get => this._gameHashesLastUpdate;
        set {
            this._gameHashesLastUpdate = value;
            Save();
        }
    }
    private DateTime _gameHashesLastUpdate;

    public DateTime LcuHashesLastUpdate {
        get => this._lcuHashesLastUpdate;
        set {
            this._lcuHashesLastUpdate = value;
            Save();
        }
    }
    private DateTime _lcuHashesLastUpdate;
    #endregion

    #region Bin Hashtable Checksums
    public DateTime BinFieldsHashesLastUpdate {
        get => this._binFieldsHashesLastUpdate;
        set {
            this._binFieldsHashesLastUpdate = value;
            Save();
        }
    }
    private DateTime _binFieldsHashesLastUpdate;

    public DateTime BinTypesHashesLastUpdate {
        get => this._binTypesHashesLastUpdate;
        set {
            this._binTypesHashesLastUpdate = value;
            Save();
        }
    }
    private DateTime _binTypesHashesLastUpdate;

    public DateTime BinHashesHashesLastUpdate {
        get => this._binHashesHashesLastUpdate;
        set {
            this._binHashesHashesLastUpdate = value;
            Save();
        }
    }
    private DateTime _binHashesHashesLastUpdate;

    public DateTime BinEntriesHashesLastUpdate {
        get => this._binEntriesHashesLastUpdate;
        set {
            this._binEntriesHashesLastUpdate = value;
            Save();
        }
    }
    private DateTime _binEntriesHashesLastUpdate;
    #endregion

    // Solo se guardan los tamaños de los archivos del servidor
    public long GameHashesFileSize {
        get => this._gameHashesFileSize;
        set {
            this._gameHashesFileSize = value;
            Save();
        }
    }
    private long _gameHashesFileSize;

    public long LcuHashesFileSize {
        get => this._lcuHashesFileSize;
        set {
            this._lcuHashesFileSize = value;
            Save();
        }
    }
    private long _lcuHashesFileSize;

    public long BinFieldsFileSize {
        get => this._binFieldsFileSize;
        set {
            this._binFieldsFileSize = value;
            Save();
        }
    }
    private long _binFieldsFileSize;

    public long BinClassesFileSize {
        get => this._binClassesFileSize;
        set {
            this._binClassesFileSize = value;
            Save();
        }
    }
    private long _binClassesFileSize;

    public long BinHashesFileSize {
        get => this._binHashesFileSize;
        set {
            this._binHashesFileSize = value;
            Save();
        }
    }
    private long _binHashesFileSize;

    public long BinObjectsFileSize {
        get => this._binObjectsFileSize;
        set {
            this._binObjectsFileSize = value;
            Save();
        }
    }
    private long _binObjectsFileSize;

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
