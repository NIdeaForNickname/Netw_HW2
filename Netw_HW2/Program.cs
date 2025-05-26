using System.Net;
using System.Net.Sockets;
using System.Text;

class Client
{
    
    private const int DEFAULT_BUFLEN = 512;
    private const string DEFAULT_PORT = "27015";

    static async Task Main()
    {
        Console.Title = "CLIENT SIDE";
        try
        {
            var ipAddress = IPAddress.Loopback;
            var remoteEndPoint = new IPEndPoint(ipAddress, int.Parse(DEFAULT_PORT));

            var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await clientSocket.ConnectAsync(remoteEndPoint);
            Console.WriteLine("Нажмите на соответствующую клавишу");
            Console.Write("[1] - Дата\n[2] - Время\n[3] - Погода\n[4] - Курс Евро\n[5] - Курс Биткоина\n[_] - Случайное число");

            var sendingTask = Task.Run(async () =>
            {
                while (true)
                {
                    var message = Console.ReadKey(true);
                    if (!int.TryParse(message.KeyChar.ToString(), out _)) break;

                    byte[] messageBytes = Encoding.UTF8.GetBytes(message.KeyChar.ToString());
                    await clientSocket.SendAsync(new ArraySegment<byte>(messageBytes), SocketFlags.None);
                }

                clientSocket.Shutdown(SocketShutdown.Send);
            });

            var receivingTask = Task.Run(async () =>
            {
                while (true)
                {
                    var buffer = new byte[DEFAULT_BUFLEN];
                    int bytesReceived = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    if (bytesReceived > 0)
                    {
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                        Console.SetCursorPosition(19, int.Parse(response[0].ToString())+1);
                        Console.Write($"\t{response.Substring(1)}");
                        Console.WriteLine("               ");
                    }
                    else
                    {
                        break;
                    }
                }
            });

            await Task.WhenAll(sendingTask, receivingTask);

            clientSocket.Close();
            Console.WriteLine("Соединение с сервером закрыто.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Произошла ошибка: {ex.Message}");
        }
    }
}