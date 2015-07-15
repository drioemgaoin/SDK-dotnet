﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quickblox.Sdk.Cryptographic;
using Quickblox.Sdk.Modules.AuthModule;
using Quickblox.Sdk.Modules.AuthModule.Models;
using Quickblox.Sdk.Modules.ChatModule;
using Quickblox.Sdk.Modules.ContentModule;
using Quickblox.Sdk.Modules.CustomObjectModule;
#if !Xamarin
using Quickblox.Sdk.Modules.MessagesModule;
#endif
using Quickblox.Sdk.Modules.NotificationModule;
using Quickblox.Sdk.Modules.UsersModule;

namespace Quickblox.Sdk
{
    public interface IQuickbloxClient
    {
        ICryptographicProvider CryptographicProvider { get; }

        ContentClient ContentClient { get; }

        AuthorizationClient CoreClient { get; }

        ChatClient ChatClient { get; }

        UsersClient UsersClient { get; }

        NotificationClient NotificationClient { get; }
#if !Xamarin
        MessagesClient MessagesClient { get; }
#endif
        CustomObjectsClient CustomObjectsClient { get; }

        DateTime LastRequest { get; }

        string ApiEndPoint { get; }

        string ChatEndpoint { get; }

        string Token { get; }

        int CurrentUserId { get; }

        Task GetAccountSettingsAsync(string accountKey);
    }

    internal interface ISessionHolder
    {
        string Token { get; }
        int CurrentUserId { get; }
        void SetSession(Session session);
    }
}