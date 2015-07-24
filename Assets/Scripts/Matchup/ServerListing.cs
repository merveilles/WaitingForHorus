using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using UnityEngine;

using JsonFx.Json;

namespace Network.MasterServer
{
    public class ExternalAddressFinder : IDisposable
    {
        public string URI = "";
        private WebClient WebClient;
        private JsonReader Reader;

        public string ExternalAddress { get; private set; }
        public delegate void ExternalAddressChangedHandler( string newExternalAddress );
        public event ExternalAddressChangedHandler OnExternalAddressChanged = delegate { };
        public event ExternalAddressChangedHandler OnExternalAddressError = delegate { };

        public ExternalAddressFinder( )
        {
            WebClient = new WebClient();
            WebClient.DownloadStringCompleted += ReceiveDownloadStringCompleted;
            Reader = new JsonReader();
        }

        public void FetchExternalAddress( )
        {
            WebClient.CancelAsync();
            var uri = new Uri( URI );
            WebClient.DownloadStringAsync( uri );
        }

        private void ReceiveDownloadStringCompleted( object sender, DownloadStringCompletedEventArgs args )
        {
            try
            {
                if( !args.Cancelled )
                {
                    string res = args.Result;
                    var parsed = Reader.Read<MyAddressRaw>( res );
                    if( parsed != null )
                    {
                        ExternalAddress = parsed.requester_address;
                        OnExternalAddressChanged( ExternalAddress );
                    }
                    else
                    {
                        OnExternalAddressError(
                            "Unable to get external IP address for this server." );
                    }
                }
            }
            catch( Exception e )
            {
                OnExternalAddressError( e.ToString() );
            }
        }

        public void Dispose( )
        {
            if( WebClient != null )
            {
                WebClient.CancelAsync();
                WebClient.Dispose();
            }
        }

        public class MyAddressRaw
        {
            // ReSharper disable once InconsistentNaming
            public string requester_address;
        }
    }

    // Handles fetching and parsing the list of active_servers from the master list
    // server
    public class ExternalServerList : IDisposable
    {
        public string URI = "";
        public MasterServerListRaw MasterListRaw { get; private set; }

        public delegate void MasterServerListChangedHandler( );
        public event MasterServerListChangedHandler OnMasterServerListChanged = delegate { };

        public delegate void MasterServerListFetchErrorHandler( string message );
        public event MasterServerListFetchErrorHandler OnMasterServerListFetchError = delegate { };

        private WebClient WebClient;
        private JsonReader Reader;
        //private JsonWriter Writer;

        public ExternalServerList( )
        {
            WebClient = new WebClient();
            WebClient.DownloadStringCompleted += ReceiveDownloadStringCompleted;
            Reader = new JsonReader();
            //Writer = new JsonWriter();
        }

        public void Refresh( )
        {
            WebClient.CancelAsync();
            var uri = new Uri( URI );
            WebClient.DownloadStringAsync( uri );
        }

        private void ReceiveDownloadStringCompleted( object sender, DownloadStringCompletedEventArgs args )
        {
            try
            {
                if( !args.Cancelled )
                {
                    string res = args.Result;
                    var parsed = Reader.Read<MasterServerListRaw>( res );
                    if( parsed != null )
                    {
                        MasterListRaw = parsed;
                        OnMasterServerListChanged();
                    }
                    else
                    {
                        OnMasterServerListFetchError(
                            "Couldn't get useful data from master server." +
                            " It might be broken or down for maintenance." );
                    }
                }
            }
            catch( Exception e )
            {
                OnMasterServerListFetchError( e.ToString() );
            }
        }

        public bool TryGetRandomServer( out ServerInfoRaw serverInfo )
        {
            // Dumb de dumb. Is there a better way to do this in C# if I want to
            // be able to use 'out' without getting compile errors?
            serverInfo = new ServerInfoRaw();
            if( MasterListRaw == null ) return false;
            if( !( MasterListRaw.active_servers.Length > 0 ) ) return false;
            int i = UnityEngine.Random.Range( 0, MasterListRaw.active_servers.Length );
            serverInfo = MasterListRaw.active_servers[i];
            return true;
        }

        public void Dispose( )
        {
            if( WebClient != null )
            {
                WebClient.CancelAsync();
                WebClient.Dispose();
            }
        }
    }

    public class MasterServerListRaw
    {
        //public string message;
        //public int connections;
        //public int activegames;
        // ReSharper disable once InconsistentNaming
        public ServerInfoRaw[] active_servers;
    }

