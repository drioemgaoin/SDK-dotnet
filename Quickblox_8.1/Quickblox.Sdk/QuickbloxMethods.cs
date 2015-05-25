﻿using System;

namespace Quickblox.Sdk.Core
{
    public static class QuickbloxMethods
    {
        public const string AccountMethod = "/account_settings.json";

        #region Auth

        public const string SessionMethod = "/session.json";

        public const string LoginMethod = "/login.json";

        #endregion

        #region Chat

        public const string CreateDialogMethod = "/chat/Dialog.json";

        public const string GetDialogsMethod = "/chat/Dialog.json";

        public const string UpdateDialogMethod = "/chat/Dialog/{0}.json";

        public const string DeleteDialogMethod = "/chat/Dialog/{0}.json";
        
        public const string GetMessagesMethod = "/chat/Message.json?chat_dialog_id={0}";

        public const string CreateMessageMethod = "/chat/Message.json";

        public const string UpdateMessageMethod = "/chat/Message/{0}.json";

        public const string DeleteMessageMethod = "/chat/Message/{0}.json";

        #endregion

        #region Messages

        public const string PushTokenMethod = "/push_tokens.json";

        public const string DeletePushTokenMethod = "/push_tokens/{0}.json";

        public const string SubscriptionsMethod = "/subscriptions.json";

        public const string DeleteSubscriptionsMethod = "/subscriptions/{0}.json";

        public const string EventsMethod = "/events.json";

        public const string GetEventByIdMethod = "/events/{0}.json";

        public const string EditEventMethod = "/events/{0}.json";

        public const string DeleteEventMethod = "/events/{0}.json";

        #endregion

        #region Users

        public const string UsersMethod = "/users.json";

        public const string GetUserMethod = "/users/{0}.json";

        public const string GetUserByLoginMethod = "/users/by_login.json";

        public const string GetUserByEmailMethod = "/users/by_email.json";

        public const string GetUserByTagsMethod = "/users/by_tags.json";
        
        public const string GetUserByFullMethod = "/users/by_full_name.json";

        public const string GetUserByFacebookIdMethod = "/users/by_facebook_id.json";

        public const string GetUserByTwitterIdMethod = "/users/by_twitter_id.json";

        public const string UpdateUserMethod = "/users/{0}.json";

        public const string DeleteUserMethod = "/users/{0}.json";

        public const string GetUserByExternalUserMethod = "/users/external/{0}.json";

        public const string DeleteUserByExternalUserMethod = "/users/external/{0}.json";

        public const string UserPasswordResetMethod = "/users/password/reset.json";

        #endregion

        #region Content

        public const string CreateFileMethod = "/blobs.json";

        public const string GetFilesMethod = "/blobs.json";

        public const string GetTaggedFilesMethod = "/blobs/tagged.json";

        public const string UploadMethod = "{0}";

        public const string CompleteUploadByFileIdMethod = "/blobs/{0}/complete";

        public const string GetFileByIdMethod = "/blobs/{0}";

        public const string DownloadFileByIdMethod = "/blobs/{0}";

        public const string GetFileByIdReadOnlyMethod = "/blobs/{0}/getblobobjectbyid";

        public const string EditFileMethod = "/blobs/{0}";

        public const string DeleteFileMethod = "/blobs/{0}";

        #endregion

        #region CustomObject
        
        public const string RetriveObjectsByIdsMethod = "/data/{0}/{1}";
        public const string RetriveObjectsMethod = "/data/{0}";
        public const string CreateCustomObjectMethod = "/data/{0}";
        public const string CreateMultiCustomObjectMethod = "/data/{0}/multi";
        public const string UpdateCustomObjectMethod = "/data/{0}/{1}";
        public const string UpdateMultiCustomObjectMethod = "/data/{0}/multi";
        public const string DeleteCustomObjectMethod = "/data/{0}/{1}";

        #endregion
    }
}
