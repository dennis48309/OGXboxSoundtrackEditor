using System;
using System.Collections.Generic;
using System.Text;

namespace OGXboxSoundtrackEditor
{
    public class FtpLogEntry
    {
        public string EntryData
        {
            get { return entryData; }
            set { entryData = value; }
        }
        private string entryData;

        public DateTime EntryTime
        {
            get { return entryTime; }
            set { entryTime = value; }
        }
        private DateTime entryTime;
    }
}
