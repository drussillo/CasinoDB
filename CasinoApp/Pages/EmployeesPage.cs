using ABI.Microsoft.UI.Xaml.Markup;
using Microsoft.Data.SqlClient;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using static System.Net.Mime.MediaTypeNames;

namespace CasinoApp.Pages
{
    public sealed partial class EmployeesPage : Page
    {
        public EmployeesPage()
        {
            InitializeComponent();


            var Connection = DB.GetConnection();
            Connection.Open();
            var Query = Connection.CreateCommand();
            Query.CommandText = "SELECT * FROM Employee";
            using (SqlDataReader reader = Query.ExecuteReader())
            {
                while(reader.Read())
                {
                    for(int i = 0; i < reader.FieldCount; i++)
                    {
                        EmpList.Text += reader[i] + " ";
                    }
                    EmpList.Text += "\n";
                }
            }

            Connection.Close();

        }
    }
}
