using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Text.RegularExpressions;

// For more information about this template visit http://aka.ms/azurebots-csharp-basic
[Serializable]
public class EchoDialog : IDialog<object>
{
    protected int count = 1;

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
        var regX = new Regex(@"R-[0-9]{10}-[0-9]{6}-[0-9]{2}*");
        var message = await argument;
        if(Regex.IsMatch(message.Text.ToUpper(), @"R-[0-9]{10}-[0-9]{6}-[0-9]{2}*"))
        {
            PromptDialog.Confirm(
                context,
                AfterResetAsync,
                "Do you want to submit your time sheets for this week as R-0034567895-000010-01 9 9 9 9 9",
                "Didn't get that!",
                promptStyle: PromptStyle.Auto);
        }
        if (message.Text.ToUpper() == "YES")
        {
            await context.PostAsync($"Your time entries are submitted");
            context.Wait(MessageReceivedAsync);
        }
        else if (message.Text.ToUpper().Contains("NO") || message.Text.ToUpper().Contains("SUBMIT"))
        {
            await context.PostAsync($"Your time entries are submitted");
            context.Wait(MessageReceivedAsync);
        }
        else
        {
            await context.PostAsync($"{message.Text} is not recognised format. Please enter timesheets in valid format.");
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
            await context.PostAsync("");
        }
        context.Wait(MessageReceivedAsync);
    }
}