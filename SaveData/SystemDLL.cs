using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SaveData
{
    public class SystemDLL
    {
        public string PATH { get; set; }
        public string Root { get; set; } = "default";

        public void INIWrite(string key, string value)
        {
            WriteINI(Root, key, value, PATH);
        }

        public string INIRead(string key)
        {
            StringBuilder temp = new StringBuilder(255);
            GetINI(Root, key, "", temp, 255, PATH);
            return temp.ToString();
        }

        private void WriteINI(string s, string key, string val, string file)
        {
            WritePrivateProfileString(s, key, val, file);
        }

        private void GetINI(string section, string key, string def, StringBuilder retVal, int size, string filePath)
        {
            GetPrivateProfileString(section, key, def, retVal, size, filePath);
        }

        #region system

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        #endregion

    }
}
