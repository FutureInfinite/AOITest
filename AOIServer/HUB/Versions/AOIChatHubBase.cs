using AOIServer.HUB.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;


namespace AOIServer.HUB.Versions
{
    public class AOIChatHubBase : Hub
    {
        #region Properties&Attributes
        private class Chater
        {
            public string? ID { get; set; }
            public string? Password { get; set; }
        }

        private static List<Chater> Chaters { get; set; } = new List<Chater>();

        
        class ConnectedUser
        {
            public bool Connected { get; set; }
            public Chater? LoginUser { get; set; }                    
        }

        private static ConcurrentDictionary<string, ConnectedUser?> Connections { get; set; } = new ConcurrentDictionary<string, ConnectedUser?>();

        #endregion Properties&Attributes


        #region Lifetime
        public AOIChatHubBase()
        {

        }

        ~AOIChatHubBase()
        {

        }
        #endregion Lifetime

        #region Operations
        public bool Active() => true;

        public override Task OnConnectedAsync()
        {
            try
            {
                if (!Connections.ContainsKey(Context.ConnectionId.Trim().ToLower()))
                    Connections.TryAdd(Context.ConnectionId.Trim().ToLower(), null);
            }
            catch { }


            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            ConnectedUser? ClearedConnection;
            int Counter = 0;

            try
            {
                while (!Connections.TryRemove(Context.ConnectionId.Trim().ToLower(), out ClearedConnection) &&
                    Counter < 10)
                {
                    Thread.Sleep(1000);
                    Counter += 1;
                }                
            }
            catch { }

            return base.OnDisconnectedAsync(exception);
        }

        public int RegisterUser(string ID, string Password)
        {
            int Result = 0;
            Chater? ExistChater;

            if (!string.IsNullOrEmpty(ID) && !string.IsNullOrEmpty(Password))
            {
                ExistChater = Chaters.Where(chater => chater.ID == ID).FirstOrDefault();

                if (ExistChater == null)
                {
                    Chaters.Add(new Chater()
                    {
                        ID = ID,
                        Password = Password
                    }
                    );

                    Result = 1;
                }
                else
                {
                    //user already exists
                    Result = -2;
                }
            }
            else //params incorrect
            {
                Result = -1;
            }

            return Result;
        }

        public int ConnectUser(string ID, string Password)
        {
            int Result = 0;
            Chater? ExistChater;


            ExistChater = Chaters.Where(chater => chater.ID == ID).FirstOrDefault();

            if (ExistChater != null)
            {
                var ConnectID = Context.ConnectionId.Trim().ToLower();

                if (Connections.ContainsKey(Context.ConnectionId.Trim().ToLower())) //validate connection to pusher
                {
                    if (Connections[ConnectID] != null && Connections[ConnectID]?.Connected == true)
                    {
                        //already connected
                        Result = -3;
                    }
                    else
                    {
                        //configure this user to this connection
                        Connections[ConnectID] =
                            new ConnectedUser()
                            {
                                Connected = true,
                                LoginUser = ExistChater
                            };

                        Result = 1;
                    }
                }
                else
                {
                    //user has not connected to HUB - should not happen
                    Result = -2;
                }

            }
            else //user not registered
            {
                Result = -1;
            }

            return Result;
        }

        public async Task<int> SendMessage(string Message)
        {
            int Res = 0;
            ConnectedUser? User = null;

            if (Connections.ContainsKey (Context.ConnectionId.Trim().ToLower()))
                User = Connections.Where(connection => connection.Key == Context.ConnectionId.Trim().ToLower()).FirstOrDefault().Value;

            if (User != null && User.Connected)
            {
                await Clients.All.SendAsync("ChatMessage", Message, User != null ? User.LoginUser.ID : "");
                Res = 1;
            }
            else //not connected
            {
                Res = -1;
            }
                        
            return Res;


            //was attempting to determine wqhich user had sent the message and exluce them from receiving the message notification
            //but the client event notification for callback operation is not working. Unclear as this has been
            //done before successfully

            //foreach (var Connection in Connections)
            //{
            //    if (Context.ConnectionId.Trim().ToLower() != Connection.Key && Connection.Value != null && Connection.Value.Connected)
            //    {                                        
            //        try
            //        {
            //            await Clients.Client(Connection.Key).SendAsync("ChatMessage", Message);
            //        }
            //        catch(HubException exception)
            //        {

            //        }

            //    }
            //}
           
            return Res;
        }
        #endregion Operations
    }
}
