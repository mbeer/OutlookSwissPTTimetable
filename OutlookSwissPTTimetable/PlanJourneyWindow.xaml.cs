using System;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using MahApps.Metro.Controls;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace OutlookSwissPTTimetable
{
    /// <summary>
    /// Interaktionslogik für PlanJourneyControl.xaml
    /// </summary>
    public partial class PlanJourneyWindow : MetroWindow
    {
        public TransportOpendataCH.Connection[] InConnections { get; set; }
        public TransportOpendataCH.Connection[] OutConnections { get; set; }

        private DataTable defaultStations;

        private Outlook.AppointmentItem appointment;
        public Outlook.AppointmentItem Appointment
        {
            get { return appointment; }
            set
            {
                appointment = value;
                MeetingDescLabel.Content = appointment.Subject;
                MeetingDescTextBlock.Inlines.Clear();
                MeetingDescTextBlock.Inlines.Add(new Run(appointment.Start.ToLongDateString() + ", " + appointment.Start.ToShortTimeString() + "–" + appointment.End.ToShortTimeString()));

                if (!String.IsNullOrEmpty(appointment.Location))
                {
                    MeetingDescTextBlock.Inlines.Add(new LineBreak());
                    MeetingDescTextBlock.Inlines.Add(new Bold(new Run(appointment.Location)));
                }
            }
        }

        public Outlook.MAPIFolder MAPIFolder { get; set; }

        private void DefaultStations_Refresh()
        {
            LocationComboBox.ItemsSource = defaultStations.DefaultView;
            InConnComboBox.ItemsSource = defaultStations.DefaultView;
            OutConnComboBox.ItemsSource = defaultStations.DefaultView;
        }

        private void DefaultStations_Reload()
        {
            try
            {
                defaultStations.ReadXml(Globals.ThisAddIn.DefaultStationsXMLFile);
            }
            catch 
            {

            }

            if (defaultStations.Rows.Count == 0)
            {
                //
                // Add sample rows
                //
                defaultStations.Rows.Add(1, "Bern", 10);
                defaultStations.Rows.Add(2, "Fribourg/Freiburg", 15);
            }

            DefaultStations_Refresh();

        }

        private void DefaultStations_Init()

        {
            defaultStations = new System.Data.DataTable();
            System.Data.DataColumn dataColumnID = new System.Data.DataColumn();
            System.Data.DataColumn dataColumnName = new System.Data.DataColumn();
            System.Data.DataColumn dataColumnDistance = new System.Data.DataColumn();

            // 
            // dataTableDefaultStations
            // 
            defaultStations.Columns.AddRange(new System.Data.DataColumn[] {
                    dataColumnID,
                    dataColumnName,
                    dataColumnDistance});
            defaultStations.TableName = "DefaultStations";

            // 
            // dataColumnID
            // 
            dataColumnID.AutoIncrement = true;
            dataColumnID.Caption = "ID";
            dataColumnID.ColumnName = "id";
            dataColumnID.DataType = typeof(int);

            // 
            // dataColumnName
            // 
            dataColumnName.AllowDBNull = false;
            dataColumnName.Caption = "Name";
            dataColumnName.ColumnName = "name";
            dataColumnName.DefaultValue = "";

            // 
            // dataColumnDistance
            // 
            dataColumnDistance.AllowDBNull = false;
            dataColumnDistance.Caption = "Distanz (in Minuten)";
            dataColumnDistance.ColumnName = "distance";
            dataColumnDistance.DataType = typeof(ushort);
            dataColumnDistance.DefaultValue = ((ushort)(0));

        }

        public PlanJourneyWindow()
        {
            InitializeComponent();
        }

        private void InConnDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InConnRecordButton.IsEnabled = (InConnDataGrid.SelectedIndex > -1);
        }

        private void OutConnDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OutConnRecordButton.IsEnabled = (OutConnDataGrid.SelectedIndex > -1);
        }


        private void PlanJourneyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DefaultStations_Init();
            DefaultStations_Reload();
        }

        private enum Dir { In, Out };

        private async void GetConnectionsAsync(Dir dir)
        {
            DateTime time;
            String from;
            String to;
            bool isArrivalTime;
            int n = 0;
            TransportOpendataCH.ConnectionsRequest creq;
            TransportOpendataCH.ConnectionsResponse cres;

            if (dir == Dir.In)
            {
                time = Appointment.Start.AddMinutes((double)LocationDistanceUpDown.Value * (-1) + Convert.ToDouble(Properties.Settings.Default.LookaheadLookbackMinutes));
                from = InConnComboBox.Text.ToString();
                to = LocationComboBox.Text.ToString();
                isArrivalTime = true;
            }
            else
            {
                time = Appointment.End.AddMinutes((double)LocationDistanceUpDown.Value - Convert.ToDouble(Properties.Settings.Default.LookaheadLookbackMinutes));
                to = OutConnComboBox.Text.ToString();
                from = LocationComboBox.Text.ToString();
                isArrivalTime = false;
            }

            if (from.Length > 2 && to.Length > 2)
            {
                if (dir == Dir.In)
                {
                    InConnProgressRing.IsActive = true;
                    InConnProgressRing.Visibility = Visibility.Visible;
                    InConnQryButton.IsEnabled = false;
                }
                else
                {
                    OutConnProgressRing.IsActive = true;
                    OutConnProgressRing.Visibility = Visibility.Visible;
                    OutConnQryButton.IsEnabled = false;
                }

                try
                {
                    creq = new TransportOpendataCH.ConnectionsRequest(from, to, dateTime: time, isArrivalTime: isArrivalTime, limit: Properties.Settings.Default.ConnectionsLimit);
                    cres = await creq.GetConnectionsAsync();
                    if (cres.Connections != null)
                    {
                        n = cres.Connections.Length;
                    }
                    if (dir == Dir.In)
                    {
                        InConnections = cres.Connections;
                        InConnDataGrid.ItemsSource = InConnections;
                        InConnProgressRing.IsActive = false;
                        InConnProgressRing.Visibility = Visibility.Collapsed;
                        InConnQryButton.IsEnabled = true;
                        if (n > 1)
                        {
                            InConnDataGrid.SelectedIndex = n - 1;
                        }
                    }
                    else
                    {
                        OutConnections = cres.Connections;
                        OutConnDataGrid.ItemsSource = OutConnections;
                        OutConnProgressRing.IsActive = false;
                        OutConnProgressRing.Visibility = Visibility.Collapsed;
                        OutConnQryButton.IsEnabled = true;
                        if (n > 1)
                        {
                            OutConnDataGrid.SelectedIndex = 0;
                        }
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("The following error occurred: " + ex.Message);
                }


            } else
            {
                MessageBox.Show("Start- und/oder Zielort der Verbindung ist nicht angegeben.");
            }

        }

        private void InConnQryButton_Click(object sender, RoutedEventArgs e)
        {
                GetConnectionsAsync(Dir.In);
        }
        private void OutConnQryButton_Click(object sender, RoutedEventArgs e)
        {
            
                GetConnectionsAsync(Dir.Out);
        }

        private void LocationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selind = LocationComboBox.SelectedIndex;
            if (selind >= 0)
            {
                LocationDistanceUpDown.Value = GetDistanceFromDefaultStations(selind);
            }
        }

        private void InConnComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selind = InConnComboBox.SelectedIndex;
            if (selind >= 0)
            {
                InConnDistanceUpDown.Value = GetDistanceFromDefaultStations(selind);
            }
        }

        private void OutConnComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selind = OutConnComboBox.SelectedIndex;
            if (selind >= 0)
            {
                OutConnDistanceUpDown.Value = GetDistanceFromDefaultStations(selind);
            }
        }

        private ushort GetDistanceFromDefaultStations(int Index)
        {
            if (Index >= 0 && Index < defaultStations.Rows.Count)
            {
                return defaultStations.Rows[Index].Field<ushort>("distance");
            }
            else
            {
                return 0;
            }
        }

        private void RecordConnection(TransportOpendataCH.Connection con, double FromOffset = 0, double ToOffset = 0)
        {
            
            try
            {
                Outlook.Application application = Globals.ThisAddIn.Application;
                Outlook.AppointmentItem newAppointment = MAPIFolder.Items.Add(Outlook.OlItemType.olAppointmentItem) as Outlook.AppointmentItem;
                newAppointment.Start = con.From.Departure.AddMinutes(FromOffset * (-1));
                newAppointment.End = con.To.Arrival.AddMinutes(ToOffset);
                newAppointment.Location = String.Join(", ", con.Products);
                newAppointment.AllDayEvent = false;
                newAppointment.Subject = "Transfer " + con.From.Location.Name + "–" + con.To.Location.Name;
                newAppointment.Body = con.ToShortString();
                newAppointment.BusyStatus = (Outlook.OlBusyStatus)Properties.Settings.Default.BusyStatus;
                string rtf = con.ToRTF();
                Encoding iso = Encoding.GetEncoding("ISO-8859-1");
                newAppointment.RTFBody = iso.GetBytes(rtf);
                newAppointment.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("The following error occurred: " + ex.Message);
            }
        }

        private void InConnRecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (InConnDataGrid.SelectedIndex > -1)
            {
                RecordConnection(
                    InConnections[InConnDataGrid.SelectedIndex],
                    (double)InConnDistanceUpDown.Value,
                    (double)LocationDistanceUpDown.Value
                    );
            }

        }

        private void OutConnRecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (OutConnDataGrid.SelectedIndex > -1)
            {
                RecordConnection(
                    OutConnections[OutConnDataGrid.SelectedIndex],
                    (double)LocationDistanceUpDown.Value,
                    (double)OutConnDistanceUpDown.Value
                    );
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow wS = new SettingsWindow();
            wS.DefaultStations = defaultStations;
            wS.DefaultStationsGrid.ItemsSource = wS.DefaultStations.DefaultView;

            if (wS.ShowDialog() ?? false)
            {
                wS.DefaultStationsGrid.CommitEdit();
                defaultStations = wS.DefaultStations;
                defaultStations.WriteXml(Globals.ThisAddIn.DefaultStationsXMLFile);
            }
            DefaultStations_Refresh();

        }
    }
}
