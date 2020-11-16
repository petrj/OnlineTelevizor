using System;

namespace SledovaniTVDownloadEvent
{
    public class PrgSettings
    {
            public string CredentialsFilePath { get; set; } = "credentials.json";
            public string ConnectionFilePath { get; set; } = "connection.json";

            public bool Silent { get; set; } = false;
            public bool ShowHelp { get; set; } = false;
            public string Url { get; set; }
            public string PathToMKV { get; set; }

            public bool Valid { get; set; } = false;
    }
}
