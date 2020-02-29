using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ETL_POC
{
    class DirectorySource
    {
        public string Parent_Directory { get { return Directory.GetCurrentDirectory(); }}
        public string RootDirectory { get { return Path.GetFullPath(Path.Combine(Parent_Directory, @"..\..\..\")); } }
        public string Data_Directory { get { return $"{RootDirectory}\\data\\"; }}
        public string SRC_Directory { get { return $"{Data_Directory}\\src_data\\"; }}
        public string CFG_Directory { get { return $"{RootDirectory}\\cfg\\"; }}
    }
}
