using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace FileManagerForOS
{
    public static class BaseStatics
    {
        public enum FileActions :int
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
    }
}
