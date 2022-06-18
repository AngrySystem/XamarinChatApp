using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Grpc.Core;
using SimpleChatApp.CommonTypes;
using SimpleChatApp.GrpcService;
using Google.Protobuf.WellKnownTypes;

namespace XamarinChatApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChatPage : ContentPage
    {
        public ChatService.ChatServiceClient chatServiceClient;
        private string sid;

        public ObservableCollection<ColoredMessageData> Messages { get; set; }

        public ChatPage()
        {
            InitializeComponent();
            Messages = new ObservableCollection<ColoredMessageData>();
            BindingContext = this;
            Messages.CollectionChanged += EndScroll;
        }

        public ChatPage(ChatService.ChatServiceClient client, string guid) : this()
        {
            chatServiceClient = client;
            sid = guid;
            GetMessages().ContinueWith(Subscribe, TaskContinuationOptions.ExecuteSynchronously);
        }

        private void EndScroll(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            messageList.ScrollTo(Messages[Messages.Count - 1], ScrollToPosition.End, true);
        }

        private async Task GetMessages()
        {
            try
            {
                var to = DateTime.MaxValue;
                var from = DateTime.MinValue;

                var timeIntervalRequest = new SimpleChatApp.GrpcService.TimeIntervalRequest()
                {
                    StartTime = Timestamp.FromDateTime(from.ToUniversalTime()),
                    EndTime = Timestamp.FromDateTime(to.ToUniversalTime()),
                    Sid = new SimpleChatApp.GrpcService.Guid() { Guid_ = sid }
                };

                var ans = await chatServiceClient.GetLogsAsync(timeIntervalRequest);

                foreach (var message in ans.Logs)
                {
                    Messages.Add(new ColoredMessageData(message.Convert()));
                }

                if (Messages.Count > 0) messageList.ScrollTo(Messages[Messages.Count - 1], ScrollToPosition.End, true);
            }
            catch (RpcException exep)
            {
                await DisplayAlert("Error", $"ErrorCode: {exep.Status.StatusCode}", "Ok");
                await Navigation.PopAsync();
            }
        }

        private async Task Subscribe(Task task)
        {
            IAsyncStreamReader<SimpleChatApp.GrpcService.Messages> stream;

            try
            {
                stream = chatServiceClient.Subscribe(new SimpleChatApp.GrpcService.Guid() { Guid_ = sid }).ResponseStream;

                while (await stream.MoveNext())
                {
                    switch (stream.Current.ActionStatus)
                    {
                        case SimpleChatApp.GrpcService.ActionStatus.Allowed:
                            foreach (var messageData in stream.Current.Logs)
                            {
                                Messages.Add(new ColoredMessageData(messageData.Convert()));
                            }
                            break;
                        case SimpleChatApp.GrpcService.ActionStatus.Forbidden:
                            await DisplayAlert("Alert", $"Forbidden action!", "Ok");
                            break;
                        case SimpleChatApp.GrpcService.ActionStatus.WrongSid:
                            await DisplayAlert("Alert", $"Wrong sid!", "Ok");
                            break;
                        case SimpleChatApp.GrpcService.ActionStatus.ServerError:
                            await DisplayAlert("Alert", $"Server error!", "Ok");
                            break;
                    }
                }
            }
            catch (RpcException exep)
            {
                await DisplayAlert("Error", $"ErrorCode: {exep.Status.StatusCode}", "Ok");
                await Unsubcribe();
                await Navigation.PopAsync();
            }
        }

        private async Task Unsubcribe()
        {
            try
            {
                await chatServiceClient.UnsubscribeAsync(new SimpleChatApp.GrpcService.Guid() { Guid_ = sid });
            }
            catch (RpcException exep)
            {
                await DisplayAlert("Error", $"ErrorCode: {exep.Status.StatusCode}", "Ok");
            }
        }

        private async void sendButtonClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(messagePlace.Text)) return;

                var text = messagePlace.Text;

                var outgoingMessage = new SimpleChatApp.GrpcService.OutgoingMessage()
                {
                    Sid = new SimpleChatApp.GrpcService.Guid() { Guid_ = sid },
                    Text = text
                };
                messagePlace.Text = string.Empty;

                var ans = await chatServiceClient.WriteAsync(outgoingMessage);

                switch (ans.ActionStatus)
                {
                    case SimpleChatApp.GrpcService.ActionStatus.Allowed:
                        break;
                    case SimpleChatApp.GrpcService.ActionStatus.Forbidden:
                        await DisplayAlert("Information", "Forbidden action!", "Ok");
                        break;
                    case SimpleChatApp.GrpcService.ActionStatus.WrongSid:
                        await DisplayAlert("Information", "Wrong sid!", "Ok");
                        break;
                    case SimpleChatApp.GrpcService.ActionStatus.ServerError:
                        await DisplayAlert("Information", "Server error!", "Ok");
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            catch (RpcException exep)
            {
                await DisplayAlert("Error", $"ErrorCode: {exep.Status.StatusCode}", "Ok");
                await Navigation.PopAsync();
            }
        }
    }

    public class ColoredMessageData
    {
        public SimpleChatApp.CommonTypes.MessageData MessageData { get; set; }
        public Color Color { get; set; }

        public ColoredMessageData(SimpleChatApp.CommonTypes.MessageData _messageData)
        {
            MessageData = _messageData;
            Color = Color.FromUint((uint)MessageData.UserLogin.GetHashCode());
        }
    }
}