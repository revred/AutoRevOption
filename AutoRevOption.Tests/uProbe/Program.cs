using IBApi;

namespace AutoRevOption.Tests.uProbe;

/// <summary>
/// Minimal IBKR connection probe with full EWrapper implementation
/// Tests if Gateway API handshake works with startApi() call
/// </summary>
public class MinimalWrapper : EWrapper
{
    private readonly ManualResetEvent _connected = new(false);
    public bool GotNextValidId { get; private set; }
    public bool GotConnectAck { get; private set; }
    public List<string> Events { get; } = new();

    private void Log(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
        Events.Add(line);
        Console.WriteLine(line);
    }

    // Critical handshake callbacks
    public void connectAck() { GotConnectAck = true; Log("✅ connectAck()"); }
    public void nextValidId(int orderId) { GotNextValidId = true; Log($"✅ nextValidId({orderId})"); _connected.Set(); }
    public void error(Exception e) { Log($"❌ error(Exception): {e.Message}"); }
    public void error(string str) { Log($"⚠️  error(string): {str}"); }
    public void error(int id, int code, string msg, string advancedOrderRejectJson)
    {
        Log($"⚠️  error({id}, {code}): {msg}");
    }
    public void error(int id, long orderId, int code, string msg, string advancedOrderRejectJson)
    {
        Log($"⚠️  error({id}, {orderId}, {code}): {msg}");
    }

    // Observable events
    public void currentTime(long time) { Log($"currentTime({time})"); }
    public void managedAccounts(string accountsList) { Log($"managedAccounts({accountsList})"); }
    public void connectionClosed() { Log("connectionClosed()"); }

    public ManualResetEvent Connected => _connected;

