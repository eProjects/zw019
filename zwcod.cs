   public static class PropertyChangedExtension
    {
        public static bool PropertyChangedRaised( this INotifyPropertyChanged notifyPropertyChanged, Action action, string propertyName)
        {
            var raised = false;
            notifyPropertyChanged.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == propertyName)
                {
                    raised = true;
                }
            };

            action();
            return raised;
        }
    }
 
----------------------------------------------------------------------------------------------- 
 var vm = (DataContext as PricerCollectionViewModel);
            // register for Closed event
            Closed += (sender, args) => vm?.Dispose();
 
 
 public class PassedTradeViewModelTest
    {
        private Mock<IPassedTradeModel> ModelMock;
        private PassedTradeViewModel viewmodel;

        public PassedTradeViewModelTest()
        {
            ModelMock = new Mock<IPassedTradeModel>();
            ModelMock.Setup(m => m.PricerModel).Returns(new Mock<IPricerModel>().Object);
            ModelMock.Setup(m => m.RecordFeedBack()).Verifiable();

            
            viewmodel = new PassedTradeViewModel();
            viewmodel.Model = ModelMock.Object;
            viewmodel.Comments = "Some comments";

        }

        [Fact]
        public void ShouldSetVMandModel()
        {
            Assert.NotNull(viewmodel);
            Assert.NotNull(viewmodel.Model);
        }

        [Fact]
        public void ShouldSetPassedTradeProperties()
        {
            Assert.False(viewmodel.ConfirmPoped);
            //Assert.Equal("Some comments", viewmodel.Model.Comments);
        }

        [Fact]
        public void ShouldOpenConfirmPopUp()
        {
            viewmodel.ConfirmFinalCommand.Execute(null);
            Assert.True(viewmodel.ConfirmPoped); 
        }

        [Fact]
        public void ShouldRecordPassedTrade()
        {
            viewmodel.ConfirmFinalCommand.Execute(null);
            Assert.True(viewmodel.ConfirmPoped);
            viewmodel.RecordFeedbackCommand.Execute(null);
            Assert.False(viewmodel.ConfirmPoped);
        }
    }
-----------------------------------------------------------------------------------------------
 public class PricerWindowViewModelTest
    {
        private PricerCollectionViewModel viewModel;
        private Mock<IPricerActionsModel> sendOrpassModelMock;
        private Mock<IPricerModel> pricerModelMock;

        public PricerWindowViewModelTest()
        {
            viewModel = new PricerCollectionViewModel();
            sendOrpassModelMock = new Mock<IPricerActionsModel>();
            pricerModelMock = new Mock<IPricerModel>();

            //verify dependency methods
            sendOrpassModelMock.Verify(m => m.OkAction(It.IsAny<IPricerModel>()), Times.Never);
            sendOrpassModelMock.Verify(m => m.KoAction(It.IsAny<IPricerModel>(), It.IsAny<string>()), Times.Never);


            viewModel.PricerActionsModel = sendOrpassModelMock.Object;
        }


        [Fact]
        public void ShouldAddNewPricertoWindow()
        {
            viewModel.AddPricerCommand.Execute(pricerModelMock.Object);
            Assert.Equal(1, viewModel.Pricers.Count);
        }

        [Fact]
        public void ShouldRemovePricerFromCollection()
        {   
            viewModel.AddPricerCommand.Execute(pricerModelMock.Object);
            Assert.Equal(1, viewModel.Pricers.Count);

            //check selected and remove
            Assert.NotNull(viewModel.SelectedPricerModel);
            viewModel.RemovePricerCommand.Execute(pricerModelMock.Object);
            Assert.Equal(0, viewModel.Pricers.Count);
        }

        [Fact]
        public void ShouldOKRFQ()
        {
            viewModel.OkCommand.Execute(new Tuple<IPricerModel, string>(pricerModelMock.Object, "deal"));
            Assert.True(viewModel.IsActionsModelSet);
        }

        [Fact]
        public void ShouldRejectRFQ()
        {
            viewModel.KoCommand.Execute(new Tuple<IPricerModel, string>(pricerModelMock.Object, "no deal"));
            Assert.True(viewModel.IsActionsModelSet);
        }
    }
