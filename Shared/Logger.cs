using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Shared
{
    public class Logger
    {
        public static void VerifyDir(string path)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                if (!dir.Exists)
                {
                    dir.Create();
                }
            }
            catch
            {
            }
        }

        public void Log(string lines)
        {
            string path = "C:/Log/";
            VerifyDir(path);
            string fileName = DateTime.Now.Day.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Year.ToString() + "_Logs.txt";
            try
            {
                System.IO.StreamWriter file = new System.IO.StreamWriter(path + fileName, true);
                file.WriteLine(DateTime.Now.ToString() + '\n' + lines);
                file.Close();
            }
            catch (Exception)
            {
            }
        }
    }
}
