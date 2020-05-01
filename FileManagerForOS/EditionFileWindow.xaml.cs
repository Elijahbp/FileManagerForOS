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
    public partial class EditionWindow : Window
    {

        private string nameFile;
        public string NameFile { get => nameFile; set => nameFile = value; }

        public EditionWindow(string type, bool rename)
        {
            InitializeComponent();
            NameFile = "";
            if (!rename)
            {
                if (type == Properties.Resources.TYPE_DIRECTORY)
                {
                    lblName.Content += " создаваемой папки";
                }
                else if (type == Properties.Resources.TYPE_FILE)
                {
                    lblName.Content += " создаваемого файла txt";
                }
            }
            else
            {
                if (type == Properties.Resources.TYPE_DIRECTORY)
                {
                    lblName.Content = "Введите новое имя папки";
                }
                else if (type == Properties.Resources.TYPE_FILE)
                {
                    lblName.Content = "Введите новое имя файла";
                }
            }
            
            
            
        }

        

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            txtBoxNameFile.Text = "";
            this.Close();
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            if (BaseStatics.isStringEquals(txtBoxNameFile.Text,""))
            {
                MessageBox.Show("Введите имя!");
                
            }
            else
            {
                NameFile = txtBoxNameFile.Text;
                this.Close();
            }
            
        }
    }
}
