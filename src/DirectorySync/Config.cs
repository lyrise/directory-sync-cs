using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DirectorySync
{
    public class Config
    {
        public string[] BaseDirectoryPathList { get; set; }
        public string[] IgnorePatternList { get; set; }
    }
}
