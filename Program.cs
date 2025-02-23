using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Spectre.Console;
using Newtonsoft.Json;

public class LastNTracker<T>
{
    public List<T> Items { get; }
    private readonly int capacity;

    public LastNTracker(int capacity)
    {
        this.capacity = capacity;
        this.Items = new List<T>(capacity);
    }

    public void Add(T item)
    {
        if (Items.Count == capacity)
        {
            Items.RemoveAt(0);
        }
        Items.Add(item);
    }

    public IReadOnlyList<T> GetItems()
    {
        return Items.AsReadOnly();
    }
}

public class TimestampMessage
{
    public DateTime Timestamp { get; set; }
    public string Message { get; set; }
}
    
public class WebSocketMessage
{
    public string Name { get; set; }
    public string Direction { get; set; }
    public string ConnectorType { get; set; }
    public bool IsConnected { get; set; }
    public bool IsFaulted { get; set; }
    public string FaultMessage { get; set; }
    public long MessagesAttempted { get; set; }
    public long MessagesAccepted { get; set; }
    public long MinimumReadMs { get; set; }
    public long MaximumReadMs { get; set; }
    public long LastReadMs { get; set; }
    public long MinimumScriptMs { get; set; }
    public long MaximumScriptMs { get; set; }
    public long LastScriptMs { get; set; }
    public long MinimumLoopMs { get; set; }
    public long MaximumLoopMs { get; set; }
    public long LastLoopMs { get; set; }
    public long LoopCount { get; set; }
    public long ConnectCount { get; set; }
    public long DisconnectCount { get; set; }
    public long FaultCount { get; set; }
    public long OutboxSendFailCount { get; set; }
    public DateTime LastUpdate { get; set; }
    public DateTime StartTime { get; set; }
    public List<string> ActiveExclusionFilters { get; set; }
    public List<string> ActiveInclusionFilters { get; set; }
    public LastNTracker<TimestampMessage> RecentErrors { get; set; }
}

public class WebSocketConsoleApp
{
    public static async Task Main()
    {
        while (true)
        {
            AnsiConsole.Clear();
            
            string webSocketUrl = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter [green]WebSocket URL[/] (or 'exit' to quit):")
                    .DefaultValue("ws://127.0.0.1:9998")
                    .ValidationErrorMessage("[red]Please enter a valid WebSocket URL[/]")
                    .Validate(url =>
                    {
                        if (url.ToLower() == "exit")
                            return ValidationResult.Success();

                        return Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult) 
                               && (uriResult.Scheme == "ws" || uriResult.Scheme == "wss") 
                               ? ValidationResult.Success() 
                               : ValidationResult.Error();
                    })
            );

            if (webSocketUrl.ToLower() == "exit")
                break;

            try
            {
                await ConnectToWebSocketAsync(webSocketUrl);
            }
            catch (Exception ex)
            {
                AnsiConsole.Write(
                    new Panel(new Text(ex.Message))
                        .Header("Connection Error")
                        .BorderColor(Color.Red)
                );

                AnsiConsole.Prompt(
                    new TextPrompt<string>("[yellow]Press Enter to continue...[/]")
                        .AllowEmpty()
                );
            }
        }

        AnsiConsole.Write(
            new FigletText("Goodbye!")
                .Centered()
                .Color(Color.Green)
        );
    }

    private static async Task ConnectToWebSocketAsync(string url)
    {
        using var client = new ClientWebSocket();
        var cancellationTokenSource = new CancellationTokenSource();
        
        var channel = System.Threading.Channels.Channel.CreateUnbounded<WebSocketMessage>();

        await client.ConnectAsync(new Uri(url), CancellationToken.None);
        AnsiConsole.MarkupLine("[green]Connected successfully![/]");

        // Receive task
        var receiveTask = Task.Run(async () =>
        {
            while (client.State == WebSocketState.Open)
            {
                try
                {
                    var buffer = new byte[1024 * 4];
                    var result = await client.ReceiveAsync(
                        new ArraySegment<byte>(buffer), 
                        cancellationTokenSource.Token
                    );

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string receivedMessage = Encoding.UTF8.GetString(
                            buffer, 0, result.Count
                        );
                        
                        var message = JsonConvert.DeserializeObject<WebSocketMessage>(receivedMessage);
                        await channel.Writer.WriteAsync(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                }
                catch(Exception ex)
                {
                    //break;
                }
            }
            
            channel.Writer.Complete();
        }, cancellationTokenSource.Token);

        // Create the table
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[blue]Name[/]").Centered())
            .AddColumn(new TableColumn("[blue]Direction[/]").Centered())
            .AddColumn(new TableColumn("[blue]Type[/]")).Centered()
            .AddColumn(new TableColumn("[blue]Connected[/]")).Centered()
            .AddColumn(new TableColumn("[blue]Faulted[/]")).Centered()
            .AddColumn(new TableColumn("[blue]Msgs (accept/attempt)[/]")).Centered()
            .AddColumn(new TableColumn("[blue]Exec.Time (min/max/last)[/]")).Centered()
            .AddColumn(new TableColumn("[blue]Count (loop/fault/connect)[/]")).Centered()
            .AddColumn(new TableColumn("[blue]Fault Msg[/]")).Centered()
            .Title("[bold]Service Status[/]");

        // Live table rendering
        await AnsiConsole.Live(table)
            .StartAsync(async ctx =>
            {
                ConcurrentDictionary<string, WebSocketMessage> messages = new ConcurrentDictionary<string, WebSocketMessage>();
                
                // Read from channel
                await foreach (var message in channel.Reader.ReadAllAsync())
                {
                    messages[message.Name] = message;
                    
                    table.Rows.Clear();

                    foreach (var msg in messages)
                    {
                        // Add message to the table
                        table.AddRow(
                            msg.Value.Name, 
                            msg.Value.Direction, 
                            msg.Value.ConnectorType,
                            msg.Value.IsConnected.ToString(),
                            msg.Value.IsFaulted.ToString(),
                            $"{msg.Value.MessagesAccepted}/{msg.Value.MessagesAttempted}",
                            $"{msg.Value.MinimumLoopMs}/{msg.Value.MaximumLoopMs}/{msg.Value.LastLoopMs}",
                            $"{msg.Value.LoopCount}/{msg.Value.FaultCount}/{msg.Value.ConnectCount}",
                            msg.Value.FaultMessage
                        );
                    }
                    
                    // Refresh the display
                    ctx.Refresh();
                }

                // Wait for receive task to complete
                await receiveTask;

                // Close connection
                await client.CloseAsync(
                    WebSocketCloseStatus.NormalClosure, 
                    "Closing connection", 
                    CancellationToken.None
                );
            });

        AnsiConsole.MarkupLine("[red]WebSocket connection closed.[/]");
    }
}