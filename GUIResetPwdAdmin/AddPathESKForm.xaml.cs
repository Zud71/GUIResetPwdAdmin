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

namespace GUIResetPwdAdmin
{
    /// <summary>
    /// Логика взаимодействия для AddPathESK.xaml
    /// </summary>
    public partial class AddPathESKForm : Window
    {
        public AddPathESKForm()
        {
            InitializeComponent();

            Status = false;
        }

        public bool Status { get; set; }
        public string ESKPath { get; set; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ESKPath = edit_ESKPath.Text;
            Status = true;
            Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Status = false;
            Close();
        }
    }
}
