﻿using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.MessageService;
using QMunicate.Helper;
using QMunicate.Models;
using Quickblox.Sdk.Modules.MessagesModule.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using QMunicate.Core.Logger;
using Quickblox.Sdk;
using Quickblox.Sdk.Logger;

namespace QMunicate.ViewModels
{
    public class GroupChatViewModel : ViewModel
    {
        #region Fields

        private string newMessageText;
        private string chatName;
        private ImageSource chatImage;
        private DialogVm dialog;
        private IGroupChatManager groupChatManager;
        private int numberOfMembers;
        private int currentUserId;

        #endregion

        #region Ctor

        public GroupChatViewModel()
        {
            Messages = new ObservableCollection<MessageVm>();
            SendCommand = new RelayCommand(SendCommandExecute, () => !IsLoading);
            ShowGroupInfoCommand = new RelayCommand(ShowGroupInfoCommandExecute, () => !IsLoading);
        }

        #endregion

        #region Properties

        public ObservableCollection<MessageVm> Messages { get; set; }

        public string NewMessageText
        {
            get { return newMessageText; }
            set { Set(ref newMessageText, value); }
        }

        public string ChatName
        {
            get { return chatName; }
            set { Set(ref chatName, value); }
        }

        public ImageSource ChatImage
        {
            get { return chatImage; }
            set { Set(ref chatImage, value); }
        }

        public int NumberOfMembers
        {
            get { return numberOfMembers; }
            set { Set(ref numberOfMembers, value); }
        }

        public RelayCommand SendCommand { get; private set; }

        public RelayCommand ShowGroupInfoCommand { get; private set; }

        #endregion

        #region Navigation

        public async override void OnNavigatedTo(NavigationEventArgs e)
        {
            var chatParameter = e.Parameter as ChatNavigationParameter;
            if (chatParameter == null) return;

            while (NavigationService.BackStack.Count > 1)
            {
                NavigationService.BackStack.RemoveAt(NavigationService.BackStack.Count - 1);
            }

            await Initialize(chatParameter);
        }

        public override void OnNavigatedFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (groupChatManager != null) groupChatManager.OnMessageReceived -= ChatManagerOnOnMessageReceived;
        }

        #endregion

        #region Base members

        protected override void OnIsLoadingChanged()
        {
            SendCommand.RaiseCanExecuteChanged();
            ShowGroupInfoCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Private methods

        private async Task Initialize(ChatNavigationParameter chatParameter)
        {
            IsLoading = true;

            currentUserId = SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.CurrentUserId);

            if (chatParameter.Dialog != null)
            {
                dialog = chatParameter.Dialog;
                ChatName = chatParameter.Dialog.Name;
                ChatImage = chatParameter.Dialog.Image;
                NumberOfMembers = chatParameter.Dialog.OccupantIds.Count;

                await QmunicateLoggerHolder.Log(QmunicateLogLevel.Debug, string.Format("Initializing GroupChat page. CurrentUserId: {0}. Group JID: {1}.", currentUserId, dialog.XmppRoomJid));

                groupChatManager = QuickbloxClient.MessagesClient.GetGroupChatManager(dialog.XmppRoomJid, chatParameter.Dialog.Id);
                groupChatManager.OnMessageReceived += ChatManagerOnOnMessageReceived;
                groupChatManager.JoinGroup(currentUserId.ToString());

                if(!string.IsNullOrEmpty(chatParameter.Dialog.Id))
                    await LoadMessages(chatParameter.Dialog.Id);
            }

            IsLoading = false;
        }

        private async Task LoadMessages(string dialogId)
        {
            var response = await QuickbloxClient.ChatClient.GetMessagesAsync(dialogId);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Messages.Clear();
                foreach (Quickblox.Sdk.Modules.ChatModule.Models.Message message in response.Result.Items)
                {
                    var msg = MessageVm.FromMessage(message, currentUserId);
                    Messages.Add(msg);
                }
            }
        }

        private async void SendCommandExecute()
        {
            if (string.IsNullOrWhiteSpace(NewMessageText)) return;

            bool isMessageSent = groupChatManager.SendMessage(NewMessageText);

            if (!isMessageSent)
            {
                var messageService = ServiceLocator.Locator.Get<IMessageService>();
                await messageService.ShowAsync("Message", "Failed to send a message");
                return;
            }

            var dialogsManager = ServiceLocator.Locator.Get<IDialogsManager>();
            await dialogsManager.UpdateDialog(dialog.Id, NewMessageText, DateTime.Now);

            NewMessageText = "";
        }

        private void ChatManagerOnOnMessageReceived(object sender, Quickblox.Sdk.Modules.MessagesModule.Models.Message message)
        {
            var messageVm = new MessageVm
            {
                MessageText = message.MessageText,
                MessageType = MessageType.Incoming,
                DateTime = DateTime.Now
            };

            var senderId = GetSenderIdFromJid(message.From);
            if (senderId == currentUserId)
            {
                messageVm.MessageType = MessageType.Outgoing;
            }


            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Messages.Add(messageVm));
        }

        private int GetSenderIdFromJid(string jid)
        {
            int senderId;
            var jidParts = jid.Split('/');
            if(int.TryParse(jidParts.Last(), out senderId))
                return senderId;

            return 0;
        }

        private void ShowGroupInfoCommandExecute()
        {
            NavigationService.Navigate(ViewLocator.GroupInfo, dialog.Id);
        }

        #endregion
    }
}