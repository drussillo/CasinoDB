using ABI.Microsoft.UI.Xaml.Markup;
using Microsoft.Data.SqlClient;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Windows.Devices.Enumeration;
using static System.Net.Mime.MediaTypeNames;

namespace CasinoApp.Pages
{
    public sealed partial class MachinePage : Page
    {
        ObservableCollection<EquipmentView> Equipment = new ObservableCollection<EquipmentView>();

        public MachinePage()
        {
            InitializeComponent();

            LoadEquipment();

        }

        private void LoadEquipment()
        {
            var Connection = DB.GetConnection();
            Connection.Open();
            var Query = Connection.CreateCommand();
            Query.CommandText = "SELECT * FROM Equipment";
            using (SqlDataReader reader = Query.ExecuteReader())
            {
                while (reader.Read())
                {
                    Equipment.Add(new EquipmentView
                    {
                        EquipmentID = reader.GetString(0),
                        Game = reader.IsDBNull(1) || string.IsNullOrWhiteSpace(reader.GetString(1)) ? "Unknown" : reader.GetString(1),
                        Pit = reader.IsDBNull(2) || string.IsNullOrWhiteSpace(reader.GetString(2)) ? "Unknown" : reader.GetString(2),
                        MinMembership = reader.IsDBNull(3) || string.IsNullOrWhiteSpace(reader.GetString(3)) ? "No Membership" : reader.GetString(3),
                        MinPlayers = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                        MaxPlayers = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                        MinBet = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                        MaxBet = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                    });
                }
            }

            Connection.Close();
        }

        private void ListViewEquipment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = ListViewEquipment.SelectedItem is EquipmentView;

            EditButton.IsEnabled = hasSelection;
            DeleteButton.IsEnabled = hasSelection;
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewEquipment.SelectedItem is not EquipmentView equipment)
                return;

            string equipmentID = equipment.EquipmentID;

            var message = new TextBlock
            {
                Text = $"Are you sure you want to delete\nEquipment {equipmentID}?",
                FontSize = 18,
                TextWrapping = TextWrapping.Wrap
            };

