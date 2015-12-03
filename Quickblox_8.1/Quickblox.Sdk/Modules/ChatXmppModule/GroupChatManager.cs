﻿using System;
using System.Collections.Generic;
using System.Linq;
using Quickblox.Sdk.GeneralDataModel.Models;
using Quickblox.Sdk.Modules.ChatXmppModule.Interfaces;
using Quickblox.Sdk.Modules.ChatXmppModule.Models;
using XMPP.tags.jabber.client;

namespace Quickblox.Sdk.Modules.ChatXmppModule
{
    //TODO: use conditions if something was different
    #if Xamarin
    #endif

    /// <summary>
    /// Manager for group dialogs.
    /// </summary>
    public class GroupChatManager : IGroupChatManager
    {
        #region Fields

        private IQuickbloxClient quickbloxClient;
        private XMPP.Client xmppClient;
        private readonly string groupJid;
        private readonly string dialogId;

        /// <summary>
        /// Event when a new group message is received.
        /// </summary>
        public event EventHandler<Message> OnMessageReceived;

        #endregion

        #region Ctor

        internal GroupChatManager(IQuickbloxClient quickbloxClient, XMPP.Client client, string groupJid, string dialogId)
        {
            this.quickbloxClient = quickbloxClient;
            xmppClient = client;
            this.groupJid = groupJid;
            this.dialogId = dialogId;
            quickbloxClient.ChatXmppClient.OnMessageReceived += MessagesClientOnOnMessageReceived;
        }

        #endregion

        #region IGroupChatManager members

        /// <summary>
        /// Sends a group chat message.
        /// </summary>
        /// <param name="message">Message text.</param>
        /// <returns>Is operation successful</returns>
        public bool SendMessage(string message)
        {
            var msg = CreateNewMessage();

            var body = new body { Value = message };

            var extraParams = new ExtraParams();
            extraParams.Add(new SaveToHistory { Value = "1" });
            extraParams.Add(new DialogId { Value = dialogId });

            msg.Add(body, extraParams);

            if (!xmppClient.Connected)
            {
                xmppClient.Connect();
                return false;
            }

            xmppClient.Send(msg);
            return true;
        }

        /// <summary>
        /// Sends notification group chat message that this group was created.
        /// </summary>
        /// <param name="occupantsIds">Created group occupants IDs</param>
        /// <returns>Is operation successful</returns>
        public bool NotifyAboutGroupCreation(IList<int> occupantsIds)
        {
            return NotifyAbountGroupOccupants(occupantsIds, true);
        }

        /// <summary>
        /// Sends notification group chat message that new occupants were added to the group.
        /// </summary>
        /// <param name="addedOccupantsIds">Added occupants IDs</param>
        /// <returns>Is operation successful</returns>
        public bool NotifyAboutGroupUpdate(IList<int> addedOccupantsIds)
        {
            return NotifyAbountGroupOccupants(addedOccupantsIds, false);
        }

        /// <summary>
        /// Sends notification group chat message that group chat image has been changed.
        /// </summary>
        /// <param name="groupImageUrl">New group chat image URL</param>
        /// <returns>Is operation successful</returns>
        public bool NotifyGroupImageChanged(string groupImageUrl)
        {
            var msg = CreateNewMessage();

            var body = new body { Value = "Notification message" };

            var extraParams = new ExtraParams();
            extraParams.Add(new SaveToHistory { Value = "1" });
            extraParams.Add(new DialogId { Value = dialogId });
            extraParams.Add(new NotificationType { Value = ((int)NotificationTypes.GroupUpdate).ToString() });
            extraParams.Add(new RoomPhoto { Value = groupImageUrl });

            msg.Add(body, extraParams);

            if (!xmppClient.Connected)
            {
                xmppClient.Connect();
                return false;
            }

            xmppClient.Send(msg);
            return true;
        }

        /// <summary>
        /// Sends notification group chat message that group chat name has been changed.
        /// </summary>
        /// <param name="groupName">New group chat name</param>
        /// <returns>Is operation successful</returns>
        public bool NotifyGroupNameChanged(string groupName)
        {
            var msg = CreateNewMessage();

            var body = new body { Value = "Notification message" };

            var extraParams = new ExtraParams();
            extraParams.Add(new SaveToHistory { Value = "1" });
            extraParams.Add(new DialogId { Value = dialogId });
            extraParams.Add(new NotificationType { Value = ((int)NotificationTypes.GroupUpdate).ToString() });
            extraParams.Add(new RoomName { Value = groupName });

            msg.Add(body, extraParams);

            if (!xmppClient.Connected)
            {
                xmppClient.Connect();
                return false;
            }

            xmppClient.Send(msg);
            return true;
        }

        /// <summary>
        /// Joins XMPP chat room.
        /// This is obligatory for group chat message sending/receiving.
        /// </summary>
        /// <param name="nickName">User nickname in XMPP room.</param>
        public void JoinGroup(string nickName)
        {
            string fullJid = string.Format("{0}/{1}", groupJid, nickName);

            var presense = new presence {to = fullJid};
            X x = new X();
            x.Add(new History() {Maxstanzas = "0"});
            presense.Add(x);

            xmppClient.Send(presense);
        }

        #endregion

        #region Private methods

        private message CreateNewMessage()
        {
            return new message
            {
                to = groupJid,
                type = message.typeEnum.groupchat,
                id = MongoObjectIdGenerator.GetNewObjectIdString()
            };
        }

        private bool NotifyAbountGroupOccupants(IList<int> occupantsIds, bool isGroupCreation)
        {
            var msg = CreateNewMessage();

            var body = new body {Value = "Notification message."};

            string occupantsIdsString = occupantsIds.Aggregate("", (current, occupantsId) => current + occupantsId.ToString() + ",");
            occupantsIdsString = occupantsIdsString.Trim(',');

            var extraParams = new ExtraParams();
            extraParams.Add(new SaveToHistory {Value = "1"});
            extraParams.Add(new DialogId {Value = dialogId});
            extraParams.Add(new NotificationType {Value = ((int) (isGroupCreation ? NotificationTypes.GroupCreate : NotificationTypes.GroupUpdate)).ToString()});
            extraParams.Add(new OccupantsIds {Value = occupantsIdsString});

            msg.Add(body, extraParams);

            if (!xmppClient.Connected)
            {
                xmppClient.Connect();
                return false;
            }

            xmppClient.Send(msg);
            return true;
        }

        private void MessagesClientOnOnMessageReceived(object sender, Message message1)
        {
            if (message1.From.Contains(groupJid))
            {
                if (string.IsNullOrEmpty(message1.MessageText)) return; // for IsTyping/PausedTyping messages

                var handler = OnMessageReceived;
                if (handler != null)
                    handler(this, message1);
            }
        }

        #endregion


    }
}