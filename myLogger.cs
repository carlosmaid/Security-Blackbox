using System;
using Sandbox.ModAPI;

public class MyLogger
{
    private static DateTime lastLog = DateTime.Now;
    private static string logBuff = "";

    public static void resetLogger()
    {
        var m_write = MyAPIGateway.Utilities.WriteFileInLocalStorage("log", typeof(MyLogger));
        m_write.Write("");
        m_write.Close();
    }

    public static void logger(string newLog)
    {
        try
        {
            logBuff += String.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTime.Now) + " - " + newLog + "\n";
            if ((DateTime.Now - lastLog).TotalSeconds > 5)
            {
                string buff = "";
                if (MyAPIGateway.Utilities.FileExistsInLocalStorage("log", typeof(MyLogger)))
                {
                    var m_reader = MyAPIGateway.Utilities.ReadFileInLocalStorage("log", typeof(MyLogger));
                    buff += m_reader.ReadToEnd();
                    m_reader.Close();
                }
                if (buff.Length > 250000)
                {
                    buff = "";
                }

                var m_write = MyAPIGateway.Utilities.WriteFileInLocalStorage("log", typeof(MyLogger));
                m_write.Write(buff);
                m_write.Write(logBuff);
                m_write.Close();
                logBuff = "";
                lastLog = DateTime.Now;
            }
        }
        catch
        {
            MyLogger.logger("ERREUR : Logger : ");
        }
    }
}