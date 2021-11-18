using BlackTundra.Foundation;
using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Serialization;

using System;
using System.Collections.Generic;

using Console = BlackTundra.Foundation.Console;
using Object = UnityEngine.Object;

namespace BlackTundra.World.Items {

    public sealed class ItemData {

        #region constant

        /// <summary>
        /// Maximum (inclusive) value that either the <see cref="width"/> or <see cref="height"/> of an item can be.
        /// </summary>
        public const int MaxLength = byte.MaxValue;

        /// <summary>
        /// <see cref="FileSystemReference"/> to the item database.
        /// </summary>
        internal static readonly FileSystemReference DatabaseFSR = new FileSystemReference(
            string.Concat(FileSystem.LocalDataDirectory, "items.dat"),
            true,
            false
        );

        /// <summary>
        /// <see cref="FileFormat"/> used to generate the item database.
        /// </summary>
        internal const FileFormat DatabaseFormat = FileFormat.Obfuscated;

        #endregion

        #region variable

        public readonly int id;

        public readonly string name;

        public readonly string description;

        public readonly int width;

        public readonly int height;

        public readonly Dictionary<string, Object> resources;

        /// <summary>
        /// Array of each <see cref="ItemData"/> entry.
        /// </summary>
        private static ItemData[] items = new ItemData[0];

        #endregion

        #region property

        public static int ItemCount => items.Length;

        #endregion

        #region constructor

        private ItemData() => throw new InvalidOperationException();

        private ItemData(
            in int id,
            in string name,
            in string description,
            in int width,
            in int height,
            in Dictionary<string, Object> resources
        ) {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (description == null) throw new ArgumentNullException(nameof(description));
            if (width < 1 || width > MaxLength) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1 || height > MaxLength) throw new ArgumentOutOfRangeException(nameof(height));
            if (resources == null) throw new ArgumentNullException(nameof(resources));
            this.id = id;
            this.name = name;
            this.description = description;
            this.width = width;
            this.height = height;
            this.resources = resources;
        }

        #endregion

        #region InitialiseItemDatabase

        /// <summary>
        /// Initialises the <see cref="ItemData"/> database.
        /// </summary>
        [CoreInitialise(int.MinValue + 1)] // this must be executed after ItemResources
        private static void InitialiseItemDatabase() {
            try {
                ReloadDatabase();
            } catch (Exception exception) {
                Core.Quit(QuitReason.FatalCrash, "Failed to initialise item database.", exception, true);
            }
        }

        #endregion

        #region Validate
#if UNITY_EDITOR

        [UnityEditor.MenuItem("Tools/Item/Validate Database")]
        private static void Validate() {
            try {
                ReloadDatabase();
            } catch (Exception exception) {
                UnityEngine.Debug.LogException(exception);
                UnityEngine.Debug.LogError("Database invalid.");
                return;
            }
            UnityEngine.Debug.Log("Database valid.");
        }

#endif
        #endregion

        #region ReloadDatabase

        private static void ReloadDatabase() {
            if (FileSystem.Read(DatabaseFSR, out byte[] database, DatabaseFormat)) {
                Console.Info("Item database found.");
                // setup to read from database:
                SerializedByteArrayReader reader = new SerializedByteArrayReader(database);
                int itemCount = reader.ReadNext<int>();
                ItemData[] items = new ItemData[itemCount];
                // read each entry:
                int id;
                string name, description, key, guid;
                int width, height, resourceCount;
                Dictionary<string, Object> resources;
                for (int i = 0; i < itemCount; i++) {
                    id = reader.ReadNext<int>();
                    if (id != i) throw new Exception($"Failed to read item database entry {i} due to index mismatch.");
                    name = reader.ReadNext<string>();
                    description = reader.ReadNext<string>();
                    width = reader.ReadNext<byte>();
                    height = reader.ReadNext<byte>();
                    resourceCount = reader.ReadNext<byte>();
                    resources = new Dictionary<string, Object>();
                    for (int j = resourceCount - 1; j >= 0; j--) {
                        key = reader.ReadNext<string>();
                        guid = reader.ReadNext<string>();
                        resources.Add(key, ItemResources.GetResource(guid));
                    }
                    items[i] = new ItemData(id, name, description, width, height, resources);
                }
                Console.Info($"Discovered {itemCount} items.");
                ItemData.items = items;
            }
        }

        #endregion

        #region GetItem

        public static ItemData GetItem(in int id) {
            if (id < 0 || id >= items.Length) throw new ArgumentOutOfRangeException(nameof(id));
            return items[id];
        }

        public static ItemData GetItem(in string name) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            ItemData item;
            for (int i = items.Length - 1; i >= 0; i--) {
                item = items[i];
                if (name.Equals(item.name)) return item;
            }
            //throw new KeyNotFoundException(name);
            return null;
        }

        #endregion

    }

}