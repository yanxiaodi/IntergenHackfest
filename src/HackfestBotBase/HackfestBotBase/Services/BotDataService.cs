﻿
using HackfestBotBase.Models;
using Microsoft.Bot.Builder.Dialogs.Internals;

namespace HackfestBotBase.Services
{
    public class BotDataService : IBotDataService
    {
        public void SetPreferredName(IBotData botData, string name)
        {
            botData.SetValue(DataStoreKey.PreferredFirstName, name);
        }

        public string GetPreferredName(IBotData botData)
        {
            return botData.GetValueOrDefault<string>(DataStoreKey.PreferredFirstName);
        }

        public void SetEmail(IBotData botData, string name)
        {
            botData.SetValue(DataStoreKey.Email, name);
        }

        public string GetEmail(IBotData botData)
        {
            return botData.GetValueOrDefault<string>(DataStoreKey.Email);
        }
    }
}