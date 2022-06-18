using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Grpc.Core;
using SimpleChatApp.GrpcService;

namespace XamarinChatApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
        private string login;
        private string password;

        public ChatService.ChatServiceClient chatServiceClient;

        public LoginPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        public string Login
        {
            get { return login; }
            set
            {
                login = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get { return password; }
            set
            {
                password = value;
                OnPropertyChanged();
            }
        }

        private void GoRegister(object sender, EventArgs e)
        {
            Navigation.PushAsync(new RegistrationPage());
        }

        private async void LoginButton(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Login))
            {
                await DisplayAlert("Warning", "Login can`t be empty", "Ok");
                return;
            }
            else if (string.IsNullOrEmpty(Password))
            {
                await DisplayAlert("Warning", "Password can`t be empty", "Ok");
                return;
            }

            try
            {
                chatServiceClient = new ChatService.ChatServiceClient(new Channel("10.0.2.2", 30051, ChannelCredentials.Insecure)); // 10.0.2.2 android emulator // 127.0.0.1 UWP

                var userData = new SimpleChatApp.GrpcService.UserData()
                {
                    Login = Login,
                    PasswordHash = SimpleChatApp.CommonTypes.SHA256.GetStringHash(Password)
                };
                var authorizationData = new AuthorizationData()
                {
                    ClearActiveConnection = true,
                    UserData = userData
                };
                var ans = await chatServiceClient.LogInAsync(authorizationData);

                switch (ans.Status)
                {
                    case SimpleChatApp.GrpcService.AuthorizationStatus.AuthorizationSuccessfull:
                        await Navigation.PushAsync(new ChatPage(chatServiceClient, ans.Sid.Guid_));
                        break;
                    case SimpleChatApp.GrpcService.AuthorizationStatus.WrongLoginOrPassword:
                        await DisplayAlert("Information", "Wrong login or password", "OK");
                        break;
                    case SimpleChatApp.GrpcService.AuthorizationStatus.AnotherConnectionActive:
                        await DisplayAlert("Information", "Another connection active", "OK");
                        break;
                    case SimpleChatApp.GrpcService.AuthorizationStatus.AuthorizationError:
                        await DisplayAlert("Information", "Authorization error", "OK");
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            catch (RpcException exep)
            {
                await DisplayAlert("Error", $"ErrorCode: {exep.Status.StatusCode}", "Ok");
            }
        }
    }
}