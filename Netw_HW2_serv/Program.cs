using System.Collections;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;

class Server
{
    private static Random rnd = new Random();
    private const int DEFAULT_BUFLEN = 512;
    private const string DEFAULT_PORT = "27015";
    private static ConcurrentQueue<(Socket, byte[])> messageQueue = new ConcurrentQueue<(Socket, byte[])>();

    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "SERVER SIDE";
        Console.WriteLine("Процесс сервера запущен!");

        try
        {
            var ipAddress = IPAddress.Any;
            var localEndPoint = new IPEndPoint(ipAddress, int.Parse(DEFAULT_PORT));

            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint); 
            listener.Listen(10); 
            Console.WriteLine("Начинается прослушивание информации от клиента.\nПожалуйста, запустите клиентскую программу!");

            var clientSocket = await listener.AcceptAsync();
            Console.WriteLine("Подключение с клиентской программой установлено успешно!");

            listener.Close();

            _ = ProcessMessages(); 

            while (true)
            {
                var buffer = new byte[DEFAULT_BUFLEN];
                int bytesReceived = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None); 

                if (bytesReceived > 0)
                {
                    messageQueue.Enqueue((clientSocket, buffer));
                    Console.WriteLine($"Добавлено сообщение в очередь.");
                }
                else
                {
                    Console.WriteLine("Ошибка при получении данных.");
                    break;
                }
            }

            clientSocket.Shutdown(SocketShutdown.Send);
            clientSocket.Close();
            Console.WriteLine("Процесс сервера завершает свою работу!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Произошла ошибка: {ex.Message}");
        }
    }

    private static async Task ProcessMessages()
    {
        while (true)
        {
            if (messageQueue.TryDequeue(out var item))
            {
                var (clientSocket, buffer) = item;
                string message = Encoding.UTF8.GetString(buffer);
                Console.WriteLine($"Процесс клиента отправил сообщение: {message}");
                
                var response = new string("6"+rnd.Next(10));
                if (int.TryParse(message, out int id))
                {
                    var weather = new List<string>{ "Солнечно", "Хмуро", "Дождь", "Ливень", "Снег" };
                    switch (id)
                    {
                        case 1: response = $"{id}{DateTime.Now:yyyy-MM-dd}";break; // ого
                        case 2: response = $"{id}{DateTime.Now:HH:mm:ss}";break;
                        case 3: response = $"{id}{weather[rnd.Next(weather.Count)]}";break;
                        case 4: response = $"{id}{Math.Round(rnd.NextDouble() * 4 + 46, 2)}";break;
                        case 5: response = $"{id}{Math.Round(rnd.NextDouble() * 40000 + 80000, 2)}";break;
                    }
                }

                byte[] responseBytes = Encoding.UTF8.GetBytes(response);

                await clientSocket.SendAsync(new ArraySegment<byte>(responseBytes), SocketFlags.None);
                Console.WriteLine($"Процесс сервера отправляет ответ: {response}");
            }

            await Task.Delay(100);  
        }
    }
}