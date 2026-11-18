using System;
using System.Collections.ObjectModel;
using System.Data;
using Microsoft.Data.SqlClient;
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

namespace CasinoApp.Pages;

public sealed partial class ExchangesPage : Page
{

    public ObservableCollection<ExchangeView> Exchanges { get; } = new();
    public ExchangesPage()
    {
        InitializeComponent();

        UpdateCollection();
    }

    private void ListViewExchanges_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool hasSelection = ListViewExchanges.SelectedItem is ExchangeView;

        EditButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (ListViewExchanges.SelectedItem is not ExchangeView exchange)
            return;

        int exchangeID = exchange.ExchangeID;

        var message = new TextBlock
        {
            Text = $"Are you sure you want to delete\nExchange {exchangeID}?",
            FontSize = 18,
            TextWrapping = TextWrapping.Wrap
        };

        var dialog = UIHelper.CreateDialog($"Delete Exchange {exchangeID}", message, "Yes", "No", XamlRoot);
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        try
        {
            using var connection = DB.GetConnection();
            connection.Open();

            using var command = new SqlCommand(
                "DELETE FROM [EXCHANGE] WHERE Exchange_ID = @id", connection);

            command.Parameters.Add("@id", SqlDbType.Int).Value = exchangeID;

            int rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                Exchanges.Remove(exchange);
            }
        }
        catch (SqlException ex)
        {
            await UIHelper.ShowErrorAsync(XamlRoot, ex.Message);
        }
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (ListViewExchanges.SelectedItem is not ExchangeView exchange)
            return;

        try
        {
            /* Dialog box inputs */
            var gamblerCombo = UIHelper.CreateGamblerComboBox(exchange.GamblerID);

            var amountBox = new NumberBox
            {
                Header = "Amount (optional)",
                Value = exchange.Amount.HasValue ? exchange.Amount.Value : double.NaN,
                Minimum = 0,
                Maximum = int.MaxValue,
                SmallChange = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden
            };

            var toChipsCheckBox = new CheckBox
            {
                Content = "To Chips",
                IsChecked = exchange.ToChips
            };

            var datePicker = new DatePicker { Header = "Exchange Date (optional)" };
            var timePicker = new TimePicker { Header = "Exchange Time (optional)" };

            if (exchange.Time.HasValue)
            {
                datePicker.SelectedDate = new DateTimeOffset(exchange.Time.Value);
                timePicker.SelectedTime = exchange.Time.Value.TimeOfDay;
            }

            var panel = UIHelper.CreateFormPanel(gamblerCombo, amountBox, toChipsCheckBox, datePicker, timePicker);
            var dialog = UIHelper.CreateDialog($"Edit Exchange {exchange.ExchangeID}", panel, "Save", "Close", XamlRoot);

            dialog.PrimaryButtonClick += (s, args) =>
            {
                bool missingRequiredField =
                    // Gambler must be selected.
                    (gamblerCombo.SelectedItem as ComboBoxItem)?.Tag is not string ||

                    // Amount is optional but must be a nonnegative whole number when provided.
                    (!double.IsNaN(amountBox.Value) &&
                        (double.IsInfinity(amountBox.Value) ||
                         amountBox.Value < 0 ||
                         amountBox.Value > int.MaxValue ||
                         amountBox.Value % 1 != 0)) ||

                    // A date and time must either both be set or both be empty.
                    datePicker.SelectedDate.HasValue != timePicker.SelectedTime.HasValue;

                args.Cancel = missingRequiredField;
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            var selectedGambler = (ComboBoxItem)gamblerCombo.SelectedItem;
            string gamblerID = (string)selectedGambler.Tag;
            string gamblerName = selectedGambler.Content.ToString() ?? gamblerID;

            int? amount = double.IsNaN(amountBox.Value) ? null : (int)amountBox.Value;
            bool toChips = toChipsCheckBox.IsChecked == true;

            DateTime? exchangeTime = null;
            if (datePicker.SelectedDate is DateTimeOffset selectedDate &&
                timePicker.SelectedTime is TimeSpan selectedTime)
            {
                exchangeTime = selectedDate.Date.Add(selectedTime);
            }

            using var connection = DB.GetConnection();
            connection.Open();

            using var command = new SqlCommand(
                @"UPDATE [EXCHANGE]
                      SET Gambler = @gambler,
                          Amount = @amount,
                          To_Chips = @tochips,
                          [Time] = @time
                      WHERE Exchange_ID = @id", connection);

            command.Parameters.Add("@id", SqlDbType.Int).Value = exchange.ExchangeID;
            command.Parameters.Add("@gambler", SqlDbType.VarChar, 50).Value = gamblerID;
            command.Parameters.Add("@amount", SqlDbType.Int).Value = (object?)amount ?? DBNull.Value;
            command.Parameters.Add("@tochips", SqlDbType.Bit).Value = toChips;
            command.Parameters.Add("@time", SqlDbType.DateTime).Value = (object?)exchangeTime ?? DBNull.Value;

            int rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                // Replace the edited item so the ListView refreshes.
                int index = Exchanges.IndexOf(exchange);
                if (index >= 0)
                {
                    Exchanges[index] = new ExchangeView
                    {
                        ExchangeID = exchange.ExchangeID,
                        GamblerID = gamblerID,
                        GamblerName = gamblerName,
                        Amount = amount,
                        ToChips = toChips,
                        Time = exchangeTime
                    };
                }
            }
        }
        catch (SqlException ex)
        {
            await UIHelper.ShowErrorAsync(XamlRoot, ex.Message);
        }
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            /* Dialog box inputs */
            var idBox = new NumberBox
            {
                Header = "Exchange ID",
                Description = "This must be unique and cannot be changed.",
                Minimum = 1,
                Maximum = int.MaxValue,
                SmallChange = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden
            };

            var gamblerCombo = UIHelper.CreateGamblerComboBox();

            var amountBox = new NumberBox
            {
                Header = "Amount",
                Minimum = 0,
                Maximum = int.MaxValue,
                SmallChange = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden
            };

            var toChipsCheckBox = new CheckBox
            {
                Content = "To Chips",
                IsChecked = false
            };

            var datePicker = new DatePicker { Header = "Exchange Date (optional)" };
            var timePicker = new TimePicker { Header = "Exchange Time (optional)" };

            var panel = UIHelper.CreateFormPanel(idBox, gamblerCombo, amountBox, toChipsCheckBox, datePicker, timePicker);
            var dialog = UIHelper.CreateDialog("Add Exchange", panel, "Save", "Close", XamlRoot);

            dialog.PrimaryButtonClick += (s, args) =>
            {
                bool missingRequiredField =
                    // Exchange ID must be a positive whole number.
                    double.IsNaN(idBox.Value) ||
                    double.IsInfinity(idBox.Value) ||
                    idBox.Value < 1 ||
                    idBox.Value > int.MaxValue ||
                    idBox.Value % 1 != 0 ||

                    // Gambler must be selected.
                    (gamblerCombo.SelectedItem as ComboBoxItem)?.Tag is not string ||

                    // Amount is optional but must be a nonnegative whole number when provided.
                    (!double.IsNaN(amountBox.Value) &&
                        (double.IsInfinity(amountBox.Value) ||
                         amountBox.Value < 0 ||
                         amountBox.Value > int.MaxValue ||
                         amountBox.Value % 1 != 0)) ||

                    // A date and time must either both be set or both be empty.
                    datePicker.SelectedDate.HasValue != timePicker.SelectedTime.HasValue;

                args.Cancel = missingRequiredField;
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            int exchangeID = (int)idBox.Value;
            var selectedGambler = (ComboBoxItem)gamblerCombo.SelectedItem;
            string gamblerID = (string)selectedGambler.Tag;
            string gamblerName = selectedGambler.Content.ToString() ?? gamblerID;

            int amount = (int)amountBox.Value;
            bool toChips = toChipsCheckBox.IsChecked == true;

            DateTime? exchangeTime = null;
            if (datePicker.SelectedDate is DateTimeOffset selectedDate &&
                timePicker.SelectedTime is TimeSpan selectedTime)
            {
                exchangeTime = selectedDate.Date.Add(selectedTime);
            }

            using var connection = DB.GetConnection();
            connection.Open();

            using var command = new SqlCommand(
                @"INSERT INTO [EXCHANGE]
                      (Gambler, Amount, To_Chips, [Time])
                      VALUES (@gambler, @amount, @tochips, @time)", connection);

            command.Parameters.Add("@gambler", SqlDbType.VarChar, 50).Value = gamblerID;
            command.Parameters.Add("@amount", SqlDbType.Int).Value = (object?)amount ?? DBNull.Value;
            command.Parameters.Add("@tochips", SqlDbType.Bit).Value = toChips;
            command.Parameters.Add("@time", SqlDbType.DateTime).Value = (object?)exchangeTime ?? DBNull.Value;

            int rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                Exchanges.Add(new ExchangeView
                {
                    ExchangeID = exchangeID,
                    GamblerID = gamblerID,
                    GamblerName = gamblerName,
                    Amount = amount,
                    ToChips = toChips,
                    Time = exchangeTime
                });
            }
        }
        catch (SqlException ex)
        {
            await UIHelper.ShowErrorAsync(XamlRoot, ex.Message);
        }
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
                Exchanges.Add(new ExchangeView
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

public class ExchangeView
{
    public int ExchangeID { get; set; } = 0;
    public string GamblerID { get; set; } = "";
    public string GamblerName { get; set; } = "";
    public int? Amount { get; set; } = null;
    public bool ToChips { get; set; } = false;
    public DateTime? Time { get; set; } = null;

    // These are only used to display readable values in the ListView.
    public string AmountDisplay => Amount?.ToString() ?? "";
    public string ToChipsDisplay => ToChips ? "Yes" : "No";
    public string TimeDisplay => Time?.ToString("g") ?? "";
}

