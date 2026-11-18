using Microsoft.Data.SqlClient;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;

namespace CasinoApp.Pages
{
    public sealed partial class GamblersPage : Page
    {
        public ObservableCollection<GamblersView> Gamblers { get; } = new();

        public GamblersPage()
        {
            InitializeComponent();
            LoadGamblers();
        }

        // Load all gamblers when the page opens.
        private void LoadGamblers()
        {
            using var connection = DB.GetConnection();
            connection.Open();

            using var command = new SqlCommand(
                @"SELECT Gambler_ID, Membership, F_Name, L_Name, ID_Type,
                         Date_Joined, Account_Status, Notes
                  FROM Gambler", connection);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                Gamblers.Add(new GamblersView
                {
                    GamblerID = reader.GetString(0),
                    Membership = reader.IsDBNull(1) || string.IsNullOrWhiteSpace(reader.GetString(1)) ? "No Membership" : reader.GetString(1), /* Since this is a foreign we need to check if null */
                    FirstName = reader.GetString(2),
                    LastName = reader.GetString(3),
                    IDType = reader.GetString(4),
                    DateJoined = reader.IsDBNull(5)
                        ? "No Date"
                        : reader.GetDateTime(5).ToString("MM-dd-yyyy"),
                    AccountStatus = reader.GetString(6),
                    Notes = reader.IsDBNull(7) ? "" : reader.GetString(7)
                });
            }
        }

  
        private void ListViewGamblers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = ListViewGamblers.SelectedItem is GamblersView;