----------------------------------------------------------------------------------------------------------------------------
 public class SalesMainViewModelTests
    {
        private SalesMainViewModel viewModel;
        private Mock<ILoginModel> loginModelMock;
        private Mock<IFreeTextRFQModel> freeTextModelMock;


        //Set up viewmodel and a mock for its dependencies.
        public SalesMainViewModelTests()
        {
            viewModel = new SalesMainViewModel();
            loginModelMock = new Mock<ILoginModel>();
            freeTextModelMock = new Mock<IFreeTextRFQModel>();

            loginModelMock.Setup(m => m.Login(It.IsAny<RoleType>())).Verifiable();
            //loginModelMock.Setup(m=>m.Logout(It.IsAny<RoleType>())).Verifiable();

            freeTextModelMock.Setup(x => x.InterpretRfq(It.IsAny<string>())).Verifiable();

            viewModel.LoginModel = loginModelMock.Object;
            viewModel.FreeTextRfqModel = freeTextModelMock.Object;
            loginModelMock.Verify();
        }

        [Fact]
        public void ShouldLoginSales()
        {
            viewModel.LoginCommand.Execute(null);
            //Assert.NotNull(loginModelMock);
            Assert.Equal(RoleType.Sales, viewModel.TestRoleType);
        }

        [Fact]
        public  void ShouldLogoutSales()
        {
            viewModel.LogoutCommand.Execute(null);
            Assert.NotNull(loginModelMock);
            Assert.Equal(RoleType.Sales, viewModel.TestRoleType);
        }

        [Fact]
        public void ShouldSendRFQ()
        {
            Assert.NotNull(freeTextModelMock);
            viewModel.SendRfqCommand.Execute("eurusd 6mth 1.3 in 8m for bc");
           
            Assert.True(viewModel.TextRFQSent);
            // Assert.Equal("Rfq model", viewModel.Status); // FreeTextRfqModel.InterpretationStatus);
        }

        [Fact]
        public void ShouldRunSimilarSearch()
        {
           
            Assert.NotNull(freeTextModelMock);
            viewModel.SearchSimilarRequestsCommand.Execute(null);

            freeTextModelMock.Verify(u => u.SearchSimilarRfqs(It.IsAny<IObservable<EventPattern<KeyEventArgs>>>(),
                                                              It.IsAny<Func<string>>(),
                                                              It.IsAny<Dispatcher>()
                                                            ), Times.AtLeastOnce);

            Assert.Equal(true, viewModel.SimilarSearchExecuted);
            //Assert.Equal(0, viewModel.FreeTextRfqModel.SearchResults.Count);
        }
    }
