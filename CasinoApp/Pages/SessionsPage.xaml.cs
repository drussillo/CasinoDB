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
using System.Security.Cryptography.Pkcs;
using Windows.Web.AtomPub;

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

        UpdateCollection();
    }

    public void UpdateCollection()
    {
        Sessions.Clear();

        var Connection = DB.GetConnection();
        Connection.Open();
        var Query = Connection.CreateCommand();
        Query.CommandText = @"
            SELECT Session.*, Employee.f_name, Employee.l_name, Equipment.game
            FROM Session JOIN Employee ON Employee.ssn = Session.employee
                         JOIN Equipment ON Equipment.equipment_id = Session.equipment
            ORDER BY session_id DESC
        ";
        using (SqlDataReader reader = Query.ExecuteReader())
        {
            while(reader.Read())
            {
                Sessions.Add(new SessionsView
                {
                    SessionID = reader.GetInt32(0),
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

    public bool DemoStarted = false;
    private CancellationTokenSource? _demoCts;
    private async void Demo_Click(object sender, RoutedEventArgs e)
    {
        if(DemoStarted)
        {
            DemoStarted = false;

            Demo.Content = "Start Demo";
            _demoCts?.Cancel();
            _demoCts?.Dispose();
            _demoCts = null;
            return;
        }
        DemoStarted = true;
        Demo.Content = "Stop Demo";

        _demoCts = new CancellationTokenSource();
        try
        {
            await RunDemoAsync(_demoCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Demo stopped.
        }
    }

    private async Task RunDemoAsync(CancellationToken token)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();

            // Add randomly generated session
            var Connection = DB.GetConnection();
            Connection.Open();

            var EmpQuery = Connection.CreateCommand();
            EmpQuery.CommandText = @"
                SELECT TOP 1 ssn
                FROM Employee
                ORDER BY NEWID();
            ";

            var EquipmentQuery = Connection.CreateCommand();
            EquipmentQuery.CommandText = @"
                SELECT TOP 1 equipment_id
                FROM Equipment
                ORDER BY NEWID();
            ";

            var empid = (string)EmpQuery.ExecuteScalar();
            var equid = (string)EquipmentQuery.ExecuteScalar();

            DateTime endTime = DateTime.Now;
            Random rng = new Random();
            DateTime startTime = endTime.AddHours(-rng.Next(1, 11));

            var SessionQuery = Connection.CreateCommand();
            SessionQuery.CommandText = $@"
                        INSERT INTO Session (employee, equipment, start_time, end_time)
                        values(@employee, @equipment, @startTime, @endTime)
            ";
            SessionQuery.Parameters.AddWithValue("@employee", empid);
            SessionQuery.Parameters.AddWithValue("@equipment", equid);
            SessionQuery.Parameters.AddWithValue("@startTime", startTime);
            SessionQuery.Parameters.AddWithValue("@endTime", endTime);

            SessionQuery.ExecuteNonQuery();
            UpdateCollection();

            // Wait until next insert
            await Task.Delay(TimeSpan.FromSeconds(1), token);
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _demoCts?.Cancel();
        _demoCts?.Dispose();
        _demoCts = null;
    }
}

public class SessionsView
{
    private SqlDateTime startTime;
    private SqlDateTime endTime;

    public int SessionID { get; set; } = -1;
    public string EmployeeID { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public string EquipmentID { get; set; } = "";
    public string Game { get; set; } = "";
    public SqlDateTime StartTime { get => startTime; set => startTime = value; }
    public SqlDateTime EndTime { get => endTime; set => endTime = value; }


    public string Display =>
        $"Session #{SessionID} | Employee: {EmployeeID} | " +
        $"Equipment: {EquipmentID} | {StartTime:g}";
}