using MongoDB.Driver.Core.Configuration;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;


namespace PCinfoServer
{
    public partial class Form1 : Form
    {
        public class ServerSettings
        {
            public int Port { get; set; }
        }
        private TcpListener tcpListener;
        public Form1()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Загрузка настроек сервера из файла
            _ = LoadSettings();
        }

       

        private ServerSettings LoadSettings()
        {
            try
            {
                string settingsFile = "server_settings.txt";

                if (File.Exists(settingsFile))
                {
                    using (StreamReader sr = new StreamReader(settingsFile))
                    {
                        string json = sr.ReadToEnd();
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<ServerSettings>(json);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки настроек сервера: " + ex.Message);
            }

            return null;
        }

        private void SaveSettings(ServerSettings settings)
        {
            try
            {
                string settingsFile = "server_settings.txt";
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(settings);

                using (StreamWriter sw = new StreamWriter(settingsFile))
                {
                    sw.Write(json);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения настроек сервера: " + ex.Message);
            }
        }

        private void StartServer(int port)
        {

            try
            {
                // Создание TCP прослушивателя
                tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();

                // Ожидание подключений
                while (true)
                {
                    // Принятие подключения
                    TcpClient client = tcpListener.AcceptTcpClient();

                    // Обработка подключения в отдельном потоке
                    HandleClient(client);
                }
            }
            catch (SocketException ex)
            {
                MessageBox.Show($"Ошибка запуска сервера: {ex.Message}");
            }
            finally
            {
                if (tcpListener != null)
                {
                    tcpListener.Stop();
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream);

            string request = reader.ReadLine();
            string response = ProcessRequest(request);

            writer.WriteLine(response);
            writer.Flush();

            client.Close();
        }

        private string ProcessRequest(string request)
        {
            if (request == "get_info")
            {
                return "Введите тип запроса: user_info (получить информацию о пользователе) или disk_info (получить информацию о диске).";
            }
            else if (request == "user_info")
            {
                string userName = Environment.UserName;
                string myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                return $"Имя пользователя: {userName}\n" +
                       $"Путь к 'Мои документы': {myDocumentsPath}";
            }
            else if (request == "disk_info")
            {
                string driveName = Directory.GetDirectoryRoot(Environment.SystemDirectory);
                DriveInfo drive = new DriveInfo(driveName);

                return $"Диск: {driveName}\n" +
                       $"Свободное место на диске: {drive.AvailableFreeSpace / (1024 * 1024)} MB";
            }

            return "Неверный запрос.";
        }
    

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Остановка сервера при закрытии формы
            if (tcpListener != null)
            {
                tcpListener.Stop();
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (tcpListener != null && tcpListener.Server.IsBound)
            {
                MessageBox.Show("Сервер уже запущен.");
                return;
            }

            int port = (int)nudPort.Value;

            if (port < 1 || port > 65535)
            {
                MessageBox.Show("Недопустимый номер порта. Введите значение от 1 до 65535.");
                return;
            }

            // Сохранение настроек сервера
            ServerSettings settings = new ServerSettings { Port = port };
            SaveSettings(settings);

            // Запуск сервера
            StartServer(port);

            MessageBox.Show("Сервер запущен!");
        }
    }
}
