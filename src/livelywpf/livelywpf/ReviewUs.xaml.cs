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
    /// Interaction logic for ReviewUs.xaml
    /// </summary>
    public partial class ReviewUs : MetroWindow
    {
        public ReviewUs()
        {
            InitializeComponent();
        }
        private void Review_click(object sender, RoutedEventArgs e)
        {
            SettingsManager.ReviewProductAsync();
            App.settings.IsRated = true;
            DialogResult = true;
            this.Close();
            SettingsJson.SaveConfig(System.IO.Path.Combine(App.AppDataDir, "Settings.json"), App.settings);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void Checkbox_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox chebox = sender as System.Windows.Controls.CheckBox;
            if (chebox.IsChecked == true)
            {
                App.settings.IsRated = true;
            }
            else
            {
                App.settings.IsRated = false;
            }
            SettingsJson.SaveConfig(System.IO.Path.Combine(App.AppDataDir, "Settings.json"), App.settings);
        }
    }
}
