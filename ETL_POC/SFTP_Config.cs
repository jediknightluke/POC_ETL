using System;
using System.Collections.Generic;
using System.Text;

namespace ETL_POC
{
    class SFTP_Config
    {
        public string IsSFTP { get; set; }
        public string SFTP_User { get; set; }

        public string SFTP_Password { get; set; }
        public string SFTP_Host { get; set; }
        public string SFTP_ExportPath { get; set; }
        public string ImportDirectory { get; set; }
    }
}
