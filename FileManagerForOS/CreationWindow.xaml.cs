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

namespace FileManagerForOS
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class CreationWindow : Window
    {

        public const int TYPE_FOLDER = 0;
        public const int TYPE_FILE = 1;

        public CreationWindow(int type)
        {

            InitializeComponent();
            if (type == TYPE_FOLDER)
            {
                lblName.Content += " создаваемой папки";
            }else if (type == TYPE_FILE)
            {
                lblName.Content += " создаваемого файла txt";
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            txtBoxNameFile.Text = "";
            this.Close();
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            if (txtBoxNameFile.Text.Equals(""))
            {
                MessageBox.Show("Введите имя!");
            }
            else
            {
                this.Close();
            }
            
        }
    }
}
