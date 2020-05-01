using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static FileManagerForOS.BaseStatics;

namespace FileManagerForOS
{
    public partial class WindowUpdate : Window
    {
        private bool isUpdate;

        public bool IsInitialize { get => isUpdate; set => isUpdate = value; }

        struct Manifest
        {
            public string name { get; set; }
            public string version { get; set; }
        }

        public WindowUpdate()
        {
            InitializeComponent();
        }

        private void checkVersion()
        {
            //В идеале манифест должен скачиваться с домена
            string jsonText = File.ReadAllText("manifest.json");
            Manifest manifest = JsonConvert.DeserializeObject<Manifest>(jsonText);

            if (isStringEquals(manifest.version,Assembly.GetExecutingAssembly().GetName().Version.ToString()))
            {
                isUpdate = true;
                
            }
            else 
            {
                isUpdate = false;
                //Должно произойти скачивание обновленной версии (В данном случае - просто иметь второй exe)
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            checkVersion();
            this.Close();
        }

    }
}
