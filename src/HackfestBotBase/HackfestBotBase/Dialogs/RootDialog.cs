using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using HackfestBotBase.Services;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Connector;

namespace HackfestBotBase.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private readonly IDialogBuilder _dialogBuilder;
        private readonly IMessageService _messageService;

        public RootDialog(IMessageService messageService, IDialogBuilder dialogBuilder)
        {
            SetField.NotNull(out _messageService, nameof(messageService), messageService);
            SetField.NotNull(out _dialogBuilder, nameof(dialogBuilder), dialogBuilder);
        }

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync); // Wait for a user to send a message.
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            /*
             * If you care about the initial message content from the user, then you can use the following to extract the message.
             *
             * string message = (await result).Text
             */


            await _messageService.PostAsync("Kia ora, Joanna! Nice to meet you. My name is Mōhio, and I'm here to assist you.");
            ShowSuggestedButtons(context);

        }

       

        private void ShowSuggestedButtons(IDialogContext context)
        {
            PromptDialog.Confirm(context, ResumeAfterConfirmationIntent, $"Did I understand correctly - that you'd like to register your baby?", $"Sorry I don't understand - try again!");

        }

        private async Task ResumeAfterConfirmationIntent(IDialogContext context, IAwaitable<bool> result)
        {
            bool confirmation = await result;

            switch (confirmation)
            {
                case true:
                    var showConfirmPregnantActionsDialog = _dialogBuilder.BuildShowSuggestedActionsDialog(context.Activity.AsMessageActivity(),
                        "Fantastic! Are you pregnant or have you already given birth?", "I'm pregnant", "I have already given birth");
                    context.Call(showConfirmPregnantActionsDialog, AfterShowingConfirmPregnantActions);
                    break;
                default:
                    PromptDialog.Confirm(context, ResumeAfterConfirmationIntent, "Are you sure? Did I understand correctly - that you'd like to register your baby?", "Sorry I didn't get that - try again!");
                    break;
            }
        }

        

        private async Task AfterShowingConfirmPregnantActions(IDialogContext context, IAwaitable<object> result)
        {
            await result;
            context.Wait(ResumeConfirmPregnant);
        }

        private async Task ResumeConfirmPregnant(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            string message = (await result)?.Text;

            if (message == "I'm pregnant")
            {
                await context.PostAsync("Great! How many weeks have you been pregnant for?");
                PregnantWeeksDialog pregnantWeeksDialog = _dialogBuilder.BuildPregnantWeeksDialog(context.Activity.AsMessageActivity());
                context.Call(pregnantWeeksDialog, AfterShowingPregnantWeeksDialog);
            }
            else
            {
                await context.PostAsync("Congratulations!");
                CallConfirmIfHaveChoosenName(context);
            }
        }

        private async Task AfterShowingPregnantWeeksDialog(IDialogContext context, IAwaitable<string> result)
        {
            string message = await result;
            await context.PostAsync(message);
            CallConfirmIfHaveChoosenName(context);
        }

        

        private void CallConfirmIfHaveChoosenName(IDialogContext context)
        {
            PromptDialog.Confirm(context, ResumeAfterConfirmationIfHaveChoosenName, $"Have you chosen a name for your baby?", $"Sorry I don't understand - try again!\n Have you chosen a name for your baby?");

        }

        private async Task ResumeAfterConfirmationIfHaveChoosenName(IDialogContext context, IAwaitable<bool> result)
        {
            bool confirmation = await result;

            switch (confirmation)
            {
                case true:
                    await context.PostAsync(
                        "Perfect. Can you please type out the full name you have chosen for your baby?\nPlease check that you've spelled it correctly!");
                    break;
                default:
                    await context.PostAsync("You can try this website for some  ideas for names");
                    await context.PostAsync("https://smartstart.services.govt.nz/news/baby-names");
                    await context.PostAsync("Let me know when you have found something you like ");
                    break;
            }
            ChildNameDialog childNameDialog = _dialogBuilder.BuildChildNameDialog(context.Activity.AsMessageActivity());
            context.Call(childNameDialog, AfterShowingChildNameDialog);
        }

        private async Task AfterShowingChildNameDialog(IDialogContext context, IAwaitable<string> result)
        {
            string message = await result;
            await context.PostAsync($"{message} is an excellent choice!");
        }


    }
}