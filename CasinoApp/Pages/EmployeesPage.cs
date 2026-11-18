using ABI.Microsoft.UI.Xaml.Markup;
using Microsoft.Data.SqlClient;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System.Collections.ObjectModel;
using System.Data;
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

            LoadEmployees();
        }

        private void LoadEmployees()
        {
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
                        Salary = (decimal)reader.GetDecimal(3),
                        Address = reader.GetString(4)
                    });
                }
            }

            Connection.Close();
        }

        private void ListViewEmplopyee_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = ListViewEmployees.SelectedItem is EmployeeView;

            EditButton.IsEnabled = hasSelection;
            DeleteButton.IsEnabled = hasSelection;
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewEmployees.SelectedItem is not EmployeeView employee)
                return;

            string fullName = $"{employee.LastName} {employee.FirstName}";
            var message = new TextBlock
            {
                Text = $"Are you sure you want to delete\n{fullName}?",
                FontSize = 18,
                TextWrapping = TextWrapping.Wrap
            };

            var dialog = UIHelper.CreateDialog($"Delete {fullName}", message, "Yes", "No", XamlRoot);
            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            try
            {
                using var connection = DB.GetConnection();
                connection.Open();

                using var command = new SqlCommand(
                    "DELETE FROM EMPLOYEE " +
                    "WHERE SSN = @ssn", 
                    connection);

                command.Parameters.AddWithValue("@ssn ", employee.SSN);

                command.ExecuteNonQuery();
                Employees.Remove(employee);
            }
            catch (SqlException)
            {
                await UIHelper.ShowErrorAsync(XamlRoot);
            }
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewEmployees.SelectedItem is not EmployeeView employee)
                return;

            /* Dialog box inputs */
            var firstNameBox = new TextBox { Header = "First Name", Text = employee.FirstName };
            var lastNameBox = new TextBox { Header = "Last Name", Text = employee.LastName };

            var salaryNumBox = new NumberBox
            {
                Header = "Salary",
                Value = (double)employee.Salary,
                Minimum = 0,
                Maximum = 1000000,
                SmallChange = 0.01,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden
            };

            var addressBox = new TextBox { Header = "Address", Text = employee.Address };

            var panel = UIHelper.CreateFormPanel(firstNameBox, lastNameBox, salaryNumBox, addressBox);
            var dialog = UIHelper.CreateDialog("Edit an Emplopyee", panel, "Save", "Cancel", XamlRoot);

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            try
            {
                using var connection = DB.GetConnection();
                connection.Open();

                using var command = new SqlCommand(
                    @"UPDATE Employee
                      SET F_Name = @firstName,
                          L_Name = @lastName,
                          Salary = @salary,
                          Address = @address
                      WHERE SSN = @ssn", connection);
                

                command.Parameters.AddWithValue("@firstName", firstNameBox.Text);
                command.Parameters.AddWithValue("@lastName", lastNameBox.Text);

                command.Parameters.Add("@salary", SqlDbType.Float).Value = (float)salaryNumBox.Value;
                command.Parameters.Add("@address", SqlDbType.NVarChar).Value = String.IsNullOrEmpty(addressBox.Text) ? DBNull.Value : addressBox.Text;

                command.Parameters.Add("@ssn", SqlDbType.NVarChar).Value = employee.SSN;

                command.ExecuteNonQuery();

                // Replace the edited item so the ListView refreshes.
                int index = Employees.IndexOf(employee);
                if (index >= 0)
                {
                    Employees[index] = new EmployeeView
                    {
                        SSN = employee.SSN,
                        FirstName = firstNameBox.Text,
                        LastName = lastNameBox.Text,
                        Salary = (decimal)salaryNumBox.Value,
                        Address = addressBox.Text,
                    };
                }
            }
            catch (SqlException ex)
            {
                await UIHelper.ShowErrorAsync(XamlRoot);
            }
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            var ssnBox = new TextBox { Header = "SSN" };
            var firstNameBox = new TextBox { Header = "First Name" };
            var lastNameBox = new TextBox { Header = "Last Name" };

            var salaryNumBox = new NumberBox
            {
                Header = "Salary",
                Value = 0.0f,
                Minimum = 0,
                Maximum = 1000000,
                SmallChange = 0.01,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden
            };

            var addressBox = new TextBox { Header = "Address" };

            var panel = UIHelper.CreateFormPanel(ssnBox, firstNameBox, lastNameBox, salaryNumBox, addressBox);
            var dialog = UIHelper.CreateDialog("Add a Gambler Account", panel, "Add", "Cancel", XamlRoot);

            // Do not close the dialog when required fields are missing.
            dialog.PrimaryButtonClick += (s, args) =>
            {
                bool missingRequiredField =
                    string.IsNullOrWhiteSpace(ssnBox.Text) ||
                    salaryNumBox.Value < 0 ||
                    ssnBox.Text.Length != 9 ||
                    string.IsNullOrWhiteSpace(firstNameBox.Text) ||
                    string.IsNullOrWhiteSpace(lastNameBox.Text) ||
                    string.IsNullOrWhiteSpace(addressBox.Text);


                args.Cancel = missingRequiredField;
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;


            try
            {
                using var connection = DB.GetConnection();
                await connection.OpenAsync();

                using var command = new SqlCommand(
                    @"INSERT INTO Employee
                          (SSN, F_Name, L_Name, Salary, Address)
                      VALUES
                          (@ssn, @firstName, @lastName, @salary, @address)", 
                    connection);

                command.Parameters.Add("@ssn", SqlDbType.NVarChar).Value = ssnBox.Text;

                command.Parameters.AddWithValue("@firstName", firstNameBox.Text);
                command.Parameters.AddWithValue("@lastName", lastNameBox.Text);

                command.Parameters.Add("@salary", SqlDbType.Float).Value = (decimal)salaryNumBox.Value;
                command.Parameters.Add("@address", SqlDbType.NVarChar).Value = addressBox.Text;

                command.ExecuteNonQuery();

                Employees.Add(new EmployeeView
                {
                    SSN = ssnBox.Text,
                    FirstName = firstNameBox.Text,
                    LastName = lastNameBox.Text,
                    Salary = (decimal)salaryNumBox.Value,
                    Address = addressBox.Text,
                });
            }
            catch (SqlException)
            {
                await UIHelper.ShowErrorAsync(XamlRoot);
            }
        }
    }

    public class EmployeeView
    {
        public string SSN { get; set; } = "";
        public string FirstName { get; set; } = "";  
        public string LastName { get; set; } = "";
        public decimal Salary { get; set; } = 0;
        public string Address { get; set; } = "";
    }
}
