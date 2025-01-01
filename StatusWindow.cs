/*using System.Collections.Concurrent;
using Newtonsoft.Json;
using Terminal.Gui;
using WebSocketSharp;
using System.Data;
using System.Linq;

namespace DIME.Console;

public class StatusWindow: Window
{
    private WebSocket _client;
    private TextField _wsText;
    private Button _wsBtnConnect;
    private ProgressBar _progressBar;
    private TableView _tableView;
    private ConcurrentDictionary<string, ConnectorStatus> _connectorStatuses;
    private System.Timers.Timer _timer;
    private object _lock = new object();
    private bool _wsIsOpen = false;

    public StatusWindow()
    {
        _connectorStatuses = new ConcurrentDictionary<string, ConnectorStatus>();
        
        var wsLabel = new Label { Text = "URI:" };

        _wsText = new TextField
        {
            X = Pos.Right(wsLabel) + 1,
            Width = Dim.Percent(70),
            Text = "ws://127.0.0.1:9998"
        };
        
        _wsBtnConnect = new Button
        {
            Text = "Connect",
            X = Pos.Right(_wsText) + 1,
            Width = Dim.Percent(30),
            IsDefault = true
        };

        _progressBar = new ProgressBar
        {
            BidirectionalMarquee = true,
            ProgressBarFormat = ProgressBarFormat.Simple,
            ProgressBarStyle = ProgressBarStyle.Continuous,
            
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = Dim.Percent(50),
            Visible = false
        };

        _tableView = new TableView()
        {
            X = 0,
            Y = Pos.Bottom(_wsText) + 1,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        
        _wsBtnConnect.MouseClick += (s, e) =>
        {
            if (_wsBtnConnect.Text == "Connect")
            {
                _wsBtnConnect.Visible = false;
                Application.Refresh();
                _client = new WebSocket(_wsText.Text);
                _client.OnOpen += ClientOnOnOpen;
                _client.OnMessage += ClientOnOnMessage; 
                _client.Connect();

                int counter = 0;
                while (_wsIsOpen == false)
                {
                    Thread.Sleep(100);
                    Application.Refresh();
                    _progressBar.Pulse();
                    counter++;
                    if (counter >= 20) break;
                }
                _wsBtnConnect.Visible = true;
            }
            else if (_wsBtnConnect.Text == "Disconnect")
            {
                
            }
        };

        Add(wsLabel, _wsText, _wsBtnConnect, _progressBar, _tableView);
        
        _timer = new System.Timers.Timer();
        _timer.Elapsed += (sender, args) =>
        {
            try
            {
                Application.Refresh();
            }
            catch (Exception ex)
            {
                
            }
            
        };
        _timer.Interval = 100;
        _timer.Enabled = true;
        _timer.Start();
    }

    private void ClientOnOnOpen(object? sender, EventArgs e)
    {
        
    }

    private void ClientOnOnMessage(object? sender, MessageEventArgs e)
    {
        var message = JsonConvert.DeserializeObject<ConnectorStatus>(e.Data);
        _connectorStatuses[message.Name] = message;
        
        lock (_lock)
        {
            _tableView.Table = new EnumerableTableSource<ConnectorStatus> (_connectorStatuses.Values.ToList(),
                new Dictionary<string, Func<ConnectorStatus, object>>() {
                    { "Name",(p)=>p.Name},
                    { "Direction",(p)=>p.Direction},
                    { "Connected",(p)=>p.IsConnected},
                    { "Faulted",(p)=>p.IsFaulted},
                    { "Loops",(p)=>p.LoopCount},
                    { "Messages",(p)=>p.MessagesAccepted},
                    { "Exec.Time",(p)=>p.LastLoopMs},
                    { "Fault",(p)=>p.FaultMessage},
                    { "Updated",(p)=>p.LastUpdate},
                });
        }
    }
}*/