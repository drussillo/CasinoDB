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
using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;
using System.Data.SqlTypes;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CasinoApp.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
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
            while(reader.Read())
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


    private async void Bet_Click(object sender, RoutedEventArgs e)
    {

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





