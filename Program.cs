using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Адрес сервера WebSocket
        string serverUri = "wss://echo.websocket.events"; // Общедоступный тестовый сервер WebSocket

        using (ClientWebSocket webSocket = new ClientWebSocket())
        {
            // Попытка подключения к серверу
            try
            {
                Console.WriteLine("Подключение к серверу...");
                await webSocket.ConnectAsync(new Uri(serverUri), CancellationToken.None);
                Console.WriteLine("Подключено!");

                // Запуск задач для получения сообщений от сервера и обработки ввода пользователя
                var receiveTask = ReceiveMessages(webSocket);
                var pingTask = SendPingPeriodically(webSocket);
                var userInputTask = HandleUserInput(webSocket);

                // Ожидание завершения всех задач
                await Task.WhenAll(receiveTask, pingTask, userInputTask);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка подключения: {ex.Message}");
            }
        }
    }

    static async Task SendPingPeriodically(ClientWebSocket webSocket)
    {
        while (webSocket.State == WebSocketState.Open)
        {
            await SendMessage(webSocket, "ping");
            await Task.Delay(30000);
        }
    }

    static async Task HandleUserInput(ClientWebSocket webSocket)
    {
        while (webSocket.State == WebSocketState.Open)
        {
            string userInput = Console.ReadLine();
            if (!string.IsNullOrEmpty(userInput))
            {
                await SendMessage(webSocket, userInput);
            }
        }
    }

    static async Task SendMessage(ClientWebSocket webSocket, string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        Console.WriteLine($"Сообщение отправлено: {message}");
    }

    static async Task ReceiveMessages(ClientWebSocket webSocket)
    {
        byte[] buffer = new byte[1024];

        while (webSocket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = null;
            var messageBuffer = new ArraySegment<byte>(buffer);

            try
            {
                result = await webSocket.ReceiveAsync(messageBuffer, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения сообщения: {ex.Message}");
                break;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                Console.WriteLine("Соединение закрыто сервером");
            }
            else
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Сообщение от сервера: {message}");
            }
        }
    }
}
