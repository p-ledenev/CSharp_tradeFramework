﻿using System;
using System.IO;
using alfaMTT.DataSources;
using core.Model;

namespace alfaMTT.Alfa
{
    class Logger
    {
        public static String infoFile = "messages.log";

        public static void printInfo(DateTime date, String message, bool toScreen = true)
        {
            writeLog(date, message, toScreen);
        }

        public static void printInfo(DateTime date, Portfolio portfolio, String message, bool toScreen = true)
        {
            String logInfo = portfolio.ticket + " " + message;

            writeLog(date, logInfo, toScreen);
        }

        public static void printInfo(DateTime date, Machine machine, String message, bool toScreen = true)
        {
            String logInfo = machine.getTicket() + ": " + machine.getDecisionStrategyName() + " " + machine.getDepth() + " " + message;

            writeLog(date, logInfo, toScreen);
        }

        protected static void writeLog(DateTime date, String message, bool toScreen)
        {
            StreamWriter file = new StreamWriter(TradeLogger.path + infoFile, true);
            file.WriteLine(date.ToString("dd.MM.yyyy HH:mm:ss") + " " + message);
            file.Close();

            if (toScreen)
                Console.WriteLine(message);
        }
    }
}