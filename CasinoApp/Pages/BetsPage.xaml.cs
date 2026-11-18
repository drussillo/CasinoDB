
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.UI.Xaml;

namespace CasinoApp.Pages;

public sealed partial class BetsPage : Page
{

    public ObservableCollection<BetsView> Bets { get; } = new();

    public BetsPage()
    {
        InitializeComponent();

        UpdateCollection();
    }

    public void UpdateCollection()
    {

        Bets.Clear();

        var Connection = DB.GetConnection();
        Connection.Open();
        var Query = Connection.CreateCommand();
        Query.CommandText = @"
            SELECT Bet.*, f_name, l_name
            FROM Bet JOIN Gambler ON gambler_id = gambler
            ORDER BY bet_id DESC
        ";
        using (SqlDataReader reader = Query.ExecuteReader())
        {
            while (reader.Read())
            {
                Bets.Add(new BetsView
                {
                    BetID = reader.GetInt32(0),
                    GamblerID = reader.GetString(1),
                    GamblerName = $"{reader.GetString(5)} {reader.GetString(6)}",
                    Wager = reader.GetInt32(2),
                    Result = reader.GetInt32(3),
                    Time = reader.GetDateTime(4),
                });
            }
        }

        Connection.Close();
    }


    public bool DemoStarted = false;
    private CancellationTokenSource? _demoCts;
    private async void Demo_Click(object sender, RoutedEventArgs e)
    {
        if (DemoStarted)
        {
            DemoStarted = false;

            Demo.Content = "Start Demo";
            _demoCts?.Cancel();
            _demoCts?.Dispose();
            _demoCts = null;
            return;
        }
        DemoStarted = true;
        Demo.Content = "Stop Demo";

        _demoCts = new CancellationTokenSource();
        try
        {
            await RunDemoAsync(_demoCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Demo stopped.
        }
    }

    private async Task RunDemoAsync(CancellationToken token)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();

            // Add randomly generated session
            var Connection = DB.GetConnection();
            Connection.Open();

            var GamQuery = Connection.CreateCommand();
            GamQuery.CommandText = @"
                SELECT TOP 1 gambler_id
                FROM Gambler
                ORDER BY NEWID();
            ";

            var gamid = (string)GamQuery.ExecuteScalar();

            var BetQuery = Connection.CreateCommand();
            BetQuery.CommandText = $@"
                INSERT INTO Bet (gambler, wager, result, time)
                values(@gambler, @wager, @result, @time)
            ";

            BetQuery.Parameters.AddWithValue("@gambler", gamid);
            BetQuery.Parameters.AddWithValue("@wager", 1);
            BetQuery.Parameters.AddWithValue("@result", 1);
            BetQuery.Parameters.AddWithValue("@time", DateTime.Now);

            BetQuery.ExecuteNonQuery();
            UpdateCollection();

            // Wait until next insert
            await Task.Delay(TimeSpan.FromSeconds(1), token);
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _demoCts?.Cancel();
        _demoCts?.Dispose();
        _demoCts = null;
    }
}


public class BetsView()
{
    private SqlDateTime time;

    public int BetID { get; set; } = -1;
    public string GamblerID { get; set; } = "";
    public string GamblerName { get; set; } = "";
    public int Wager { get; set; } = -1;
    public int Result { get; set; } = -1;
    public SqlDateTime Time { get => time; set => time = value; }
}

