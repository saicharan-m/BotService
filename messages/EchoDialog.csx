#load "Message.csx"

using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.ConnectorEx;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


// For more information about this template visit http://aka.ms/azurebots-csharp-basic
[Serializable]
public class EchoDialog : IDialog<object>
{
    protected int count = 1;
    private string ADMIN_USER_ID = $"29:1W-wNIQJyoFA5Nz6WBAojU5zpceYHsB96f8Kar20Ul6k";
    public Task StartAsync(IDialogContext context)
    {
        try
        {
            context.Wait(MessageReceivedAsync);
        }
        catch (OperationCanceledException error)
        {
            return Task.FromCanceled(error.CancellationToken);
        }
        catch (Exception error)
        {
            return Task.FromException(error);
        }

        return Task.CompletedTask;
    }

    public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
    {
        var message = await argument;
        if (message.Text.ToUpper().Contains("INITIATE FILLING"))
        {
            if (context.Activity.ToConversationReference().User.Id == ADMIN_USER_ID)
            {
                // Retrieve storage account from connection string.
                var storageAccount = CloudStorageAccount.Parse(Utils.GetAppSetting("AzureWebJobsStorage"));

                // Create the table client.
                var tableClient = storageAccount.CreateCloudTableClient();

                // Retrieve a reference to a table.
                CloudTable messageTable = tableClient.GetTableReference("messageTable");
                // Construct the query operation for all users entities where PartitionKey="Smith".
                TableQuery<MessageString> query = new TableQuery<MessageString>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "malineni"));

                // Print the fields for each customer.
                TableContinuationToken token = null;
                do
                {
                    TableQuerySegment<MessageString> resultSegment = await messageTable.ExecuteQuerySegmentedAsync(query, token);
                    token = resultSegment.ContinuationToken;

                    foreach (MessageString entity in resultSegment.Results)
                    {
                        //IActivity triggerEvent = context.Activity;
                        var tMessage = JsonConvert.DeserializeObject<Message>(entity.SerializedMessage);
                        var messageactivity = (Activity)tMessage.RelatesTo.GetPostToBotMessage();

                        var client = new ConnectorClient(new Uri(messageactivity.ServiceUrl));
                        var triggerReply = messageactivity.CreateReply();
                        triggerReply.Text = $"{tMessage.Text}";
                        await client.Conversations.ReplyToActivityAsync(triggerReply);
                    }
                } while (token != null);
            }
            else
            {
                await context.PostAsync($"Your are not authorised to intiate the filling process");
                context.Wait(MessageReceivedAsync);
            }

        }
        else if (message.Text.ToUpper().Contains("HI"))
        {
            var queueMessage = new Message
            {
                RelatesTo = context.Activity.ToConversationReference(),
                Text = $"Do you want to submit your time sheets for this week as R-0034567895-000010-01 9 9 9 9 9"
            };
            var tableMessage = new MessageString(context.Activity.ToConversationReference().User.Id);

            tableMessage.SerializedMessage = JsonConvert.SerializeObject(queueMessage);
            tableMessage.IsActive = "Y";
            try
            {
                //rite the message to table
                await AddMessageToTableAsync(tableMessage);
                await context.PostAsync($"Your subscription is saved");
            }
            catch (Exception ex)
            {
                await context.PostAsync($"Your have subscribed already");
            }

            context.Wait(MessageReceivedAsync);
        }
        else if (message.Text.ToUpper() == "YES")
        {
            try
            {
                await AddTimeSheetToTableAsync($"R-0034567895-000010-01 9 9 9 9 9", context.Activity.ToConversationReference().User.Id);
            }
            catch (Exception)
            {
                await context.PostAsync($"Error occured while submitting Please update your time entries manually in SAP");
            }
            context.Wait(MessageReceivedAsync);
        }
        else if (message.Text.ToUpper() == "NO")
        {
            await context.PostAsync($"Please specify your time entries in valid format(WBS 9 0 8 8 9)");
            context.Wait(MessageReceivedAsync);
        }
        else if (Regex.IsMatch(message.Text.ToUpper(), @"R-[0-9]{10}-[0-9]{6}-[0-9]{2}\s[0-9]\s[0-9]\s[0-9]\s[0-9]\s[0-9]"))
        {
            try
            {
                await AddTimeSheetToTableAsync(message.Text, context.Activity.ToConversationReference().User.Id);

            }
            catch (Exception)
            {
                await context.PostAsync($"Error occured while submitting Please update your time entries manually in SAP");
            }
            context.Wait(MessageReceivedAsync);
        }
        else
        {
            await context.PostAsync($"{message.Text} is not recognised format. Please enter valid message.");
            context.Wait(MessageReceivedAsync);
        }
    }

    public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
    {
        var confirm = await argument;
        if (confirm)
        {
            this.count = 1;
            await context.PostAsync($"Your time entries are submitted");
        }
        else
        {
            await context.PostAsync($"Please specify your time entries in valid format(Submit WBS hour perday with space between each day)");
        }
        context.Wait(MessageReceivedAsync);
    }

    public static async Task AddMessageToQueueAsync(string message)
    {
        // Retrieve storage account from connection string.
        var storageAccount = CloudStorageAccount.Parse(Utils.GetAppSetting("AzureWebJobsStorage"));

        // Create the queue client.
        var queueClient = storageAccount.CreateCloudQueueClient();

        // Retrieve a reference to a queue.
        var queue = queueClient.GetQueueReference("bot-queue");

        // Create the queue if it doesn't already exist.
        await queue.CreateIfNotExistsAsync();

        // Create a message and add it to the queue.
        var queuemessage = new CloudQueueMessage(message);
        await queue.AddMessageAsync(queuemessage);
    }

    public static async Task AddMessageToTableAsync(MessageString myMessageTableEntity)
    {
        // Retrieve storage account from connection string.
        var storageAccount = CloudStorageAccount.Parse(Utils.GetAppSetting("AzureWebJobsStorage"));

        // Create the table client.
        var tableClient = storageAccount.CreateCloudTableClient();

        // Retrieve a reference to a table.
        CloudTable messageTable = tableClient.GetTableReference("messageTable");

        // Create the queue if it doesn't already exist.
        await messageTable.CreateIfNotExistsAsync();

        // Create a insert query
        TableOperation insertOperation = TableOperation.Insert(myMessageTableEntity);

        // Execute the insert operation.
        await messageTable.ExecuteAsync(insertOperation);
    }

    public static async Task AddTimeSheetToTableAsync(string message, string userId, IDialogContext context)
    {
        // Retrieve storage account from connection string.
        var storageAccount = CloudStorageAccount.Parse(Utils.GetAppSetting("AzureWebJobsStorage"));

        // Create the table client.
        var tableClient = storageAccount.CreateCloudTableClient();

        // Retrieve a reference to a table.
        CloudTable trsTable = tableClient.GetTableReference("TimesheetEntityTbl");

        // Create the queue if it doesn't already exist.
        await trsTable.CreateIfNotExistsAsync();

        DateTime Firstday = dt.AddDays(-(int)dt.DayOfWeek);
        DateTime Endaday = Firstday.AddDays(6);

        // Geting entry from table

        TableOperation retrieveOperation = TableOperation.Retrieve<TimesheetEntity>(Endaday.ToShortDateString(), userId);

        // Execute the retrieve operation.
        TableResult retrievedResult = await peopleTable.ExecuteAsync(retrieveOperation);

        // Print the phone number of the result.
        if (retrievedResult.Result != null)
        {
            TimesheetEntity updateEntity = (TimesheetEntity)retrievedResult.Result;
            updateEntity.WBS = message.Substring(0, 22);
            var dayHoursString = message.Substring(22);
            var daysHours = dayHoursString.Split(' ');
            updateEntity.Day1 = Convert.ToInt32(daysHours[0]);
            updateEntity.Day2 = Convert.ToInt32(daysHours[1]);
            updateEntity.Day3 = Convert.ToInt32(daysHours[2]);
            updateEntity.Day4 = Convert.ToInt32(daysHours[3]);
            updateEntity.Day5 = Convert.ToInt32(daysHours[4]);
            // Create the Replace TableOperation.
            TableOperation updateOperation = TableOperation.Replace(updateEntity);
            // Execute the operation.
            trsTable.Execute(updateOperation);
            await context.PostAsync($"Your time entries are updated");
        }
        else
        {
            var mytrsTableEntity = new TimesheetEntity(userId, Endaday.ToShortDateString());
            mytrsTableEntity.WBS = message.Substring(0, 22);
            var dayHoursString = message.Substring(22);
            var daysHours = dayHoursString.Split(' ');
            mytrsTableEntity.Day1 = Convert.ToInt32(daysHours[0]);
            mytrsTableEntity.Day2 = Convert.ToInt32(daysHours[1]);
            mytrsTableEntity.Day3 = Convert.ToInt32(daysHours[2]);
            mytrsTableEntity.Day4 = Convert.ToInt32(daysHours[3]);
            mytrsTableEntity.Day5 = Convert.ToInt32(daysHours[4]);

            // Create a insert query
            TableOperation insertOperation = TableOperation.Insert(mytrsTableEntity);

            // Execute the insert operation.
            await trsTable.ExecuteAsync(insertOperation);
            await context.PostAsync($"Your time entries are submitted");
        }
    }
}
