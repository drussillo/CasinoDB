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

namespace CasinoApp.Pages
{
    public sealed partial class SearchPage : Page
    {
        public ObservableCollection<TableView> Tables { get; } = new();
        public ObservableCollection<ColumnView> Columns { get; } = new();

        public SearchPage()
        {
            InitializeComponent();
            LoadTables();
        }

        private void LoadTables ()
        {
            Console.WriteLine("Loading Tables");
            var connection = DB.GetConnection();
            connection.Open();

            using var command = new SqlCommand(
                @"SELECT 
                     TABLE_NAME
                  FROM
                     information_schema.tables
                  WHERE
                     TABLE_NAME != 'sysdiagrams'", connection);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Tables.Add(new TableView
                    {
                        TableName = reader.GetString(0)
                    });
                }
            }
           
            connection.Close();

        }

        private void LoadColumn()
        {
            Console.WriteLine("Loading Column");
            
            string Table = TableBox.SelectedItem.ToString();
            Console.WriteLine("Selected Table is" + Table);

            using var connection = DB.GetConnection();
            connection.Open();
            using var command = new SqlCommand(
                @"SELECT COLUMN_NAME
                  FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_NAME = @table",
                connection);
            command.Parameters.AddWithValue("@table", Table);

            using (var reader2 = command.ExecuteReader())
            {
                while (reader2.Read())
                {
                    Columns.Add(new ColumnView
                    {
                        ColumnName = reader2.GetString(0)
                    });
                }
            }
            connection.Close();

        }
        void OnClickUpdate(object sender, RoutedEventArgs e)
        {
            LoadColumn();
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
    public class SearchView
    {

    }

}