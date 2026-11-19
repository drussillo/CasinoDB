using Microsoft.Data.SqlClient;
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json.Serialization;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CasinoApp.Pages
{

    public sealed partial class MembershipPage : Page
    {
        public ObservableCollection<MembershipView> Memberships { get; } = new();
        public MembershipPage()
        {
            InitializeComponent();
            LoadMemberships();
        }

        private void LoadMemberships()
        {
            var Connection = DB.GetConnection();
            Connection.Open();

            var Query = Connection.CreateCommand();
            Query.CommandText = "SELECT * FROM Membership";

            using (SqlDataReader reader = Query.ExecuteReader())
            {
                while (reader.Read())
                {
                    Memberships.Add(new MembershipView
                    {
                        Name = reader.GetString(0),
                        Tier = reader.GetInt32(1),
                        Cost = (decimal)reader.GetDecimal(2),
                        Description = reader.GetString(3)
                    });
                }
            }

            Connection.Close();
        }

        private void ListViewMemberships_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = ListViewMemberships.SelectedItem is MembershipView;

            EditButton.IsEnabled = hasSelection;
            DeleteButton.IsEnabled = hasSelection;
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewMemberships.SelectedItem is not MembershipView membership)
                return;

            var message = new TextBlock
            {
                Text = $"Are you sure you want to delete\n${membership.Name}?",
                FontSize = 18,
                TextWrapping = TextWrapping.Wrap
            };

            var dialog = UIHelper.CreateDialog($"Delete {membership.Name}", message, "Yes", "No", XamlRoot);
            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            try
            {
                using var connection = DB.GetConnection();
                connection.Open();

                using var command = new SqlCommand(
                    "DELETE FROM Membership " +
                    "WHERE Name = @name",
                    connection);

                command.Parameters.AddWithValue("@name ", membership.Name);

                command.ExecuteNonQuery();
                Memberships.Remove(membership);
            }
            catch (SqlException)
            {
                await UIHelper.ShowErrorAsync(XamlRoot);
            }
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewMemberships.SelectedItem is not MembershipView membership)
                return;

            /* Dialog box inputs */
            // var nameBox = new TextBox { Header = "Name", Text = membership.Name }; DONT WANT TO CHANGE THIS IS PRIMARY KEY

            var tierBox = new NumberBox
            {
                Header = "Tier",
                Value = (int)membership.Tier,
                Minimum = 0,
                Maximum = 1000000,
                SmallChange = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden
            };

            var costBox = new NumberBox
            {
                Header = "Cost",
                Value = (double)membership.Cost,
                Minimum = 0,
                Maximum = 1000000,
                SmallChange = 0.01,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden
            };

            var descriptionBox = new TextBox { Header = "Description", Text = membership.Description };

            var panel = UIHelper.CreateFormPanel(tierBox, costBox, descriptionBox);
            var dialog = UIHelper.CreateDialog("Edit a Membership", panel, "Save", "Cancel", XamlRoot);

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            try
            {
                using var connection = DB.GetConnection();
                connection.Open();

                using var command = new SqlCommand(
                    @"UPDATE Membership
                      SET Tier = @tier,
                          Cost = @cost,
                          Description = @description
                      WHERE Name = @name", connection);


                command.Parameters.Add("@name", SqlDbType.NChar).Value = membership.Name;

                command.Parameters.Add("@tier", SqlDbType.Int).Value = membership.Tier;
                command.Parameters.Add("@cost", SqlDbType.Decimal).Value = membership.Cost;
                command.Parameters.Add("@description", SqlDbType.NChar).Value = membership.Description;

                command.ExecuteNonQuery();

                // Replace the edited item so the ListView refreshes.
                int index = Memberships.IndexOf(membership);
                if (index >= 0)
                {
                    Memberships[index] = new MembershipView
                    {
                        Name = membership.Name,
                        Tier = (int)tierBox.Value,
                        Cost = (decimal)costBox.Value,
                        Description = descriptionBox.Text
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
            var nameBox = new TextBox { Header = "Name", Description = "This must be unique and cannot be changed." }; 

            var tierBox = new NumberBox
            {
                Header = "Tier",
                Value = 0,
                Minimum = 0,
                Maximum = 1000000,
                SmallChange = 1,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden
            };

            var costBox = new NumberBox
            {
                Header = "Cost",
                Value = 0,
                Minimum = 0,
                Maximum = 1000000,
                SmallChange = 0.01,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden
            };

            var descriptionBox = new TextBox { Header = "Description" };

            var panel = UIHelper.CreateFormPanel(nameBox, tierBox, costBox, descriptionBox);
            var dialog = UIHelper.CreateDialog("Add a Membership", panel, "Add", "Cancel", XamlRoot);

            // Do not close the dialog when required fields are missing.
            dialog.PrimaryButtonClick += (s, args) =>
            {
                bool missingRequiredField =
                    string.IsNullOrWhiteSpace(nameBox.Text) ||
                    costBox.Value <= 0 ||
                    tierBox.Value < 0;

                args.Cancel = missingRequiredField;
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;


            try
            {
                using var connection = DB.GetConnection();
                await connection.OpenAsync();

                using var command = new SqlCommand(
                    @"INSERT INTO Membership
                          (Name, Tier, Cost, Description)
                      VALUES
                          (@name, @tier, @cost, @description)",
                    connection);

                command.Parameters.Add("@name", SqlDbType.NChar).Value = nameBox.Text;

                command.Parameters.Add("@tier", SqlDbType.Int).Value = (int)tierBox.Value;
                command.Parameters.Add("@cost", SqlDbType.Decimal).Value = (decimal)costBox.Value;
                command.Parameters.Add("@description", SqlDbType.NChar).Value = descriptionBox.Text;

                command.ExecuteNonQuery();

                Memberships.Add(new MembershipView
                {
                    Name = nameBox.Text,
                    Tier = (int)tierBox.Value,
                    Cost = (decimal)costBox.Value,
                    Description = descriptionBox.Text
                });
            }
            catch (SqlException ex)
            {
                await UIHelper.ShowErrorAsync(XamlRoot, ex.Message);
            }
        }
    }

    

    public class MembershipView
    {
        public string Name { get; set; } = "";
        public int Tier { get; set; } = 0;
        public decimal Cost { get; set; } = 0;
        public string Description { get; set; } = "";
    }
}