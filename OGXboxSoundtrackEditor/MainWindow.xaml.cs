using EnkiFtpClient;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
        Thread thrFtpControl;

        FtpClient ftpClient;

        // wma stuff
        WindowsMediaPlayer wmp = new WindowsMediaPlayer();

        // saved settings variables
        string outputFolder;
        string ftpIpAddress;
        string ftpUsername;
        string ftpPassword;

        List<string> ftpLocalPaths = new List<string>();
        List<string> ftpDestPaths = new List<string>();
        List<string> ftpSoundtrackIds = new List<string>();

        List<string> toDeleteFiles = new List<string>();

        // stuff not in the db file
        ObservableCollection<Soundtrack> soundtracks = new ObservableCollection<Soundtrack>();
        int songGroupCount = 0;
        int paddingBetween = 0;
        long fileLength = 0;

        //header
        int magic;
        int numSoundtracks;
        int nextSoundtrackId;
        int[] soundtrackIds = new int[100];
        int nextSongId;
        byte[] padding = new byte[96];

        public MainWindow()
        {
            InitializeComponent();

            outputFolder = Properties.Settings.Default.outputFolder;
            ftpIpAddress = Properties.Settings.Default.ftpIpAddress;
            ftpUsername = Properties.Settings.Default.ftpUsername;
            ftpPassword = Properties.Settings.Default.ftpPassword;
        }

        private void mnuNew_Click(object sender, RoutedEventArgs e)
        {
            // create header
            magic = 0x00000001;
            numSoundtracks = 0;
            nextSoundtrackId = 0;
            soundtrackIds = new int[100];
            nextSongId = 0;
            padding = new byte[96];

            songGroupCount = 0;
            paddingBetween = 0;

            soundtracks.Clear();
            listSoundtracks.ItemsSource = soundtracks;
            btnAddSoundtrack.IsEnabled = true;

            if (DeleteAllFromFtp())
            {
                txtStatus.Text = "Deleted All From FTP";
            }
            else
            {

            }
        }

        private void OpenDB()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Xbox Soundtrack|*.DB";
            Nullable<bool> dResult = ofd.ShowDialog();
        }

        private void OpenDbFromStream()
        {
            ftpClient = new FtpClient(ftpIpAddress, ftpUsername, ftpPassword);
            if (!ftpClient.Login())
            {
                return;
            }
            if (!ftpClient.ChangeWorkingDirectory(@"/E/TDATA/fffe0000/music"))
            {
                return;
            }
            if (!ftpClient.Size(@"ST.DB"))
            {
                return;
            }
            if (!ftpClient.Retrieve(outputFolder + @"\ST.DB", @"ST.DB"))
            {
                return;
            }
            ftpClient.Disconnect();

            BinaryReader bReader = new BinaryReader(new MemoryStream(ftpClient.downloadedBytes), Encoding.Unicode);
            magic = bReader.ReadInt32();
            numSoundtracks = bReader.ReadInt32();
            nextSoundtrackId = bReader.ReadInt32();
            for (int i = 0; i < 100; i++)
            {
                soundtrackIds[i] = bReader.ReadInt32();
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
                    sGroup.songId[a] = bReader.ReadInt16();
                    bReader.ReadInt16();
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
            bReader.Close();

            Dispatcher.Invoke(new Action(() => {
                listSoundtracks.ItemsSource = soundtracks;
                txtStatus.Text = "DB Loaded Successfully";
            }));
        }

        private void mnuSettings_Click(object sender, RoutedEventArgs e)
        {
            UserSettings wndSettings = new UserSettings();
            wndSettings.ShowDialog();
            outputFolder = wndSettings.outputFolder;
            ftpIpAddress = wndSettings.ftpIpAddress;
            ftpUsername = wndSettings.ftpUsername;
            ftpPassword = wndSettings.ftpPassword;
        }

        private void CalculateSongGroupIndexes()
        {
            int curIndex = 0;
            foreach (Soundtrack sTrack in soundtracks)
            {
                sTrack.songGroupIds = new int[84];

                for (int i = 0; i < sTrack.songGroups.Count; i++)
                {
                    sTrack.songGroupIds[i] = curIndex;

                    curIndex++;
                }
            }
        }

        private int GetDbSize()
        {
            int dbSize = 0;
            dbSize = 51200 - (numSoundtracks * 512);
            dbSize += 512 + (numSoundtracks * 512);
            foreach (Soundtrack sTrack in soundtracks)
            {
                dbSize = dbSize + (sTrack.songGroups.Count * 512);
            }
            return dbSize;
        }

        private void mnuSave_Click(object sender, RoutedEventArgs e)
        {
        }

        private byte[] GetDbBytes()
        {
            CalculateSongGroupIndexes();
            CalculateSoundtrackIds();

            byte[] dbBytes = new byte[GetDbSize()];

            MemoryStream mStream = new MemoryStream(dbBytes);
            BinaryWriter bWriter = new BinaryWriter(mStream, Encoding.Unicode);

            // header
            bWriter.Write(magic);
            bWriter.Write(numSoundtracks);
            bWriter.Write(nextSoundtrackId);
            for (int i = 0; i < 100; i++)
            {
                bWriter.Write(soundtrackIds[i]);
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
            return dbBytes;
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
            else
            {
                btnAddSongs.IsEnabled = true;
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

            foreach (string path in oDialog.FileNames)
            {
                ftpLocalPaths.Add(path);
                AddSongLoop(soundtrackId, path);
            }

            btnFTPChanges.IsEnabled = true;
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
                                soundtracks[b].allSongs.Add(new Song { Name = new string(songTitle).Trim(), TimeMs = GetSongLengthInMs(path), songGroupId = soundtracks[b].songGroups[i].id, soundtrackId = soundtracks[b].id, id = nextSongId });
                                soundtracks[b].CalculateTotalTimeMs();

                                ftpSoundtrackIds.Add(soundtrackId.ToString("X4"));
                                ftpDestPaths.Add(soundtrackId.ToString("X4") + nextSongId.ToString("X4") + @".wma");

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
                    soundtracks[b].allSongs.Add(new Song { Name = new string(songTitle).Trim(), TimeMs = GetSongLengthInMs(path), songGroupId = sGroup.id, soundtrackId = soundtracks[b].id, id = nextSongId });

                    ftpSoundtrackIds.Add(soundtrackId.ToString("X4"));
                    ftpDestPaths.Add(soundtrackId.ToString("X4") + nextSongId.ToString("X4") + @".wma");

                    FindNextSongId();
                    return;
                }
            }
        }

        private void CalculateSoundtrackIds()
        {
            soundtrackIds = new int[100];

            for (int i = 0; i < soundtracks.Count; i++)
            {
                soundtrackIds[i] = soundtracks[i].id;
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
                FindNextSoundtrackId();
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
                    }
                }

                ftpClient = new FtpClient(ftpIpAddress, ftpUsername, ftpPassword);
                if (!ftpClient.ChangeWorkingDirectory(@"/E/TDATA/fffe0000/music/" + tempSong.soundtrackId.ToString("X4")))
                {
                    return;
                }
                if (!ftpClient.DeleteFile(tempSong.soundtrackId.ToString("X4") + tempSong.id.ToString("X4") + @".wma"))
                {
                    return;
                }
                ftpClient.Disconnect();

                tempSoundtrack.numSongs--;
            }

            txtStatus.Text = "Songs Deleted Successfully";

            tempSoundtrack.RefreshAllSongNames();
            ReorderSongsInGroups(tempSoundtrack);
            FindNextSongId();
        }

        
        private void ReorderSongsInGroups(Soundtrack soundtrack)
        {
            if (soundtrack.allSongs.Count == 0)
            {
                return;
            }

            soundtrack.songGroups.Clear();
            int totalSongGroups;
            if (soundtrack.allSongs.Count > 6)
            {
                totalSongGroups = soundtrack.allSongs.Count / 6;
                if (soundtrack.allSongs.Count % 6 > 0)
                {
                    totalSongGroups++;
                }
            }
            else
            {
                totalSongGroups = 1;
            }

            for (int i = 0; i < totalSongGroups; i++)
            {
                SongGroup tempGroup = new SongGroup();
                tempGroup.magic = 0x00031073;
                tempGroup.soundtrackId = soundtrack.id;
                tempGroup.id = i;
                tempGroup.padding = 0x00000001;

                soundtrack.songGroups.Add(tempGroup);
            }

            int currentIndex = 0;
            int currentSongGroupIndex = 0;
            foreach (Song song in soundtrack.allSongs)
            {
                if (currentIndex == 6)
                {
                    currentSongGroupIndex++;
                    currentIndex = 0;
                }

                char[] songName = new char[32];
                song.Name.CopyTo(0, songName, 0, song.Name.Length);
                soundtrack.songGroups[currentSongGroupIndex].songId[currentIndex] = song.id;
                soundtrack.songGroups[currentSongGroupIndex].songNames[currentIndex] = songName;
                soundtrack.songGroups[currentSongGroupIndex].songTimeMilliseconds[currentIndex] = song.TimeMs;

                currentIndex++;
            }
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

        private void btnFTPChanges_Click(object sender, RoutedEventArgs e)
        {
            gridMain.IsEnabled = false;
            txtStatus.Text = "Uploading New Soundtracks/Songs";
            thrFtpControl = new Thread(new ThreadStart(FtpChanges));
            thrFtpControl.Start();
        }

        private void FtpChanges()
        {
            try
            {
                ftpClient = new FtpClient(ftpIpAddress, ftpUsername, ftpPassword);
                if (!ftpClient.Login())
                {
                    return;
                }
                if (!ftpClient.ChangeWorkingDirectory(@"/E/TDATA/fffe0000/music"))
                {
                    return;
                }

                ftpClient.DeleteFile("ST.DB");

                ftpClient.toUploadBytes = GetDbBytes();
                if (!ftpClient.Store("ST.DB"))
                {
                    return;
                }

                Dispatcher.Invoke(new Action(() =>
                {
                    progFtpTransfer.Value = 0;
                    progFtpTransfer.Maximum = ftpDestPaths.Count;
                }));

                for (int i = 0; i < ftpDestPaths.Count; i++)
                {
                    ftpClient.MakeDirectory(ftpSoundtrackIds[i]);

                    if (!ftpClient.ChangeWorkingDirectory(@"/E/TDATA/fffe0000/music/" + ftpSoundtrackIds[i]))
                    {
                        return;
                    }

                    if (!ftpClient.Store(ftpLocalPaths[i], ftpDestPaths[i]))
                    {
                        return;
                    }

                    Dispatcher.Invoke(new Action(() =>
                    {
                        progFtpTransfer.Value++;
                    }));

                    if (!ftpClient.ChangeWorkingDirectory(@"/E/TDATA/fffe0000/music"))
                    {
                        return;
                    }
                }

                ftpClient.Disconnect();

                Dispatcher.Invoke(new Action(() =>
                {
                    txtStatus.Text = "Uploading Success";
                    gridMain.IsEnabled = true;
                }));
            }
            catch
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    txtStatus.Text = "Failure";
                    gridMain.IsEnabled = true;
                }));
            }
            ftpDestPaths.Clear();
            ftpLocalPaths.Clear();
            ftpSoundtrackIds.Clear();
            return;
        }

        private bool DeleteAllFromFtp()
        {
            try
            {
                FtpClient ftpClient = new FtpClient(ftpIpAddress, ftpUsername, ftpPassword);
                if (!ftpClient.Login())
                {
                    return false;
                }
                if (!ftpClient.ChangeWorkingDirectory(@"/E/TDATA/fffe0000/music"))
                {
                    return false;
                }

                ftpClient.List();
                List<FtpDirectory> soundtrackFolders = ftpClient.GetDirectories();
                List<FtpFile> soundtrackFiles = ftpClient.GetFiles();

                foreach (FtpDirectory tempDir in soundtrackFolders)
                {
                    if (!ftpClient.ChangeWorkingDirectory(tempDir.name))
                    {
                        return false;
                    }

                    ftpClient.List();
                    List<FtpFile> subfolderFiles = ftpClient.GetFiles();

                    foreach (FtpFile tempFile in subfolderFiles)
                    {
                        if (!ftpClient.DeleteFile(tempFile.name))
                        {
                            return false;
                        }
                    }
                    if (!ftpClient.ChangeWorkingDirectory(@"/E/TDATA/fffe0000/music"))
                    {
                        return false;
                    }
                    if (!ftpClient.DeleteFolder(tempDir.name))
                    {
                        return false;
                    }
                }

                foreach (FtpFile tempFile in soundtrackFiles)
                {
                    if (!ftpClient.DeleteFile(tempFile.name))
                    {
                        return false;
                    }
                }

                ftpClient.Disconnect();
            }
            catch
            {
                return false;
            }

            return true;
        }

        private void mnuOpenFromFtp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenDbFromStream();
                txtStatus.Text = "ST.DB Opened From FTP";
            }
            catch
            {
                txtStatus.Text = "Failed: Check Log For Details";
            }
        }
    }
}
