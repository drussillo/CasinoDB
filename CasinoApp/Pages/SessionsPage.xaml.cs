using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using System.Data.SqlTypes;
using System.Collections.ObjectModel;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CasinoApp.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SessionsPage : Page
{
    public ObservableCollection<SessionsView> Sessions { get; } = new();
    public SessionsPage()
    {
        InitializeComponent();

        var Connection = DB.GetConnection();
        Connection.Open();
        var Query = Connection.CreateCommand();
        Query.CommandText = @"
            SELECT Session.*, Employee.f_name, Employee.l_name, Equipment.game
            FROM Session JOIN Employee ON Employee.ssn = Session.employee
                         JOIN Equipment ON Equipment.equipment_id = Session.equipment
        ";
        using (SqlDataReader reader = Query.ExecuteReader())
        {
            while(reader.Read())
            {
                Sessions.Add(new SessionsView
                {
                    SessionID = reader.GetString(0),
                    EmployeeID = reader.GetString(1),
                    EmployeeName = $"{reader.GetString(5)} {reader.GetString(6)}",
                    EquipmentID = reader.GetString(2),
                    Game = reader.GetString(7),
                    StartTime = reader.GetDateTime(3),
                    EndTime = reader.GetDateTime(4)
                });
            }
        }

        Connection.Close();
    }


}

public class SessionsView
{
    private SqlDateTime startTime;
    private SqlDateTime endTime;

    public string SessionID { get; set; } = "";
    public string EmployeeID { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public string EquipmentID { get; set; } = "";
    public string Game { get; set; } = "";
    public SqlDateTime StartTime { get => startTime; set => startTime = value; }
    public SqlDateTime EndTime { get => endTime; set => endTime = value; }
}