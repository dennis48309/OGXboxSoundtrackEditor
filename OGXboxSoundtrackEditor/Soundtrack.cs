using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OGXboxSoundtrackEditor
{
    public class Soundtrack
    {
        public int magic;
        public int id;
        public uint numSongs;
        public int[] songGroupIds = new int[84];
        public int totalTimeMilliseconds;
        public char[] name = new char[64];
        public string Name
        {
            get { return new string(name).Trim(); }
        }

        public string ID
        {
            get { return id.ToString(); }
        }

        public byte[] padding = new byte[32];

        public List<SongGroup> songGroups = new List<SongGroup>();
        public ObservableCollection<Song> allSongs = new ObservableCollection<Song>();

        public void CalculateTotalTimeMs()
        {
            int time = 0;
            foreach (Song song in allSongs)
            {
                time += song.TimeMs;
            }
            totalTimeMilliseconds = time;
        }

        public string GetNameString()
        {
            return new string(name).Trim();
        }

        public void RefreshAllSongNames()
        {
            allSongs.Clear();
            foreach (SongGroup sGroup in songGroups)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (sGroup.songTimeMilliseconds[i] > 0)
                    {
                        string songName = new string(sGroup.songNames[i]).Trim();
                        allSongs.Add(new Song { Name = songName, TimeMs = sGroup.songTimeMilliseconds[i], id = sGroup.songId[i], songGroupId = sGroup.id, soundtrackId = id } );
                    }
                }
            }
        }
    }
}
