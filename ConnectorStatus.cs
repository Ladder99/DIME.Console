namespace DIME.Console;

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