---------------------------------------------------------------------------------------------------------------------------------
 public class TradeConfirmationViewModelTest
    {
        private Mock<ITradeConfirmModel> ModelMock;
        TradeConfirmationViewModel viewmodel;

        public TradeConfirmationViewModelTest()
        {
            ModelMock = new Mock<ITradeConfirmModel>();
            ModelMock.Setup(m => m.PricerModel).Returns(new Mock<IPricerModel>().Object);
            ModelMock.Setup(m => m.PricerModel.OptionStrategy).Returns(new SingleLegOptionStrategyModel());

            ModelMock.Setup(pc => pc.PricerModel.PricerSettings).Returns(new Mock<IPricerSettingsModel>().Object);
            ModelMock.Setup(pc => pc.PricerModel.PricerSettings.PremiumType).Returns(PremiumType.Spot);
            ModelMock.SetupProperty(pc => pc.PricerModel.PricerSettings.PremiumType);

            ModelMock.Setup(pc => pc.OrderType).Returns("buys");
            #region MARKET DATA
            ModelMock.Setup(pc => pc.SalesVolatility).Returns("23.02");
            ModelMock.Setup(pc => pc.DealSwapPoint).Returns("1.0200");
            ModelMock.Setup(pc => pc.DealSpot).Returns("1.0301");
            ModelMock.Setup(pc => pc.DealForwardPoint).Returns("2.0301");
            #endregion
            ModelMock.Setup(x => x.PricerModel.LatestQuote).Returns(new QuoteModel
            {
                Volatility = new BidAsk { Ask = 7.01, Bid = 6.11 },
                Spot = new BidAsk { Ask = 1.03000, Bid = 1.02100 },
                //SwapPoints = new BidAsk { Ask = 1.0200, Bid = 1.1400 },
                AskPremium = 10.032,
                BidPremium = 20.093,
                Vega = 1.03,
                Gamma = 1.20,
                //Delta = 1.00,
                HedgeAmount = 23,
            });


            ModelMock.Setup(pc => pc.PricerModel.PricerSettings.PremiumUnit).Returns(PremiumUnit.AmountCcy1);
            ModelMock.SetupProperty(pc => pc.PricerModel.PricerSettings.PremiumUnit);

           

            viewmodel = new TradeConfirmationViewModel();
            viewmodel.Model = ModelMock.Object;
        }

        [Fact]
        public void ShouldHaveModelAndSetups()
        {
            Assert.NotNull(viewmodel.Model);
            Assert.NotNull(viewmodel.Model.PricerModel);
            Assert.NotNull(viewmodel.Model.PricerModel.OptionStrategy);
            Assert.NotNull(viewmodel.Model.PricerModel.LatestQuote);
        }

        [Fact]
        public void ShouldSetVMProperties()
        {
            Assert.Equal("1.0200", viewmodel.SwapPoints);
            //Assert.True(viewmodel.Model.PricerModel?.LatestQuote?.PremiumPrice != 0D);
            Assert.Equal("6.11 / 7.01", viewmodel.LiveVolatility);
            Assert.Equal("1.14000 / 1.02000", viewmodel.LiveSwapPoints);
            var liveFwd = (ModelMock.Object.PricerModel.LatestQuote.Spot + ModelMock.Object.PricerModel.LatestQuote.SwapPoints / 10000D).ToString("F5");
            Assert.Equal(liveFwd, viewmodel.LiveForward);
            Assert.Equal("1.02100 / 1.03000", viewmodel.LiveSpot);

            Assert.Equal("15.0625", viewmodel.Model.PricerModel?.LatestQuote?.PremiumPrice.ToString());

            Assert.Equal(2, viewmodel.Premiums.Count);
            Assert.Equal(2, viewmodel.OptionTypes.Count);
            Assert.Equal(3, viewmodel.HedgeTypes.Count);
            Assert.Equal(2, viewmodel.Currencies.Count);

            var premium = (0.5 * (ModelMock.Object.PricerModel.LatestQuote.BidPremium + ModelMock.Object.PricerModel.LatestQuote.AskPremium)).ToString("N0");

            //Assert.Equal(premium, viewmodel.CurrencyPercent);
            // Assert.Equal(premium, viewmodel.PremiumPrice);

            Assert.Equal("buys", viewmodel.BuySell);
            Assert.Equal("23.02", viewmodel.SalesVol);


            Assert.Equal(6, viewmodel.TextBoxStyle.Count);
            Assert.Equal("JP Morgan", viewmodel.Client);
            Assert.Equal(DateTime.Now.AddMonths(3).Day, viewmodel.SettlementDate.Value.Day);
            Assert.Equal(DateTime.Now.AddMonths(3).Day, viewmodel.FixingDate.Value.Day);

           // Assert.Equal("50,000,000", viewmodel.Notional);
            Assert.Equal("cad / usd", viewmodel.CurrencyPair);

        }
    }
-------------------------------------------------------------------------------------------------------------------------------
 public class TraderMainviewModeltest
    {
        private TraderMainViewModel viewModel;
        private Mock<ILoginModel> loginModelMock;
        private Mock<INotifierModel> notifierModelMock;
        private bool StatusSet = false;

        public TraderMainviewModeltest()
        {
            viewModel = new TraderMainViewModel();
            loginModelMock = new Mock<ILoginModel>();
            notifierModelMock = new Mock<INotifierModel>();

            loginModelMock.Setup(m => m.Login(It.IsAny<RoleType>())).Verifiable();
            loginModelMock.Setup(m=>m.Logout(It.IsAny<RoleType>())).Verifiable();

            notifierModelMock.Setup(n => n.PostStatus(It.IsAny<string>()));
            
            notifierModelMock.Setup(n => n.StatusCheck).Returns(Observable.Return<string>("success"))
                                                               .Callback(() => StatusSet = true);

            viewModel.LoginModel = loginModelMock.Object;
            viewModel.NotifierModel = notifierModelMock.Object;
        }

        [Fact]
        public void ShouldLoginSales()
        {
            viewModel.LoginCommand.Execute(null);
            Assert.True(StatusSet);
            Assert.Equal("Done with Status Check Subscription.", viewModel.StatusCheck);
            Assert.NotNull(loginModelMock);
            //Assert.Equal(RoleType.Sales, viewModel.TestRoleType);
        }
    }
