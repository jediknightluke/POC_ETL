using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ETL_POC
{
    class ConfigModel
    {
        public static DirectorySource dirSRC = new DirectorySource();

        public string Delimeter { get; set; }
        public string _fileName { get; set; }
        public string FileName { get { return $"{GetFileName(_fileName)}"; }}
        public string OutputFile { get { return $"{dirSRC.Data_Directory}{_outputFile}"; }}
        public string _outputFile { get; set; }
        public List<int> CamelCase { get { return GenerateIntList(CamelCaseStringResult); } }
        public string CamelCaseStringResult { get; set; }
        public List<int> Sum { get { return GenerateIntList(SumStringResult); } }
        public string SumStringResult { get; set; }
        public string Header { get; set; }
        public string Trailer { get; set; }
        public string ColumnHeaders { get; set; }
        public int ColumnHeaderCount { get { return CountColumns(ColumnHeaders); } }
        public string CheckRowLength { get; set; }
        public string CheckAgainstFirstRow { get; set; }




        private static string GetFileName(string fileName)
        {
            DirectoryInfo SRC_Directory = new DirectoryInfo(dirSRC.SRC_Directory);
            FileInfo[] filesInDir = SRC_Directory.GetFiles("*" + fileName + "*.*");
            return filesInDir[0].ToString();
        }

        private static List<int> GenerateIntList(string Values)
        {
            List<int> IntConverts = new List<int>();
            string[] IntListSplit = Values.Split(',');

            foreach (string x in IntListSplit)
            {
                IntConverts.Add(Int32.Parse(x));
            }
            return IntConverts;
        }

        private static int CountColumns(string Headers)
        {
            string[] IntListSplit = Headers.Split(',');
            return IntListSplit.Length;
        }


    }




}
