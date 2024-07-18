using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Server_Client
{
    public class ClientInfo
    {
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public string Name { get; set; }
    }

    public partial class MainWindow : Window
    {
        private TcpListener listener;
        private int port = 45454;
        private string name;
        int Count = 0;
        private ObservableCollection<string> connectedClients = new ObservableCollection<string>();
        private List<TcpClient> connectedTcpClients = new List<TcpClient>();
        public MainWindow()
        {
            InitializeComponent();
            log.ItemsSource = connectedClients;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            Thread listenThread = new Thread(Listen);
            listenThread.Start();
            Dispatcher.Invoke(() => connectedClients.Add("Сервер включен."));
        }


        void Listen()
        {   

            try
            {
                listener.Start();
                bool isServerStarted = true;

                while (isServerStarted)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();
                    IPEndPoint clientEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
                    ClientInfo clientInfo = new ClientInfo
                    {
                        IPAddress = clientEndPoint.Address.ToString(),
                        Port = 45454,
                        Name = name
                    };
                    connectedTcpClients.Add(client); // Store the connected client
                    Count = Count + 1;
                    byte[] data = BitConverter.GetBytes(Count);
                    stream.Write(data, 0, data.Length);
                    Dispatcher.BeginInvoke(new Action(() => connectedClients.Add("New client connected.")));

                    Thread clientThread = new Thread(() => Process(client));
                    clientThread.Start();
                    

                }
            }
            catch (SocketException ex)
            {
                // Handle the exception and provide feedback to the user
                Dispatcher.BeginInvoke(new Action(() => connectedClients.Add("Error occurred: " + ex.Message)));
            }
            finally
            {
                // Ensure that the listener is stopped when the server is no longer running
                if (listener != null)
                {
                    listener.Stop();
                }
            }
        }

        public async Task SendMessageAsync(TcpClient client, string message)
        {
            NetworkStream stream = client.GetStream();
            byte[] data = Encoding.Unicode.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);
        }

        public void Process(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream stream = null;

            try
            {
                stream = tcpClient.GetStream();
                byte[] data = new byte[64];

                while (tcpClient.Connected)
                {
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;

                    do
                    {
                        bytes = stream.Read(data, 0, data.Length);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (stream.DataAvailable);

                    string message = builder.ToString();
                    string recipientTcpClient = ExtractRecipient(message);

                    Dispatcher.BeginInvoke(new Action(() => connectedClients.Add(message)));

                    // Send the message to all connected clients
                    byte[] responseData = Encoding.Unicode.GetBytes(message);
                    byte[] recipientData = Encoding.Unicode.GetBytes(recipientTcpClient);
                    foreach (var connectedClient in connectedTcpClients)
                    {
                        
                        // Другие свойства клиента, которые вы хотите вывести
                        Console.WriteLine();

                        NetworkStream clientStream = connectedClient.GetStream();
                        //NetworkStream recipientStream = connectedTcpClients.FirstOrDefault(selectedClient => selectedClient.Name == recipientTcpClient)?.GetStream();
                        if (string.IsNullOrEmpty(recipientTcpClient))
                        {
                            clientStream.Write(responseData, 0, responseData.Length);
                        }
                        else {
                            clientStream.Write(recipientData, 0, recipientData.Length);
                            //clientStream.Write(responseData, 0, responseData.Length); // Отправка сообщения только адресату
                            
                        }
                        // Check if the recipient is in the list of connected clients

                    }



                }
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(new Action(() => connectedClients.Add(ex.Message)));
            }
            finally
            {
                stream?.Close();
                tcpClient.Close();
            }
        }

        private string ExtractRecipient(string message)
        {
            int recipientIndex = message.IndexOf(" to ");
            if (recipientIndex != -1)
            {
                int recipientEndIndex = message.IndexOf(":", recipientIndex);
                if (recipientEndIndex != -1)
                {
                    return message.Substring(recipientIndex + 4, recipientEndIndex - (recipientIndex + 4)).Trim();
                }
            }
            return string.Empty;
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => connectedClients.Add("Сервер выключен.")));
            listener.Stop();
        }
    }
}
