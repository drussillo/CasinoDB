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

            SqlConnection Connection;

            Connection = new SqlConnection(
                $@"Server=(localdb)\MSSQLLocalDB;
                AttachDbFilename=C:\ProgramData\test\Casino.mdf;"
                );  
            // TODO^^^: the path needs to be updated. Has to be outside of project scope.
            // Option 1: Separate repo that clones to some absolute path
            // Option 2: Script that puts db in some absolute path

            Connection.Open();
            var Query = Connection.CreateCommand();
            Query.CommandText = "SELECT * FROM Gambler";
            var reader = Query.ExecuteReader();
            Connection.Close();

            EmpList.Text = "hello";
        }
    }
}
