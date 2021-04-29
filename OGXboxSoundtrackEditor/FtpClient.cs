using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace OGXboxSoundtrackEditor
{
    public enum DataTransferType { DownloadList, UploadFile, UploadFileArray, DownloadFile };

    public class FtpClient
    {
        DataTransferType dataTransferType;

        BinaryReader bReader;
        StreamWriter sWriter;
        StreamReader sReader;
        NetworkStream nStream;
        NetworkStream nStreamData;
        TcpClient client;
        TcpClient clientData;

        public byte[] downloadedBytes;
        public byte[] toUploadBytes;

        Thread thrFtpData;
        string readData = null;

        string hostname;
        string username;
        string password;
        int passivePort;

        public List<FtpLogEntry> ftpLogEntries = new List<FtpLogEntry>();

        public string currentWorkingDirectory = @"/";

        public FtpClient(string hostname, string username, string password)
        {
            this.hostname = hostname;
            this.username = username;
            this.password = password;
        }

        public bool Connect()
        {
            client = new TcpClient(hostname, 21);
            nStream = client.GetStream();
            sWriter = new StreamWriter(nStream);
            sReader = new StreamReader(nStream, Encoding.UTF8);
            bReader = new BinaryReader(nStream, Encoding.UTF8);

            List<string> lines = ReadAllLines();
            if (lines[lines.Count - 1].Substring(0, 3) != "220")
            {
                return false;
            }
            return true;
        }

        private List<string> ReadAllLines()
        {
            List<string> lines = new List<string>();
            while (true)
            {
                string line = ReadResponseLine();
                line = line.Replace("\r\n", "");
                ftpLogEntries.Add(new FtpLogEntry { EntryData = line, EntryTime = DateTime.Now });
                Debug.WriteLine(line);
                lines.Add(line);
                if (line.Length > 3)
                {
                    if (line.Substring(3, 1) == " ")
                    {
                        break;
                    }
                }
            }
            return lines;
        }

        private string ReadResponseLine()
        {
            List<byte> bytes = new List<byte>();
            while (true)
            {
                try
                {
                    byte b = (byte)sReader.Read();
                    if (b == 0x0d)
                    {
                        byte c = (byte)sReader.Read();
                        if (c == 0x0a)
                        {
                            return Encoding.UTF8.GetString(bytes.ToArray());
                        }
                        else
                        {
                            bytes.Add(b);
                        }
                    }
                    else
                    {
                        bytes.Add(b);
                    }
                }
                catch
                {
                    return Encoding.UTF8.GetString(bytes.ToArray());
                }
            }
        }

        public bool Login()
        {
            
            SendMessage(@"USER " + username);
            Debug.WriteLine("[C] USER " + username);
            List<string> lines2 = ReadAllLines();
            if (lines2[lines2.Count - 1].Substring(0, 3) != "331")
            {
                return false;
            }
            SendMessage(@"PASS " + password);
            Debug.WriteLine("[C] PASS ******");
            List<string> lines3 = ReadAllLines();
            if (lines3[lines3.Count - 1].Substring(0, 3) != "230")
            {
                return false;
            }
            return true;
        }

        private void SendMessage(string msg)
        {
            ftpLogEntries.Add(new FtpLogEntry { EntryData = "[Client] " + msg, EntryTime = DateTime.Now });
            sWriter.Write(msg + (char)0x0d + (char)0x0a);
            sWriter.Flush();
        }

        public bool MakeDirectory(string name)
        {
            SendMessage("MKD " + name);
            Debug.WriteLine("[C] MKD " + name);
            List<string> lines = ReadAllLines();
            if (lines[lines.Count - 1].Substring(0, 3) == "257")
            {
                return true;
            }
            return false;
        }

        public bool DeleteFolder(string name)
        {
            if (name == "..")
            {
                return true;
            }
            SendMessage("RMD " + name);
            List<string> lines = ReadAllLines();
            if (lines[lines.Count - 1].Substring(0, 3) == "250")
            {
                return true;
            }
            return false;
        }

        public bool DeleteFile(string name)
        {
            SendMessage("DELE " + name);
            List<string> lines = ReadAllLines();
            if (lines[lines.Count - 1].Substring(0, 3) == "250")
            {
                return true;
            }
            return false;
        }

        public void Disconnect()
        {
            SendMessage("QUIT");
            Debug.WriteLine("[C] QUIT");
            //List<string> lines = ReadAllLines();
            /*
            if (lines[lines.Count - 1].Substring(0, 3) == "221")
            {

            }
            */
            if (client != null)
            {
                client.Close();
            }
            if (clientData != null)
            {
                clientData.Close();
            }
        }

        public bool PrintWorkingDirectory()
        {
            SendMessage("PWD");
            List<string> lines = ReadAllLines();
            if (lines[lines.Count - 1].Substring(0, 3) == "257")
            {
                return true;
            }
            return false;
        }

        int fileSize = 0;
        public bool Size(string filename)
        {
            SendMessage("SIZE " + filename);
            Debug.WriteLine("[C] SIZE " + filename);
            List<string> lines = ReadAllLines();
            if (lines[lines.Count - 1].Substring(0, 3) == "213")
            {
                string[] split = lines[lines.Count - 1].Split(' ');
                fileSize = int.Parse(split[1]);
                return true;
            }
            return false;
        }

        public bool ChangeWorkingDirectory(string path)
        {
            SendMessage("CWD " + path);
            Debug.WriteLine("[C] CWD " + path);
            List<string> lines = ReadAllLines();
            if (lines[lines.Count - 1].Substring(0, 3) == "250")
            {
                Regex regex = new Regex(@"(?<=" + (char)34 + @").+(?=" + (char)34 + @")");
                Match match = regex.Match(lines[lines.Count - 1]);
                currentWorkingDirectory = match.Value;
                return true;
            }
            return false;
        }

        public bool Retrieve(string filename)
        {
            if (!Size(filename))
            {
                return false;
            }
            Passive();
            SendMessage("RETR " + filename);
            Debug.WriteLine("[C] RETR " + filename);
            dataTransferType = DataTransferType.DownloadFile;
            List<string> lines = ReadAllLines();
            if (lines[lines.Count - 1].Substring(0, 3) != "150")
            {
                return false;
            }

            List<string> lines2 = ReadAllLines();
            if (lines2[lines2.Count - 1].Substring(0, 3) == "226")
            {
                while (thrFtpData.IsAlive)
                {

                }
                return true;
            }
            return false;
        }

        string pathOfFile;
        public bool Store(string filePath, string filename)
        {
            dataTransferType = DataTransferType.UploadFile;
            pathOfFile = filePath;
            //Type();
            Passive();
            SendMessage("STOR " + filename);
            Debug.WriteLine("[C] STOR " + filename);
            
            List<string> lines = ReadAllLines();
            if (lines[lines.Count - 1].Substring(0, 3) != "150" && lines[lines.Count - 1].Substring(0, 3) != "125")
            {
                return false;
            }

            List<string> lines2 = ReadAllLines();
            if (lines2[lines2.Count - 1].Substring(0, 3) == "226")
            {
                return true;
            }
            return false;
        }

        public bool Store(string filename)
        {
            dataTransferType = DataTransferType.UploadFileArray;
            //Type();
            Passive();
            SendMessage("STOR " + filename);
            Debug.WriteLine("[C] STOR " + filename);

            List<string> lines = ReadAllLines();
            if (lines[lines.Count - 1].Substring(0, 3) != "150" && lines[lines.Count - 1].Substring(0, 3) != "125")
            {
                return false;
            }
            List<string> lines2 = ReadAllLines();
            if (lines2[lines2.Count - 1].Substring(0, 3) == "226")
            {
                return true;
            }
            return false;
        }

        public bool TransferBinaryMode()
        {
            SendMessage("TYPE I");
            List<string> lines = ReadAllLines();
            if (lines[lines.Count - 1].Substring(0, 3) == "200")
            {
                return true;
            }
            return false;
        }

        public void Passive()
        {
            SendMessage("PASV");
            Debug.WriteLine("[C] PASV");
            List<string> lines = ReadAllLines();
            if (lines[lines.Count - 1].Substring(0, 3) == "227")
            {
                Regex regex = new Regex(@"(?<=\().+(?=\))");
                Match match = regex.Match(lines[lines.Count - 1]);
                string[] split = match.Value.Split(',');
                passivePort = int.Parse(split[4]) * 256 + int.Parse(split[5]);

                clientData = new TcpClient(hostname, passivePort);
                thrFtpData = new Thread(new ThreadStart(HandleData));
                thrFtpData.Start();
            }
        }

        public List<string> ListPrint()
        {
            List<string> newList = new List<string>();

            Passive();
            SendMessage(@"LIST -al");
            dataTransferType = DataTransferType.DownloadList;
            List<string> lines = ReadAllLines();
            if (lines[lines.Count - 1].Substring(0, 3) == "226")
            {
                while (thrFtpData.IsAlive)
                {

                }
                string[] splitFiles = readData.Split('\n');
                foreach (string s in splitFiles)
                {
                    if (s != "")
                    {
                        newList.Add(s);
                    }
                }
            }
            return newList;
        }

        public List<FtpDirectory> GetDirectories()
        {
            List<FtpDirectory> tempList = new List<FtpDirectory>();

            Regex regex = new Regex(@"(?<= )[^ ]+?(?= )");
            Regex regexTimeName = new Regex(@"(?<=:.. ).+");
            Regex regexTime = new Regex(@"(?<= )[0-9][0-9]:[0-9][0-9](?= )");
            Regex regexNonTimeName = new Regex(@"(?<=[0-9]  [0-9][0-9][0-9][0-9] ).+");

            string[] stringSeparators = new string[] { "\r\n" };
            string[] splitFiles = readData.Split(stringSeparators, StringSplitOptions.None);
            foreach (string sTemp in splitFiles)
            {
                if (sTemp != "")
                {
                    string s = sTemp.Replace("\r\n", "");
                    if (s.Substring(0, 1) == "d")
                    {
                        FtpDirectory newDir = new FtpDirectory();
                        Match mTime = regexTime.Match(s);
                        if (mTime.Success)
                        {
                            Match mTimeName = regexTimeName.Match(s);
                            newDir.Name = mTimeName.Value.Trim();
                        }
                        else
                        {
                            Match match = regexNonTimeName.Match(s);
                            newDir.Name = match.Value.Trim();
                        }

                        MatchCollection m = regex.Matches(" " + s + " ");

                        newDir.attributes = m[0].Value;
                        tempList.Add(newDir);
                    }
                }
            }
            return tempList;
        }

        public List<FtpFile> GetFiles()
        {
            List<FtpFile> tempList = new List<FtpFile>();

            Regex regex = new Regex(@"(?<= )[^ ]+?(?= )");
            Regex regexTimeName = new Regex(@"(?<=:.. ).+");
            Regex regexTime = new Regex(@"(?<= )[0-9][0-9]:[0-9][0-9](?= )");
            Regex regexNonTimeName = new Regex(@"(?<=[0-9]  [0-9][0-9][0-9][0-9] ).+");

            string[] stringSeparators = new string[] { "\r\n" };
            string[] splitFiles = readData.Split(stringSeparators, StringSplitOptions.None);
            foreach (string sTemp in splitFiles)
            {
                if (sTemp != "")
                {
                    string s = sTemp.Replace("\r\n", "");
                    if (s.Substring(0, 1) != "d")
                    {
                        FtpFile newFile = new FtpFile();
                        Match mTime = regexTime.Match(s);
                        if (mTime.Success)
                        {
                            Match mTimeName = regexTimeName.Match(s);
                            newFile.name = mTimeName.Value.Trim();
                        }
                        else
                        {
                            Match match = regexNonTimeName.Match(s);
                            newFile.name = match.Value.Trim();
                        }

                        MatchCollection m = regex.Matches(" " + s + " ");

                        newFile.attributes = m[0].Value;
                        tempList.Add(newFile);
                    }
                }
            }
            return tempList;
        }

        public void List()
        {
            readData = null;
            Passive();
            SendMessage(@"LIST -al");
            Debug.WriteLine("[C] LIST -al");
            dataTransferType = DataTransferType.DownloadList;
            List<string> lines = ReadAllLines();
            if (lines[lines.Count - 1].Substring(0, 3) == "150")
            {
                while (thrFtpData.IsAlive)
                {

                }
            }
            List<string> lines2 = ReadAllLines();
            if (lines2[lines2.Count - 1].Substring(0, 3) == "226")
            {

            }
        }

        public bool Type()
        {
            SendMessage("TYPE I");
            Debug.WriteLine("[C] TYPE I");
            List<string> lines = ReadAllLines();
            if (lines[lines.Count - 1].Substring(0, 3) == "200")
            {
                return true;
            }
            return false;
        }

        private bool FileExists(string name)
        {
            foreach (FtpFile f in GetFiles())
            {
                if (f.name == name)
                {
                    return true;
                }
            }
            return false;
        }

        private bool DirectoryExists(string name)
        {
            foreach (FtpDirectory d in GetDirectories())
            {
                if (d.Name == name)
                {
                    return true;
                }
            }
            return false;
        }

        int bufferSize = 1238;
        private void UploadFile()
        {
            nStreamData = clientData.GetStream();
            BinaryWriter writer = new BinaryWriter(nStreamData);
            FileInfo fInfo = new FileInfo(pathOfFile);
            using (FileStream fStream = new FileStream(pathOfFile, FileMode.Open))
            {
                int totalBytesToSend = (int)fInfo.Length;
                while (true)
                {
                    if (totalBytesToSend == 0)
                    {
                        break;
                    }
                    else if (totalBytesToSend < bufferSize)
                    {
                        byte[] allBytesOfFile = new byte[totalBytesToSend];
                        fStream.Read(allBytesOfFile, 0, totalBytesToSend);
                        writer.Write(allBytesOfFile);
                        writer.Flush();
                        totalBytesToSend = 0;
                    }
                    else
                    {
                        byte[] allBytesOfFile = new byte[bufferSize];
                        fStream.Read(allBytesOfFile, 0, bufferSize);
                        writer.Write(allBytesOfFile);
                        writer.Flush();
                        totalBytesToSend -= bufferSize;
                    }
                }
            }
            clientData.Close();
        }

        private void UploadFileArray()
        {
            nStreamData = clientData.GetStream();
            BinaryWriter writer = new BinaryWriter(nStreamData);
            using (MemoryStream fStream = new MemoryStream(toUploadBytes))
            {
                int totalBytesToSend = toUploadBytes.Length;
                while (true)
                {
                    if (totalBytesToSend == 0)
                    {
                        break;
                    }
                    else if (totalBytesToSend < bufferSize)
                    {
                        byte[] tempArray = new byte[totalBytesToSend];
                        fStream.Read(tempArray, 0, totalBytesToSend);
                        writer.Write(tempArray);
                        writer.Flush();
                        totalBytesToSend = 0;
                    }
                    else
                    {
                        byte[] tempArray = new byte[bufferSize];
                        fStream.Read(tempArray, 0, bufferSize);
                        writer.Write(tempArray);
                        writer.Flush();
                        totalBytesToSend -= bufferSize;
                    }
                }
            }
            clientData.Close();
        }

        private void DownloadFile()
        {
            nStreamData = clientData.GetStream();
            BinaryReader bReader = new BinaryReader(nStreamData);
            downloadedBytes = bReader.ReadBytes(fileSize);
            clientData.Close();
        }

        private void HandleData()
        {
            if (dataTransferType == DataTransferType.UploadFile)
            {
                UploadFile();
            }
            else if (dataTransferType == DataTransferType.DownloadList)
            {
                DownloadData();
            }
            else if (dataTransferType == DataTransferType.DownloadFile)
            {
                DownloadFile();
            }
            else if (dataTransferType == DataTransferType.UploadFileArray)
            {
                UploadFileArray();
            }
        }

        private void DownloadData()
        {
            nStreamData = clientData.GetStream();
            StreamReader reader = new StreamReader(nStreamData);
            readData = reader.ReadToEnd();
        }
    }
}