    public class ServerInfoRaw
    {
        public string address;
        //public int players;
        public string map;
        //public int id;
        public int version;
        //public string message;
        public string name;
        public string game;
        public bool VersionMismatch { get { return version != Relay.Instance.CurrentVersionID; } }
    }

    // Handles notifying the master list server about a server that's being run,
    // so that it will be listed.
    public class ExternalServerNotifier : IDisposable
    {
        // TODO don't copy and paste this
        public string URI = "";
        public string DeleteURI = "";

        private WebClient WebClient;
        //private JsonReader Reader;
        private JsonWriter Writer;

        private float TimeBetweenSends = 15f;
        private float TimeUntilNextSend = 0.5f;

        public string CurrentMapName { get; set; }
        public int NumberOfPlayers { get; set; }
        public string Address { get; set; }
        public int Version { get; set; }
        public int ID { get; private set; }
        public bool HasID { get { return ID != -1; } }

        public bool IsListedOk { get; private set; }

        public string Name { get; set; }

        public delegate void ServerNotifierSuccessHandler( );
        public event ServerNotifierListedOkHandler OnServerNotifierSuccess = delegate { };

        public delegate void ServerNotifierListedOkHandler( );
        public event ServerNotifierListedOkHandler OnServerNotifierListedStateChanged = delegate { };

        public delegate void ServerNotifierErrorHandler( string message );
        public event ServerNotifierErrorHandler OnServerNotifierError = delegate { };

        private ServerInfoRaw AsServerInfoRaw
        {
            get
            {
                return new ServerInfoRaw
                {
                    address = Address,
                    //ip = Address,
                    //players = NumberOfPlayers,
                    map = CurrentMapName,
                    //id = ID,
                    version = Version,
                    name = Name,
                    game = "horus"
                };
            }
        }

        public ExternalServerNotifier( )
        {
            ID = -1;
            CurrentMapName = "?";
            NumberOfPlayers = 0;
            Address = "?";
            Version = Relay.Instance.PublicizedVersionID;
            IsListedOk = false;

            WebClient = new WebClient();
            WebClient.UploadValuesCompleted += ReceiveUploadValuesCompleted;
            //Reader = new JsonReader();
            Writer = new JsonWriter();
            Name = "Unnamed server";
        }

        public void Start( )
        {
        }

        public void Update( )
        {
            TimeUntilNextSend -= Time.deltaTime;
            if( TimeUntilNextSend <= 0f && !String.IsNullOrEmpty( Address ) )
            {
                TimeUntilNextSend = TimeBetweenSends;
                SendNow();
            }
        }

        public void SendNow( )
        {
            SendWithCmd();
        }

        private void SendWithCmd( )
        {
            WebClient.CancelAsync();
            var jsonString = Writer.Write( AsServerInfoRaw );
            var nameValueCollection = new NameValueCollection { { "data", jsonString } };
            Uri uri = new Uri( URI );
            WebClient.UploadValuesAsync( uri, nameValueCollection );
        }

        private void ReceiveUploadValuesCompleted( object sender, UploadValuesCompletedEventArgs args )
        {
            try
            {
                if( !args.Cancelled )
                {
                    if( !HasID )
                    {
                        var response = Encoding.ASCII.GetString( args.Result );
                        bool wasListed = IsListedOk;
                        ID = int.Parse( response );
                        if( HasID )
                        {
                            IsListedOk = true;
                            if( !wasListed )
                            {
                                OnServerNotifierListedStateChanged();
                            }
                            OnServerNotifierSuccess();
                        }
                    }
                    else
                    {
                        OnServerNotifierSuccess();
                    }
                }
            }
            catch( Exception e )
            {
                IsListedOk = false;
                OnServerNotifierListedStateChanged();
                OnServerNotifierError( e.ToString() );
            }
        }

        public void BecomeUnlisted( )
        {
            WebClient.CancelAsync();
            // TODO
            //if (!HasID) return;
            Uri uri = new Uri( DeleteURI );
            var jsonString = Writer.Write( AsServerInfoRaw );
            var nameValueCollection = new NameValueCollection { { "data", jsonString } };
            WebClient.UploadValuesAsync( uri, nameValueCollection );
            // todo will need to pass this guy off to Relay to keep it holding
            // on until the server is unregistered, cancelasync/dispose will
            // stop the request from finishing. (maybe?)
        }

        public void Dispose( )
        {
            WebClient.CancelAsync();
            WebClient.Dispose();
        }
    }
}