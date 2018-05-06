using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using HackfestBotBase.Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Connector;

namespace HackfestBotBase.Dialogs
{
    [Serializable]
    public class PregnantWeeksDialog : IDialog<string>
    {
        private readonly IDialogBuilder _dialogBuilder;
        private readonly IMessageService _messageService;

        public PregnantWeeksDialog(IMessageService messageService, IDialogBuilder dialogBuilder)
        {
            SetField.NotNull(out _messageService, nameof(messageService), messageService);
            SetField.NotNull(out _dialogBuilder, nameof(dialogBuilder), dialogBuilder);
        }

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            /*
             * If you care about the initial message content from the user, then you can use the following to extract the message.
             *
             * string message = (await result).Text
             */
            string message = (await result).Text;
            int weeks = 0;
            if (int.TryParse(message, out weeks))
            {
                //await _messageService.PostAsync($"Great! You have been pregnant for {weeks} weeks!");
                context.Done<string>($"Great! You have been pregnant for {weeks} weeks!");
            }
            else
            {
                await _messageService.PostAsync($"Please input a valid number!");
                context.Wait(MessageReceivedAsync);

            }
        }
        
    }
}