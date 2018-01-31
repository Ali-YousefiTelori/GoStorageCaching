using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoStorageCaching
{
    public static class DownloadStreamEngine
    {
        static List<FileDownloadInfo> DownloadQueues { get; set; } = new List<FileDownloadInfo>();


        static FileDownloadInfo TakeItem(FileDownloadInfo newFileDownloadInfo)
        {
            return DownloadQueues.FirstOrDefault(x => x == newFileDownloadInfo);
        }

        internal static void Add(FileDownloadInfo fileDownloadInfo)
        {
            lock (DownloadQueues)
            {
                var find = TakeItem(fileDownloadInfo);
                if (find != null)
                {
                    lock (find)
                    {
                        find.FinishAction += (status, savepath, ex) =>
                        {
                            fileDownloadInfo.FinishAction(status, savepath, ex);
                            Dispose(fileDownloadInfo);
                        };
                    }
                    return;
                }
                else
                {
                    DownloadQueues.Add(fileDownloadInfo);
                }
            }
            InitializeNewDownload(fileDownloadInfo);
        }

        static void InitializeNewDownload(FileDownloadInfo fileDownloadInfo)
        {
            lock (fileDownloadInfo)
            {
                var find = StorageCachingEngine.Exist(fileDownloadInfo.TableName, fileDownloadInfo.FileId);
                if (find != null)
                {
                    FileInfo file = new FileInfo(find.SavePath);

                    bool isExist = file.Exists;
                    if (isExist)
                        fileDownloadInfo.FinishAction(FileDownloadStatus.Success, find.SavePath, null);
                    var lastUpdate = fileDownloadInfo.GetLastUpdateFunction();

                    var dbDate = find.LastUpdateDateTime.AddTicks(-find.LastUpdateDateTime.Ticks % TimeSpan.TicksPerSecond);
                    lastUpdate = lastUpdate.AddTicks(-lastUpdate.Ticks % TimeSpan.TicksPerSecond);
                    if (lastUpdate != dbDate || !isExist || file.Length == 0 || !find.IsComplete)
                        Download(fileDownloadInfo, lastUpdate);
                    else
                    {
                        Dispose(fileDownloadInfo);
                    }
                }
                else
                {
                    var lastUpdate = fileDownloadInfo.GetLastUpdateFunction();
                    Download(fileDownloadInfo, lastUpdate);
                }
            }
        }

        static void Download(FileDownloadInfo fileDownloadInfo, DateTime lastUpdate)
        {
            bool exceptionWhenDownloading = false;
            try
            {
                var streamAndSize = fileDownloadInfo.GetStreamFunction();
                //حواست باشه که در این موارد اگر رید و رایت تداخل بخوره و از طرف دیگه گوشی یا سیستم داره فایل رو میخونه و اینجا داره رایت میشه ممکنه فایل خراب نشون داده بشه یا خطا بخوره
                int downloadedSize = 0;
                using (var fileStream = new FileStream(fileDownloadInfo.SavePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    exceptionWhenDownloading = true;
                    while (downloadedSize < streamAndSize.FileLength)
                    {
                        byte[] bytes = new byte[1024 * 20];
                        var socketStream = (streamAndSize.Stream as System.Net.Sockets.NetworkStream);

                        //if (socketStream != null)
                        //{
                        //    var p = socketStream.GetType().GetProperty("Socket", System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        //    var socket = (System.Net.Sockets.Socket)p.GetValue(socketStream);
                        //    if (!socket.Poll(10000, System.Net.Sockets.SelectMode.SelectRead))
                        //        throw new TimeoutException();
                        //    else if (!socket.Connected)
                        //        throw new Exception("socket closed");
                        //}

                        var readCount = streamAndSize.Stream.Read(bytes, 0, bytes.Length);
                        downloadedSize += readCount;
                        //if (readCount == 0)
                        //{
                        //    var p = socketStream.GetType().GetProperty("Socket", System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        //    var socket = (System.Net.Sockets.Socket)p.GetValue(socketStream);
                        //    if (!socket.Poll(10000, System.Net.Sockets.SelectMode.SelectRead))
                        //        throw new TimeoutException();
                        //    else if (!socket.Connected)
                        //        throw new Exception("socket closed");
                        //    while (!socketStream.DataAvailable && socket.Connected)
                        //    {
                        //        Thread.Sleep(1000);
                        //    }
                        //}
                        if (readCount <= 0)
                            throw new Exception("read zero client disconnected");
                        fileStream.Write(bytes, 0, readCount);
                    }
                }

                lock (DownloadQueues)
                {
                    StorageCachingEngine.AddOrUpdate(fileDownloadInfo.TableName, fileDownloadInfo.FileId, fileDownloadInfo.SavePath, lastUpdate, true);
                    fileDownloadInfo.FinishAction(FileDownloadStatus.Success, fileDownloadInfo.SavePath, null);
                    Dispose(fileDownloadInfo);
                }
            }
            catch (Exception ex)
            {
                lock (DownloadQueues)
                {
                    if (exceptionWhenDownloading && File.Exists(fileDownloadInfo.SavePath))
                        DeleteFile(fileDownloadInfo.SavePath);
                    StorageCachingEngine.Delete(fileDownloadInfo.TableName, fileDownloadInfo.FileId);
                    fileDownloadInfo.FinishAction(FileDownloadStatus.Error, fileDownloadInfo.SavePath, ex);
                    Dispose(fileDownloadInfo);
                }
            }
        }

        public static void Dispose(FileDownloadInfo fileDownloadInfo)
        {
            var find = TakeItem(fileDownloadInfo);
            if (find != null)
            {
                find.Dispose();
                DownloadQueues.Remove(fileDownloadInfo);
            }
            fileDownloadInfo.Dispose();
        }

        static void DeleteFile(string fileName)
        {
            try
            {
                File.Delete(fileName);
            }
            catch (Exception ex)
            {

            }
        }
    }
}
