using AOIClient.Interfaces;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AOIClient.ViewModel
{
    public class PropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                    handler(this, new PropertyChangedEventArgs(propertyName));
            }));
        }
    }
    public class ChatEntry : PropertyChangedBase
    {
        public string User { get; set; }
        public DateTime DateTime { get; set; }

        public int Index { get; set; }

        public string Message { get; set; }
    }

    public class AOIChatViewModel : IAOIChatViewModel
    {
        #region Properties&Attributes
        public DelegateCommand<object> AttemptRegisterCommand { get; set; }
        public DelegateCommand<object> AttemptConnectCommand { get; set; }
        public DelegateCommand<object> SendMessageCommand { get; set; }        
        public string ID { get; set; }
        public string Password { get; set; }
        public string Message { get; set; }        

        protected bool Continue { get; set; } = true;
        private bool ActivatingTradeInterface = false;
        private static object PreparingHub = new object();
        private string Connection;
        protected static HubConnection? ClientHubConnection { get; private set; }

        public ObservableCollection<ChatEntry> ChatEntries { get; private set; } = new ObservableCollection<ChatEntry>();
        public ObservableCollection<ChatEntry> ChatEvents { get; private set; } = new ObservableCollection<ChatEntry>();

        protected static bool HubConnected = false;                
        private int MessageCount = 0;
        private int ActivityCount = 0;

        private enum RegisterResult
        {
            rrUnknown = 0,
            rrBadParam  = -1,
            rrUserExists = -2,
            rrSucceeded = 1
        }

        private enum ConnectResult
        {
            crUnknown = 0,
            ccUserNotRegistered = -1,
            ccUserNotConnected = -2,
            ccUserAlreadyConnected = -3,
            ccUserAlreadyParametersIncorrect = -4,
            ccSucceeded = 1
        }

        private enum MessageResult
        {
            mrUnknown = 0,
            mrNotConnected = -1,
            mrSubmitted = 1
        }
        #endregion Properties&Attributes

        #region lifetime
        public AOIChatViewModel()
        {
#if DEBUG
            Connection = "https://localhost:7283/AOIChatHub";
#else            
            Connection = "https://aoitestserver.azurewebsites.net/AOIChatHub";
#endif

            AttemptRegisterCommand = new DelegateCommand<object>(TryRegister);
            AttemptConnectCommand = new DelegateCommand<object>(TryConnect);
            SendMessageCommand = new DelegateCommand<object>(SendMessage);

            ///simple thread logic to start up 
            ///and maintain the connection
            ///to the AOI Chat Interface
            Task.Factory.StartNew(
                        () =>
                        {
                            ActivityEvent("Chat Startup");

                            while (Continue)
                            {
                                try
                                {
                                    ActivateAOIChatClientInterface();
                                }
                                finally
                                {
                                    ///5sec interval for AOI interface refresh
                                    Thread.Sleep(5000);
                                }

                            }
                        }
                        , TaskCreationOptions.LongRunning
                    );

        }
        #endregion lifetime

        #region Operations
        private void TryRegister(object commandArg)
        {
            int result;

            ActivityEvent("Attempting To Register");

            if (ClientHubConnection != null)
            {                
                result = ClientHubConnection.InvokeAsync<int>("RegisterUser", ID, Password).Result;

                switch ((RegisterResult)result)
                {
                    case RegisterResult.rrUserExists:
                        ActivityEvent("User Already Exists");
                    break;

                    case RegisterResult.rrSucceeded:
                        ActivityEvent("Registration Succeeded");
                    break;

                    case RegisterResult.rrBadParam:
                        ActivityEvent("Supplied params are not good");
                    break;

                    default:
                        ActivityEvent("Unknown result attempting to register");
                    break;
                }
            }
            else
                ActivityEvent("Not connected to chat server");
        }

        private void TryConnect(object commandArg)
        {
            int result;

            ActivityEvent("Attempting To Connect to Chat Server");

            if (ClientHubConnection != null)
            {
                result = ClientHubConnection.InvokeAsync<int>("ConnectUser", ID, Password).Result;

                switch ((ConnectResult)result)
                {
                    case ConnectResult.ccSucceeded:
                        ActivityEvent("Connected to chat server");
                    break;

                    case ConnectResult.ccUserNotRegistered:
                        ActivityEvent("User not registered");
                    break;

                    case ConnectResult.ccUserAlreadyConnected:
                        ActivityEvent("User already connected");
                    break;

                    case ConnectResult.ccUserNotConnected:
                        ActivityEvent("User not connected");
                    break;

                    case ConnectResult.ccUserAlreadyParametersIncorrect:
                        ActivityEvent("Parameters not connected");
                    break;

                    default:
                        ActivityEvent("Unknown result connecting to chat server");
                    break;

                }
            }
            else
                ActivityEvent("Not connected to chat server");
        }

        private void SendMessage(object commandArg)
        {
            int result;

            ActivityEvent("Attempting to send message to chat server");

            if (ClientHubConnection != null)
            {
                result = ClientHubConnection.InvokeAsync<int>("SendMessage", Message).Result;
                
                switch((MessageResult)result)
                {
                    case MessageResult.mrSubmitted:
                        ActivityEvent("Message was submitted to server");
                    break;

                    case MessageResult.mrNotConnected:
                        ActivityEvent("You are not connected to chat server");
                    break;

                    default:
                        ActivityEvent("Unknown result sending message to chat server");
                    break;
                }

            }
            else
                ActivityEvent("Not connected to chat server");
        }

        protected virtual Task AOIChatHubDisconnect(Exception? arg)
        {
            ActivityEvent("Lost connection to chat server");

            AOIChatHubConnection_Connect().Wait();
            
            return null;
        }

        protected async Task<bool> AOIChatHubConnection_Connect()
        {
            bool Res = false;
            Exception? exception = null;

            try
            {                

                if (ClientHubConnection != null)
                {
                    ActivityEvent("Attempt to connect to chat server");

                    await ClientHubConnection.StartAsync().ContinueWith(task =>
                    {
                        Exception? except = null;

                        if (task.IsFaulted)
                        {
                            ActivityEvent("Error occurred connecting to chat server");
                            ActivityEvent(task.Exception.Message);
                            if (task.Exception != null) ActivityEvent(task.Exception.InnerException.Message);

                            exception = task.Exception;
                            except = task.Exception?.InnerException;
                            while (except != null)
                            {
                                except = except.InnerException;
                            }
                        }
                        else
                        {
                            ActivityEvent("connected to chat server");

                            Res = true;
                        }
                    }
                    );
                }

            }
            catch { }

            return Res;
        }

        private bool ActivateAOIChatClientInterface()
        {
            bool bRes = false;

            if (!ActivatingTradeInterface)
            {
                try
                {
                    ActivatingTradeInterface = true;

                    if (PrepareAOIChatActivityHub())
                        bRes = true;
                    else
                        StopHubConnection();
                }
                catch (Exception ex)
                {
                    StopHubConnection();
                }
                finally { ActivatingTradeInterface = false; }
            }
            else
                bRes = HubConnected;

            return bRes;
        }
       
        protected virtual bool PrepareAOIChatActivityHub()
        {
            Action<string,string> ExternalMessageResult = ExternalMessage;

            lock (PreparingHub)
            {
                if (ClientHubConnection == null)
                {
                    ActivityEvent("Prepare connection to chat server");

                    ClientHubConnection = new HubConnectionBuilder()
                        .WithUrl(Connection)
                        .WithAutomaticReconnect()
                        .Build();
                    ClientHubConnection.Closed += AOIChatHubDisconnect;
                    var Connect = AOIChatHubConnection_Connect();
                    Connect.Wait();

                    if (!Connect.Result)
                    {
                        ActivityEvent("Error preparing connection to chat server");
                        HubConnected = false;
                    }
                    else
                    {
                        ActivityEvent("Succeeded preparing connection to chat server");

                        HubConnected = true;
                        ClientHubConnection.On<string,string>("ChatMessage", ExternalMessageResult);
                    }
                }
                else //make sure still connected
                {
                    if (!ClientHubConnection.InvokeAsync<bool>("Active").Result)
                    {
                        ActivityEvent("Cannot communicate with chat server. Attempt to reconnect");

                        StopHubConnection();
                    }                    
                }

            }
            return HubConnected;
        }

        private void Test()
        {

        }

        protected virtual void StopHubConnection()
        {
            try
            {
                if (ClientHubConnection != null && ClientHubConnection.State == HubConnectionState.Connected)
                    ClientHubConnection.StopAsync();

                ClientHubConnection = null;
            }
            catch { }
            finally
            {
            }
        }

        public void ExternalMessage(string Message, string User)
        {
            MessageEvent(Message, User);
        }

        private void MessageEvent(string ChatMessage, string SendingUser)
        {
            try
            {                
                Application.Current?.Dispatcher?.Invoke(new Action(() =>
                {
                    MessageCount += 1;

                    if (ChatEntries.Count > 49)
                    {
                        ChatEntries.RemoveAt(0);
                    }

                    ChatEntries.Add(new ChatEntry()
                    {
                        User = SendingUser,
                        DateTime = DateTime.Now,
                        Index = MessageCount,
                        Message = ChatMessage
                    });
                }));                                    

            }
            catch { }
        }

        private void ActivityEvent(string ChatActivity)
        {            
            try
            {
                Application.Current?.Dispatcher?.Invoke(new Action(() =>
                {
                    ActivityCount += 1;

                    if (ChatEvents.Count > 49)
                    {
                        ChatEvents.RemoveAt(0);
                    }

                    ChatEvents.Add(new ChatEntry()
                    {
                        DateTime = DateTime.Now,
                        Index = ActivityCount,
                        Message = ChatActivity
                    });
                }));

            }
            catch { }
        }

        #endregion Operations
    }
}
