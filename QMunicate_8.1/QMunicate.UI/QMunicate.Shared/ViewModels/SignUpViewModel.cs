﻿using QMunicate.Core.Command;
using QMunicate.Core.DependencyInjection;
using QMunicate.Core.MessageService;
using QMunicate.Helper;
using QMunicate.Models;
using Quickblox.Sdk;
using Quickblox.Sdk.GeneralDataModel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Quickblox.Logger;
using Quickblox.Sdk.Modules.ContentModule;
using Quickblox.Sdk.Modules.UsersModule.Models;
using Quickblox.Sdk.Modules.UsersModule.Requests;

namespace QMunicate.ViewModels
{
    public class SignUpViewModel : ViewModel, IFileOpenPickerContinuable
    {
        #region Fields

        private string fullName;
        private string email;
        private string password;
        private byte[] userImageBytes;
        private ImageSource userImage;
        private readonly IMessageService messageService;

        #endregion

        #region Ctor

        public SignUpViewModel()
        {
            messageService = ServiceLocator.Locator.Get<IMessageService>();

            ChoosePhotoCommand = new RelayCommand(ChoosePhotoCommandExecute, () => !IsLoading);
            SignUpCommand = new RelayCommand(SignUpCommandExecute, () => !IsLoading);
            LoginCommand = new RelayCommand(LoginCommandExecute, () => !IsLoading);
        }

        #endregion

        #region Properties

        public string FullName
        {
            get { return fullName; }
            set { Set(ref fullName, value); }
        }

        public string Email
        {
            get { return email; }
            set { Set(ref email, value); }
        }

        public string Password
        {
            get { return password; }
            set { Set(ref password, value); }
        }

        public ImageSource UserImage
        {
            get { return userImage; }
            set { Set(ref userImage, value); }
        }

        public RelayCommand ChoosePhotoCommand { get; set; }

        public RelayCommand SignUpCommand { get; set; }

        public RelayCommand LoginCommand { get; set; }

        #endregion

        #region Base members

        protected override void OnIsLoadingChanged()
        {
            ChoosePhotoCommand.RaiseCanExecuteChanged();
            SignUpCommand.RaiseCanExecuteChanged();
            LoginCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Public methods

        public async void ContinueFileOpenPicker(IReadOnlyList<StorageFile> files)
        {
            if (files != null && files.Any())
            {
                var stream = (FileRandomAccessStream) await files[0].OpenAsync(FileAccessMode.Read);
                var streamForImage = stream.CloneStream();
                
                userImageBytes = new byte[stream.Size];
                using (var dataReader = new DataReader(stream))
                {
                    await dataReader.LoadAsync((uint)stream.Size);
                    dataReader.ReadBytes(userImageBytes);
                }

                UserImage = Helpers.CreateBitmapImage(streamForImage, 100);
            }
        }

        #endregion

        #region Private methods

        private async void ChoosePhotoCommandExecute()
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".png");
#if WINDOWS_PHONE_APP
            picker.PickSingleFileAndContinue();
#endif
        }

        private async void SignUpCommandExecute()
        {
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                await messageService.ShowAsync("Message", "Please fill all empty input fields");
                return;
            }

            IsLoading = true;

            await CreateSession();

            var response = await QuickbloxClient.UsersClient.SignUpUserAsync(null, Password, email: Email, fullName: FullName);

            if (response.StatusCode == HttpStatusCode.Created)
            {
                int? userId = await Login();

                if (userId != null)
                {
                    if (userImageBytes != null)
                        await UploadUserImage(response.Result.User, userImageBytes);

                    NavigationService.Navigate(ViewLocator.Dialogs,
                                                    new DialogsNavigationParameter
                                                    {
                                                        CurrentUserId = userId.Value,
                                                        Password = Password
                                                    });
                }
            }
            else await Helpers.ShowErrors(response.Errors, messageService);

            IsLoading = false;
        }

        private async Task CreateSession()
        {
            if (!string.IsNullOrEmpty(QuickbloxClient.Token)) return;

            var sessionResponse = await QuickbloxClient.CoreClient.CreateSessionBaseAsync(ApplicationKeys.ApplicationId,
                ApplicationKeys.AuthorizationKey, ApplicationKeys.AuthorizationSecret, new DeviceRequest() { Platform = Platform.windows_phone, Udid = Helpers.GetHardwareId() });
            if (sessionResponse.StatusCode == HttpStatusCode.Created)
            {
                QuickbloxClient.Token = sessionResponse.Result.Session.Token;
            }
            else
            {
                await Helpers.ShowErrors(sessionResponse.Errors, messageService);
            }
        }

        private async Task<int?> Login()
        {
            var loginResponse = await QuickbloxClient.CoreClient.ByEmailAsync(Email, Password);
            if (loginResponse.StatusCode == HttpStatusCode.Accepted)
            {
                SettingsManager.Instance.WriteToSettings(SettingsKeys.CurrentUserId, loginResponse.Result.User.Id);
                return loginResponse.Result.User.Id;
            }
            else
            {
                await Helpers.ShowErrors(loginResponse.Errors, messageService);
                return null;
            }
        }

        private async Task UploadUserImage(User user, byte[] imageBytes)
        {
            var contentHelper = new ContentClientHelper(QuickbloxClient.ContentClient);
            var uploadId = await contentHelper.UploadPrivateImage(imageBytes);
            if (uploadId == null)
            {
                await FileLogger.Instance.Log(LogLevel.Warn, "SignUpViewModel. Failed to upload user image");
                return;
            }

            UpdateUserRequest updateUserRequest = new UpdateUserRequest { User = new UserRequest { BlobId = uploadId } };
            await QuickbloxClient.UsersClient.UpdateUserAsync(user.Id, updateUserRequest);
        }

        private void LoginCommandExecute()
        {
            NavigationService.Navigate(ViewLocator.Login);
        }

        #endregion

    }
}
