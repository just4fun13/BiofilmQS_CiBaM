using Assets.Scripts.MVVM_CA.Models.ModelParams;
using System;
using System.IO;
using UnityEngine;

namespace CellularAutomaton
{
    public class MyLogger
    {
        // В Editor: корень Unity-проекта
        // В Build: папка рядом с .exe
        private static string RootFolder
        {
            get
            {
                string dataPath = Application.dataPath;

                DirectoryInfo dir = Directory.GetParent(dataPath);

                if (dir != null)
                    return dir.FullName;

                return Environment.CurrentDirectory;
            }
        }

        private static string BaseLogFolder =>
            System.IO.Path.Combine(RootFolder, "Log");

        public static string LogLocation =>
            System.IO.Path.Combine(BaseLogFolder, "Log.txt");

        public static string ParamtersLocation =>
            System.IO.Path.Combine(BaseLogFolder, "Pars.json");

        public static string PictureLocation =>
            System.IO.Path.Combine(BaseLogFolder, "Pictures");

        private static void EnsureLogFolders()
        {
            if (!Directory.Exists(BaseLogFolder))
                Directory.CreateDirectory(BaseLogFolder);

            if (!Directory.Exists(PictureLocation))
                Directory.CreateDirectory(PictureLocation);
        }

        public static void InitLogger()
        {
            EnsureLogFolders();

            if (!File.Exists(LogLocation))
                File.WriteAllText(LogLocation, "");

            Debug.Log("Log folder: " + BaseLogFolder);
            Debug.Log("Log file: " + LogLocation);
        }

        public static void ClearLog()
        {
            EnsureLogFolders();
            File.WriteAllText(LogLocation, "");
        }

        private static int NowTime()
        {
            var nowTime = DateTime.Now;
            return 3600 * nowTime.Hour + 60 * nowTime.Minute + nowTime.Second;
        }

        public static void WriteLog(string message)
        {
            EnsureLogFolders();

            using (StreamWriter writer = new StreamWriter(LogLocation, true))
            {
                writer.WriteLine(message);
            }
        }

        public static void Screenshot(string name = "screenshot", bool addNowTime = true)
        {
            EnsureLogFolders();

            string fileName;

            if (addNowTime)
            {
                fileName = name
                    + "_"
                    + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")
                    + "_"
                    + Guid.NewGuid().ToString("N").Substring(0, 8)
                    + ".png";
            }
            else
            {
                fileName = name + ".png";
            }

            string path = System.IO.Path.Combine(PictureLocation, fileName);

            ScreenCapture.CaptureScreenshot(path);

            Debug.Log("Screenshot saved to: " + path);
        }

        public static void SaveSettings()
        {
            EnsureLogFolders();

            string json = JsonUtility.ToJson(ModelParameters.mainParameters, true);

            File.WriteAllText(ParamtersLocation, json);

            Debug.Log("Настройки сохранены: " + ParamtersLocation);
        }

        public static string GetLogFolderPath()
        {
            EnsureLogFolders();
            return BaseLogFolder;
        }
    }
}