            var dialog = UIHelper.CreateDialog($"Delete Equipment {equipmentID}", message, "Yes", "No", XamlRoot);

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            try
            {
                using var connection = DB.GetConnection();
                connection.Open();

                using var command = new SqlCommand(
                    "DELETE FROM Equipment WHERE Equipment_ID = @id",
                    connection
                );

                command.Parameters.Add("@id", SqlDbType.VarChar).Value = equipmentID;

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    Equipment.Remove(equipment);
                }
            }
            catch (SqlException)
            {
                await UIHelper.ShowErrorAsync(XamlRoot);
            }
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewEquipment.SelectedItem is not EquipmentView equipment)
                return;

            /* Dialog box inputs */
            var gameCombo = UIHelper.CreateGameComboBox(equipment.Game);
            var pitCombo = UIHelper.CreatePitComboBox(equipment.Pit);
            var membershipCombo = UIHelper.CreateMembershipComboBox(equipment.MinMembership);

            var minPlayersBox = new NumberBox
            {
                Header = "Min Players",
                Value = equipment.MinPlayers,
                Minimum = 0,
                Maximum = 30,
                SmallChange = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden
            };

            var maxPlayersBox = new NumberBox
            {
                Header = "Max Players",
                Value = equipment.MaxPlayers,
                Minimum = 0,
                Maximum = 30,
                SmallChange = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden
            };

            var minBetBox = new NumberBox
            {
                Header = "Min Bet",
                Value = equipment.MinBet,
                Minimum = 1,
                Maximum = 1000000,
                SmallChange = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden
            };

            var maxBetBox = new NumberBox
            {
                Header = "Max Bet",
                Value = equipment.MaxBet,
                Minimum = 1,
                Maximum = 1000000,
                SmallChange = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden
            };

            var panel = UIHelper.CreateFormPanel(gameCombo, pitCombo, membershipCombo, minPlayersBox, maxPlayersBox, minBetBox, maxBetBox);
            var dialog = UIHelper.CreateDialog("Edit Equipment", panel, "Save", "Close", XamlRoot);

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            try
            {
                using var connection = DB.GetConnection();
                connection.Open();

                using var command = new SqlCommand(
                    @"UPDATE Equipment
                      SET
                          Game = @game,
                          Pit = @pit,
                          Min_Membership = @minmember
                          Min_Players = @minplayer
                          Max_Players = @maxplayer
                          Min_Bet = @minbet
                          Max_Bet = @maxbet
                      WHERE Equipment_ID = @id", connection);

                command.Parameters.Add("@id", SqlDbType.VarChar).Value = equipment.EquipmentID;

                command.Parameters.Add("@game", SqlDbType.VarChar).Value = gameCombo.SelectedItem.ToString();
                command.Parameters.Add("@pit", SqlDbType.VarChar).Value = pitCombo.SelectedItem.ToString();
                command.Parameters.Add("@minmember", SqlDbType.VarChar).Value = membershipCombo.SelectedItem.ToString();
                command.Parameters.Add("@minplayer", SqlDbType.Int).Value = (int)minPlayersBox.Value;
                command.Parameters.Add("@maxplayer", SqlDbType.Int).Value = (int)maxPlayersBox.Value;
                command.Parameters.Add("@minbet", SqlDbType.Int).Value = (int)minBetBox.Value;
                command.Parameters.Add("@maxbet", SqlDbType.Int).Value = (int)maxBetBox.Value;

                command.ExecuteNonQuery();

                // Replace the edited item so the ListView refreshes.
                int index = Equipment.IndexOf(equipment);
                if (index >= 0)
                {
                    Equipment[index] = new EquipmentView
                    {
                        EquipmentID = equipment.EquipmentID,
                        Game = gameCombo.SelectedItem.ToString(),
                        Pit = pitCombo.SelectedItem.ToString(),
                        MinMembership = membershipCombo.SelectedItem.ToString(),
                        MinPlayers = (int)minPlayersBox.Value,
                        MaxPlayers = (int)maxPlayersBox.Value,
                        MinBet = (int)minBetBox.Value,
                        MaxBet = (int)maxBetBox.Value
                    };

                }
            }
            catch (SqlException ex)
            {
                await UIHelper.ShowErrorAsync(XamlRoot, ex.Message);
            }
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            /* Dialog box inputs */
            var idBox = new TextBox 
            { 
                Header = "Equipment ID", 
                Description = "This must be unique and cannot be changed." 
            };

            var gameCombo = UIHelper.CreateGameComboBox();
            var pitCombo = UIHelper.CreatePitComboBox();
            var membershipCombo = UIHelper.CreateMembershipComboBox();

            var minPlayersBox = new NumberBox
            {
                Header = "Min Players",
                Value = 1,
                Minimum = 1,
                Maximum = 30,
                SmallChange = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden
            };

            var maxPlayersBox = new NumberBox
            {
                Header = "Max Players",
                Value = 1,
                Minimum = 1,
                Maximum = 30,
                SmallChange = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden
            };

            var minBetBox = new NumberBox
            {
                Header = "Min Bet",
                Value = 1,
                Minimum = 1,
                Maximum = 1000000,
                SmallChange = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden
            };

            var maxBetBox = new NumberBox
            {
                Header = "Max Bet",
                Value = 1,
                Minimum = 1,
                Maximum = 1000000,
                SmallChange = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden
            };

            var panel = UIHelper.CreateFormPanel(idBox, gameCombo, pitCombo, membershipCombo, minPlayersBox, maxPlayersBox, minBetBox, maxBetBox);
            var dialog = UIHelper.CreateDialog("Edit Equipment", panel, "Save", "Close", XamlRoot);

            dialog.PrimaryButtonClick += (s, args) =>
            {
                bool missingRequiredField =
                    // Equipment ID
                    string.IsNullOrWhiteSpace(idBox.Text) ||

                    // ComboBoxes
                    gameCombo.SelectedItem == null ||
                    pitCombo.SelectedItem == null ||
                    membershipCombo.SelectedItem == null ||

                    // Invalid or empty numbers
                    double.IsNaN(minPlayersBox.Value) ||
                    double.IsNaN(maxPlayersBox.Value) ||
                    double.IsNaN(minBetBox.Value) ||
                    double.IsNaN(maxBetBox.Value) ||

                    // Negative or zero values
                    minPlayersBox.Value < 1 ||
                    maxPlayersBox.Value < 1 ||
                    minBetBox.Value < 0 ||
                    maxBetBox.Value < 0 ||

                    // Minimum cannot exceed maximum
                    minPlayersBox.Value > maxPlayersBox.Value ||
                    minBetBox.Value > maxBetBox.Value ||

                    // Players must be whole numbers
                    minPlayersBox.Value % 1 != 0 ||
                    maxPlayersBox.Value % 1 != 0 ||

                    // Reject infinite values
                    double.IsInfinity(minPlayersBox.Value) ||
                    double.IsInfinity(maxPlayersBox.Value) ||
                    double.IsInfinity(minBetBox.Value) ||
                    double.IsInfinity(maxBetBox.Value);

                args.Cancel = missingRequiredField;
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            try
            {
                using var connection = DB.GetConnection();
                connection.Open();

                using var command = new SqlCommand(
                    @"INSERT INTO Equipment
                    (
                        Equipment_ID,
                        Game,
                        Pit,
                        Min_Membership,
                        Min_Players,
                        Max_Players,
                        Min_Bet,
                        Max_Bet
                    )
                    VALUES
                    (
                        @id,
                        @game,
                        @pit,
                        @minmember,
                        @minplayer,
                        @maxplayer,
                        @minbet,
                        @maxbet
                    )", connection);

                command.Parameters.Add("@id", SqlDbType.VarChar).Value = idBox.Text;

                command.Parameters.Add("@game", SqlDbType.VarChar).Value = gameCombo.SelectedItem.ToString();
                command.Parameters.Add("@pit", SqlDbType.VarChar).Value = pitCombo.SelectedItem.ToString();
                command.Parameters.Add("@minmember", SqlDbType.VarChar).Value = membershipCombo.SelectedItem.ToString();
                command.Parameters.Add("@minplayer", SqlDbType.Int).Value = (int)minPlayersBox.Value;
                command.Parameters.Add("@maxplayer", SqlDbType.Int).Value = (int)maxPlayersBox.Value;
                command.Parameters.Add("@minbet", SqlDbType.Int).Value = (int)minBetBox.Value;
                command.Parameters.Add("@maxbet", SqlDbType.Int).Value = (int)maxBetBox.Value;

                command.ExecuteNonQuery();

                Equipment.Add(new EquipmentView
                {
                    EquipmentID = idBox.Text,
                    Game = gameCombo.SelectedItem.ToString(),
                    Pit = pitCombo.SelectedItem.ToString(),
                    MinMembership = membershipCombo.SelectedItem.ToString(),
                    MinPlayers = (int)minPlayersBox.Value,
                    MaxPlayers = (int)maxPlayersBox.Value,
                    MinBet = (int)minBetBox.Value,
                    MaxBet = (int)maxBetBox.Value
                });
            }
            catch (SqlException ex)
            {
                await UIHelper.ShowErrorAsync(XamlRoot, ex.Message);
            }
        }
    }

    public class EquipmentView
    {
        public string EquipmentID { get; set; } = "";
        public string? Game { get; set; } = "";
        public string? Pit { get; set; } = "";
        public string? MinMembership { get; set; } = "";
        public int MinPlayers { get; set; } = 0;
        public int MaxPlayers { get; set; } = 0;
        public int MinBet { get; set; } = 0;
        public int MaxBet { get; set; } = 0;
    }
}
