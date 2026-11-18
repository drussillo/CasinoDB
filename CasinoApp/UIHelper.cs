using Microsoft.Data.SqlClient;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;

// This was added to prevent code duplication when designing UI elements

namespace CasinoApp
{
    public static class UIHelper
    {
        /* Used for when you want to create a stack panel when creating a dialog box */
        public static StackPanel CreateFormPanel(params UIElement[] controls)
        {
            var panel = new StackPanel { Spacing = 10 };

            foreach (var control in controls)
            {
                panel.Children.Add(control);
            }

            return panel;
        }

        public static ContentDialog CreateDialog(string title, object content, string primaryButton, string closeButton, XamlRoot root)
        {
            return new ContentDialog
            {
                Title = title,

                Content = new ScrollViewer
                {
                    Content = new Border
                    {
                        Child = content as UIElement,
                        Padding = new Thickness(0, 0, 20, 0)
                    },
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollMode = ScrollMode.Enabled,
                    MaxHeight = 500
                },

                PrimaryButtonText = primaryButton,
                CloseButtonText = closeButton,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = root
            };
        }

        public static async Task ShowErrorAsync(XamlRoot root, string errmessage = "None Provided")
        {
            var message = new TextBlock
            {
                Text = "The database operation failed. Check the database restrictions and try again.\nAdditonal Information:\n" + errmessage,
                TextWrapping = TextWrapping.Wrap
            };

            var dialog = UIHelper.CreateDialog("Database Error", message, "Close", "", root);
            await dialog.ShowAsync();
        }

        /* Combo boxes */
        public static ComboBox CreateMembershipComboBox(string? currentMembership = null)
        {
            var comboBox = new ComboBox
            {
                Header = "Membership Type",
                Description = "This is a foreign key.",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            using var connection = DB.GetConnection();
            connection.Open();

            using var command = new SqlCommand("SELECT NAME FROM MEMBERSHIP", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                comboBox.Items.Add(reader.GetString(0));
            }

            // Keep the existing membership selected when editing.
            if (currentMembership != null)
            {
                comboBox.SelectedItem = currentMembership;
            }

            return comboBox;
        }

        public static ComboBox CreatePitComboBox(string? currentPit = null)
        {
            var comboBox = new ComboBox
            {
                Header = "Pit",
                Description = "This is a foreign key.",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            using var connection = DB.GetConnection();
            connection.Open();

            using var command = new SqlCommand("SELECT NAME FROM PIT", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                comboBox.Items.Add(reader.GetString(0));
            }

            // Keep the existing membership selected when editing.
            if (currentPit != null)
            {
                comboBox.SelectedItem = currentPit;
            }

            return comboBox;
        }

        public static ComboBox CreateGameComboBox(string? currentGame = null)
        {
            var comboBox = new ComboBox
            {
                Header = "Game",
                Description = "This is a foreign key.",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            using var connection = DB.GetConnection();
            connection.Open();

            using var command = new SqlCommand("SELECT NAME FROM Game", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                comboBox.Items.Add(reader.GetString(0));
            }

            // Keep the existing membership selected when editing.
            if (currentGame != null)
            {
                comboBox.SelectedItem = currentGame;
            }

            return comboBox;
        }

        public static ComboBox CreatePitBossComboBox(string? selectedSSN = null)
        {
            var combo = new ComboBox
            {
                Header = "Pit Boss",
                PlaceholderText = "Select an employee",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                DisplayMemberPath = "Name"
            };

            using var connection = DB.GetConnection();
            connection.Open();

            using var command = new SqlCommand(
                "SELECT SSN, F_Name, L_Name FROM Employee ORDER BY L_Name, F_Name", connection);

            using SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                var employee = new EmployeeOption
                {
                    SSN = reader.GetString(0),
                    Name = $"{reader.GetString(1)} {reader.GetString(2)}"
                };

                combo.Items.Add(employee);
                if (employee.SSN == selectedSSN)
                    combo.SelectedItem = employee;
            }

            return combo;
        }

        public static ComboBox CreateGamblerComboBox(string? selectedGamblerID = null)
        {
            var combo = new ComboBox
            {
                Header = "Gambler",
                PlaceholderText = "Select a gambler",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            using var connection = DB.GetConnection();
            connection.Open();

            using var command = new SqlCommand(
                @"SELECT Gambler_ID, F_Name, L_Name
          FROM GAMBLER
          ORDER BY L_Name, F_Name", connection);

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                string gamblerID = reader.GetString(0);
                string firstName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                string lastName = reader.IsDBNull(2) ? "" : reader.GetString(2);

                var item = new ComboBoxItem
                {
                    Content = $"{firstName} {lastName} ({gamblerID})".Trim(),
                    Tag = gamblerID
                };

                combo.Items.Add(item);

                if (gamblerID == selectedGamblerID)
                {
                    combo.SelectedItem = item;
                }
            }

            return combo;
        }
    }
    public class EmployeeOption
    {
        public string SSN { get; set; } = "";
        public string Name { get; set; } = "";
    }
}
