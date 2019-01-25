namespace Backend.Persistence
{
    public sealed class MongoDBContext : IDisposable
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof (MongoDBContext));
        private readonly IMongoClient   _mongoClient;
        private readonly IDisposable _statusSubscription;

        public static MongoDBContext Context { get; } = new MongoDBContext(ConfigurationManager.AppSettings["Persistence.Server"], ConfigurationManager.AppSettings["Persistence.Database"]);

        private MongoDBContext(string server, string database)
        {
            _mongoClient = new MongoClient(server);
            var db = _mongoClient.GetDatabase(database);

            // initialize collections
            Events = db.GetCollection<BsonDocument>(ConfigurationManager.AppSettings["Persistence.Collection.Events"]);
            DealClosing = db.GetCollection<BsonDocument>(ConfigurationManager.AppSettings["Persistence.Collection.DealClosing"]);
            Clients = db.GetCollection<BsonDocument>(ConfigurationManager.AppSettings["Persistence.Collection.Clients"]);
            Users = db.GetCollection<BsonDocument>(ConfigurationManager.AppSettings["Persistence.Collection.Users"]);            

            // transforms the delegation of cluster description changes into an observable
            ConnectionStatusObservable = Observable.FromEventPattern<ClusterDescriptionChangedEventArgs>(
                handler => _mongoClient.Cluster.DescriptionChanged += handler,
                handler => _mongoClient.Cluster.DescriptionChanged -= handler).Select(evt => evt.EventArgs.NewClusterDescription.State == ClusterState.Connected);

            _statusSubscription = ConnectionStatusObservable.Subscribe(evt =>
            {
                _connected = evt;
                _logger.Info($"MongoDB is {(_connected ? "connected" : "disconnected")}");
            });

            // sets the status is case we missed the response
            _connected = _mongoClient.Cluster.Description.State == ClusterState.Connected;
            _logger.Info($"MongoDB is {(_connected ? "connected" : "disconnected")}");
        }

        public IMongoCollection<BsonDocument> Events { get; }
        public IMongoCollection<BsonDocument> DealClosing { get; }
        public IMongoCollection<BsonDocument> Clients { get; }
        public IMongoCollection<BsonDocument> Users { get; }        

        public IObservable<bool> ConnectionStatusObservable { get; }

        private volatile bool _connected;
        public bool Connected => _connected;
        public void Dispose()
        {
            _statusSubscription.Dispose();
        }
    }
	
	
	
	
	
	//////////////////////////////////////Add to Project refs: MongoDB.Bson, MongoDB.Driver, MongoDB.Driver.Core from nudget /////
	public class AdministrativeActor : ReceiveActor
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(AdministrativeActor));

        private readonly MongoDBContext _mongoContext;
    

        public static Props Props(MongoDBContext mongoContext)
        {
            return Akka.Actor.Props.Create<AdministrativeActor>(mongoContext);
        }

        private Task<ClientDetails.ClientDetail> SearchClientDetailAsync(UpsertClientDetail details)
        {                            
            return _mongoContext.Clients
                .UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", details.ShortName.Trim()),
                    Builders<BsonDocument>.Update.Set("fullname", details.FullName))                    
                    .ContinueWith(t => new Tuple<bool, ClientDetails.ClientDetail>(t.Result.ModifiedCount > 0, new ClientDetails.ClientDetail(details.ShortName, details.FullName, details.EntryDate)))
                    .ContinueWith(t =>
                    {
                        var updateSucceeded = t.Result.Item1;
                        var clientDetails = t.Result.Item2;
                        
                        // insert if needed
                        if (!updateSucceeded) _mongoContext.Clients.InsertOneAsync(new BsonDocument
                        {
                            { "_id", clientDetails.ShortName},
                            { "fullname", clientDetails.FullName},
                            { "date", clientDetails.EntryDate}
                        });
                        return clientDetails;
                    });            
        }

        public AdministrativeActor(MongoDBContext mongoContext)
        {
            _mongoContext = mongoContext;

            Receive<UpsertClientDetail>(clientDetail =>
            {
                if (_mongoContext.Connected)
                {
                    _logger.Info($"UpsertClient: {clientDetail}");

                    var clientSearch = SearchClientDetailAsync(clientDetail);

                    // on Success
                    clientSearch.PipeTo(Self, Sender);

                    // on failure
                    clientSearch.ContinueWith(t =>
                    {
                        AggregateException ex = t.Exception;
                                                
                        _logger.Error($"Error while searching {clientDetail}: {t.Exception}");
                    },
                        TaskContinuationOptions.OnlyOnFaulted);
                }
                else { _logger.Warn($"MongoDB is not available to save {clientDetail}"); }
            });


            Receive<ClientDetails.ClientDetail>(clientDetail =>
            {
                _logger.Info($"Result of UpsertClient: {clientDetail}");
                PersistenceCache.Cache.AddOrUpdate(clientDetail.ShortName, clientDetail, (shortName, d) => clientDetail);
            });
			
		 //save new user...
            Receive<User>(newuser =>
            {
                _logger.Info($"Saving: {newuser} from {Sender}");
                if (mongoContext.Connected)
                {
                    try
                    {
                        mongoContext.Users.InsertOne(new BsonDocument
                        {
                            {"nickname", newuser.NickName.Trim() },
                            {"fullname", newuser.FullName.Trim() },
                            {"groupname",newuser.Group.Trim() },
                            {"entrydate",newuser.EntryDate }
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error while saving {newuser} : {ex}");
                    }
                }
                else { _logger.Warn($"MongoDB is not available to save {newuser}"); }
            });


            Receive<List<User>>(newusers =>
            {
                if (mongoContext.Connected)
                {
                    try
                    {
                        var bulkBSON = new List<BsonDocument>();
                        newusers.ForEach((s) =>
                        {
                           bulkBSON.Add(new BsonDocument
                           {
                                {"nickname", s.NickName.Trim() },
                                {"fullname", s.FullName.Trim() },
                                {"groupname",s.Group.Trim() },
                                {"entrydate",s.EntryDate }
                           });
                        });
                        
                        mongoContext.Users.InsertMany(bulkBSON);
                        
                    }
                    catch(Exception ex)
                    {
                        _logger.Error($"Error while saving {newusers.Count} new users : {ex}");
                    }
                }
                else { _logger.Warn($"MongoDB is not available to save {newusers.Count} new users"); }

            });
        }
	}
                    
}