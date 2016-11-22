using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;

public static class Utilities
{
    private static object LockSendTo = new Object();

    public static ushort clientIdMessage = 1203;
    public static ushort clientUpdateId = 1259;
    public static ushort serverIdOrder = 1212;
    public static ushort serverIdChatHistory = 1178;

    // Message send
    public static void SendMessageToServer(ushort id, string message)
    {
        lock (LockSendTo)
            MyAPIGateway.Multiplayer.SendMessageToServer(id, System.Text.Encoding.Unicode.GetBytes(message));
    }

    public static void SendMessageToOther(ushort id, string message)
    {
        lock (LockSendTo)
            MyAPIGateway.Multiplayer.SendMessageToOthers(id, System.Text.Encoding.Unicode.GetBytes(message));
    }
    public static void SendMessage(string message)
    {
        lock (LockSendTo)
            MyAPIGateway.Utilities.SendMessage(message);
    }
    public static void SendMessageTo(ushort id, string message, IMyPlayer player)
    {
        lock (LockSendTo)
        {
            try
            {
                ulong steamID = player.SteamUserId;
                MyAPIGateway.Multiplayer.SendMessageTo(id, System.Text.Encoding.Unicode.GetBytes(message), steamID);
            }
            catch (Exception e)
            {
                MyLogger.logger("Utils::sendMsg->Exception : " + e.ToString());
            }
        }
    }

    public static void SendMessage(string message, int duration, MyFontEnum color, IMyPlayer player)
    {
        lock (LockSendTo)
        {
            string msg = "PLAYERMSG@" + duration.ToString() + "@" + ((int)color).ToString() + "@" + message;
            SendMessageTo(clientIdMessage, msg, player);
        }
    }
    //================================================================================================
}