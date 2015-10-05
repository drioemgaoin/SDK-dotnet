﻿using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.Logger;
using QMunicate.Core.MessageService;
using QMunicate.Helper;
using QMunicate.Models;
using Quickblox.Sdk;
using Quickblox.Sdk.Modules.MessagesModule.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Quickblox.Sdk.GeneralDataModel.Filters;
using Quickblox.Sdk.GeneralDataModel.Models;
using Quickblox.Sdk.Modules.ChatModule.Requests;
using Message = Quickblox.Sdk.GeneralDataModel.Models.Message;

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
            while (NavigationService.BackStack.Count > 1)
            {
                NavigationService.BackStack.RemoveAt(NavigationService.BackStack.Count - 1);
            }

            await Initialize(e.Parameter as string);
        }

        public override void OnNavigatedFrom(NavigationEventArgs e)
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

        private async Task Initialize(string dialogId)
        {
            IsLoading = true;

            var dialogManager = ServiceLocator.Locator.Get<IDialogsManager>();
            dialog = dialogManager.Dialogs.FirstOrDefault(d => d.Id == dialogId);

            if (dialog == null) return;

            currentUserId = SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.CurrentUserId);

            ChatName = dialog.Name;
            ChatImage = dialog.Image;
            NumberOfMembers = dialog.OccupantIds.Count;

            await QmunicateLoggerHolder.Log(QmunicateLogLevel.Debug, string.Format("Initializing GroupChat page. CurrentUserId: {0}. Group JID: {1}.", currentUserId, dialog.XmppRoomJid));

            await LoadMessages(dialogId);

            groupChatManager = QuickbloxClient.MessagesClient.GetGroupChatManager(dialog.XmppRoomJid, dialog.Id);
            groupChatManager.OnMessageReceived += ChatManagerOnOnMessageReceived;
            groupChatManager.JoinGroup(currentUserId.ToString());

            IsLoading = false;
        }

        private async Task LoadMessages(string dialogId)
        {
            var retrieveMessagesRequest = new RetrieveMessagesRequest();
            var aggregator = new FilterAggregator();
            aggregator.Filters.Add(new FieldFilter<string>(() => new Message().ChatDialogId, dialogId));
            aggregator.Filters.Add(new SortFilter<long>(SortOperator.Desc, () => new Message().DateSent));
            retrieveMessagesRequest.Filter = aggregator;

            var response = await QuickbloxClient.ChatClient.GetMessagesAsync(retrieveMessagesRequest);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Messages.Clear();
                for (int i = response.Result.Items.Length - 1; i >= 0; i--)
                {
                    var msg = MessageVm.FromMessage(response.Result.Items[i], currentUserId);
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
            await dialogsManager.UpdateDialogLastMessage(dialog.Id, NewMessageText, DateTime.Now);

            NewMessageText = "";
        }

        private async void ChatManagerOnOnMessageReceived(object sender, Message message)
        {
            var messageVm = new MessageVm
            {
                MessageText = message.MessageText,
                MessageType = MessageType.Incoming,
                DateTime = DateTime.Now
            };

            await GenerateProperNotificationMessages(message, messageVm);

            var senderId = GetSenderIdFromJid(message.From);
            if (senderId == currentUserId)
            {
                messageVm.MessageType = MessageType.Outgoing;
            }

            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                Messages.Add(messageVm);
                if (message.NotificationType == NotificationTypes.GroupUpdate)
                    await UpdateGroupInfo(message);
            });
        }

        private async Task GenerateProperNotificationMessages(Message message, MessageVm messageVm)
        {
            if (message.NotificationType == NotificationTypes.GroupCreate)
            {
                messageVm.MessageText = await BuildGroupCreateMessage(message);
            }

            if (message.NotificationType == NotificationTypes.GroupUpdate)
            {
                messageVm.MessageText = await BuildGroupUpdateMessage(message);
            }
        }

        private async Task<string> BuildGroupCreateMessage(Message message)
        {
            int senderId = GetSenderId(message);
            var cachingQbClient = ServiceLocator.Locator.Get<ICachingQuickbloxClient>();
            var senderUser = await cachingQbClient.GetUserById(senderId);

            var addedUsersBuilder = new StringBuilder();
            List<int> occupantsIds = ConvertStringToIntArray(message.OccupantsIds);
            foreach (var userId in occupantsIds.Where(o => o != senderId))
            {
                var user = await cachingQbClient.GetUserById(userId);
                if(user != null)
                    addedUsersBuilder.Append(user.FullName + ", ");
            }
            if (addedUsersBuilder.Length > 1)
                addedUsersBuilder.Remove(addedUsersBuilder.Length - 2, 2);

            return string.Format("{0} has added {1} to the group chat", senderUser == null ? "" : senderUser.FullName, addedUsersBuilder);
        }

        private async Task<string> BuildGroupUpdateMessage(Message message)
        {
            int senderId = GetSenderId(message);
            var cachingQbClient = ServiceLocator.Locator.Get<ICachingQuickbloxClient>();
            var senderUser = await cachingQbClient.GetUserById(senderId);

            string messageText = null;
            if (!string.IsNullOrEmpty(message.RoomName))
                messageText = string.Format("{0} has changed the chat name to {1}", senderUser == null ? "" : senderUser.FullName, message.RoomName);

            if(!string.IsNullOrEmpty(message.RoomPhoto))
                messageText = string.Format("{0} has changed the chat picture", senderUser == null ? "" : senderUser.FullName);

            if (!string.IsNullOrEmpty(message.OccupantsIds))
            {
                var addedUsersBuilder = new StringBuilder();
                List<int> occupantsIds = ConvertStringToIntArray(message.OccupantsIds);
                foreach (var userId in occupantsIds.Where(o => o != senderId))
                {
                    var user = await cachingQbClient.GetUserById(userId);
                    if (user != null)
                        addedUsersBuilder.Append(user.FullName + ", ");
                }
                if (addedUsersBuilder.Length > 1)
                    addedUsersBuilder.Remove(addedUsersBuilder.Length - 2, 2);

                messageText = string.Format("{0} has added {1} to the group chat", senderUser == null ? "" : senderUser.FullName, addedUsersBuilder);
            }

            return messageText;
        }


        private int GetSenderId(Message message)
        {
            if (message.SenderId != 0) return message.SenderId;

            return GetSenderIdFromJid(message.From);
        }

        private List<int> ConvertStringToIntArray(string occupantsIdsString)
        {
            var occupantsIds = new List<int>();
            if (string.IsNullOrEmpty(occupantsIdsString)) return occupantsIds;

            var idsStrings = occupantsIdsString.Split(',');
            foreach (string idsString in idsStrings)
            {
                int id;
                if(int.TryParse(idsString, out id))
                    occupantsIds.Add(id);
            }

            return occupantsIds;
        }

        private async Task UpdateGroupInfo(Message notificationMessage)
        {
            if (!string.IsNullOrEmpty(notificationMessage.RoomPhoto))
            {
                var imagesService = ServiceLocator.Locator.Get<IImageService>();
                ChatImage = await imagesService.GetPublicImage(notificationMessage.RoomPhoto);
            }

            if (!string.IsNullOrEmpty(notificationMessage.RoomName))
            {
                ChatName = notificationMessage.RoomName;
            }
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
