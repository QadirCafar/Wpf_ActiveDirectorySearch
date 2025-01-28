using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveDirectorySearch.Model
{
    public class SearchResultModel
    {
        public string Path { get; set; }
        public string FileName { get; set; }
        public int FileCount { get; set; }
        public long TotalSizeMB { get; set; }
    }
}