------------------------------------------------------------------------------------------------------------------------------
 public class PricerControlVMtest
    {
        private Mock<IPrincingTriggersModel> modelMock;
        private Mock<IPricerModel> pricerModelMock;
        private PricerViewModel viewmodel;

        public PricerControlVMtest()
        {
            modelMock = new Mock<IPrincingTriggersModel>();
            modelMock.Setup(md => md.OnPricingRequested(It.IsAny<IPricerModel>()));

            modelMock.Setup(md => md.OnPricingAbandoned(It.IsAny<IPricerModel>()));
            


            pricerModelMock = new Mock<IPricerModel>();
            pricerModelMock.Setup(pm => pm.PropertyObserver).Returns(new PropertyObserver());
     
            viewmodel = new PricerViewModel();

            viewmodel.PricerModel = pricerModelMock.Object;
            viewmodel.PrincingTriggersModel = modelMock.Object;
        }

        [Fact]
        public void ShouldSetupPricerModels()
        {
            Assert.NotNull(viewmodel.PricerModel);
            Assert.NotNull(viewmodel.PrincingTriggersModel);
        }

        [Fact]
        public void ShouldSetProperties()
        {
            Assert.NotNull(viewmodel.TrackedProperties);
            Assert.False(viewmodel.TrackedPropertiesVisible);
        }

        [Fact]
        public void ShouldTestRepricingCommands()
        {
            viewmodel.RepriceCommand.Execute(null); //TODO: find a way to increase event counts
        }
    }
----------------------------------------------------------------------------------------------------------------------------------
public class SingleOptionStrategyVMtest
    {
        private Mock<IPricerModelProvider> pricerModelProviderMock;
        private Mock<IPricerModel> pricerModelMock;

        private SingleOptionStrategyViewModel viewModel;

        public SingleOptionStrategyVMtest()
        {
            pricerModelProviderMock = new Mock<IPricerModelProvider>();
            pricerModelMock = new Mock<IPricerModel>();

            pricerModelMock.Setup(pc => pc.OptionStrategy).Returns(new SingleLegOptionStrategyModel());

            pricerModelMock.Setup(pc => pc.PricerSettings).Returns(new Mock<IPricerSettingsModel>().Object);

            viewModel = new SingleOptionStrategyViewModel();
            viewModel.PricerModelProvider = pricerModelProviderMock.Object;
            viewModel.PricerModel = pricerModelMock.Object;
        }


        [Fact]
        public void ShouldRaisePropertyChangedEvent()
        {
            // 
            var NotionalRaised = viewModel.PropertyChangedRaised(
                                    () => viewModel.Notional = "40,000,000",
                                    nameof(viewModel.Notional));

            var ExpiryRaised = viewModel.PropertyChangedRaised(
                                    () => viewModel.Expiry = DateTime.Now.AddMonths(5),
                                    nameof(viewModel.Expiry));

            var StrikeRaised = viewModel.PropertyChangedRaised(
                                   () => viewModel.Strike = "1.2203",
                                   nameof(viewModel.Strike));

            Assert.True(NotionalRaised);
            Assert.True(ExpiryRaised);
            Assert.True(StrikeRaised);
        }

        [Fact]
        public void ShouldHaveaPricer()
        {
            Assert.NotNull(pricerModelMock.Object.OptionStrategy);
            Assert.NotNull(viewModel.PricerModel);
            Assert.NotNull(viewModel.StrategyModel);
        }

        [Fact]
        public void ShouldSetProperties()
        {
            Assert.Equal("cad", viewModel.DealtCurrency);
            Assert.Equal("cad", viewModel.StrategyModel.Ccy1);
            Assert.Equal("usd", viewModel.StrategyModel.Ccy2);
            Assert.Equal("JP Morgan", viewModel.StrategyModel.Account);
            //Assert.Equal(1.0201, viewModel.StrategyModel.Strike);
            Assert.Equal(PutOrCall.Call, viewModel.PutOrCall);
        }

        [Fact]
        public void ShouldRunCommands()
        {
            viewModel.ChooseDealtCurrencyCommand.Execute(null);
            viewModel.PutOrCallCommand.Execute(null);
            Assert.Equal(PutOrCall.Put, viewModel.PutOrCall);
        }
    }
