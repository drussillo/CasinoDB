using Microsoft.Data.SqlClient;
using Microsoft.ML.OnnxRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

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
                    GamblerName = $"{reader.GetString(6)} {reader.GetString(7)}",
                    Wager = reader.GetInt32(2),
                    Result = reader.GetInt32(3),
                    Time = reader.GetDateTime(4),
                    SessionID = reader.GetInt32(5)
                });
            }
        }

        Connection.Close();
    }


    // Parts of the following functions were generated using AI
    private async void Bet_Click(object sender, RoutedEventArgs e)
    {
        SessionOption? selectedSession = null;
        GamblerOption? selectedGambler = null;

        // Session search
        var sessionBox = new AutoSuggestBox
        {
            Header = "Session",
            PlaceholderText = "Search Session",
            DisplayMemberPath = "Display",
            TextMemberPath = "Display",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 550
        };

        var sessionDetails = new TextBlock
        {
            Text = "No session selected.",
            TextWrapping = TextWrapping.Wrap,
            MinWidth = 650
        };

        // Gambler search
        var gamblerBox = new AutoSuggestBox
        {
            Header = "Gambler",
            PlaceholderText = "Search Gambler",
            DisplayMemberPath = "Display",
            TextMemberPath = "Display",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var gamblerDetails = new TextBlock
        {
            Text = "No gambler selected.",
            TextWrapping = TextWrapping.Wrap
        };

        // Random gambler button
        var randomButton = new Button
        {
            Content = "Random Gambler",
            HorizontalAlignment = HorizontalAlignment.Left
        };

        // Wager information
        var wagerLimits = new TextBlock
        {
            Text = "Select a session to see wager limits.",
            TextWrapping = TextWrapping.Wrap
        };

        var wagerBox = new NumberBox
        {
            Header = "Wager",
            PlaceholderText = "Enter wager amount",
            Value = 0,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
            IsEnabled = false,
        };

        // Session search suggestions
        sessionBox.TextChanged += (s, args) =>
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
                return;

            selectedSession = null;
            wagerBox.IsEnabled = false;
            wagerLimits.Text = "Select a session to see wager limits.";
            sessionDetails.Text = "No session selected.";

            var matches = SearchSessions(sessionBox.Text);

            sessionBox.ItemsSource = matches;
        };

        // Session selection
        sessionBox.SuggestionChosen += (s, args) =>
        {
            if (args.SelectedItem is not SessionOption session)
                return;

            selectedSession = session;

            sessionDetails.Text =
                $"Session ID: {session.SessionID}\n" +
                $"Employee: {session.Employee}\n" +
                $"Equipment: {session.Equipment}\n" +
                $"Start: {session.StartTime}\n" +
                $"End: {session.EndTime}";

            wagerLimits.Text =
                $"Minimum wager: {session.MinBet}\n" +
                $"Maximum wager: {session.MaxBet}";

            wagerBox.Minimum = session.MinBet;
            wagerBox.Maximum = session.MaxBet;
            wagerBox.Value = session.MinBet;
            wagerBox.IsEnabled = true;
        };

        // Gambler search suggestions
        gamblerBox.TextChanged += (s, args) =>
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
                return;

            selectedGambler = null;
            gamblerDetails.Text = "No gambler selected.";

            gamblerBox.ItemsSource = SearchGamblers(gamblerBox.Text);
        };

        // Gambler selection
        gamblerBox.SuggestionChosen += (s, args) =>
        {
            if (args.SelectedItem is not GamblerOption gambler)
                return;

            selectedGambler = gambler;

            gamblerDetails.Text =
                $"Gambler ID: {gambler.GamblerID}\n" +
                $"Name: {gambler.FirstName} {gambler.LastName}\n" +
                $"Membership: {gambler.Membership}";
        };

        // Random gambler selection
        randomButton.Click += (s, args) =>
        {
            var gambler = GetRandomGambler();

            if (gambler == null)
            {
                gamblerDetails.Text = "No gamblers found.";
                selectedGambler = null;
                return;
            }

            selectedGambler = gambler;
            gamblerBox.Text = gambler.Display;

            gamblerDetails.Text =
                $"Gambler ID: {gambler.GamblerID}\n" +
                $"Name: {gambler.FirstName} {gambler.LastName}\n" +
                $"Membership: {gambler.Membership}";
        };

        var panel = CreateFormPanel(
            sessionBox,
            sessionDetails,
            gamblerBox,
            randomButton,
            gamblerDetails,
            wagerLimits,
            wagerBox
        );

        var dialog = CreateDialog(
            "Place a Bet",
            panel,
            "Place Bet",
            "Cancel"
        );


        // Validate before allowing the dialog to close.
        dialog.PrimaryButtonClick += (s, args) =>
        {
            bool invalid =
                selectedSession == null ||
                selectedGambler == null ||
                !double.IsFinite(wagerBox.Value) ||
                wagerBox.Value != Math.Truncate(wagerBox.Value) ||
                wagerBox.Value < selectedSession?.MinBet ||
                wagerBox.Value > selectedSession?.MaxBet;

            args.Cancel = invalid;
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        try
        {
            int wager = (int)wagerBox.Value;

            // TODO: Replace this with your actual game logic.
            int result = (int)CalculateBetResult(selectedSession!, wager);

            // Save the bet to the database.
            int betID = InsertBet(
                selectedSession!.SessionID,
                selectedGambler!.GamblerID,
                wager,
                result
            );

            // Show the result.
            var resultDialog = CreateDialog(
                "Bet Placed!",
                new TextBlock
                {
                    Text =
                        $"Bet ID: {betID}\n" +
                        $"Gambler: {selectedGambler.FirstName} " +
                        $"{selectedGambler.LastName}\n" +
                        $"Wager: {wager}\n" +
                        $"Result: {result}",
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 18
                },
                "OK",
                ""
            );

            await resultDialog.ShowAsync();

            // Refresh the existing ListView.
            UpdateCollection();
        }
        catch (SqlException)
        {
            //await ShowErrorAsync();
        }


        UpdateCollection();

    }

    private List<SessionOption> SearchSessions(string search)
    {
        var results = new List<SessionOption>();

        if (string.IsNullOrWhiteSpace(search))
            return results;

        using var connection = DB.GetConnection();
        connection.Open();

        using var command = new SqlCommand(@"
        SELECT TOP 20
            s.session_id,
            s.employee,
            s.equipment,
            s.start_time,
            s.end_time,
            e.min_bet,
            e.max_bet,
            emp.f_name,
            emp.l_name
        FROM [Session] s JOIN Equipment e ON s.equipment = e.equipment_id
                         JOIN Employee emp ON s.employee = emp.ssn
        WHERE
            CAST(s.session_id AS NVARCHAR(20)) LIKE @search
            OR s.employee LIKE @search
            OR CONCAT(emp.f_name, ' ', emp.l_name) LIKE @search
            OR s.equipment LIKE @search
            OR CONVERT(NVARCHAR(30), s.start_time, 120) LIKE @search
            OR CONVERT(NVARCHAR(30), s.end_time, 120) LIKE @search
        ORDER BY s.session_id DESC
    ", connection);

        command.Parameters.Add("@search", SqlDbType.NVarChar).Value =
            $"%{search}%";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            results.Add(new SessionOption
            {
                SessionID = reader.GetInt32(0),
                Employee = $"{reader.GetString(7)} {reader.GetString(8)} {reader.GetString(1)}",
                Equipment = reader.GetString(2),
                StartTime = reader.GetDateTime(3),
                EndTime = reader.GetDateTime(4),
                MinBet = reader.GetInt32(5),
                MaxBet = reader.GetInt32(6)
            });
        }

        return results;
    }


    private List<GamblerOption> SearchGamblers(string search)
    {
        var results = new List<GamblerOption>();

        if (string.IsNullOrWhiteSpace(search))
            return results;

        using var connection = DB.GetConnection();
        connection.Open();

        using var command = new SqlCommand(@"
        SELECT TOP 20
            gambler_id,
            f_name,
            l_name,
            membership
        FROM GAMBLER
        WHERE
            gambler_id LIKE @search
            OR CONCAT(f_name, ' ', l_name) LIKE @search
            OR membership LIKE @search
        ORDER BY f_name
    ", connection);

        command.Parameters.Add("@search", SqlDbType.NVarChar).Value =
            $"%{search}%";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            results.Add(new GamblerOption
            {
                GamblerID = reader.GetString(0),
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                Membership = reader.GetString(3)
            });
        }

        return results;
    }


    private GamblerOption? GetRandomGambler()
    {
        using var connection = DB.GetConnection();
        connection.Open();

        using var command = new SqlCommand(@"
        SELECT TOP 1
            gambler_id,
            f_name,
            l_name,
            membership
        FROM GAMBLER
        ORDER BY NEWID()
    ", connection);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return new GamblerOption
        {
            GamblerID = reader.GetString(0),
            FirstName = reader.GetString(1),
            LastName = reader.GetString(2),
            Membership = reader.GetString(3)
        };
    }


    private int InsertBet(
    int sessionID,
    string gamblerID,
    int wager,
    int result)
    {
        using var connection = DB.GetConnection();
        connection.Open();

        using var command = new SqlCommand(@"
        INSERT INTO BET
            (gambler, wager, result, time, sessionid)
        OUTPUT INSERTED.bet_id
        VALUES
            (@gambler, @wager, @result, @time, @sessionid)
    ", connection);

        command.Parameters.Add("@gambler", SqlDbType.NVarChar).Value =
            gamblerID;

        command.Parameters.Add("@wager", SqlDbType.Int).Value =
            wager;

        command.Parameters.Add("@result", SqlDbType.Int).Value =
            result;

        command.Parameters.Add("@time", SqlDbType.DateTime).Value =
            DateTime.Now;

        command.Parameters.Add("@sessionid", SqlDbType.Int).Value =
            sessionID;


        return (int)command.ExecuteScalar()!;
    }


    private int CalculateBetResult(SessionOption session, int wager)
    {
        int roll = Random.Shared.Next(100);

        return (int)(roll switch
        {
            < 40 => -1m,    // 40%
            < 60 => -0.5m,  // 20%
            < 75 => 0m,     // 15%
            < 90 => 1m,     // 15%
            < 97 => 2m,     // 7%
            _ => 10m        // 3%
        }) * wager;

    }




    // Build a vertical form without repeating Children.Add calls.
    private static StackPanel CreateFormPanel(params UIElement[] controls)
    {
        var panel = new StackPanel { Spacing = 12 };

        foreach (var control in controls)
        {
            panel.Children.Add(control);
        }

        return panel;
    }

    // Use the same dialog settings throughout the page.
    private ContentDialog CreateDialog(
        string title, object content, string primaryButton, string closeButton)
    {
        return new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = primaryButton,
            CloseButtonText = closeButton,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };
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
    public int SessionID { get; set; } = -1;
    public SqlDateTime Time { get => time; set => time = value; }
}


public class SessionOption
{
    public int SessionID { get; set; }
    public string Employee { get; set; } = "";
    public string Equipment { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public int MinBet { get; set; }
    public int MaxBet { get; set; }

    public string Display =>
        $"ID: {SessionID} | Emp: {Employee} | " +
        $"Eq: {Equipment} | {StartTime:g}";
}

public class GamblerOption
{
    public string GamblerID { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Membership { get; set; } = "";

    public string Display =>
        $"{FirstName} {LastName} | {GamblerID} | {Membership}";
}


