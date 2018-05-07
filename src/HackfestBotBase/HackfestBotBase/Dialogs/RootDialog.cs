using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private readonly IBotDataService _botDataService;


        public RootDialog(IMessageService messageService, IDialogBuilder dialogBuilder, IBotDataService botDataService)
        {
            SetField.NotNull(out _messageService, nameof(messageService), messageService);
            SetField.NotNull(out _dialogBuilder, nameof(dialogBuilder), dialogBuilder);
            SetField.NotNull(out _botDataService, nameof(botDataService), botDataService);
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


            await _messageService.PostAsync(
                "Kia ora, Joanna! Nice to meet you. My name is Mōhio, and I'm here to assist you.");
            ShowSuggestedButtons(context);

        }



        private void ShowSuggestedButtons(IDialogContext context)
        {
            PromptDialog.Confirm(context, ResumeAfterConfirmationIntent,
                $"Did I understand correctly - that you'd like to register your baby?",
                $"Sorry I don't understand - try again!");

        }

        private async Task ResumeAfterConfirmationIntent(IDialogContext context, IAwaitable<bool> result)
        {
            bool confirmation = await result;
            switch (confirmation)
            {
                case true:
                    await context.PostAsync("Congratulations, that's fantastic news! 😁");
                    PromptDialog.Confirm(context, ResumeAfterConfirmationQuestion,
                        "Is it okay if I ask you a few details about your child? It'll take about 6 minutes.",
                        "Sorry I didn't get that - try again!");
                    break;
                default:
                    PromptDialog.Confirm(context, ResumeAfterConfirmationIntent,
                        "Are you sure? Did I understand correctly - that you'd like to register your baby?",
                        "Sorry I didn't get that - try again!");
                    break;
            }

        }

        private async Task ResumeAfterConfirmationQuestion(IDialogContext context, IAwaitable<bool> result)
        {
            bool confirmation = await result;
            switch (confirmation)
            {
                case true:
                    var showConfirmPregnantActionsDialog = _dialogBuilder.BuildShowSuggestedActionsDialog(
                        context.Activity.AsMessageActivity(),
                        "Fantastic! Are you pregnant or have you already given birth?", "I'm pregnant",
                        "I have already given birth");
                    context.Call(showConfirmPregnantActionsDialog, AfterShowingConfirmPregnantActions);
                    break;
                default:
                    await context.PostAsync("Goodbye!");
                    break;
            }
        }

        #region Pregnant




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
                await context.PostAsync("Great! 🙂 How many weeks have you been pregnant for?");
                PregnantWeeksDialog pregnantWeeksDialog =
                    _dialogBuilder.BuildPregnantWeeksDialog(context.Activity.AsMessageActivity());
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

        #endregion


        #region Child name



        private void CallConfirmIfHaveChoosenName(IDialogContext context)
        {
            PromptDialog.Confirm(context, ResumeAfterConfirmationIfHaveChoosenName,
                $"Have you chosen a name for your baby?",
                $"Sorry I don't understand - try again!\n Have you chosen a name for your baby?");

        }

        private async Task ResumeAfterConfirmationIfHaveChoosenName(IDialogContext context, IAwaitable<bool> result)
        {
            bool confirmation = await result;

            switch (confirmation)
            {
                case true:
                    await context.PostAsync(
                        "Perfect. Can you please type out the full name you have chosen for your baby?\nPlease check that you've spelled it correctly! 😉");
                    break;
                default:
                    await context.PostAsync("You can try this website for some  ideas for names");
                    await context.PostAsync("https://smartstart.services.govt.nz/news/baby-names");
                    await context.PostAsync("Let me know when you have found something you like 🙂");
                    break;
            }

            ChildNameDialog childNameDialog = _dialogBuilder.BuildChildNameDialog(context.Activity.AsMessageActivity());
            context.Call(childNameDialog, AfterShowingChildNameDialog);
        }

        private async Task AfterShowingChildNameDialog(IDialogContext context, IAwaitable<string> result)
        {
            string name = await result;
            _botDataService.SetPreferredName(context, name);
            await context.PostAsync($"{name} is an excellent choice! 😍");
            var showConfirmGenderActionsDialog = _dialogBuilder.BuildShowSuggestedActionsDialog(
                context.Activity.AsMessageActivity(),
                "Do you know the gender of your child?", "Boy", "Girl", "Unsure");
            context.Call(showConfirmGenderActionsDialog, AfterShowingConfirmGenderActions);

        }

        #endregion

        #region Gender

        private async Task AfterShowingConfirmGenderActions(IDialogContext context, IAwaitable<object> result)
        {
            await result;
            context.Wait(ResumeConfirmGender);
        }

        private async Task ResumeConfirmGender(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            await context.PostAsync("Great. We're almost done with the questions.");
            var showConfirmDescendantActionsDialog = _dialogBuilder.BuildShowSuggestedActionsDialog(
                context.Activity.AsMessageActivity(),
                "Is your baby the descendant of a New Zealand Māori?", "Yes", "No", "Unsure");
            context.Call(showConfirmDescendantActionsDialog, AfterShowingConfirmDescendantActions);
        }


        #endregion

        #region Descendant

        private async Task AfterShowingConfirmDescendantActions(IDialogContext context, IAwaitable<object> result)
        {
            await result;
            context.Wait(ResumeConfirmDescendant);
        }

        private async Task ResumeConfirmDescendant(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            await context.PostAsync("OK.");
            var showConfirmPhoneActionsDialog = _dialogBuilder.BuildShowSuggestedActionsDialog(
                context.Activity.AsMessageActivity(),
                "Is your phone number still 021 123 456 789?", "Yes", "No", "I don't have a phone");
            context.Call(showConfirmPhoneActionsDialog, AfterShowingConfirmPhoneActions);
        }



        #endregion

        #region Phone

        private async Task AfterShowingConfirmPhoneActions(IDialogContext context, IAwaitable<object> result)
        {
            await result;
            context.Wait(ResumeConfirmPhone);
        }

        private async Task ResumeConfirmPhone(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            string message = (await result)?.Text;

            if (message == "Yes")
            {
                await context.PostAsync("Great!");
                ShowAddressDialog(context);
            }
            else if (message == "No")
            {
                PromptDialog.Text(context, ResumeAfterConfirmationPhoneText,
                    "Please tell me your current phone number:", "Sorry I didn't get that - try again!");

            }
            else
            {
                ShowAddressDialog(context);

            }

        }

        private void ShowAddressDialog(IDialogContext context)
        {
            PromptDialog.Confirm(context, ResumeAfterConfirmationAddress,
                "And do you still live at 6 Hathaway Ave, Karori in Wellington?",
                "Sorry I didn't get that - try again!");
        }

        private async Task ResumeAfterConfirmationPhoneText(IDialogContext context, IAwaitable<string> result)
        {
            string phone = await result;
            // TODO: check phone number
            await context.PostAsync($"You phone number is updated to {phone}");
            ShowAddressDialog(context);

        }




        #endregion


        #region Address

        private async Task ResumeAfterConfirmationAddress(IDialogContext context, IAwaitable<bool> result)
        {
            bool confirmation = await result;

            switch (confirmation)
            {
                case true:
                    var name = _botDataService.GetPreferredName(context);
                    await ShowConfirmFinacialSupportDialog(context, name);
                    break;
                default:
                    PromptDialog.Text(context, ResumeAfterConfirmationAddressText,
                        "Please tell me your current address:", "Sorry I didn't get that - try again!");
                    break;
            }
        }

        private async Task ShowConfirmFinacialSupportDialog(IDialogContext context, string name)
        {
            await context.PostAsync(
                $"Fantastic! 😁\nOne last thing\nWe'd love to give you a hand in financially supporting {name}");
            var showConfirmFinacialSupportActionsDialog = _dialogBuilder.BuildShowSuggestedActionsDialog(
                context.Activity.AsMessageActivity(),
                $"If you want, we can set up a savings account for {name} right now. Would you like to do that?", "Yes",
                "No",
                "Tell me more");
            context.Call(showConfirmFinacialSupportActionsDialog, AfterShowingConfirmFinacialSupportActions);
        }

        private async Task ResumeAfterConfirmationAddressText(IDialogContext context, IAwaitable<string> result)
        {
            string address = await result;
            // TODO: check address...
            await context.PostAsync($"You address is updated to {address}");
            var name = _botDataService.GetPreferredName(context);
            await ShowConfirmFinacialSupportDialog(context, name);

        }

        #endregion

        #region Account

        private async Task AfterShowingConfirmFinacialSupportActions(IDialogContext context, IAwaitable<object> result)
        {
            await result;
            context.Wait(ResumeConfirmFinacialSupport);
        }

        private async Task ResumeConfirmFinacialSupport(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            string message = (await result)?.Text;
            string name = _botDataService.GetPreferredName(context);

            switch (message)
            {
                case "Yes":
                    await ShowAccountInfo(context, name);
                    await ShowConfirmEmailDialog(context, name);
                    break;
                case "No":
                    await ShowConfirmEmailDialog(context, name);
                    break;
                case "Tell me more":
                    await context.PostAsync(
                        $"Well, I can open a savings account right now for {name} with BNZ or any other bank you choose.");
                    await context.PostAsync(
                        "The government will add $100 per month to her account. It won't cost you a cent.");
                    await context.PostAsync($"And when {name} turns 18, she can access the money we've saved up");
                    await context.PostAsync("At the current interest rates, that'll be just over NZ$ 32 000! 😮");
                    PromptDialog.Confirm(context, ResumeAfterConfirmationAccount,
                        "Should we open a savings account for him/her?", "Sorry I didn't get that - try again!");

                    break;
            }
        }

        private static async Task ShowAccountInfo(IDialogContext context, string name)
        {
            await context.PostAsync("Excellent!");
            await context.PostAsync($"{name} has been registered and has a savings account with BNZ.");
        }

        private async Task ResumeAfterConfirmationAccount(IDialogContext context, IAwaitable<bool> result)
        {
            bool confirmation = await result;
            string name = _botDataService.GetPreferredName(context);

            switch (confirmation)
            {
                case true:
                    await ShowAccountInfo(context, name);
                    break;
                default:
                    break;
            }

            await ShowConfirmEmailDialog(context, name);

        }

        private async Task ShowConfirmEmailDialog(IDialogContext context, string name)
        {
            await context.PostAsync("We're pretty much done here");
            _botDataService.SetEmail(context, "jane.johnson@gmail.com");
            PromptDialog.Confirm(context, ResumeAfterConfirmationEmail,
                "Is jane.johnson@gmail.com still your email address?",
                "Sorry I didn't get that - try again!");
        }

        #endregion

        #region Email

        private async Task ResumeAfterConfirmationEmail(IDialogContext context, IAwaitable<bool> result)
        {
            bool confirmation = await result;

            switch (confirmation)
            {
                case true:
                    await ShowConfirmOtherRequestDialog(context);
                    break;
                default:
                    PromptDialog.Text(context, ResumeAfterConfirmationEmailText,
                        "Please tell me your current email address:", "Sorry I didn't get that - try again!");

                    break;
            }
        }

        private async Task ShowConfirmOtherRequestDialog(IDialogContext context)
        {
            string email = _botDataService.GetEmail(context);
            await context.PostAsync($"Great. We'll send you the details via email as well to {email}");
            var showConfirmOtherRequestActionsDialog = _dialogBuilder.BuildShowSuggestedActionsDialog(
                context.Activity.AsMessageActivity(),
                "Is there anything else I can help you with?", "No, thanks", "Yes, I need more help");
            context.Call(showConfirmOtherRequestActionsDialog, AfterShowingConfirmOtherRequestActions);
        }

        private async Task ResumeAfterConfirmationEmailText(IDialogContext context, IAwaitable<string> result)
        {
            string email = await result;
            // TODO: check address...
            Regex regex = new Regex(@"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
            if (regex.IsMatch(email))
            {
                _botDataService.SetEmail(context, email);
                await context.PostAsync($"You email address is updated to {email}");
                await ShowConfirmOtherRequestDialog(context);
            }
            else
            {
                PromptDialog.Text(context, ResumeAfterConfirmationEmailText,
                    "Your input is not a valid email address. Please tell me your current email address:", "Sorry I didn't get that - try again!");
            }
        }


        #endregion


        #region Other requests

        private async Task AfterShowingConfirmOtherRequestActions(IDialogContext context, IAwaitable<object> result)
        {
            await result;
            context.Wait(ResumeConfirmOtherRequest);
        }

        private async Task ResumeConfirmOtherRequest(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            string message = (await result)?.Text;

            switch (message)
            {
                case "No, thanks":
                    await SendGoodbye(context);
                    break;
                case "Yes, I need more help":
                    await SendFurtherInformaiton(context);
                    break;
            }

            context.Wait(MessageReceivedAsync);
        }

        private static async Task SendGoodbye(IDialogContext context)
        {
            await context.PostAsync("Great!");
            await context.PostAsync("Best of luck with the rest of your pregnancy and take care");
            await context.PostAsync("😃");
            await context.PostAsync("Goodbye!");
        }

        private static async Task SendFurtherInformaiton(IDialogContext context)
        {
            await context.PostAsync("Please call our hotline: 021-111-124");
            await context.PostAsync("Goodbye!");
        }



        #endregion
    }
}