using System.Text;
using UnityEngine;

namespace JU.SaveLoad
{
    /// <summary>
    /// The base to save and load data to a component.
    /// </summary>
    public class JUSaveLoadComponent : MonoBehaviour, ISave, ILoad
    {
        private JUSaveLoadModeComponent _mode;

        private bool _isQuitting;
        private bool _runtimeStateCleared;
        private bool _loaded;
        private string _saveLoadSceneName;

        private string _saveLoadKeyBase;

        /// <summary>
        /// The scene used to store the save of this object (if <see cref="SaveLoadMode"/> is <see cref="SaveLoadModes.Scene"/>).
        /// </summary>
        private string SaveLoadSceneName
        {
            get
            {
                if (string.IsNullOrEmpty(_saveLoadSceneName))
                    _saveLoadSceneName = gameObject.scene.name;

                return _saveLoadSceneName;
            }
        }

        /// <summary>
        /// Return <see cref="JUSaveLoadModeComponent.Mode"/> if have an <see cref="JUSaveLoadModeComponent"/> instance.
        /// If not, return <see cref="SaveLoadModes.Scene"/>.
        /// </summary>
        public virtual SaveLoadModes SaveLoadMode
        {
            get
            {
                if (JUSaveLoadModeComponent.Instance)
                    return JUSaveLoadModeComponent.Instance.Mode;

                return SaveLoadModes.Scene;
            }
        }

        /// <summary>
        /// Create component instance.
        /// </summary>
        protected JUSaveLoadComponent()
        {
        }

        /// <summary>
        /// Called by editor to validate values.
        /// </summary>
        protected virtual void OnValidate()
        {
        }

        /// <summary>
        /// Called by editor to reset to default values.
        /// </summary>
        protected virtual void Reset()
        {
        }

        /// <summary>
        /// Called before <see cref="Start"/>.
        /// </summary>
        protected virtual void Awake()
        {
            _isQuitting = false;
            _runtimeStateCleared = false;
            JUSaveLoadManager.AddObjectToSave(this);
            Load();
        }

        /// <summary>
        /// Called before after <see cref="Awake"/> and <see cref="Start"/> on every time that enable the component.
        /// </summary>
        protected virtual void OnEnable()
        {
        }

        /// <summary>
        /// Called when the component is disabled.
        /// </summary>
        protected virtual void OnDisable()
        {
        }

        /// <summary>
        /// Called on destroy component.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_isQuitting)
            {
                OnExitPlayMode();
                return;
            }

            // Can't save if is unloading the scene.
            // The OnDestroy is called on scene unloading.
            if (!gameObject.scene.isLoaded)
            {
                OnExitPlayMode();
                return;
            }

            JUSaveLoadManager.RemoveObjectToSave(this);

            Save();
            OnExitPlayMode();
        }

        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
            OnExitPlayMode();
        }

        /// <summary>
        /// Start is called before the first frame update.
        /// </summary>
        protected virtual void Start()
        {
        }

        /// <summary>
        /// Called on every frame.
        /// </summary>
        protected virtual void Update()
        {
        }

        /// <summary>
        /// Send the changed data to <see cref="JUSaveLoad"/>. This does not write on save file.
        /// Call <see cref="JUSaveLoadManager.SaveOnFile"/> to write on file.
        /// </summary>
        public virtual void Save()
        {
            // Unsure that the data was loaded before to save. Used to not override the save data with default object data.
            if (!_loaded)
                Load();
        }

        /// <summary>
        /// Load the component data from <see cref="JUSaveLoad"/>.
        /// </summary>
        public virtual void Load()
        {
            _loaded = true;
        }

        protected void SetValue<T>(string key, T value)
        {
            key = GenerateFullKey(key);

            SaveLoadModes mode = SaveLoadModes.Scene;

            if (_mode)
                mode = _mode.Mode;

            switch (SaveLoadMode)
            {
                case SaveLoadModes.Scene:
                    JUSaveLoad.SetSceneValue(SaveLoadSceneName, key, value);
                    break;
                case SaveLoadModes.Global:
                    JUSaveLoad.SetGlobalValue(key, value);
                    break;
                default:
                    throw new UnityException("Invalid save mode.");
            }
        }

        protected T GetValue<T>(string key, T defaultValue)
        {
            key = GenerateFullKey(key);

            switch (SaveLoadMode)
            {
                case SaveLoadModes.Scene:
                    return JUSaveLoad.GetSceneValue<T>(SaveLoadSceneName, key, defaultValue);
                case SaveLoadModes.Global:
                    return JUSaveLoad.GetGlobalValue<T>(key, defaultValue);
                default:
                    throw new UnityException("Invalid save mode.");
            }
        }

        protected bool HasValue(string key)
        {
            key = GenerateFullKey(key);

            switch (SaveLoadMode)
            {
                case SaveLoadModes.Scene:
                    return JUSaveLoad.HasSceneValue(SaveLoadSceneName, key);
                case SaveLoadModes.Global:
                    return JUSaveLoad.HasGlobalValue(key);
                default:
                    throw new UnityException("Invalid save mode.");
            }
        }

        private string GenerateFullKey(string baseKey)
        {
            if (string.IsNullOrEmpty(_saveLoadKeyBase))
            {
                StringBuilder builder = new StringBuilder(GetType().Name);
                builder.Append(" - ");

                Transform root = gameObject.transform.root;
                if (root)
                {
                    builder.Append(root.name);
                    builder.Append(' ');
                }

                builder.Append(gameObject.name);
                _saveLoadKeyBase = builder.ToString();
            }

            return $"{_saveLoadKeyBase} {baseKey}";
        }

        /// <summary>
        /// Reset save/load runtime cache when the object is being destroyed or the application is quitting.
        /// </summary>
        protected virtual void OnExitPlayMode()
        {
            if (_runtimeStateCleared)
                return;

            _runtimeStateCleared = true;
            JUSaveLoadManager.RemoveObjectToSave(this);

            _loaded = false;
            _saveLoadKeyBase = null;
            _saveLoadSceneName = null;
        }
    }
}
