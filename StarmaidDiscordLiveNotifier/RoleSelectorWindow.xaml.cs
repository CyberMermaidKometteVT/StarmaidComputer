using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace StarmaidDiscordLiveNotifier
{
    public partial class RoleSelectorWindow : Window
    {
        public List<string> SelectedRoles { get; set; }

        public RoleSelectorWindow(List<string> roles)
        {
            InitializeComponent();

            RoleListBox.ItemsSource = roles;

            SelectedRoles = new List<string>();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedRoles = RoleListBox.SelectedItems.Cast<string>().ToList();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}