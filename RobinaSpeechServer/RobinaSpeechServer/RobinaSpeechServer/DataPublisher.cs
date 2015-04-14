using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
namespace RobinaSpeechServer
{
    public class DataPublisher
    {
        Socket socket; //udp socket
        EndPoint endpoint;
        System.Threading.Mutex mutex = new System.Threading.Mutex();
        public DataPublisher(string host,string self,int port)
        {
            //socket.Bind(new IPEndPoint(IPAddress.Parse(self), port));

            endpoint = new IPEndPoint(IPAddress.Parse(host), port);
            //socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            init();
        }
        ~DataPublisher()
        {
            if (socket != null)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                
            }
            mutex.Close();
        }
        private void init()
        {
            do
            {
                try
                {
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    Console.WriteLine("Conntecting to the {0}",endpoint);
                    socket.Connect(endpoint);
                    Console.WriteLine("Conntected to the host");
                    break;
                }
                catch (SocketException)
                {
                    Console.WriteLine("address is invalid or network is not working");
                    System.Threading.Thread.Sleep(1000);
                    continue;
                }
            } while (true);
        }
        public void publish(Messages.Message d)
        {
            do
            {

                try
                {
                    mutex.WaitOne();
                    StringWriter writer = new StringWriter();
                    Newtonsoft.Json.JsonSerializer serialization = new Newtonsoft.Json.JsonSerializer();
                    serialization.Serialize(writer, d);
                    //socket.SendTo(ASCIIEncoding.ASCII.GetBytes(writer.ToString()),endpoint);

                    socket.Send(ASCIIEncoding.ASCII.GetBytes(writer.ToString()));
                    mutex.ReleaseMutex();
                }
                catch (SocketException)
                {
                    mutex.ReleaseMutex();
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    init();
                    continue;
                }
            } while (false);

        }
        
    }
}
