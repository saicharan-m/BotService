﻿using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Azure.WebJobs.Host;
public class Message
{
    public ConversationReference RelatesTo { get; set; }
    public String Text { get; set; }
}
