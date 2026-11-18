using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Data.SqlTypes;
using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CasinoApp.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ExchangesPage : Page
{

    public ObservableCollection<ExchangesView> Exchanges { get; } = new();
    public ExchangesPage()
    {
        InitializeComponent();

        UpdateCollection();
    }

    public void UpdateCollection()
    {

        Exchanges.Clear();

        var Connection = DB.GetConnection();
        Connection.Open();
        var Query = Connection.CreateCommand();
        Query.CommandText = @"
            SELECT Exchange.*, f_name, l_name
            FROM Exchange JOIN Gambler ON gambler_id = gambler
            ORDER BY exchange_id DESC
        ";
        using (SqlDataReader reader = Query.ExecuteReader())
        {
            while(reader.Read())
            {
                Exchanges.Add(new ExchangesView
                {
                    ExchangeID = reader.GetInt32(0),
                    GamblerID = reader.GetString(1),
                    GamblerName = $"{reader.GetString(5)} {reader.GetString(6)}",
                    Amount = reader.GetInt32(2),
                    ToChips = reader.GetBoolean(3),
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
        if(DemoStarted)
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

            var ExchangeQuery = Connection.CreateCommand();
            ExchangeQuery.CommandText = $@"
                INSERT INTO Exchange (gambler, amount, to_chips, time)
                values(@gambler, @amount, @to_chips, @time)
            ";

            var rng = new Random();

            ExchangeQuery.Parameters.AddWithValue("@gambler", gamid);
            ExchangeQuery.Parameters.AddWithValue("@amount", rng.Next(1, 20000));
            ExchangeQuery.Parameters.AddWithValue("@to_chips", rng.Next()%2);
            ExchangeQuery.Parameters.AddWithValue("@time", DateTime.Now);

            ExchangeQuery.ExecuteNonQuery();
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

public class ExchangesView()
{
    private SqlDateTime time;

    public int ExchangeID { get; set; } = -1;
    public string GamblerID { get; set; } = "";
    public string GamblerName { get; set; } = "";
    public int Amount { get; set; } = -1;
    public bool ToChips { get; set; } = true;
    public SqlDateTime Time { get => time; set => time = value; }
    
}
