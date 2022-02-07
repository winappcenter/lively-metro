using CoollePDFConverter.Model;
using livelywpf;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace TrineZip
{
    /// <summary>
    /// Interaction logic for RemindManual.xaml
    /// </summary>
    public partial class RemindManual : MetroWindow
    {
        public RemindManual()
        {
            InitializeComponent();
        }
        private void Review_click(object sender, RoutedEventArgs e)
        {
            SettingsManager.OpenHelpAsync();
            this.Close();
        }

        private void Checkbox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox chebox = sender as CheckBox;
            if (chebox.IsChecked == true)
            {
                App.settings.IsPopupHelp = false;
            }
            else
            {
                App.settings.IsPopupHelp = true;
            }
            SettingsJson.SaveConfig(System.IO.Path.Combine(App.AppDataDir, "Settings.json"), App.settings);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
