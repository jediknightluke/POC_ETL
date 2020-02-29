using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace ETL_POC
{
class SFTP_SRC
    {
        public static async Task RetrieveSFTPFilesAsync(string configFileName)
        {
            SFTP_Config rootConfigVariable = readInRootConfigurations(configFileName);

            using (SftpClient sftp = new SftpClient(rootConfigVariable.SFTP_Host, rootConfigVariable.SFTP_User, rootConfigVariable.SFTP_Password))
            {
                DirectorySource DirSRC = new DirectorySource();
                try
                {
                    sftp.Connect();

                    //Download Subdirectory flag.
                    bool recursiveDownload = true;

                    // Start download of the directory
                    await DownloadDirectoryAsync(
                        sftp,
                        rootConfigVariable.ImportDirectory,
                        DirSRC.SRC_Directory,
                        recursiveDownload
                    );

                    sftp.Disconnect();
                }
                catch (Exception er)
                {
                    System.ArgumentException argEx = new System.ArgumentException($"Could Not Connect To {rootConfigVariable.SFTP_Host}.");
                    throw argEx;
                }
            }


        }

        private static async Task DownloadDirectoryAsync(SftpClient client, string source, string destination, bool recursive = false)
        {
            // List the files and folders of the directory
            var files = client.ListDirectory(source);
            List<Task<string[]>> SFTP_tasks = new List<Task<string[]>>();
            // Iterate over them

            await Task.Run(() =>
            {
                Parallel.ForEach<SftpFile>(files, (SFTPFILE) =>
                {
                    try
                    {
                        SFTP_tasks.Add(DownloadFilesAsync(client, SFTPFILE, destination));

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error: {e}");
                    }
                });
            });
        }

        public static Task<string[]> DownloadFilesAsync(SftpClient client, SftpFile file, string directory)
        {
            return DownloadFilesAsync(client, file, directory, Encoding.UTF8);
        }
        public static async Task<string[]> DownloadFilesAsync(SftpClient client, SftpFile file, string directory, Encoding encoding)
        {
            var lines = new List<string>();
            Console.WriteLine("Downloading {0}", file.FullName);

            using (Stream fileStream = File.OpenWrite(Path.Combine(directory, file.Name)))
            {
                client.DownloadFile(file.FullName, fileStream);
            }

            return lines.ToArray();
        }


    public static SFTP_Config readInRootConfigurations(string configFileName)
    {
            DirectorySource DirSRC = new DirectorySource();
            SFTP_Config SFTPCFG = new SFTP_Config();
            XmlDocument doc = new XmlDocument();
            doc.Load(configFileName);

            SFTPCFG.IsSFTP = doc.DocumentElement.SelectSingleNode("IsSFTP").InnerText;
            if (SFTPCFG.IsSFTP == "True")
            {
                SFTPCFG.SFTP_User = doc.DocumentElement.SelectSingleNode("SFTP_Username").InnerText;
                SFTPCFG.SFTP_Password = doc.DocumentElement.SelectSingleNode("SFTP_Password").InnerText;
                SFTPCFG.SFTP_Host = doc.DocumentElement.SelectSingleNode("SFTP_Host").InnerText;
                SFTPCFG.ImportDirectory = doc.DocumentElement.SelectSingleNode("ImportDirectory").InnerText;

            }
            if (SFTPCFG.IsSFTP == "False")
            {
                SFTPCFG.ImportDirectory = doc.DocumentElement.SelectSingleNode("ImportDirectory").InnerText;
            }


            return SFTPCFG;

        }

    }
}

