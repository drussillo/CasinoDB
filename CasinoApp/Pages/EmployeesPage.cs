using ABI.Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System.Security.Cryptography.X509Certificates;

namespace CasinoApp.Pages
{
    public sealed partial class EmployeesPage : Page
    {
        public EmployeesPage()
        {
            InitializeComponent();

            EmpList.Text = "hello";
        }
    }
}
