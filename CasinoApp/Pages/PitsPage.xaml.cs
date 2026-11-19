using System;
using System.Collections.ObjectModel;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CasinoApp.Pages
{
    public sealed partial class PitsPage : Page
    {
        // Keep the collection in the page, just like MachinePage.
        ObservableCollection<PitView> Pits = new ObservableCollection<PitView>();

        public PitsPage()
        {
            InitializeComponent();
            LoadPits();
        }

        private void LoadPits()
        {
            using var connection = DB.GetConnection();
            connection.Open();

            // The Employee join shows the pit boss's name instead of only an SSN.
            using var command = new SqlCommand(
                @"SELECT p.Name, p.Pit_Boss, p.Floor, e.F_Name, e.L_Name
                  FROM Pit AS p
                  LEFT JOIN Employee AS e ON p.Pit_Boss = e.SSN", connection);

            using SqlDataReader reader = command.ExecuteReader();
            Pits.Clear();

            while (reader.Read())
            {
                string? bossSSN = reader.IsDBNull(1) ? null : reader.GetValue(1).ToString();
                string bossName = reader.IsDBNull(3) || reader.IsDBNull(4)
                    ? "Unassigned"
                    : $"{reader.GetString(3)} {reader.GetString(4)}";

                Pits.Add(new PitView
                {
                    Name = reader.GetString(0),
                    PitBoss = bossSSN,
                    PitBossName = bossName,
                    Floor = reader.IsDBNull(2) ? "" : reader.GetValue(2).ToString() ?? ""
                });
            }
        }

        private void ListViewPits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = ListViewPits.SelectedItem is PitView;

            EditButton.IsEnabled = hasSelection;
            DeleteButton.IsEnabled = hasSelection;
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewPits.SelectedItem is not PitView pit)
                return;

            var message = new TextBlock
            {
                Text = $"Are you sure you want to delete\nPit {pit.Name}?",
                FontSize = 18,
                TextWrapping = TextWrapping.Wrap
            };

            var dialog = UIHelper.CreateDialog($"Delete Pit {pit.Name}", message, "Yes", "No", XamlRoot);
            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            try
            {
                using var connection = DB.GetConnection();
                connection.Open();

                using var command = new SqlCommand(
                    "DELETE FROM Pit WHERE Name = @name", connection);

                command.Parameters.Add("@name", SqlDbType.VarChar).Value = pit.Name;

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    Pits.Remove(pit);
                }
            }
            catch (SqlException ex)
            {
                await UIHelper.ShowErrorAsync(XamlRoot, ex.Message);
            }
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewPits.SelectedItem is not PitView pit)
                return;

            /* Dialog box inputs */
            var bossCombo = UIHelper.CreatePitBossComboBox(pit.PitBoss);
            var floorBox = new TextBox
            {
                Header = "Floor",
                Text = pit.Floor
            };

            var panel = UIHelper.CreateFormPanel(bossCombo, floorBox);
            var dialog = UIHelper.CreateDialog($"Edit Pit {pit.Name}", panel, "Save", "Close", XamlRoot);

            dialog.PrimaryButtonClick += (s, args) =>
            {
                bool missingRequiredField =
                    bossCombo.SelectedItem is not EmployeeOption ||
                    string.IsNullOrWhiteSpace(floorBox.Text);

                args.Cancel = missingRequiredField;
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            var selectedBoss = (EmployeeOption)bossCombo.SelectedItem;

            try
            {
                using var connection = DB.GetConnection();
                connection.Open();

                using var command = new SqlCommand(
                    @"UPDATE Pit
                      SET Pit_Boss = @boss,
                          Floor = @floor
                      WHERE Name = @name", connection);

                command.Parameters.Add("@name", SqlDbType.VarChar).Value = pit.Name;
                command.Parameters.Add("@boss", SqlDbType.VarChar).Value = selectedBoss.SSN;
                command.Parameters.Add("@floor", SqlDbType.VarChar).Value = floorBox.Text.Trim();

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    // Replace the edited item so the ListView refreshes.
                    int index = Pits.IndexOf(pit);
                    if (index >= 0)
                    {
                        Pits[index] = new PitView
                        {
                            Name = pit.Name,
                            PitBoss = selectedBoss.SSN,
                            PitBossName = selectedBoss.Name,
                            Floor = floorBox.Text.Trim()
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
            /* Dialog box inputs */
            var nameBox = new TextBox
            {
                Header = "Pit Name",
                Description = "This must be unique and cannot be changed."
            };
            var bossCombo = UIHelper.CreatePitBossComboBox();
            var floorBox = new TextBox
            {
                Header = "Floor"
            };

            var panel = UIHelper.CreateFormPanel(nameBox, bossCombo, floorBox);
            var dialog = UIHelper.CreateDialog("Add Pit", panel, "Save", "Close", XamlRoot);

            dialog.PrimaryButtonClick += (s, args) =>
            {
                bool missingRequiredField =
                    string.IsNullOrWhiteSpace(nameBox.Text) ||
                    bossCombo.SelectedItem is not EmployeeOption ||
                    string.IsNullOrWhiteSpace(floorBox.Text);

                args.Cancel = missingRequiredField;
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            var selectedBoss = (EmployeeOption)bossCombo.SelectedItem;

            try
            {
                using var connection = DB.GetConnection();
                connection.Open();

                using var command = new SqlCommand(
                    @"INSERT INTO Pit (Name, Pit_Boss, Floor)
                      VALUES (@name, @boss, @floor)", connection);

                command.Parameters.Add("@name", SqlDbType.VarChar).Value = nameBox.Text.Trim();
                command.Parameters.Add("@boss", SqlDbType.VarChar).Value = selectedBoss.SSN;
                command.Parameters.Add("@floor", SqlDbType.VarChar).Value = floorBox.Text.Trim();

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    Pits.Add(new PitView
                    {
                        Name = nameBox.Text.Trim(),
                        PitBoss = selectedBoss.SSN,
                        PitBossName = selectedBoss.Name,
                        Floor = floorBox.Text.Trim()
                    });
                }
            }
            catch (SqlException ex)
            {
                await UIHelper.ShowErrorAsync(XamlRoot, ex.Message);
            }
        }

        // Displays an employee name, but saves the matching SSN as the foreign key.
        

        
    }

   

    public class PitView
    {
        public string Name { get; set; } = "";
        public string? PitBoss { get; set; } = null;
        public string PitBossName { get; set; } = "Unassigned";
        public string Floor { get; set; } = "";
    }
}
