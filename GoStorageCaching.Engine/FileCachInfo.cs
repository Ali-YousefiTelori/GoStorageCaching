using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoStorageCaching
{
    internal class FileCachInfo
    {
        public int Id { get; set; }
        public int FileId { get; set; }
        public string SavePath { get; set; }
        public DateTime LastUpdateDateTime { get; set; }
        public bool IsComplete { get; set; }
    }
}
