using Microsoft.Data.SqlClient;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;


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
            using var connection = DB.GetConnection();
            connection.Open();

            using var command = new SqlCommand(
                @"SELECT 
                     TABLE_NAME
                  FROM
                     information_schema.tables
                  WHERE
                     TABLE_NAME != 'sysdiagrams'", connection);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                Tables.Add(new TableView
                {
                    TableName = reader.GetString(0)
                });
            }

        }

        private void LoadColumn()
        {
            using var connection = DB.GetConnection();
            connection.Open();
            
            string Table = TableBox.SelectedItem.ToString();
            using var command = new SqlCommand(
                @"SELECT COLUMN_NAME
                  FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_NAME = @Table"
                );

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                Columns.Add(new ColumnView
                {
                    ColumnName = reader.GetString(0)
                });
            }
        }

        public void OnDropDownOpened(object sender, object e)
        {
            if (!TableBox.IsDropDownOpen)
            {
                LoadColumn();
            }
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