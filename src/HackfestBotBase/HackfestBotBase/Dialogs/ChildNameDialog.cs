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
    public class ChildNameDialog : IDialog<string>
    {
        private readonly IDialogBuilder _dialogBuilder;
        private readonly IMessageService _messageService;

        public ChildNameDialog(IMessageService messageService, IDialogBuilder dialogBuilder)
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
            string name = (await result).Text;
            
            if (_checkChildNameValid(name))
            {
                context.Done<string>(name);
            }
            else
            {
                await context.PostAsync(
                    "I'm really sorry, but unfortunately I can't register this name because it includes or resembles an official title or rank (like Justice or Sir)");
                var showAskNameDialog = _dialogBuilder.BuildShowSuggestedActionsDialog(context.Activity.AsMessageActivity(),
                    "Do you maybe have another name in mind? 🙂", "Yes", "No", "Suggest a name");
                context.Call(showAskNameDialog, AfterShowingAskNameDialogActions);
            }
        }

        private async Task AfterShowingAskNameDialogActions(IDialogContext context, IAwaitable<object> result)
        {
            await result;
            context.Wait(ResumeAskNameDialog); // Wait for the next message from the user.
        }

        private async Task ResumeAskNameDialog(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            string message = (await result)?.Text;
            switch (message)
            {
                case "Yes":
                    await context.PostAsync(
                        "Can you please type out the full name you have chosen for your baby?\nPlease check that you've spelled it correctly!");
                    context.Wait(MessageReceivedAsync);
                    break;
                case "No":
                    await context.PostAsync(
                        "Please input the name after you get one...");
                    context.Wait(MessageReceivedAsync);
                    break;
                case "Suggest a name":
                    await context.PostAsync("You can try this website for some  ideas for names");
                    await context.PostAsync("https://smartstart.services.govt.nz/news/baby-names");
                    await context.PostAsync("Let me know when you have found something you like 🙂");
                    context.Wait(MessageReceivedAsync);
                    break;
                default:
                    await context.PostAsync("Sorry I can't understand... Can you please type out the full name you have chosen for your baby?");
                    context.Wait(MessageReceivedAsync);
                    break;
                    
            }
            
        }

        private readonly Func<string, bool> _checkChildNameValid = name =>
        {
            if (name.ToLower().Contains("queen") || name.ToLower().Contains("sir"))
            {
                return false;
            }

            return true;
        };
    }
}