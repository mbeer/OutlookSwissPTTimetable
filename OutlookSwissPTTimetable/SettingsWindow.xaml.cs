using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;

namespace OutlookSwissPTTimetable
{
    /// <summary>
    /// Interaktionslogik für SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : MetroWindow
    {
        public DataTable DefaultStations { get; set; }

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            this.DialogResult = true;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.ToString());
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = DefaultStationsGrid.SelectedIndex;
            if (selectedIndex > -1 && selectedIndex < DefaultStations.Rows.Count)
            {
                DefaultStations.Rows.RemoveAt(selectedIndex);
            }
        }

        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            DataView dv = DefaultStations.DefaultView;
            dv.Sort = "name ASC";
            DefaultStations = dv.ToTable();
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = DefaultStationsGrid.SelectedIndex;
            if (selectedIndex > 0 && selectedIndex < DefaultStations.Rows.Count)
            {
                DataRow selectedRow = DefaultStations.Rows[selectedIndex];
                DataRow newRow = DefaultStations.NewRow();
                newRow["name"] = selectedRow["name"];
                newRow["distance"] = selectedRow["distance"];
                DefaultStations.Rows.RemoveAt(selectedIndex);
                DefaultStations.Rows.InsertAt(newRow, selectedIndex - 1);
                DefaultStationsGrid.SelectedIndex = selectedIndex - 1;
            }

        }

    

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = DefaultStationsGrid.SelectedIndex;
            if (selectedIndex > -1 && selectedIndex < DefaultStations.Rows.Count - 1)
            {
                DataRow selectedRow = DefaultStations.Rows[selectedIndex];
                DataRow newRow = DefaultStations.NewRow();
                newRow["name"] = selectedRow["name"];
                newRow["distance"] = selectedRow["distance"];
                DefaultStations.Rows.RemoveAt(selectedIndex);
                DefaultStations.Rows.InsertAt(newRow, selectedIndex + 1);
                DefaultStationsGrid.SelectedIndex = selectedIndex + 1;
            }
        }
    }
}
