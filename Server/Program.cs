using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Common;
using Newtonsoft.Json;

namespace Server
{
    public class Program
    {
        public static List<User> Users = new List<User>();
        public static IPAddress IpAddress;
        public static int Port;
        static void Main(string[] args)
        {
            Users.Add(new User("aooshepkov", "Asdfg123", @"A:\Авиатехникум"));
            Console.Write("Введите Ip адрес сервера: ");
            string sIpAdress = Console.ReadLine();
            Console.Write("Введите порт: ");
            string sPort = Console.ReadLine();
            if (int.TryParse(sPort, out Port) && IPAddress.TryParse(sIpAdress, out IpAddress))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Данные успешно введены. Запуская сервер.");
                StartServer();
            }
            Console.Read();
        }
        public static bool AuthorizationUser(string login, string password)
        {
            User user = null;
            user = Users.Find(x => x.login == login && x.password == password);
            return user != null;
        }
        public static List<string> GetDirectory(string src)
        {
            List<string> FolderFiles = new List<string>();
            if (Directory.Exists(src))
            {
                string[] dirs = Directory.GetDirectories(src);
                foreach (string dir in dirs)
                {
                    string NameDirectory = dir.Replace(src, "");
                    FolderFiles.Add(NameDirectory + "/");
                }
                string[] files = Directory.GetFiles(src);
                foreach (string file in files)
                {
                    string NameFile = file.Replace(src, "");
                    FolderFiles.Add(NameFile);
                }
            }
            return FolderFiles;
        }
        public static void StartServer()
        {
            IPEndPoint endPoint = new IPEndPoint(IpAddress, Port);
            Socket sListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sListener.Bind(endPoint);
            sListener.Listen(10);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Сервер запущен.");
            while (true)
            {
                try
                {
                    Socket Handler = sListener.Accept();
                    string Data = null;
                    byte[] Bytes = new byte[10485760];
                    int ByteRec = Handler.Receive(Bytes);
                    Data += Encoding.UTF8.GetString(Bytes, 0, ByteRec);
                    Console.Write("Сообщение от пользователя: " + Data + "\n");
                    string Reply = "";
                    ViewModelSend ViewModelSend = JsonConvert.DeserializeObject<ViewModelSend>(Data);
                    if (ViewModelSend != null)
                    {
                        ViewModelMessage viewModelMessage;
                        string[] DataCommand = ViewModelSend.Message.Split(new string[1] { " " }, StringSplitOptions.None);
                        if (DataCommand[0] == "connect")
                        {
                            string[] DataMessage = ViewModelSend.Message.Split(new string[1] { " " }, StringSplitOptions.None);
                            if (AuthorizationUser(DataMessage[1], DataMessage[2]))
                            {
                                int IdUser = Users.FindIndex(x => x.login == DataMessage[1] && x.password == DataMessage[2]);
                                viewModelMessage = new ViewModelMessage("authorization", IdUser.ToString());
                            }
                            else
                            {
                                viewModelMessage = new ViewModelMessage("message", "Не правильный логин и пароль пользователя.");
                            }
                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                        }
                        else if (DataCommand[0] == "cd")
                        {
                            if (ViewModelSend.Id != -1)
                            {
                                string[] DataMessage = ViewModelSend.Message.Split(new string[1] { " " }, StringSplitOptions.None);
                                List<string> FolderFiles = new List<string>();
                                if (DataMessage.Length == 1)
                                {
                                    Users[ViewModelSend.Id].temp_src = Users[ViewModelSend.Id].src;
                                    FolderFiles = GetDirectory(Users[ViewModelSend.Id].src);
                                }
                                else
                                {
                                    string cdFolder = "";
                                    for (int i = 1; i < DataMessage.Length; i++)
                                    {
                                        if (cdFolder == "")
                                            cdFolder += DataMessage[i];
                                        else
                                            cdFolder += " " + DataMessage[i];
                                    }
                                    Users[ViewModelSend.Id].temp_src = Users[ViewModelSend.Id].temp_src + cdFolder;
                                    FolderFiles = GetDirectory(Users[ViewModelSend.Id].temp_src);
                                }
                                if (FolderFiles.Count == 0)
                                    viewModelMessage = new ViewModelMessage("message", "Директория пуста или не существует.");
                                else
                                    viewModelMessage = new ViewModelMessage("cd", JsonConvert.SerializeObject(FolderFiles));
                            }
                            else
                            {
                                viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться.");
                            }
                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                        }
                        else if (DataCommand[0] == "get")
                        {
                            if (ViewModelSend.Id != -1)
                            {
                                string[] DataMessage = ViewModelSend.Message.Split(new string[1] { " " }, StringSplitOptions.None);
                                string getFile = "";
                                for (int i = 1; i < DataMessage.Length; i++)
                                {
                                    if (getFile == "")
                                        getFile += DataMessage[i];
                                    else
                                        getFile += " " + DataMessage[i];
                                }
                                byte[] byteFile = File.ReadAllBytes(Users[ViewModelSend.Id].temp_src + getFile);
                                viewModelMessage = new ViewModelMessage("file", JsonConvert.SerializeObject(byteFile));
                            }
                            else
                            {
                                viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться.");
                            }
                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                        }
                        else
                        {
                            if (ViewModelSend.Id != -1)
                            {
                                FileInfoFTP SendFileInfo = JsonConvert.DeserializeObject<FileInfoFTP>(ViewModelSend.Message);
                                File.WriteAllBytes(Users[ViewModelSend.Id].temp_src + @"\" + SendFileInfo.Name, SendFileInfo.Data);
                                viewModelMessage = new ViewModelMessage("message", "Файл зфгружен");
                            }
                            else
                            {
                                viewModelMessage = new ViewModelMessage("message", "Необходимо авторизоваться.");
                            }
                            Reply = JsonConvert.SerializeObject(viewModelMessage);
                            byte[] message = Encoding.UTF8.GetBytes(Reply);
                            Handler.Send(message);
                        }
                    }
                }
                catch (Exception exp)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Что-то случилось: " + exp.Message);
                }
            }
        }
    }
}
