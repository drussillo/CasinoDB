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
using System.Threading.Tasks;
using System.Windows;
using System.Data;



namespace CasinoApp.Pages
{
    public sealed partial class SearchPage : Page
    {
     
        public ObservableCollection<TableView> Tables { get; } = new();
        //public ObservableCollection<ColumnView> Columns { get; } = new();

       

        public SearchPage()
        {
            InitializeComponent();
            Loaded += MyPage_Loaded;
        }

        private async void MyPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadTables();
        }

        private async Task LoadTables()
        {
            Console.WriteLine("Loading Tables");
            var connection = DB.GetConnection();
            await connection.OpenAsync();

            using var command = new SqlCommand(
                @"SELECT 
                     TABLE_NAME
                  FROM
                     information_schema.tables
                  WHERE
                     TABLE_NAME != 'sysdiagrams'", connection);

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    Tables.Add(new TableView
                    {
                        TableName = reader.GetString(0)
                    });
                }
            }

            connection.Close();

        }

        private async void TableBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ColumnBox.ItemsSource = null;
            ColumnBox.IsEnabled = false;

            // Check if a valid category is selected
            if (TableBox.SelectedItem is TableView selectedTable)
            {
                await LoadColumn(selectedTable.TableName);
            }
        }

        private async Task LoadColumn(string Table)
        {
            var columns = new List<ColumnView>();

            Console.WriteLine("Loading Column");

            if (Table == null) return;

            using var connection = DB.GetConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand(
                @"SELECT COLUMN_NAME
                  FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_NAME = @tablename",
                connection);
            command.Parameters.AddWithValue("@tablename", Table);

            using (var reader2 = await command.ExecuteReaderAsync())
            {
                while (await reader2.ReadAsync())
                {
                    columns.Add(new ColumnView
                    {
                        ColumnName = reader2.GetString(0)
                    });
                }
            }
            connection.Close();

            ColumnBox.ItemsSource = columns;
            ColumnBox.IsEnabled = columns.Count > 0;

        }

        private async void OnClick1(object sender, RoutedEventArgs e)
        {
            if (TableBox.SelectedItem is not TableView selectedTable ||
                ColumnBox.SelectedItem is not ColumnView selectedColumn)
               {
                return; 
                }

            string tableName = selectedTable.TableName;
            string columnName = selectedColumn.ColumnName;
            string searchText = SearchBox.Text;

            var results = new ObservableCollection<SearchResultItem>();

            using (var connection = DB.GetConnection())
            {
                await connection.OpenAsync();

                string query = $"SELECT * FROM [{tableName}] WHERE CAST([{columnName}] AS NVARCHAR(MAX)) LIKE @search";
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@search", $"%{searchText}%");

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string matchedValue = reader[columnName]?.ToString() ?? "NULL";
                    var details = new List<string>();

                    // Loop through every column in the row to build the details line
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string col = reader.GetName(i);
                        string val = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString();
                        details.Add($"{col}: {val}");
                    }

                    results.Add(new SearchResultItem
                    {
                        PrimaryText = $"{columnName}: {matchedValue}",
                        SecondaryText = string.Join("   |   ", details)
                    });
                }
                connection.Close();
            }

            ResultsListView.ItemsSource = results;


        }




    }
    public class TableView
    {
        public string TableName { get; set; } = "";
    }
    public class ColumnView
    {
        public string ColumnName { get; set; } = "";
    }
    public class SearchResultItem
    {
        public string PrimaryText { get; set; } = "";
        public string SecondaryText { get; set; } = "";
    }

}