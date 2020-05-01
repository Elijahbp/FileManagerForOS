using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Management;
using System.IO;
using static FileManagerForOS.BaseStatics;

namespace FileManagerForOS
{
    /// <summary>
    /// Логика взаимодействия для WindowLogs.xaml
    /// </summary>
    public partial class WindowLogs : Window
    {

        private List<object[]> listActions;
        private int totalRAM;

        private const string forNamePC = "Имя компьютера: ";
        private const string workVirtualMemory = "Используемая вируальная память: ";
        private const string maxVirtualMemory = "Максимальное значение виртуальной памяти: ";
        private const string timeWork = "Время работы текущего сеанса: ";
        private const string splitter = "---------------------------------------------------------------------------";

        Process processInfo;
        Thread threadMemoryStat;

        public WindowLogs(List<object[]> listActions, int totalRAM)
        {
            InitializeComponent();
            this.listActions = listActions;
            this.totalRAM = totalRAM;
            baseInit();

        }

        private bool isStringEquals(string a, string b)
        {
            return a.Equals(b, StringComparison.CurrentCulture);
        }

        private void baseInit()
        {
            lblNamePC.Content = forNamePC + System.Environment.MachineName;

            threadMemoryStat = new Thread(SetMemoryStat);
            threadMemoryStat.Start();

            initComboBoxElements();

            txtBlockFirstSelectedLogs.Text = getSelectedLogs(FileActions.All);
            txtBlockSecondSelectedLogs.Text = getSelectedLogs(FileActions.All);

            

        }

        private void initComboBoxElements()
        {
            foreach (FileActions action in Enum.GetValues(typeof(FileActions)))
            {
                if (action.Equals(FileActions.All))
                {
                    cmbBoxSelectTypeFileActionsToSave.Items.Add(new ComboBoxItem()
                    {
                        Content = action,
                        Tag = action,
                        IsSelected = true,
                    });
                    cmbBoxSelectTypeFileActionsToViewFirst.Items.Add(new ComboBoxItem()
                    {
                        Content = action,
                        Tag = action,
                        IsSelected = true,
                    });
                    cmbBoxSelectTypeFileActionsToViewSecond.Items.Add(new ComboBoxItem()
                    {
                        Content = action,
                        Tag = action,
                        IsSelected = true,
                    });
                }
                else
                {
                    cmbBoxSelectTypeFileActionsToSave.Items.Add(new ComboBoxItem()
                    {
                        Content = action,
                        Tag = action,
                    });
                    cmbBoxSelectTypeFileActionsToViewFirst.Items.Add(new ComboBoxItem()
                    {
                        Content = action,
                        Tag = action,
                    });
                    cmbBoxSelectTypeFileActionsToViewSecond.Items.Add(new ComboBoxItem()
                    {
                        Content = action,
                        Tag = action,
                    });
                }
                
            }
        }

        private void SetMemoryStat (){
            double percentUsingRam;
            double usingRAM;
            double maxRAM;
            while (threadMemoryStat.IsAlive)
            {
                this.processInfo = Process.GetCurrentProcess();
                this.Dispatcher.Invoke(new Action(() =>
                {
                    
                    usingRAM = ConvertBytesToMegabytes(processInfo.VirtualMemorySize64);
                    maxRAM = ConvertBytesToMegabytes(processInfo.PeakVirtualMemorySize64);
                    percentUsingRam = Math.Round(usingRAM/totalRAM * 100,2);
                    
                    lblTimeSeance.Content = timeWork + (DateTime.Now-processInfo.StartTime).ToString();
                    lblVirtualMemory.Content = workVirtualMemory + usingRAM +" Мб" +" ("+percentUsingRam+"%)";
                    lblMaxVirtulMemory.Content = maxVirtualMemory +maxRAM + "Мб";

                }));
                Thread.Sleep(1000);
            }


        }

        private static double ConvertBytesToMegabytes(long bytes)
        {
            return Math.Round((bytes / 1024f) / 1024f,2);
        }


        private string getSelectedLogs(FileActions action)
        {
            if (action.Equals(FileActions.All)){
                StringBuilder stringBuilder = new StringBuilder();
                foreach (object[] element in listActions)
                {
                    stringBuilder.AppendLine(element[1].ToString()).AppendLine(splitter);
                }
                return stringBuilder.ToString();
            }
            else
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (object[] element in listActions)
                {
                    if(action.Equals((FileActions)element[0]))
                    {
                        stringBuilder.AppendLine(element[1].ToString()).AppendLine(splitter);
                    }
                }
                return stringBuilder.ToString(); 
            }
            return null;
        }

        private void cmbBoxSelectTypeFileActionsToViewFirst_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            txtBlockFirstSelectedLogs.Text = getSelectedLogs((FileActions)((ComboBoxItem)cmbBoxSelectTypeFileActionsToViewFirst.SelectedItem).Tag);
        }

        private void cmbBoxSelectTypeFileActionsToViewSecond_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            txtBlockSecondSelectedLogs.Text = getSelectedLogs((FileActions)((ComboBoxItem)cmbBoxSelectTypeFileActionsToViewSecond.SelectedItem).Tag);
        }

        private void Button_Click_SaveLogs(object sender, RoutedEventArgs e)
        {
            EditionWindow creationWindow = new EditionWindow(Properties.Resources.TYPE_FILE,false);
            creationWindow.Owner = this;
            creationWindow.ShowDialog();
            if (creationWindow.NameFile.Length != 0)
            {
                string logFileName = creationWindow.NameFile + ".txt";
                string pathToSave = Environment.CurrentDirectory;
                try
                {

                    File.WriteAllText(pathToSave + "\\" + logFileName, getSelectedLogs((FileActions)((ComboBoxItem)cmbBoxSelectTypeFileActionsToSave.SelectedItem).Tag));
                    MessageBox.Show("Файл успешно создан!\nЕго расположение: " + Environment.CurrentDirectory);
                }
                catch (IOException w)
                {
                    MessageBox.Show("Невозможно создать файл логов!\nОшибка:" + w.Message);
                }
            }
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Закрываем все потоки
            threadMemoryStat.Abort();
        }


    }
}
