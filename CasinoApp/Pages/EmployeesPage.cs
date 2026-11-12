using ABI.Microsoft.UI.Xaml.Markup;
using Microsoft.Data.SqlClient;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using static System.Net.Mime.MediaTypeNames;

namespace CasinoApp.Pages
{
    public sealed partial class EmployeesPage : Page
    {

        public ObservableCollection<EmployeeView> Employees { get; } = new();

        public EmployeesPage()
        {
            InitializeComponent();


            var Connection = DB.GetConnection();
            Connection.Open();
            var Query = Connection.CreateCommand();
            Query.CommandText = "SELECT * FROM Employee";
            using (SqlDataReader reader = Query.ExecuteReader())
            {
                while (reader.Read())
                {
                    Employees.Add(new EmployeeView
                    {
                        SSN = reader.GetString(0),
                        FirstName = reader.GetString(1),
                        LastName = reader.GetString(2),
                        Salary = (float)reader.GetDecimal(3),
                        Address = reader.GetString(4)
                    });
                }
            }

            Connection.Close();
        }
    }

    public class EmployeeView
    {
        public string SSN { get; set; } = "";
        public string FirstName { get; set; } = "";  
        public string LastName { get; set; } = "";
        public float Salary { get; set; } = 0.0f;
        public string Address { get; set; } = "";
    }
}
