using Microsoft.Data.SqlClient;
using Microsoft.UI.Text;
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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Gaming.Preview.GamesEnumeration;
using Windows.UI.StartScreen;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CasinoApp.Pages
{
    public sealed partial class GamesPage : Page
    {
        public ObservableCollection<CasinoGameView> Games { get; } = new();
        public GamesPage()
        { 
            InitializeComponent();

            var Connection = DB.GetConnection();
            Connection.Open();

            var Query = Connection.CreateCommand();
            Query.CommandText = "SELECT * FROM Game";

            using (SqlDataReader reader = Query.ExecuteReader())
            {
                while (reader.Read())
                {
                    Games.Add(new CasinoGameView
                    {
                        Name = reader.GetString(0),
                        Type = reader.GetString(1),
                        Description = reader.GetString(2)
                    });
                }
            }

            Connection.Close();

        }

        private void ListViewGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListViewGames.SelectedItem is CasinoGameView game)
            {
                EditButton.IsEnabled = true;
                DeleteButton.IsEnabled = true;
            }
            else
            {
                EditButton.IsEnabled = false;
                DeleteButton.IsEnabled = false;
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {

            // TODO:
            // 1. DELETE FROM Games WHERE Id = $id  (database)
            // 2. Games.Remove(game);                (screen)

            if (ListViewGames.SelectedItem is not CasinoGameView game) return;

            var textLabel = new TextBlock
            {
                Text = "Are you sure you want to delete " + game.Name,
                FontSize = 18,
                TextWrapping = TextWrapping.Wrap
            };

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(textLabel);

            var dialog = new ContentDialog
            {
                Title = "Delete " + game.Name,
                Content = panel,
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // DO delete operation
                using (SqlConnection conn = DB.GetConnection())
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(
                        @"DELETE FROM Game
                          WHERE Name = @name AND Type = @type"
                        , conn);

                    cmd.Parameters.AddWithValue("@name", game.Name);
                    cmd.Parameters.AddWithValue("@type", game.Type);

                    cmd.ExecuteNonQuery();
                }

                // Update the screen
                int index = Games.IndexOf(game);
                Games.RemoveAt(index);
            }
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewGames.SelectedItem is not CasinoGameView game) return;

            var nameBox = new TextBox { Header = "Name", Text = game.Name };
            var typeBox = new TextBox { Header = "Type", Text = game.Type };
            var descBox = new TextBox { Header = "Description", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Text = game.Description };


            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(nameBox);
            panel.Children.Add(typeBox);
            panel.Children.Add(descBox);

            var dialog = new ContentDialog
            {
                Title = "Edit A Casino Game",
                Content = panel,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // update that hoe
                using (SqlConnection conn = DB.GetConnection())
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(
                    @"UPDATE Game
                        SET Name = @newName, Type = @newType, Description = @newDesc
                        WHERE Name = @oldName AND Type = @oldType",
                    conn);

                    cmd.Parameters.AddWithValue("@newName", nameBox.Text);
                    cmd.Parameters.AddWithValue("@newType", typeBox.Text);
                    cmd.Parameters.AddWithValue("@newDesc", descBox.Text);

                    cmd.Parameters.AddWithValue("@oldName", game.Name);
                    cmd.Parameters.AddWithValue("@oldType", game.Type);

                    cmd.ExecuteNonQuery();
                }

                // Update the screen
                int index = Games.IndexOf(game);
                Games[index] = new CasinoGameView
                {
                    Name = nameBox.Text,
                    Type = typeBox.Text,
                    Description = descBox.Text
                };
            }
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            // open the same dialog, but empty, then INSERT on save
            // this shii doesnt work yet...
            var nameBox = new TextBox { Header = "Name" };
            var typeBox = new TextBox { Header = "Type" };
            var descBox = new TextBox { Header = "Description", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(nameBox);
            panel.Children.Add(typeBox);
            panel.Children.Add(descBox);

            var dialog = new ContentDialog
            {
                Title = "Add A Casino Game",
                Content = panel,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot   
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Save pressed: read nameBox.Text, typeBox.Text, descBox.Text
                using (SqlConnection conn = DB.GetConnection())
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(
                        "INSERT INTO Game (Name, Type, Description) VALUES (@name, @type, @desc)",
                        conn);

                    cmd.Parameters.AddWithValue("@name", nameBox.Text);
                    cmd.Parameters.AddWithValue("@type", typeBox.Text);
                    cmd.Parameters.AddWithValue("@desc", descBox.Text);

                    cmd.ExecuteNonQuery();
                }

                // Update the screen
                Games.Add(new CasinoGameView
                {
                    Name = nameBox.Text,
                    Type = typeBox.Text,
                    Description = descBox.Text
                });
            }
        }

    }

    public class CasinoGameView
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";   // Card, or Slots
        public string Description { get; set; } = "";
    }


}