            EditButton.IsEnabled = hasSelection;
            DeleteButton.IsEnabled = hasSelection;
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewGamblers.SelectedItem is not GamblersView gambler)
                return;

            string fullName = $"{gambler.LastName} {gambler.FirstName}";
            var message = new TextBlock
            {
                Text = $"Are you sure you want to delete\n{fullName}?",
                FontSize = 18,
                TextWrapping = TextWrapping.Wrap
            };

            var dialog = UIHelper.CreateDialog($"Delete {fullName}", message, "Yes", "No", XamlRoot);
            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            try
            {
                using var connection = DB.GetConnection();
                connection.Open();

                using var command = new SqlCommand(
                    "DELETE FROM Gambler WHERE Gambler_ID = @gamblerId", connection);
                command.Parameters.AddWithValue("@gamblerId", gambler.GamblerID);

                command.ExecuteNonQuery();
                Gamblers.Remove(gambler);
            }
            catch (SqlException)
            {
                await UIHelper.ShowErrorAsync(XamlRoot);
            }
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewGamblers.SelectedItem is not GamblersView gambler)
                return;

            // Fill the form with the gambler's current values.
            var firstNameBox = new TextBox { Header = "First Name", Text = gambler.FirstName };
            var lastNameBox = new TextBox { Header = "Last Name", Text = gambler.LastName };
            var idTypeBox = new TextBox { Header = "ID Type", Text = gambler.IDType };
            var membershipCombo = UIHelper.CreateMembershipComboBox(gambler.Membership);
            var datePicker = new DatePicker { Header = "Date Joined" };
            var statusBox = new TextBox { Header = "Account Status", Text = gambler.AccountStatus };
            var notesBox = new TextBox { Header = "Notes", Text = gambler.Notes };

            if (DateTime.TryParse(gambler.DateJoined, out var existingDate))
            {
                datePicker.Date = new DateTimeOffset(existingDate);
            }

            var panel = UIHelper.CreateFormPanel(firstNameBox, lastNameBox, idTypeBox, membershipCombo, datePicker, statusBox, notesBox);

            var dialog = UIHelper.CreateDialog("Edit a Gambler Account", panel, "Save", "Cancel", XamlRoot);
            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            string membership = membershipCombo.SelectedItem?.ToString() ?? gambler.Membership;
            string dateText = datePicker.SelectedDate?.ToString("MM-dd-yyyy") ?? "No Date";

            try
            {
                using var connection = DB.GetConnection();
                connection.Open();

                using var command = new SqlCommand(
                    @"UPDATE Gambler
                      SET Membership = @membership,
                          F_Name = @firstName,
                          L_Name = @lastName,
                          ID_Type = @idType,
                          Date_Joined = @dateJoined,
                          Account_Status = @status,
                          Notes = @notes
                      WHERE Gambler_ID = @gamblerId", connection);

                command.Parameters.AddWithValue("@membership", membership);
                command.Parameters.AddWithValue("@firstName", firstNameBox.Text);
                command.Parameters.AddWithValue("@lastName", lastNameBox.Text);
                command.Parameters.AddWithValue("@idType", idTypeBox.Text);
                command.Parameters.Add("@dateJoined", SqlDbType.Date).Value =
                    datePicker.SelectedDate.HasValue
                        ? datePicker.SelectedDate.Value.Date
                        : DBNull.Value;
                command.Parameters.AddWithValue("@status", statusBox.Text);
                command.Parameters.Add("@notes", SqlDbType.NVarChar).Value =
                    string.IsNullOrWhiteSpace(notesBox.Text)
                        ? DBNull.Value
                        : notesBox.Text;
                command.Parameters.AddWithValue("@gamblerId", gambler.GamblerID);

                command.ExecuteNonQuery();

                // Replace the edited item so the ListView refreshes.
                int index = Gamblers.IndexOf(gambler);
                if (index >= 0)
                {
                    Gamblers[index] = new GamblersView
                    {
                        GamblerID = gambler.GamblerID,
                        Membership = membership,
                        FirstName = firstNameBox.Text,
                        LastName = lastNameBox.Text,
                        IDType = idTypeBox.Text,
                        DateJoined = dateText,
                        AccountStatus = statusBox.Text,
                        Notes = notesBox.Text
                    };
                }
            }
            catch (SqlException)
            {
                await UIHelper.ShowErrorAsync(XamlRoot);
            }
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            // Gambler IDs are entered manually and must be unique.
            var gamblerIdBox = new TextBox
            {
                Header = "Member ID",
                Description = "This must be unique and cannot be changed."
            };

            var firstNameBox = new TextBox { Header = "First Name" };
            var lastNameBox = new TextBox { Header = "Last Name" };
            var idTypeBox = new TextBox { Header = "ID Type" };
            var membershipCombo = UIHelper.CreateMembershipComboBox();
            var datePicker = new DatePicker { Header = "Date Joined" };
            var statusBox = new TextBox { Header = "Account Status" };
            var notesBox = new TextBox { Header = "Notes" };

            var panel = UIHelper.CreateFormPanel(gamblerIdBox, firstNameBox, lastNameBox, idTypeBox, membershipCombo, datePicker, statusBox, notesBox);
            var dialog = UIHelper.CreateDialog("Add a Gambler Account", panel, "Add", "Cancel", XamlRoot);

            // Do not close the dialog when required fields are missing.
            dialog.PrimaryButtonClick += (s, args) =>
            {
                bool missingRequiredField =
                    string.IsNullOrWhiteSpace(gamblerIdBox.Text) ||
                    string.IsNullOrWhiteSpace(firstNameBox.Text) ||
                    string.IsNullOrWhiteSpace(lastNameBox.Text) ||
                    string.IsNullOrWhiteSpace(idTypeBox.Text) ||
                    string.IsNullOrWhiteSpace(statusBox.Text) ||
                    membershipCombo.SelectedItem == null ||
                    !datePicker.SelectedDate.HasValue;

                args.Cancel = missingRequiredField;
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            string membership = membershipCombo.SelectedItem!.ToString()!;

            try
            {
                using var connection = DB.GetConnection();
                await connection.OpenAsync();

                using var command = new SqlCommand(
                    @"INSERT INTO Gambler
                          (Gambler_ID, Membership, F_Name, L_Name, ID_Type,
                           Date_Joined, Account_Status, Notes)
                      VALUES
                          (@gamblerId, @membership, @firstName, @lastName, @idType,
                           @dateJoined, @status, @notes)", connection);

                command.Parameters.Add("@gamblerId", SqlDbType.NVarChar).Value = gamblerIdBox.Text;
                command.Parameters.Add("@membership", SqlDbType.NVarChar).Value = membership;
                command.Parameters.Add("@firstName", SqlDbType.NVarChar).Value = firstNameBox.Text;
                command.Parameters.Add("@lastName", SqlDbType.NVarChar).Value = lastNameBox.Text;
                command.Parameters.Add("@idType", SqlDbType.NVarChar).Value = idTypeBox.Text;
                command.Parameters.Add("@dateJoined", SqlDbType.Date).Value = datePicker.SelectedDate!.Value.Date;
                command.Parameters.Add("@status", SqlDbType.NVarChar).Value = statusBox.Text;
                command.Parameters.Add("@notes", SqlDbType.NVarChar).Value =
                    string.IsNullOrWhiteSpace(notesBox.Text)
                        ? DBNull.Value
                        : notesBox.Text;

                command.ExecuteNonQuery();

                Gamblers.Add(new GamblersView
                {
                    GamblerID = gamblerIdBox.Text,
                    Membership = membership,
                    FirstName = firstNameBox.Text,
                    LastName = lastNameBox.Text,
                    IDType = idTypeBox.Text,
                    DateJoined = datePicker.SelectedDate?.ToString("MM-dd-yyyy") ?? "No Date",
                    AccountStatus = statusBox.Text,
                    Notes = notesBox.Text
                });
            }
            catch (SqlException)
            {
                await UIHelper.ShowErrorAsync(XamlRoot);
            }
        }
    }

    public class GamblersView
    {
        public string GamblerID { get; set; } = "";
        public string Membership { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string IDType { get; set; } = "";
        public string DateJoined { get; set; } = "";
        public string AccountStatus { get; set; } = "";
        public string Notes { get; set; } = "";
    }
}
