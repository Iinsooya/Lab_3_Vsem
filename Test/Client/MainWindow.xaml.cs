using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Client
{
    /// <summary>
    /// Логика взаимодействия для Win1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpClient client = null;
        NetworkStream stream = null;
        public string username;
        public int port = 45454;
        public int Count;
        public string address = "127.0.0.1";
        
        public MainWindow()
        {
            InitializeComponent();

        }

        void listen()
        {
            try
            {
                while (true)
                {
                    byte[] data = new byte[64];
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    do
                    {
                        bytes = stream.Read(data, 0, data.Length);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (stream.DataAvailable);
                    string message = builder.ToString();
                    Dispatcher.BeginInvoke(new Action(() => Clientlog.Items.Add(message)));
                }
            }
            catch (Exception ex)
            {
                Clientlog.Items.Add(ex.Message);
            }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            
            try
            {
                username = Name.Text;
                client = new TcpClient();
                stream = client.GetStream();
                MessageBox.Show("Подключено к серверу.");

                byte[] receivedData = new byte[4]; // Предполагается, что счетчик представлен 4 байтами
                stream.Read(receivedData, 0, receivedData.Length);
                int counter = BitConverter.ToInt32(receivedData, 0);

                string[] parts = address.Split('.');
                int lastNumber = int.Parse(parts[parts.Length - 1]);
                int newLastNumber = lastNumber + counter;
                parts[parts.Length - 1] = newLastNumber.ToString();
                string Add = string.Join(".", parts);
                
                
                




                Thread listenThread = new Thread(() => listen());

                // Сделать занесение при подключении данных о клиенте (Имя, адрес, порт)



                listenThread.Start();
               
                //byte[] name = Encoding.Unicode.GetBytes(Name.Text);
                byte[] count = Encoding.Unicode.GetBytes(Add.ToString());
                // stream.Write(data, 0, data.Length);
                //stream.Write(name, 0, name.Length);
                stream.Write(count, 0, count.Length);

            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(new Action(() => Clientlog.Items.Add("Ошибка: " + ex.Message)));
            }
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            string message = Mes.Text;
            if (!string.IsNullOrEmpty(Address.Text))
            {
                message = String.Format("{0} to {1}: {2}", username, Address.Text, message);
            }
            else
            {
                message = String.Format("{0}: {1}", username, message);
            }
            byte[] data = Encoding.Unicode.GetBytes(message);
            stream.Write(data, 0, data.Length);
        }


        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            string mes = "Отключился от сервера";
            mes = String.Format("{0}: {1}", username, mes);
            byte[] data = Encoding.Unicode.GetBytes(mes);
            stream.Write(data, 0, data.Length);
            MessageBox.Show("Отключено от сервера.");
        }

       
    }
}