--------------------------------------------------------------------------------------------------
 public class UserInteractionVMtest
    {
        private Mock<IPricerModel> pricerModelMock;
        private Mock<IOrderExecutionModel> orderExecModelMock;
        private UserInteractionViewModel viewmodel;

        public UserInteractionVMtest()
        {
            pricerModelMock = new Mock<IPricerModel>();
            orderExecModelMock = new Mock<IOrderExecutionModel>();
            viewmodel = new UserInteractionViewModel();
            //Strategy
            pricerModelMock.Setup(pc => pc.OptionStrategy).Returns(new SingleLegOptionStrategyModel());

            //Quote
            pricerModelMock.Setup(x => x.LatestQuote).Returns(new QuoteModel
            {
                Volatility = new BidAsk { Ask = 4.02, Bid = 5.10 },
                Spot = new BidAsk { Ask = 1.0220, Bid = 1.0010 },
                //SwapPoints = new BidAsk { Ask = 1.01, Bid = 1.20 },
                
                Vega = 1.03,
                Gamma = 1.20,
                //Delta = 1.00,
                HedgeAmount = 23,
            });
            pricerModelMock.Setup(pc => pc.Bid).Returns(10.25);
            pricerModelMock.Setup(pc => pc.Ask).Returns(13.04);
            pricerModelMock.Setup(pc => pc.Mid).Returns(7.15);

            pricerModelMock.Setup(pc => pc.PricerSettings).Returns(new Mock<IPricerSettingsModel>().Object);

            pricerModelMock.Setup(pc => pc.PricerSettings.PriceExpression).Returns(PriceExpressionType.InVolatility);
            pricerModelMock.SetupProperty(pc => pc.PricerSettings.PriceExpression);

            //Order execution 
            orderExecModelMock.Verify(x => x.Buy(It.IsAny<IPricerModel>()), Times.Never);
            orderExecModelMock.Verify(x => x.Sell(It.IsAny<IPricerModel>()), Times.Never);
            //orderExecModelMock.Verify();
            viewmodel.PricerModel = pricerModelMock.Object;
            viewmodel.OrderExecutionModel = orderExecModelMock.Object;
        }

        [Fact]
        public void ShouldHavePricerAndExecModel()
        {
            viewmodel.OrderExecutionModel.Buy(pricerModelMock.Object);
            Assert.NotNull(viewmodel.PricerModel);
            Assert.NotNull(viewmodel.OrderExecutionModel);
        }

        [Fact]
        public void ShouldSetProperties()
        {
            Assert.Equal(2, viewmodel.PriceExpressionEnumeration.Count());
            Assert.NotNull(viewmodel.PricerModel.OptionStrategy);
            Assert.Equal(
                  (pricerModelMock.Object.LatestQuote.Spot + pricerModelMock.Object.LatestQuote.SwapPoints / 10000D).ToString("F4"), 
                  viewmodel.ForwardPoints);

            //Assert.Equal("eur", viewmodel)
            Assert.Equal("10.25", viewmodel.Bid);
            Assert.Equal("13.04", viewmodel.Ask);
            Assert.Equal("7.15", viewmodel.Mid);

            //Tests IsInSaleMode() private method
            Assert.Equal("10.25", viewmodel.VolReceive);
            Assert.Equal("13.04", viewmodel.VolPay);

            Assert.Equal(DateTime.Now.AddMonths(3).AddBusinessDays(2).ToString("ddd-d-MMM-yy"), viewmodel.SettlementDate);
        }

        
        [Theory]
        [InlineData("Buy")]
        [InlineData("Sell")]
        public void ShouldExecuteBuySellCommands(string direction)
        {
            viewmodel.ExecuteOrderCommand.Execute(direction);
            viewmodel.ShowDetailsCommand.Execute(null);
            Assert.True(viewmodel.OpenDetails);  
        }
    }