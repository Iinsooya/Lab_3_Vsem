using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Server_Client
{
    public partial class MainWindow : Window
    {
        private TcpListener listener;
        private int port = 45454;
        private ObservableCollection<string> connectedClients = new ObservableCollection<string>();

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

        private List<TcpClient> connectedTcpClients = new List<TcpClient>();

        void Listen()
        {
            try
            {
                listener.Start();
                bool isServerStarted = true;

                while (isServerStarted)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    connectedTcpClients.Add(client); // Store the connected client

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
                    Dispatcher.BeginInvoke(new Action(() => connectedClients.Add(message)));

                    // Send the message to all connected clients
                    byte[] responseData = Encoding.Unicode.GetBytes(message);
                    foreach (var connectedClient in connectedTcpClients)
                    {
                        NetworkStream clientStream = connectedClient.GetStream();
                        clientStream.Write(responseData, 0, responseData.Length);
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


        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => connectedClients.Add("Сервер выключен.")));
            listener.Stop();
        }
    }
}
