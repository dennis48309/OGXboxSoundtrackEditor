using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WMPLib;

namespace OGXboxSoundtrackEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool canAddSoundtracks = false;
        bool canAddSongs = false;

        // wma stuff
        WindowsMediaPlayer wmp = new WindowsMediaPlayer();

        string outputFolder;

        // stuff not in the db file
        ObservableCollection<Soundtrack> soundtracks = new ObservableCollection<Soundtrack>();
        int songGroupCount = 0;
        int paddingBetween = 0;
        long fileLength = 0;

        //header
        int magic;
        int numSoundtracks;
        int nextSoundtrackId;
        int[] songGroupIndexes = new int[100];
        int nextSongId;
        byte[] padding = new byte[96];

        public MainWindow()
        {
            InitializeComponent();
            outputFolder = Properties.Settings.Default.outputFolder;
        }

        private void mnuNew_Click(object sender, RoutedEventArgs e)
        {
            // create header
            magic = 0x00000001;
            numSoundtracks = 0;
            nextSoundtrackId = 0;
            songGroupIndexes = new int[100];
            nextSongId = 0;
            padding = new byte[96];

            songGroupCount = 0;
            paddingBetween = 0;

            soundtracks.Clear();
            listSoundtracks.ItemsSource = soundtracks;
        }

        private void mnuOpen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenDB();
            }
            catch
            {
                MessageBox.Show("Unknown error.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenDB()
        {
            TreeViewItem rootNode = new TreeViewItem();
            rootNode.Header = "Soundtracks";

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Xbox Soundtrack|*.DB";
            Nullable<bool> dResult = ofd.ShowDialog();

            FileInfo fInfo = new FileInfo(ofd.FileName);
            fileLength = fInfo.Length;

            BinaryReader bReader = new BinaryReader(File.OpenRead(ofd.FileName), Encoding.Unicode);
            magic = bReader.ReadInt32();
            numSoundtracks = bReader.ReadInt32();
            nextSoundtrackId = bReader.ReadInt32();
            for (int i = 0; i < 100; i++)
            {
                songGroupIndexes[i] = bReader.ReadInt32();
            }
            nextSongId = bReader.ReadInt32();
            for (int i = 0; i < 96; i++)
            {
                padding[i] = bReader.ReadByte();
            }

            for (int i = 0; i < numSoundtracks; i++)
            {
                Soundtrack s = new Soundtrack();
                s.magic = bReader.ReadInt32();
                s.id = bReader.ReadInt32();
                s.numSongs = bReader.ReadUInt32();
                for (int a = 0; a < 84; a++)
                {
                    s.songGroupIds[a] = bReader.ReadInt32();
                }
                songGroupCount++;
                for (int a = 1; a < 84; a++)
                {
                    if (s.songGroupIds[a] != 0)
                    {
                        songGroupCount++;
                    }
                }

                s.totalTimeMilliseconds = bReader.ReadInt32();
                for (int a = 0; a < 64; a++)
                {
                    s.name[a] = bReader.ReadChar();
                }
                bReader.ReadBytes(32);

                soundtracks.Add(s);
            }

            byte h;
            do
            {
                h = bReader.ReadByte();
                if (h != 0x73)
                {
                    paddingBetween++;
                }
            } while (h != 0x73);
            bReader.ReadBytes(3);

            for (int i = 0; i < songGroupCount; i++)
            {

                TreeViewItem sGroupNode = new TreeViewItem();
                SongGroup sGroup = new SongGroup();
                if (i == 0)
                {
                    sGroup.magic = 0x00031073;
                }
                else
                {
                    sGroup.magic = bReader.ReadInt32();
                }
                sGroup.soundtrackId = bReader.ReadInt32();
                sGroup.id = bReader.ReadInt32();
                sGroup.padding = bReader.ReadInt32();
                for (int a = 0; a < 6; a++)
                {
                    sGroup.songId[a] = bReader.ReadInt32();
                }
                for (int a = 0; a < 6; a++)
                {
                    sGroup.songTimeMilliseconds[a] = bReader.ReadInt32();
                }
                for (int a = 0; a < 6; a++)
                {
                    char[] newArray = new char[32];
                    for (int b = 0; b < 32; b++)
                    {
                        newArray[b] = bReader.ReadChar();
                    }
                    sGroup.songNames[a] = newArray;
                }
                for (int a = 0; a < 64; a++)
                {
                    sGroup.paddingChar[a] = bReader.ReadByte();
                }
                for (int p = 0; p < soundtracks.Count; p++)
                {
                    if (soundtracks[p].id == sGroup.soundtrackId)
                    {
                        soundtracks[p].songGroups.Add(sGroup);
                    }
                }
            }

            for (int i = 0; i < soundtracks.Count; i++)
            {
                TreeViewItem soundtrackNode = new TreeViewItem();
                soundtrackNode.Header = new string(soundtracks[i].name).Trim(); ;
                soundtrackNode.Tag = soundtracks[i].id;
                //soundtrackNode.ContextMenu = soundtrackMenu;
                for (int a = 0; a < soundtracks[i].songGroups.Count; a++)
                {
                    TreeViewItem songGroupNode = new TreeViewItem();
                    songGroupNode.Header = "SongGroup" + soundtracks[i].songGroups[a].id.ToString("X4");
                    songGroupNode.Tag = soundtracks[i].songGroups[a].id;
                    for (int b = 0; b < 6; b++)
                    {
                        if (soundtracks[i].songGroups[a].songTimeMilliseconds[b] != 0)
                        {
                            TreeViewItem songNameNode = new TreeViewItem();
                            songNameNode.Header = new string(soundtracks[i].songGroups[a].songNames[b]);
                            songNameNode.Tag = soundtracks[i].songGroups[a].songId[b];
                            songGroupNode.Items.Add(songNameNode);
                        }
                    }
                    soundtrackNode.Items.Add(songGroupNode);
                }
                rootNode.Items.Add(soundtrackNode);
            }

            //treeSoundtracks.Items.Add(rootNode);

            listSoundtracks.ItemsSource = soundtracks;

            bReader.Close();

            txtStatus.Text = "DB Loaded Successfully";
        }

        private void mnuSettings_Click(object sender, RoutedEventArgs e)
        {
            UserSettings wndSettings = new UserSettings();
            wndSettings.ShowDialog();
            outputFolder = wndSettings.outputFolder;
        }

        private void CalculateSongGroupIndexes()
        {
            int curIndex = 0;
            foreach (Soundtrack sTrack in soundtracks)
            {
                foreach (SongGroup sGroup in sTrack.songGroups)
                {
                    songGroupIndexes[curIndex] = curIndex;
                    curIndex++;
                }
            }
        }

        private void mnuSave_Click(object sender, RoutedEventArgs e)
        {
            CalculateSongGroupIndexes();
            CalculateSongGroupIds();

            BinaryWriter bWriter = new BinaryWriter(File.OpenWrite(outputFolder + @"\ST.DB"), Encoding.Unicode);

            // header
            bWriter.Write(magic);
            bWriter.Write(numSoundtracks);
            bWriter.Write(nextSoundtrackId);
            for (int i = 0; i < 100; i++)
            {
                bWriter.Write(songGroupIndexes[i]);
            }
            bWriter.Write(nextSongId);
            for (int i = 0; i < 96; i++)
            {
                bWriter.Write(padding[i]);
            }

            // soundtracks
            foreach (Soundtrack s in soundtracks)
            {
                bWriter.Write(s.magic);
                bWriter.Write(s.id);
                bWriter.Write(s.numSongs);
                for (int i = 0; i < 84; i++)
                {
                    bWriter.Write(s.songGroupIds[i]);
                }
                bWriter.Write(s.totalTimeMilliseconds);
                for (int i = 0; i < 64; i++)
                {
                    bWriter.Write(s.name[i]);
                }
                for (int i = 0; i < 32; i++)
                {
                    bWriter.Write(s.padding[i]);
                }
            }

            paddingBetween = 51200 - (numSoundtracks * 512);
            for (int i = 0; i < paddingBetween; i++)
            {
                bWriter.Write((byte)0);
            }

            // song groups
            foreach (Soundtrack t in soundtracks)
            {
                foreach (SongGroup s in t.songGroups)
                {
                    bWriter.Write(s.magic);
                    bWriter.Write(s.soundtrackId);
                    bWriter.Write(s.id);
                    bWriter.Write(s.padding);
                    for (int i = 0; i < 6; i++)
                    {
                        bWriter.Write((short)s.songId[i]);
                        if (s.songTimeMilliseconds[i] != 0)
                        {
                            bWriter.Write((short)s.soundtrackId);
                        }
                        else
                        {
                            bWriter.Write((short)0);
                        }
                    }
                    for (int i = 0; i < 6; i++)
                    {
                        bWriter.Write(s.songTimeMilliseconds[i]);
                    }
                    for (int i = 0; i < 6; i++)
                    {
                        if (s.songNames[i] == null)
                        {
                            bWriter.Write(new char[32]);
                        }
                        else
                        {
                            bWriter.Write(s.songNames[i]);
                        }
                    }
                    for (int i = 0; i < 64; i++)
                    {
                        bWriter.Write(s.paddingChar[i]);
                    }
                }
            }

            bWriter.Flush();
            bWriter.Close();
            txtStatus.Text = "DB Written Successfully";
        }

        private void mnuExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void listSoundtracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listSoundtracks.SelectedItem == null)
            {
                listSongs.ItemsSource = null;
                btnDeleteSoundtrack.IsEnabled = false;
                btnAddSongs.IsEnabled = false;
                btnDeleteSongs.IsEnabled = false;
                return;
            }

            ListBox listBox = (ListBox)sender;
            Soundtrack soundtrack = (Soundtrack)listBox.SelectedItem;
            soundtrack.RefreshAllSongNames();
            listSongs.ItemsSource = soundtrack.allSongs;
            btnDeleteSoundtrack.IsEnabled = true;
        }

        private void btnAddSoundtrack_Click(object sender, RoutedEventArgs e)
        {
            string title = Interaction.InputBox("Enter a new soundtrack title", "Soundtrack Title", "", -1, -1).Trim();
            if (title == "")
            {
                MessageBox.Show("Title cannot be empty.  Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if (title.Length > 64)
            {
                MessageBox.Show("Title cannot be longer than 64 characters.  Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // create soundtrack
            Soundtrack sTrack = new Soundtrack();
            sTrack.magic = 0x00021371;
            sTrack.id = nextSoundtrackId;
            title.CopyTo(0, sTrack.name, 0, title.Length);
            sTrack.padding = new byte[32];

            soundtracks.Add(sTrack);

            numSoundtracks++;

            FindNextSoundtrackId();

            listSoundtracks.SelectedItem = listSoundtracks.Items[soundtracks.Count - 1];
            listSoundtracks.Focus();
        }

        private void btnAddSongs_Click(object sender, RoutedEventArgs e)
        {
            Soundtrack sTrack = (Soundtrack)listSoundtracks.SelectedItem;
            int soundtrackId = sTrack.id;

            OpenFileDialog oDialog = new OpenFileDialog();
            oDialog.Filter = "Windows Media Audio files (*.wma)|*.wma";
            oDialog.Multiselect = true;

            oDialog.ShowDialog();

            Directory.CreateDirectory(outputFolder + @"\" + soundtrackId.ToString("X4"));

            foreach (string titleInDialog in oDialog.FileNames)
            {
                string newPath = outputFolder + @"\" + soundtrackId.ToString("X4") + @"\" + soundtrackId.ToString("X4") + nextSongId.ToString("X4") + ".wma";
                File.Copy(titleInDialog, newPath);

                AddSongLoop(soundtrackId, titleInDialog);
            }
        }

        private void AddSongLoop(int soundtrackId, string path)
        {
            char[] songTitle = GetSongTitle(path);
            for (int b = 0; b < soundtracks.Count; b++)
            {
                if (soundtracks[b].id == soundtrackId)
                {
                    for (int i = 0; i < soundtracks[b].songGroups.Count; i++)
                    {
                        for (int a = 0; a < 6; a++)
                        {
                            if (soundtracks[b].songGroups[i].songTimeMilliseconds[a] == 0)
                            {
                                soundtracks[b].songGroups[i].songId[a] = nextSongId;
                                soundtracks[b].songGroups[i].songNames[a] = songTitle;
                                soundtracks[b].songGroups[i].songTimeMilliseconds[a] = GetSongLengthInMs(path);
                                soundtracks[b].numSongs++;

                                soundtracks[b].allSongs.Add(new Song { Name = new string(songTitle).Trim(), TimeMs = GetSongLengthInMs(path) });

                                soundtracks[b].CalculateTotalTimeMs();
                                FindNextSongId();
                                return;
                            }
                        }
                    }
                    // no available song groups so create one
                    SongGroup sGroup = new SongGroup();
                    sGroup.magic = 0x00031073;
                    sGroup.soundtrackId = soundtrackId;
                    sGroup.id = soundtracks[b].songGroups.Count;
                    sGroup.padding = 0x00000001;
                    sGroup.songId[0] = nextSongId;
                    sGroup.songNames[0] = songTitle;
                    sGroup.songTimeMilliseconds[0] = GetSongLengthInMs(path);
                    soundtracks[b].songGroups.Add(sGroup);
                    soundtracks[b].numSongs++;

                    soundtracks[b].CalculateTotalTimeMs();

                    soundtracks[b].allSongs.Add(new Song { Name = new string(songTitle).Trim(), TimeMs = GetSongLengthInMs(path) });
                    FindNextSongId();
                    return;
                }
            }
        }

        private void CalculateSongGroupIds()
        {
            int songGroupCount = 0;
            for (int z = 0; z < soundtracks.Count; z++)
            {
                for (int y = 0; y < soundtracks[z].songGroups.Count; y++)
                {
                    soundtracks[z].songGroupIds[y] = songGroupCount;
                    songGroupCount++;
                }
            }
        }

        private int GetSongLengthInMs(string path)
        {
            IWMPMedia mediainfo = wmp.newMedia(path);
            return (int)(mediainfo.duration * 1000);
        }

        private char[] GetSongTitle(string path)
        {
            char[] titleChars = new char[32];
            IWMPMedia mediainfo = wmp.newMedia(path);
            string title = mediainfo.name;
            if (title.Length > 32)
            {
                bool validTitle = false;
                while (!validTitle)
                {
                    title = Interaction.InputBox("Song name " + mediainfo.name + " is too long, please enter a new one", "Song Title", "", -1, -1).Trim();
                    if (title.Length > 32)
                    {
                        MessageBox.Show("Title cannot be longer than 32 characters.  Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (title.Trim() == "")
                    {
                        MessageBox.Show("Title cannot be blank.  Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        validTitle = true;
                    }
                }
            }
            title.CopyTo(0, titleChars, 0, title.Length);
            return titleChars;
        }

        private string GetSongTitleString(string path)
        {
            IWMPMedia mediainfo = wmp.newMedia(path);
            return mediainfo.name;
        }

        private void btnDeleteSoundtrack_Click(object sender, RoutedEventArgs e)
        {
            if (listSoundtracks.SelectedItem == null)
            {
                MessageBox.Show("No soundtrack selected.  Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Soundtrack temp = (Soundtrack)listSoundtracks.SelectedItem;

            if (MessageBox.Show("Are you sure you want to delete soundtrack " + temp.Name + "?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                soundtracks.Remove((Soundtrack)listSoundtracks.SelectedItem);
                FindNextSongId();
            }
        }

        private void btnDeleteSongs_Click(object sender, RoutedEventArgs e)
        {
            if (listSongs.SelectedItems.Count == 0)
            {
                MessageBox.Show("No songs selected.  Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Soundtrack tempSoundtrack = (Soundtrack)listSoundtracks.SelectedItem;

            if (MessageBox.Show("Are you sure you want to delete the selected songs from soundtrack " + tempSoundtrack.Name + "?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            for (int i = 0; i < listSongs.SelectedItems.Count; i++)
            {
                Song tempSong = (Song)listSongs.SelectedItems[i];

                for (int a = tempSoundtrack.songGroups.Count - 1; a >= 0; a--)
                {
                    if (tempSoundtrack.songGroups[a].id == tempSong.songGroupId)
                    {
                        for (int z = 0; z < 6; z++)
                        {
                            if (tempSoundtrack.songGroups[a].songId[z] == tempSong.id)
                            {
                                tempSoundtrack.songGroups[a].songId[z] = 0;
                                tempSoundtrack.songGroups[a].songNames[z] = new char[32];
                                tempSoundtrack.songGroups[a].songTimeMilliseconds[z] = 0;
                            }
                        }

                        // delete song group if now empty
                        bool deleteIt = true;
                        for (int z = 0; z < 6; z++)
                        {
                            if (tempSoundtrack.songGroups[a].songTimeMilliseconds[z] > 0)
                            {
                                deleteIt = false;
                                break;
                            }
                        }
                        if (deleteIt)
                        {
                            tempSoundtrack.songGroups.RemoveAt(a);
                        }
                    }
                }
            }

            tempSoundtrack.RefreshAllSongNames();
            FindNextSongId();
            RemoveEmptySongGroups(tempSoundtrack);
        }

        private void FindNextSongId()
        {
            int curSongId = 0;
            while (true)
            {
                bool foundCurSongId = false;
                foreach (Soundtrack sTrack in soundtracks)
                {
                    foreach (SongGroup sGroup in sTrack.songGroups)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            if (sGroup.songTimeMilliseconds[i] > 0)
                            {
                                if (sGroup.songId[i] == curSongId)
                                {
                                    foundCurSongId = true;
                                }
                            }
                        }
                    }
                }

                if (!foundCurSongId)
                {
                    nextSongId = curSongId;
                    break;
                }
                curSongId++;
            }
        }

        private void FindNextSoundtrackId()
        {
            int curSoundtrackId = 0;
            while (true)
            {
                bool foundCurSoundtrackId = false;
                foreach (Soundtrack sTrack in soundtracks)
                {
                    if (sTrack.id == curSoundtrackId)
                    {
                        foundCurSoundtrackId = true;
                    }
                }

                if (!foundCurSoundtrackId)
                {
                    nextSoundtrackId = curSoundtrackId;
                    break;
                }
                curSoundtrackId++;
            }
        }

        private void listSongs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listSongs.SelectedItem == null)
            {
                btnDeleteSongs.IsEnabled = false;
            }
            else
            {
                btnDeleteSongs.IsEnabled = true;
            }
        }

        private void RemoveEmptySongGroups(Soundtrack sTrack)
        {
            if (sTrack.songGroups.Count == 0)
            {
                return;
            }
            for (int i = sTrack.songGroups.Count - 1; i >= 0; i--)
            {
                bool isEmpty = false;
                for (int a = 0; a < 6; a++)
                {
                    if (sTrack.songGroups[i].songTimeMilliseconds[a] > 0)
                    {
                        isEmpty = true;
                    }
                }
                if (!isEmpty)
                {
                    sTrack.songGroups.RemoveAt(i);
                }
            }
        }
    }
}
