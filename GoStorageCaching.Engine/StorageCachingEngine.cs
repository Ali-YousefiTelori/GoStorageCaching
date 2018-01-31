using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoStorageCaching
{
    public enum DefaultTableNames
    {
        FileInfo,
        IconInfo
    }

    public static class StorageCachingEngine
    {
        static string _DataBasePath;

        public static string DataBasePath
        {
            get
            {
                return _DataBasePath;
            }
            set
            {
                _DataBasePath = value;
                DataBaseFilePath = Path.Combine(DataBasePath, "GoStorageCaching.db");
            }
        }

        static string DataBaseFilePath { get; set; } = "GoStorageCaching.db";

        internal static void AddOrUpdate(DefaultTableNames tableName, int fileId, string savePath, DateTime lastUpdate, bool isComplete)
        {
            AddOrUpdate(tableName.ToString(), fileId, savePath, lastUpdate, isComplete);
        }

        internal static void AddOrUpdate(string tableName, int fileId, string savePath, DateTime lastUpdate, bool isComplete)
        {
            // Open database (or create if not exits)
            using (var db = new LiteDatabase(DataBaseFilePath))
            {
                var files = db.GetCollection<FileCachInfo>(tableName);
                var fileInfo = files.FindOne(x => x.FileId == fileId);
                if (fileInfo == null)
                {
                    fileInfo = new FileCachInfo
                    {
                        FileId = fileId,
                        LastUpdateDateTime = lastUpdate,
                        SavePath = savePath,
                        IsComplete = isComplete
                    };
                    files.EnsureIndex(x => x.FileId);
                    files.Insert(fileInfo);
                }
                else
                {
                    fileInfo.LastUpdateDateTime = lastUpdate;
                    fileInfo.SavePath = savePath;
                    fileInfo.IsComplete = isComplete;
                    files.Update(fileInfo);
                }
            }
        }

        internal static void Add(DefaultTableNames tableName, int fileId, string savePath, DateTime lastUpdate)
        {
            Add(tableName.ToString(), fileId, savePath, lastUpdate);
        }

        internal static void Add(string tableName, int fileId, string savePath, DateTime lastUpdate)
        {
            // Open database (or create if not exits)
            using (var db = new LiteDatabase(DataBaseFilePath))
            {
                var files = db.GetCollection<FileCachInfo>(tableName);
                var fileInfo = new FileCachInfo
                {
                    FileId = fileId,
                    LastUpdateDateTime = lastUpdate,
                    SavePath = savePath
                };
                files.EnsureIndex(x => x.FileId);
                files.Insert(fileInfo);
            }
        }

        internal static FileCachInfo Exist(DefaultTableNames tableName, int fileId)
        {
            return Exist(tableName.ToString(), fileId);
        }

        internal static FileCachInfo Exist(string tableName, int fileId)
        {
            // Open database (or create if not exits)
            using (var db = new LiteDatabase(DataBaseFilePath))
            {
                var files = db.GetCollection<FileCachInfo>(tableName);

                return files.FindOne(x => x.FileId == fileId);
            }
        }

        internal static void Delete(string tableName, int fileId)
        {
            // Open database (or create if not exits)
            using (var db = new LiteDatabase(DataBaseFilePath))
            {
                var files = db.GetCollection<FileCachInfo>(tableName);

                files.Delete(x => x.FileId == fileId);
            }
        }
    }
}
