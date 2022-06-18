using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Grpc.Core;
using SimpleChatApp.GrpcService;

namespace XamarinChatApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RegistrationPage : ContentPage
    {
        private string login;
        private string password;
        private string rePassword;

        public ChatService.ChatServiceClient chatServiceClient;

        public RegistrationPage()
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

        public string RePassword
        {
            get { return rePassword; }
            set
            {
                rePassword = value;
                OnPropertyChanged();
            }
        }

        private async void RegisterClicked(object sender, EventArgs e)
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
            else if (string.IsNullOrEmpty(RePassword))
            {
                await DisplayAlert("Warning", "Repeat password can`t be empty", "Ok");
                return;
            }

            if (Password != RePassword)
            {
                await DisplayAlert("Dismiss", "Passwords do not match", "Ok");
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
                var ans = await chatServiceClient.RegisterNewUserAsync(userData);

                switch (ans.Status)
                {
                    case SimpleChatApp.GrpcService.RegistrationStatus.RegistrationSuccessfull:
                        await DisplayAlert("Information", "Registration successfull", "OK");
                        break;
                    case SimpleChatApp.GrpcService.RegistrationStatus.LoginAlreadyExist:
                        await DisplayAlert("Information", "Login already exist", "OK");
                        break;
                    case SimpleChatApp.GrpcService.RegistrationStatus.BadInput:
                        await DisplayAlert("Information", "Invalid symbols in username or password", "OK");
                        break;
                    case SimpleChatApp.GrpcService.RegistrationStatus.RegistratioError:
                        await DisplayAlert("Information", "Server error", "OK");
                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (ans.Status is SimpleChatApp.GrpcService.RegistrationStatus.RegistrationSuccessfull)
                {
                    await Navigation.PopAsync();
                }
            }
            catch (RpcException exep)
            {
                await DisplayAlert("Error", $"ErrorCode: {exep.Status.StatusCode}", "Ok");
            }
        }
    }
}