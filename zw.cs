 public class ClientsDataActor : ReceiveActor
    {
        private readonly ILog logger = LogManager.GetLogger(typeof(ClientsDataActor));
        public ClientsDataActor()
        {
            _dbContext = DataAccess.Context;

            #region MESSAGE PROCESSING
            Receive<ClientSearch.FindAll>(srch =>
            {
                var EndTask = SearchClientsAsync();
                if (!EndTask.IsFaulted) EndTask.PipeTo(Self, Sender);

                if(EndTask.IsFaulted)
                {
                    logger.Error($"Error searching all clients: {EndTask.Exception}");
                }
            });

            Receive<ClientSearch.FindUniques>(clt =>
            {
                var EndTask = SearchClientsAsync(clt.NickName);
                if (!EndTask.IsFaulted) EndTask.PipeTo(Self, Sender);
            });

            Receive<List<ClientData>>(results =>
            {
                Sender.Tell(results); 
            });

            Receive<InsertClient>(insert =>
            {
                var InsertTask = UpSertClientData(insert.client);

                InsertTask.PipeTo(Self, Sender);


                InsertTask.ContinueWith(r =>
                {
                    AggregateException ex = r.Exception; //Log exception
                }, TaskContinuationOptions.OnlyOnFaulted);

            });

            Receive<ClientData>(cd =>
            {
                if (cd.IsUpdated)
                {
                    Sender.Tell(true);
                }
                else { Sender.Tell(cd); }
            });

            Receive<UpdateClient.Update>(msg => 
            {
                var updateTask = UpSertClientData(msg.client, true);
                if (!updateTask.IsFaulted) updateTask.PipeTo(Self, Sender);

                updateTask.ContinueWith(rr =>
                {
                    AggregateException ex = rr.Exception; //Log it
                }, TaskContinuationOptions.OnlyOnFaulted);
             });

            Receive<UpdateClient.Delete>(msg =>
            {
                _dbContext.Clients.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", msg.Key))
                .ContinueWith(c =>
                {
                    AggregateException ex = c.Exception;
                    logger.Error($"Error deleting client with Id: {msg}. Description: {c.Exception}");
                }, TaskContinuationOptions.OnlyOnFaulted);
            });

            #endregion
        }

        private async Task<List<ClientData>> SearchClientsAsync(string name = null)
        {
            if(_dbContext.IsReady)
            {
                return await _dbContext.Clients.FindAsync((string.IsNullOrEmpty(name)) ?
                                        Builders<BsonDocument>.Filter.Empty : Builders<BsonDocument>.Filter.Eq("focusname", name.Trim()))
                   .ContinueWith(r =>
                       r.Result.ToList()
                       .Select
                       (
                           rr => new ClientData(new ObjectId(rr["_id"].ToString()), rr["shortname"].ToString(), 
                                                        rr["fullname"].ToString(), rr["focusname"].ToString(), rr["date"].ToUniversalTime())
                       ).ToList());
            }
            else
            {
                logger.Error($"Error. Database not ready");
                return null;
            }
        
        }

        private async Task<ClientData> UpSertClientData(ClientData client, bool isUpdate = false)
        {
            if (_dbContext.IsReady)
            {
                return await _dbContext.Clients.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", client.Key),
                                                            Builders<BsonDocument>.Update.Set("fullname", client.FullName.Trim())
                                                                                         .Set("shortname", client.ShortName.Trim())
                                                                                         .Set("focusname", client.FocusName.Trim())
                                                                                         .Set("date", client.EntryDate))
                 .ContinueWith(r => new Tuple<bool, ClientData>(r.Result.ModifiedCount > 0, new ClientData(client.Key, client.ShortName, client.FullName, client.FocusName,client.EntryDate, isUpdate)))
                 .ContinueWith(rr =>
                 {
                     var updated = rr.Result.Item1;
                     var updatingClient = rr.Result.Item2;

                     if (!updated)
                     {
                         var newClient = new BsonDocument
                         {
                             { "shortname", updatingClient.ShortName.Trim()},
                             { "focusname", updatingClient.FocusName.Trim()},
                             { "fullname", updatingClient.FullName.Trim()},
                             { "date", updatingClient.EntryDate}
                         };
                         _dbContext.Clients.InsertOneAsync(newClient);

                         var Id = newClient["_id"];
                         updatingClient.Key = new ObjectId(Id.ToString());
                     }

                     return updatingClient;
                 });
            } else
            {
                logger.Error($"Error. Database not ready");
                return null;
            }       
        }




        public static Props Props()
        {
            return Akka.Actor.Props.Create<ClientsDataActor>();
        }
        private readonly DataAccess _dbContext;

    }
	
	------------------------------------------------------------------------------------------------------------------------------
	
	  public class ClientsDataService : IClientsDbOperations, IDisposable
    {
        private readonly ILog logger = LogManager.GetLogger(typeof(ClientsDataService));
        ICanTell DbActor = null;
        private bool isLocal => Convert.ToBoolean(Convert.ToInt32(ConfigurationManager.AppSettings["RunLocal"]));
        private ActorSystem SrvActorSys;
        public ClientsDataService()
        {
            if(ConfigSetUp() == null)
            {
                logger.Error("DbActor returns NULL");
            }
            else
            {
                var location = isLocal ? "Local" : "Remote";
                logger.Info($"Client Database actor running on {location}-- READY.");
            }
        }
        
       

        /// <summary>
        /// Returns all clients available in the system.
        /// Awaitable.
        /// </summary>
        /// <returns></returns>
        public async Task<IList<ClientInfo>> GetAllClients()
        {
            IList<ClientInfo> ResultList = new List<ClientInfo>();
            
            var QueryTask =  DbActor?.Ask<List<ClientData>>(new ClientSearch.FindAll());
            await QueryTask.ContinueWith(x =>
            {
                x.Result.ForEach(u =>
                {
                    ResultList.Add
                    ( new ClientInfo
                        {
                            DBId = u.Key.ToString(),
                            Name = u.FocusName,
                            ShortName = u.ShortName,
                            FullName = u.FullName,
                            DateEntered = u.EntryDate
                    });
                });
            }, TaskContinuationOptions.OnlyOnRanToCompletion);

            return ResultList;
        }

        /// <summary>
        /// Get all clients associated with this nickname
        /// Awaitable
        /// </summary>
        /// <param name="nickname">Different possible names used for a client</param>
        /// <returns></returns>
        public async Task<IList<ClientInfo>> GetAllClients(string nickname)
        {
            IList<ClientInfo> ResultList = new List<ClientInfo>();
            var QueryTask = DbActor?.Ask <List<ClientData>> (new ClientSearch.FindUniques(nickname));
            await QueryTask.ContinueWith(x =>
            {
                x.Result.ForEach(u =>
                {
                    ResultList.Add
                    (new ClientInfo
                    {
                        DBId = u.Key.ToString(),
                        Name = u.FocusName,
                        ShortName = u.ShortName,
                        FullName = u.FullName,
                        DateEntered = u.EntryDate
                    });
                });
            }, TaskContinuationOptions.OnlyOnRanToCompletion);

            return ResultList;
        }

        /// <summary>
        /// Add a new client to the system and redecorates the client with database Id.
        /// </summary>
        public void InsertClient(ClientInfo client)
        {
            var insertTask = DbActor?.Ask<ClientData>(new InsertClient(new ObjectId(), client.ShortName,client.Name, client.FullName, DateTime.Now));

            insertTask.ContinueWith(r =>
            {
                client.DBId = r.Result.Key.ToString(); 
                client.ShortName = r.Result.ShortName;
                client.FullName = r.Result.FullName;
                client.Name = r.Result.FocusName;
                client.DateEntered = r.Result.EntryDate;
            });
        }

        /// <summary>
        /// Updates a current client's info and return success status
        /// </summary>
        /// <param name="client"> Client being updated</param>
        /// <returns></returns>
        public bool UpdateClient(ClientInfo client)
        {
            return (DbActor?.Ask<bool>(new 
                                    UpdateClient.Update(new 
                                                 ObjectId(client.DBId), client.ShortName, client.FullName, client.Name,client.DateEntered)
                                       ).Result).Value;
        }


        /// <summary>
        /// Deletes the current client
        /// </summary>
        /// <param name="key">Database Id associated with client</param>
        public void DeleteClient(string key)
        {
            DbActor?.Tell(new UpdateClient.Delete(new ObjectId(key)), null);
        }

        public void Dispose()
        {
            SrvActorSys.Terminate();
            logger.Info("Client service actor system terminated");
        }

        #region PRIVATE METHODS
        private ICanTell ConfigSetUp()
        {
            var configString = Local2RemoteConfig();

            if (isLocal)
            {
                SrvActorSys = ActorSystem.Create("ServiceActorSystem");
                DbActor = SrvActorSys.ActorOf(ClientsDataActor.Props(), "ClientDbActor");
            }
            else
            {
                SrvActorSys = ActorSystem.Create("serviceActors", ConfigurationFactory.ParseString(configString));
                DbActor = SrvActorSys.ActorSelection(ConfigurationManager.AppSettings["ClientsActor_RemoteConfigPath"]);
            }
            return DbActor;
        }