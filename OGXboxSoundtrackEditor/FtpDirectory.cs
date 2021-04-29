using System;
using System.Collections.Generic;
using System.Text;

namespace OGXboxSoundtrackEditor
{
    public class FtpDirectory
    {
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        private string name;

        public string attributes;
        public int size = 0;
        public string dateModified;
        public string timeModified;
    }
}
