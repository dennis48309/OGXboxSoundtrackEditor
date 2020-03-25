using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OGXboxSoundtrackEditor
{
    public class Song
    {
        public int id;
        public int songGroupId;

        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        private int timeMs;
        public int TimeMs
        {
            get { return timeMs; }
            set { timeMs = value; }
        }
    }
}
