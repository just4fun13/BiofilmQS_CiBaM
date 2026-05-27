using Assets.Scripts.MVVM_CA.Models.ModelParams;
using System;
using System.Collections;
using System.IO;
using UnityEngine;


namespace CellularAutomaton
{
    public class MyLogger 
    {
        public static string LogLocation = Environment.CurrentDirectory + @"\Assets\Resources\log\Log.txt";
        public static string ParamtersLocation = Environment.CurrentDirectory + @"\Log\Pars.json";
        public static string PictureLocation = Environment.CurrentDirectory + $@"\Log\Pictures\";
        public static void ClearLog()
        {
            using (StreamWriter sw = new StreamWriter(LogLocation, false))
            {
                // Call the Truncate() method of the underlying file stream to clear the file contents
                sw.BaseStream.SetLength(0);
            }
        }
        private static int NowTime()
        {
            var nowTime = DateTime.Now;
            return 3600 * nowTime.Hour + 60 * nowTime.Minute + nowTime.Second;
        }
        public static void WriteLog(string message) 
        {
            using (StreamWriter writer = new StreamWriter(LogLocation, true)) 
            {
                writer.WriteLine(message);
            }

        }
        public static void Screenshot(string name, bool addNowTime = false)
        {
            string path = PictureLocation + name + "_" + ".jpg";
            if (addNowTime)
                path = PictureLocation + name + "_" + NowTime().ToString() + ".jpg";
            ScreenCapture.CaptureScreenshot(path);
        }
        public static void SaveSettings()
        {
            string[] s = new string[5];
            s[1] = JsonUtility.ToJson(ModelParameters.geometryParameters);
            s[2] = JsonUtility.ToJson(ModelParameters.nutrientParameters);
            s[3] = JsonUtility.ToJson(ModelParameters.bacteriaParameters);
            s[4] = JsonUtility.ToJson(ModelParameters.aHLParameters);


        string json = JsonUtility.ToJson(ModelParameters.mainParameters);
            MainParameters pars = JsonUtility.FromJson<MainParameters>(json);
            File.WriteAllText(ParamtersLocation, json);
            Console.WriteLine("Настройки сохранены.");
        }
        

                
    }
}
