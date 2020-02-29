using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml;
using System.Diagnostics.Contracts;

namespace ETL_POC
{
    class Program
    {
        private const FileOptions DefaultOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
        public static string RootCFG { get { return "rootConfig.cfg"; } }
        static async Task Main()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ConfigModel cfgModel = new ConfigModel();
            List<Task<string[]>> tasks = new List<Task<string[]>>();
            SFTP_SRC src = new SFTP_SRC();
            DirectorySource DirSRC = new DirectorySource();

            //Getting directory ready
            //processingPrep(cfgModel);

            //Retrieving Files
            //await SFTP_SRC.RetrieveSFTPFilesAsync($"{DirSRC.CFG_Directory}{Program.RootCFG}");



            await Task.Run(() =>
            {
                Parallel.ForEach<string>(setFileNames(), (dir) =>
                {
                    try
                    {
                        tasks.Add(ReadAllLinesAsync(readInConfigurations(dir)));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to process file read for {dir}.");
                        Console.WriteLine($"Error: {e}");
                    }
                });
            });
            stopwatch.Stop();
            Console.WriteLine(stopwatch.Elapsed.TotalSeconds);

        }

        public static List<string> setFileNames()
        {
            DirectorySource dirSRC = new DirectorySource();
            List<string> fileNamesArray = new List<string>();
            try
            {
                // Only get files that have .cfg extension.
                string[] dirs = Directory.GetFiles(dirSRC.CFG_Directory, "*.cfg");
                foreach (string dir in dirs)
                {
                    //Ignoring the root config which does not contain file info.
                    if (dir == dirSRC.CFG_Directory+RootCFG)
                        {
                        Console.WriteLine("Skipping Root Config");
                            continue;
                        }
                    else
                    {
                        fileNamesArray.Add(dir);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }

            return fileNamesArray;
        }


        public static ConfigModel readInConfigurations(string configFileName)
        {

            ConfigModel cfgModel = new ConfigModel();
            XmlDocument doc = new XmlDocument();
            doc.Load(configFileName);

            cfgModel.Delimeter = doc.DocumentElement.SelectSingleNode("Delimeter").InnerText;
            cfgModel._fileName = doc.DocumentElement.SelectSingleNode("FileName").InnerText;
            cfgModel.Header = doc.DocumentElement.SelectSingleNode("Header").InnerText;
            cfgModel.Trailer = doc.DocumentElement.SelectSingleNode("Trailer").InnerText;
            cfgModel._outputFile = doc.DocumentElement.SelectSingleNode("OutputFile").InnerText;
            cfgModel.CamelCaseStringResult = doc.DocumentElement.SelectSingleNode("CamelCase").InnerText;
            cfgModel.SumStringResult = doc.DocumentElement.SelectSingleNode("Sum").InnerText;
            cfgModel.CheckRowLength = doc.DocumentElement.SelectSingleNode("CheckRowLength").InnerText;
            cfgModel.ColumnHeaders = doc.DocumentElement.SelectSingleNode("ColumnHeaders").InnerText;
            cfgModel.CheckAgainstFirstRow = doc.DocumentElement.SelectSingleNode("CheckAgainstFirstRow").InnerText;


            //Creating Outputfile
            var cfgFile = System.IO.File.Create(cfgModel.OutputFile);
            cfgFile.Close();


            Console.WriteLine(@$"

**************************************************
Reading in Config file for {configFileName}
**************************************************


{Path.GetFileName(configFileName)}.PARM.Delimeter: {cfgModel.Delimeter}
{Path.GetFileName(configFileName)}.PARM.CamelCase: {cfgModel.CamelCaseStringResult}
{Path.GetFileName(configFileName)}.PARM.Sum: {cfgModel.SumStringResult}
{Path.GetFileName(configFileName)}.PARM.FileName: {cfgModel._fileName}
{Path.GetFileName(configFileName)}.PARM.Header: {cfgModel.Header}
{Path.GetFileName(configFileName)}.PARM.Trailer: {cfgModel.Trailer}


**************************************************
Reading of {configFileName} Complete!
**************************************************");

            return cfgModel;
        }

        public static void processingPrep(ConfigModel cfgModel)
        {
            DirectorySource dirSRC = new DirectorySource();
            //Check that directory exists
            if (!Directory.Exists(dirSRC.Data_Directory))
                return;

            //Clearing the directory
            string[] filePaths = System.IO.Directory.GetFiles(dirSRC.Data_Directory, "*.txt");
            foreach (string filePath in filePaths)
            {
                System.IO.File.Delete(filePath);
            }


            return;
        }

        public static void WriteToFile(string row, string fileType, string outputFileName)
        {
            DirectorySource src = new DirectorySource();
            if (fileType == "header")
            {
                string fileName = $"{src.Data_Directory}{Path.GetFileNameWithoutExtension(outputFileName)}.hdr.txt";
                using (StreamWriter sw = File.CreateText(fileName))
                {
                    sw.Write(row);
                }
            }
            if (fileType == "trailer")
            {
                string fileName = $"{src.Data_Directory}{Path.GetFileNameWithoutExtension(outputFileName)}.trl.txt";
                using (StreamWriter sw = File.CreateText(fileName))
                {
                    sw.Write(row);
                }
            }
            if (fileType == "sum")
            {
                string fileName = $"{src.Data_Directory}{Path.GetFileNameWithoutExtension(outputFileName)}.sum.txt";
                using (StreamWriter sw = File.CreateText(fileName))
                {
                    sw.Write(row);
                }
            }
        }
        public static Task<string[]> ReadAllLinesAsync(ConfigModel cfgModel)
        {
            return ReadAllLinesAsync(cfgModel, Encoding.UTF8);
        }

        public static async Task<string[]> ReadAllLinesAsync(ConfigModel cfgModel, Encoding encoding)
        {
            Console.WriteLine($"****Beginning Reading of file {cfgModel.FileName}*****************");

            //Initiating list that will hold processed lines to be written to output file.
            List<string> processedData = new List<string>();

            // Creates a TextInfo based on the "en-US" culture.
            TextInfo myTI = new CultureInfo("en-US", false).TextInfo;
            var lines = new List<string>();

            // Open the FileStream with the same FileMode, FileAccess
            // and FileShare as a call to File.OpenText would've done.
            var buffer = 104857600;
            using (var stream = new FileStream(cfgModel.FileName, FileMode.Open, FileAccess.Read, FileShare.Read, buffer, DefaultOptions))
            using (var reader = new StreamReader(stream, encoding))
            {
                string line;
                //Returns FileInfo type from a string path
                FileInfo newFile = new FileInfo(cfgModel.FileName);


                int count = 0;
                decimal sumCount = 0;
                List<int> CamcelCaseList = new List<int>();
                List<int> sumList = new List<int>();


                while ((line = await reader.ReadLineAsync()) != null)
                {
                    //Adding one to the row count through every iteration
                    count = count + 1;

                    //delimiting file row by the value given in config file.
                    var array = line.Split(new string[] { $"{cfgModel.Delimeter}" }, StringSplitOptions.RemoveEmptyEntries);

                    //Creating the header file, if header parm == Y
                    if (count == 1 && cfgModel.Header == "Y")
                    {
                        WriteToFile(line, "header", cfgModel._outputFile);
                        continue;
                    }

                    //Checking Row Length if CheckRowLength set to True
                    if (cfgModel.CheckRowLength == "Y")
                    {
                        if (cfgModel.ColumnHeaderCount != array.Length)
                        {
                            Console.WriteLine($"{cfgModel._fileName}: Current Line Does Not Align With Header. Header Count {cfgModel.ColumnHeaderCount}, Current Row Count {array.Length}");
                            Console.WriteLine("Skipping Line");
                            continue;
                        }
                    }

                    //Handling the sum values
                    if (cfgModel.SumStringResult != "NONE")
                    {
                        foreach (int i in cfgModel.Sum)
                        {
                            try
                            {
                                if (array[i] == "NULL")
                                {
                                    //Console.WriteLine("Found NULL, skipping..");
                                }
                                else
                                {
                                    decimal DecimalValue = decimal.Parse(array[i]);
                                    //int sumValue = Int32.Parse(array[i]);
                                    sumCount = sumCount + DecimalValue;
                                }
                            }
                            catch
                            {
                                Console.WriteLine($"{array[i]} could not be converted to an integer");
                            }

                        }
                    }
                    else
                    {
                        //Do nothing, maybe log the event? 
                    }


                    //Handling the Camel Case
                    if (cfgModel.CamelCaseStringResult != "NONE")
                    {

                        foreach (int i in cfgModel.CamelCase)
                        {
                            var casedValue = myTI.ToTitleCase(array[i].ToLower());
                            array[i] = casedValue;
                        }
                    }
                    else
                    {
                        //Do nothing, maybe log the event? 
                    }


                    //Adding processed row to list. The data is stored until the list reaches 10k items. Then the items are written to the Output file.
                    processedData.Add(ConvertStringArrayToString(array));

                    if (processedData.Count() == 150000)
                    {
                        Console.WriteLine($"Processing Check: File {cfgModel._fileName} -> Writing Row: {count}");
                        WriteToFile(processedData, cfgModel.OutputFile);
                        processedData = new List<string>();
                        continue;
                    }
                }
                if (sumCount < 10000)
                {
                    Console.WriteLine($"Writing Contents To {cfgModel.OutputFile}");
                    WriteToFile(processedData, cfgModel.OutputFile);
                    processedData = new List<string>();
                }

                WriteToFile(sumCount.ToString(), "sum", cfgModel._outputFile);
            }
            Console.WriteLine($"\n");
            Console.WriteLine($"*****************{cfgModel._fileName} Processing Complete! *****************");
            return lines.ToArray();
        }


        static string WriteToFile(List<string> ToWrite, string fileName)
        {
            //Handling file writing
            using (StreamWriter sw = File.AppendText(fileName))
            {
                //sw.WriteLine((String.Join(Environment.NewLine, array)));
                foreach (string value in ToWrite)
                {
                    sw.Write("|");
                    sw.Write(value);
                    sw.Write("\n");
                }
            }
            return "Success";
        }

        static string ConvertStringArrayToString(string[] array)
        {
            // Concatenate all the elements into a StringBuilder.
            StringBuilder builder = new StringBuilder();
            foreach (string value in array)
            {
                builder.Append(value);
                builder.Append('|');
            }
            return builder.ToString();
        }

        static string ConvertStringArrayToStringJoin(string[] array)
        {
            // Use string Join to concatenate the string elements.
            string result = string.Join("|", array);
            return result;
        }
    }

}
