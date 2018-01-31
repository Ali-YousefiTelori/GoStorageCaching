using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoStorageCaching
{
    public enum FileDownloadStatus
    {
        Success,
        Error
    }

    public class FileDownloadInfo : IDisposable
    {
        internal int FileId { get; private set; }
        internal string TableName { get; private set; }
        internal string SavePath { get; set; }

        internal Action<FileDownloadStatus, string, Exception> FinishAction { get; set; }
        internal Func<(Stream Stream, long FileLength)> GetStreamFunction { get; set; }
        internal Func<DateTime> GetLastUpdateFunction { get; set; }

        public FileDownloadInfo(int fileId, string tableName, string savePath, Func<DateTime> getLastUpdateFunction,
            Func<(Stream, long)> getStreamFunction, Action<FileDownloadStatus, string, Exception> finishAction)
        {
            SavePath = savePath;
            FileId = fileId;
            TableName = tableName;
            FinishAction = finishAction;
            GetStreamFunction = getStreamFunction;
            GetLastUpdateFunction = getLastUpdateFunction;
            DownloadStreamEngine.Add(this);
        }

        public FileDownloadInfo(int fileId, DefaultTableNames tableName, string savePath, Func<DateTime> getLastUpdateFunction
            , Func<(Stream, long)> getStreamFunction, Action<FileDownloadStatus, string, Exception> finishAction)
            : this(fileId, tableName.ToString(), savePath, getLastUpdateFunction, getStreamFunction, finishAction)
        {

        }

        public override bool Equals(object obj)
        {
            var file = obj as FileDownloadInfo;
            return file.FileId == this.FileId && file.TableName == this.TableName;
        }

        public void Dispose()
        {
            SavePath = null;
            FinishAction = null;
            FinishAction = null;
            GetStreamFunction = null;
            GetLastUpdateFunction = null;
            GC.SuppressFinalize(this);
        }
    }
}