    // All other EWrapper methods as no-ops to satisfy interface
    public void tickPrice(int tickerId, int field, double price, TickAttrib attribs) { }
    public void tickSize(int tickerId, int field, decimal size) { }
    public void tickOptionComputation(int tickerId, int field, int tickAttrib, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice) { }
    public void tickGeneric(int tickerId, int field, double value) { }
    public void tickString(int tickerId, int tickType, string value) { }
    public void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureLastTradeDate, double dividendImpact, double dividendsToLastTradeDate) { }
    public void orderStatus(int orderId, string status, decimal filled, decimal remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice) { }
    public void orderStatus(int orderId, string status, decimal filled, decimal remaining, double avgFillPrice, long permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice) { }
    public void openOrder(int orderId, Contract contract, Order order, OrderState orderState) { }
    public void openOrderEnd() { }
    public void updateAccountValue(string key, string value, string currency, string accountName) { }
    public void updatePortfolio(Contract contract, decimal position, double marketPrice, double marketValue, double averageCost, double unrealisedPNL, double realisedPNL, string accountName) { }
    public void updateAccountTime(string timestamp) { }
    public void accountDownloadEnd(string account) { }
    public void bondContractDetails(int reqId, ContractDetails contract) { }
    public void contractDetails(int reqId, ContractDetails contractDetails) { }
    public void contractDetailsEnd(int reqId) { }
    public void execDetails(int reqId, Contract contract, IBApi.Execution execution) { }
    public void execDetailsEnd(int reqId) { }
    public void updateMktDepth(int tickerId, int position, int operation, int side, double price, decimal size) { }
    public void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, decimal size, bool isSmartDepth) { }
    public void updateNewsBulletin(int msgId, int msgType, string newsMessage, string originExch) { }
    public void receiveFA(int faDataType, string faXmlData) { }
    public void historicalData(int reqId, Bar bar) { }
    public void historicalDataEnd(int reqId, string start, string end) { }
    public void scannerParameters(string xml) { }
    public void scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr) { }
    public void scannerDataEnd(int reqId) { }
    public void realtimeBar(int reqId, long date, double open, double high, double low, double close, decimal volume, decimal wap, int count) { }
    public void fundamentalData(int reqId, string data) { }
    public void deltaNeutralValidation(int reqId, DeltaNeutralContract deltaNeutralContract) { }
    public void tickSnapshotEnd(int reqId) { }
    public void marketDataType(int reqId, int marketDataType) { }
    public void commissionReport(CommissionReport commissionReport) { }
    public void commissionAndFeesReport(CommissionAndFeesReport commissionReport) { }
    public void position(string account, Contract contract, decimal position, double averageCost) { }
    public void positionEnd() { }
    public void accountSummary(int reqId, string account, string tag, string value, string currency) { }
    public void accountSummaryEnd(int reqId) { }
    public void verifyMessageAPI(string apiData) { }
    public void verifyCompleted(bool isSuccessful, string errorText) { }
    public void displayGroupList(int reqId, string groups) { }
    public void displayGroupUpdated(int reqId, string contractInfo) { }
    public void verifyAndAuthMessageAPI(string apiData, string xyzChallenge) { }
    public void verifyAndAuthCompleted(bool isSuccessful, string errorText) { }
    public void positionMulti(int reqId, string account, string modelCode, Contract contract, decimal position, double averageCost) { }
    public void positionMultiEnd(int reqId) { }
    public void accountUpdateMulti(int reqId, string account, string modelCode, string key, string value, string currency) { }
    public void accountUpdateMultiEnd(int reqId) { }
    public void securityDefinitionOptionalParameter(int reqId, string exchange, int underlyingConId, string tradingClass, string multiplier, HashSet<string> expirations, HashSet<double> strikes) { }
    public void securityDefinitionOptionalParameterEnd(int reqId) { }
    public void softDollarTiers(int reqId, SoftDollarTier[] tiers) { }
    public void familyCodes(FamilyCode[] familyCodes) { }
    public void symbolSamples(int reqId, ContractDescription[] contractDescriptions) { }
    public void mktDepthExchanges(DepthMktDataDescription[] depthMktDataDescriptions) { }
    public void tickNews(int tickerId, long timeStamp, string providerCode, string articleId, string headline, string extraData) { }
    public void smartComponents(int reqId, Dictionary<int, KeyValuePair<string, char>> theMap) { }
    public void tickReqParams(int tickerId, double minTick, string bboExchange, int snapshotPermissions) { }
    public void newsProviders(NewsProvider[] newsProviders) { }
    public void newsArticle(int requestId, int articleType, string articleText) { }
    public void historicalNews(int requestId, string time, string providerCode, string articleId, string headline) { }
    public void historicalNewsEnd(int requestId, bool hasMore) { }
    public void headTimestamp(int reqId, string headTimestamp) { }
    public void histogramData(int reqId, IBApi.HistogramEntry[] items) { }
    public void historicalDataUpdate(int reqId, Bar bar) { }
    public void rerouteMktDataReq(int reqId, int conid, string exchange) { }
    public void rerouteMktDepthReq(int reqId, int conid, string exchange) { }
    public void marketRule(int marketRuleId, PriceIncrement[] priceIncrements) { }
    public void pnl(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL) { }
    public void pnlSingle(int reqId, decimal pos, double dailyPnL, double unrealizedPnL, double realizedPnL, double value) { }
    public void historicalTicks(int reqId, HistoricalTick[] ticks, bool done) { }
    public void historicalTicksBidAsk(int reqId, HistoricalTickBidAsk[] ticks, bool done) { }
    public void historicalTicksLast(int reqId, HistoricalTickLast[] ticks, bool done) { }
    public void tickByTickAllLast(int reqId, int tickType, long time, double price, decimal size, TickAttribLast tickAttribLast, string exchange, string specialConditions) { }
    public void tickByTickBidAsk(int reqId, long time, double bidPrice, double askPrice, decimal bidSize, decimal askSize, TickAttribBidAsk tickAttribBidAsk) { }
    public void tickByTickMidPoint(int reqId, long time, double midPoint) { }
    public void orderBound(long orderId, int apiClientId, int apiOrderId) { }
    public void completedOrder(Contract contract, Order order, OrderState orderState) { }
    public void completedOrdersEnd() { }
    public void replaceFAEnd(int reqId, string text) { }
    public void wshMetaData(int reqId, string dataJson) { }
    public void wshEventData(int reqId, string dataJson) { }
    public void historicalSchedule(int reqId, string startDateTime, string endDateTime, string timeZone, HistoricalSession[] sessions) { }
    public void userInfo(int reqId, string whiteBrandingId) { }

    // ProtoBuf methods
    public void orderStatusProtoBuf(IBApi.protobuf.OrderStatus orderStatusProto) { }
    public void openOrderProtoBuf(IBApi.protobuf.OpenOrder openOrderProto) { }
    public void openOrdersEndProtoBuf(IBApi.protobuf.OpenOrdersEnd openOrdersEndProto) { }
    public void errorProtoBuf(IBApi.protobuf.ErrorMessage errorMessageProto) { Log($"⚠️  errorProtoBuf: {errorMessageProto}"); }
    public void execDetailsProtoBuf(IBApi.protobuf.ExecutionDetails executionDetailsProto) { }
    public void execDetailsEndProtoBuf(IBApi.protobuf.ExecutionDetailsEnd executionDetailsEndProto) { }
    public void completedOrderProtoBuf(IBApi.protobuf.CompletedOrder completedOrderProto) { }
    public void completedOrdersEndProtoBuf(IBApi.protobuf.CompletedOrdersEnd completedOrdersEndProto) { }
    public void positionProtoBuf(IBApi.protobuf.Position positionProto) { }
    public void positionEndProtoBuf(IBApi.protobuf.PositionEnd positionEndProto) { }
    public void accountSummaryProtoBuf(IBApi.protobuf.AccountSummary accountSummaryProto) { }
    public void accountSummaryEndProtoBuf(IBApi.protobuf.AccountSummaryEnd accountSummaryEndProto) { }
    public void accountValueProtoBuf(IBApi.protobuf.AccountValue accountValueProto) { }
    public void portfolioValueProtoBuf(IBApi.protobuf.PortfolioValue portfolioValueProto) { }
    public void accountUpdateTimeProtoBuf(IBApi.protobuf.AccountUpdateTime accountUpdateTimeProto) { }
    public void accountDownloadEndProtoBuf(IBApi.protobuf.AccountDownloadEnd accountDownloadEndProto) { }
    public void positionMultiProtoBuf(IBApi.protobuf.PositionMulti positionMultiProto) { }
    public void positionMultiEndProtoBuf(IBApi.protobuf.PositionMultiEnd positionMultiEndProto) { }
    public void accountUpdateMultiProtoBuf(IBApi.protobuf.AccountUpdateMulti accountUpdateMultiProto) { }
    public void accountUpdateMultiEndProtoBuf(IBApi.protobuf.AccountUpdateMultiEnd accountUpdateMultiEndProto) { }
    public void contractDetailsProtoBuf(IBApi.protobuf.ContractDetails contractDetailsProto) { }
    public void contractDetailsEndProtoBuf(IBApi.protobuf.ContractDetailsEnd contractDetailsEndProto) { }
    public void bondContractDetailsProtoBuf(IBApi.protobuf.BondContractDetails bondContractDetailsProto) { }
    public void pnlProtoBuf(IBApi.protobuf.Pnl pnlProto) { }
    public void pnlSingleProtoBuf(IBApi.protobuf.PnlSingle pnlSingleProto) { }
    public void historicalTicksProtoBuf(IBApi.protobuf.HistoricalTicks historicalTicksProto) { }
    public void historicalTicksBidAskProtoBuf(IBApi.protobuf.HistoricalTicksBidAsk historicalTicksBidAskProto) { }
    public void historicalTicksLastProtoBuf(IBApi.protobuf.HistoricalTicksLast historicalTicksLastProto) { }
    public void tickByTickProtoBuf(IBApi.protobuf.TickByTick tickByTickProto) { }
    public void orderBoundProtoBuf(IBApi.protobuf.OrderBound orderBoundProto) { }
    public void historicalDataProtoBuf(IBApi.protobuf.HistoricalData historicalDataProto) { }
    public void historicalDataEndProtoBuf(IBApi.protobuf.HistoricalDataEnd historicalDataEndProto) { }
    public void historicalDataUpdateProtoBuf(IBApi.protobuf.HistoricalDataUpdate historicalDataUpdateProto) { }
    public void headTimestampProtoBuf(IBApi.protobuf.HeadTimestamp headTimestampProto) { }
    public void histogramProtoBuf(IBApi.protobuf.HistogramData histogramProto) { }
    public void historicalNewsProtoBuf(IBApi.protobuf.HistoricalNews historicalNewsProto) { }
    public void historicalNewsEndProtoBuf(IBApi.protobuf.HistoricalNewsEnd historicalNewsEndProto) { }
    public void newsArticleProtoBuf(IBApi.protobuf.NewsArticle newsArticleProto) { }
    public void newsProvidersProtoBuf(IBApi.protobuf.NewsProviders newsProvidersProto) { }
    public void tickNewsProtoBuf(IBApi.protobuf.TickNews tickNewsProto) { }
    public void historicalScheduleProtoBuf(IBApi.protobuf.HistoricalSchedule historicalScheduleProto) { }
    public void rerouteMarketDataRequestProtoBuf(IBApi.protobuf.RerouteMarketDataRequest rerouteMktDataReqProto) { }
    public void rerouteMarketDepthRequestProtoBuf(IBApi.protobuf.RerouteMarketDepthRequest rerouteMktDepthReqProto) { }
    public void secDefOptParameterProtoBuf(IBApi.protobuf.SecDefOptParameter secDefOptParameterProto) { }
    public void secDefOptParameterEndProtoBuf(IBApi.protobuf.SecDefOptParameterEnd secDefOptParameterEndProto) { }
    public void softDollarTiersProtoBuf(IBApi.protobuf.SoftDollarTiers softDollarTiersProto) { }
    public void familyCodesProtoBuf(IBApi.protobuf.FamilyCodes familyCodesProto) { }
    public void symbolSamplesProtoBuf(IBApi.protobuf.SymbolSamples symbolSamplesProto) { }
    public void smartComponentsProtoBuf(IBApi.protobuf.SmartComponents smartComponentsProto) { }
    public void marketRuleProtoBuf(IBApi.protobuf.MarketRule marketRuleProto) { }
    public void userInfoProtoBuf(IBApi.protobuf.UserInfo userInfoProto) { }
    public void nextValidIdProtoBuf(IBApi.protobuf.NextValidId nextValidIdProto) { GotNextValidId = true; Log($"✅ nextValidIdProtoBuf({nextValidIdProto.OrderId})"); _connected.Set(); }
    public void currentTimeProtoBuf(IBApi.protobuf.CurrentTime currentTimeProto) { }
    public void currentTimeInMillisProtoBuf(IBApi.protobuf.CurrentTimeInMillis currentTimeInMillisProto) { }
    public void verifyMessageApiProtoBuf(IBApi.protobuf.VerifyMessageApi verifyMessageApiProto) { }
    public void verifyCompletedProtoBuf(IBApi.protobuf.VerifyCompleted verifyCompletedProto) { }
    public void displayGroupListProtoBuf(IBApi.protobuf.DisplayGroupList displayGroupListProto) { }
    public void displayGroupUpdatedProtoBuf(IBApi.protobuf.DisplayGroupUpdated displayGroupUpdatedProto) { }
    public void marketDepthExchangesProtoBuf(IBApi.protobuf.MarketDepthExchanges marketDepthExchangesProto) { }
}

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        string host = args.Length > 0 ? args[0] : "127.0.0.1";
        int port = args.Length > 1 ? int.Parse(args[1]) : 4001;
        int clientId = args.Length > 2 ? int.Parse(args[2]) : 10;

        Console.WriteLine($"uProbe — IBKR Gateway Handshake Test");
        Console.WriteLine($"Attempting: {host}:{port} clientId={clientId}");
        Console.WriteLine();

        var signal = new EReaderMonitorSignal();
        var wrapper = new MinimalWrapper();
        var client = new EClientSocket(wrapper, signal);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        Console.WriteLine("[1/5] Calling eConnect()...");
        try
        {
            client.eConnect(host, port, clientId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ eConnect() threw exception: {ex.Message}");
            return 1;
        }

        if (!client.IsConnected())
        {
            Console.WriteLine("❌ eConnect() returned but IsConnected=false");
            return 1;
        }
        Console.WriteLine($"✅ eConnect() completed, IsConnected=true ({sw.Elapsed.TotalMilliseconds:F0}ms)");

        Console.WriteLine("[2/5] Starting EReader...");
        var reader = new EReader(client, signal);
        reader.Start();

        Console.WriteLine("[3/5] Starting message processing thread...");
        _ = Task.Run(() =>
        {
            while (client.IsConnected())
            {
                signal.waitForSignal();
                reader.processMsgs();
            }
        });

        Console.WriteLine("[4/5] Calling startApi()... ⚡ CRITICAL STEP");
        client.startApi();

        Console.WriteLine("[5/5] Waiting for nextValidId (10s timeout)...");
        var gotCallback = wrapper.Connected.WaitOne(TimeSpan.FromSeconds(10));

        sw.Stop();

        Console.WriteLine();
        if (gotCallback && wrapper.GotNextValidId)
        {
            Console.WriteLine($"✅ SUCCESS in {sw.Elapsed.TotalMilliseconds:F0}ms");
            Console.WriteLine($"   connectAck: {wrapper.GotConnectAck}");
            Console.WriteLine($"   nextValidId: {wrapper.GotNextValidId}");

            if (client.IsConnected())
                client.eDisconnect();

            return 0;
        }
        else
        {
            Console.WriteLine($"❌ TIMEOUT after {sw.Elapsed.TotalMilliseconds:F0}ms");
            Console.WriteLine($"   connectAck: {wrapper.GotConnectAck}");
            Console.WriteLine($"   nextValidId: {wrapper.GotNextValidId}");
            Console.WriteLine($"   Events captured: {wrapper.Events.Count}");

            if (wrapper.Events.Count > 0)
            {
                Console.WriteLine("\nEvents:");
                wrapper.Events.ForEach(Console.WriteLine);
            }

            if (client.IsConnected())
                client.eDisconnect();

            return 2;
        }
    }
}
