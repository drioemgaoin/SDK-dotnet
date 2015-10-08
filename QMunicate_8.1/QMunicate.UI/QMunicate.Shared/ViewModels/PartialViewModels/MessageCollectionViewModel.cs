﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.Observable;
using QMunicate.Helper;
using Quickblox.Sdk;
using Quickblox.Sdk.GeneralDataModel.Filters;
using Quickblox.Sdk.GeneralDataModel.Models;
using Quickblox.Sdk.Modules.ChatModule.Requests;

namespace QMunicate.ViewModels.PartialViewModels
{
    /// <summary>
    /// MessageCollectionViewModel acts as a holder for current dialog's messages.
    /// Allows to load them from a dialog or to add manually.
    /// Proper notifications messages are generated automatically.
    /// </summary>
    public class MessageCollectionViewModel : ObservableObject
    {
        #region Ctor

        public MessageCollectionViewModel()
        {
            Messages = new ObservableCollection<MessageViewModel>();
        }

        #endregion

        #region Properties

        public ObservableCollection<MessageViewModel> Messages { get; set; }

        #endregion

        #region Public methods

        public async Task LoadMessages(string dialogId)
        {
            var retrieveMessagesRequest = new RetrieveMessagesRequest();
            var aggregator = new FilterAggregator();
            aggregator.Filters.Add(new FieldFilter<string>(() => new Message().ChatDialogId, dialogId));
            aggregator.Filters.Add(new SortFilter<long>(SortOperator.Desc, () => new Message().DateSent));
            retrieveMessagesRequest.Filter = aggregator;

            var quickbloxClient = ServiceLocator.Locator.Get<IQuickbloxClient>();
            int currentUserId = SettingsManager.Instance.ReadFromSettings<int>(SettingsKeys.CurrentUserId);

            var response = await quickbloxClient.ChatClient.GetMessagesAsync(retrieveMessagesRequest);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Messages.Clear();
                for (int i = response.Result.Items.Length - 1; i >= 0; i--)
                {
                    var msg = MessageViewModel.FromMessage(response.Result.Items[i], currentUserId);
                    await GenerateProperNotificationMessages(msg, response.Result.Items[i]);
                    Messages.Add(msg);
                }
            }
        }

        public async Task AddNewMessage(MessageViewModel messageViewModel)
        {
            await AddMessage(messageViewModel);
        }

        public async Task AddNewMessageAndCorrectText(MessageViewModel messageViewModel, Message originalMessage = null)
        {
            await AddMessage(messageViewModel, originalMessage);
        }

        #endregion

        #region Private methods

        private async Task AddMessage(MessageViewModel messageViewModel, Message originalMessage = null)
        {
            if (originalMessage != null)
            {
                await GenerateProperNotificationMessages(messageViewModel, originalMessage);
            }

            Messages.Add(messageViewModel);
        }

        private async Task GenerateProperNotificationMessages(MessageViewModel messageViewModel, Message originalMessage)
        {
            if (originalMessage.NotificationType == NotificationTypes.FriendsRequest)
            {
                messageViewModel.MessageText = "Contact request";
            }

            if (originalMessage.NotificationType == NotificationTypes.FriendsAccept)
            {
                messageViewModel.MessageText = "Request accepted";
            }

            if (originalMessage.NotificationType == NotificationTypes.FriendsReject)
            {
                messageViewModel.MessageText = "Request rejected";
            }

            if (originalMessage.NotificationType == NotificationTypes.GroupCreate)
            {
                messageViewModel.MessageText = await BuildGroupCreateMessage(originalMessage);
            }

            if (originalMessage.NotificationType == NotificationTypes.GroupUpdate)
            {
                messageViewModel.MessageText = await BuildGroupUpdateMessage(originalMessage);
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
                if (user != null)
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

            if (!string.IsNullOrEmpty(message.RoomPhoto))
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

        private List<int> ConvertStringToIntArray(string occupantsIdsString)
        {
            var occupantsIds = new List<int>();
            if (string.IsNullOrEmpty(occupantsIdsString)) return occupantsIds;

            var idsStrings = occupantsIdsString.Split(',');
            foreach (string idsString in idsStrings)
            {
                int id;
                if (int.TryParse(idsString, out id))
                    occupantsIds.Add(id);
            }

            return occupantsIds;
        }

        private int GetSenderId(Message message)
        {
            if (message.SenderId != 0) return message.SenderId;

            return Helpers.GetUserIdFromJid(message.From);
        }

        #endregion

    }
}
