using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

public class Message
{
    public ConversationReference RelatesTo { get; set; }
    public String Text { get; set; }
}

public class MessageString:TableEntity
{
    public string SerializedMessage { get; set; }
    public string IsActive { get; set; }
    public MessageString(int key)
    {
        this.RowKey = key;
    }
}
