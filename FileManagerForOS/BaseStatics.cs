using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FileManagerForOS
{
    public static class BaseStatics
    {
        private static List<string> listProhibitedNames = new List<string>(new string[] { "CON", "AUX", "COM1", "COM2", "COM3", "COM4", "LPT1", "LPT2", "LPT3", "PRN", "NUL" });


        public enum FileActions : int
        {
            All = 0, //Пока используется для сортировки логов
            Create = 1,
            Delete = 2,
            Copy = 3,
            Excision = 4,
            Rename = 5,
            Open = 6,
            //Insert = 7,
        }

        public static bool isProhibitedName(string name){
            bool l = false;
            listProhibitedNames.ForEach(delegate (String prohibitedName){ 
                if (prohibitedName.Equals(name.ToUpper()))
                {
                     l = true;
                }
            });
            return l;
        }

        public static int getRAM()
        {
            ObjectQuery wql = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(wql);
            ManagementObjectCollection results = searcher.Get();
            int ramPCMb = 0;
            foreach (ManagementObject result in results)
            {
                ramPCMb = (int)(ulong)result["TotalVisibleMemorySize"] / 1024;
            }
            return ramPCMb;
        }

        public static bool isStringEquals(string a, string b)
        {
            if (a != null){

                return a.Equals(b, StringComparison.CurrentCulture);
            }
            return false;
        }

        public static void SaveLogs(string nameLogs, List<object[]> listActions, FileActions fa)
        {
            string logFileName = nameLogs + ".txt";
            string pathToSave = Environment.CurrentDirectory;
            try
            {
                File.WriteAllText(pathToSave + "\\" + logFileName, getSelectedLogs(fa, listActions));
                MessageBox.Show("Файл успешно создан!\nЕго расположение: " + Environment.CurrentDirectory);
            }
            catch (IOException w)
            {
                MessageBox.Show("Невозможно создать файл логов!\nОшибка:" + w.Message);
            }
        }

        public static string getSelectedLogs(FileActions action, List<object[]> listActions)
        {
            if (action.Equals(FileActions.All))
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (object[] element in listActions)
                {
                    stringBuilder.AppendLine(element[1].ToString()).AppendLine(Properties.Resources.splitter);
                }
                return stringBuilder.ToString();
            }
            else
            {
                StringBuilder stringBuilder = new StringBuilder();
                foreach (object[] element in listActions)
                {
                    if (action.Equals((FileActions)element[0]))
                    {
                        stringBuilder.AppendLine(element[1].ToString()).AppendLine(Properties.Resources.splitter);
                    }
                }
                return stringBuilder.ToString();
            }
            return null;
        }
    }
}
