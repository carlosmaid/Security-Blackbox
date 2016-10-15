﻿ using System;
using Sandbox.ModAPI;

class myLogger
{
    private static string logBuff = "";
    private static Object loggerLock = new Object();

    public static void resetLogger()
    {
        var m_write = MyAPIGateway.Utilities.WriteFileInLocalStorage("log", typeof(myLogger));
        m_write.Write("");
        m_write.Close();
    }

    public static void logger(string newLog)
    {

        logBuff += String.Format("{0:dd/MM-HH:mm:ss}", DateTime.Now) + " - " + newLog + "\n";
        try
        {
            string fileName = "log";
            if (!MyAPIGateway.Multiplayer.IsServer)
                fileName = "log";
            string buff = "";
            if (MyAPIGateway.Utilities.FileExistsInLocalStorage(fileName, typeof(myLogger)))
            {
                var m_reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(fileName, typeof(myLogger));
                buff += m_reader.ReadToEnd();
                m_reader.Close();
            }
            if (buff.Length > 250000)
            {
                buff = "";
            }
            var m_write = MyAPIGateway.Utilities.WriteFileInLocalStorage(fileName, typeof(myLogger));
            m_write.Write(buff);
            m_write.Write(logBuff);
            m_write.Close();
            logBuff = "";
        }
     catch (Exception)
        {
     }
    }  

}
       
