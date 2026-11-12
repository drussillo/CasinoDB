using ABI.Microsoft.UI.Xaml.Markup;
using Microsoft.Data.SqlClient;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using static System.Net.Mime.MediaTypeNames;

namespace CasinoApp.Pages
{
    public sealed partial class MachinePage : Page
    {
        public MachinePage()
        {
            InitializeComponent();


            var Connection = DB.GetConnection();
            Connection.Open();
            var Query = Connection.CreateCommand();
            Query.CommandText = "SELECT * FROM EQUIPMENT";
            using (SqlDataReader reader = Query.ExecuteReader())
            {
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        MachineList.Text += reader[i] + " ";
                    }
                    MachineList.Text += "\n";
                }
            }

            Connection.Close();

        }
    }
}
