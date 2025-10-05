// IbkrConnection.cs — Read-only IBKR connection using TWS API

using IBApi;
using System.Collections.Concurrent;
using AutoRevOption.Shared.Configuration;

namespace AutoRevOption.Shared.Ibkr;

public class AccountInfo
{
    public string AccountId { get; set; } = string.Empty;
    public decimal NetLiquidation { get; set; }
    public decimal Cash { get; set; }
    public decimal BuyingPower { get; set; }
    public decimal MaintenanceMargin { get; set; }
    public decimal MaintenancePct { get; set; }
    public Dictionary<string, string> AllValues { get; set; } = new();
}

public class PositionInfo
{
    public string Account { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string SecType { get; set; } = string.Empty;
    public string Right { get; set; } = string.Empty;
    public double Strike { get; set; }
    public string Expiry { get; set; } = string.Empty;
    public decimal Position { get; set; }
    public double AvgCost { get; set; }
    public double MarketPrice { get; set; }
    public double MarketValue { get; set; }
    public double UnrealizedPnL { get; set; }
    public double RealizedPnL { get; set; }
}

/// <summary>
/// Read-only IBKR connection wrapper - monitors account and positions
/// </summary>
public class IbkrConnection : EWrapper
{
    private readonly EClientSocket _client;
    private readonly IBKRCredentials _credentials;
    private readonly EReaderSignal _signal;

    private bool _isConnected;
    private int _nextRequestId = 1;

    // Data storage
    private readonly ConcurrentDictionary<string, AccountInfo> _accountData = new();
    private readonly ConcurrentDictionary<string, PositionInfo> _positions = new();
    private readonly ManualResetEvent _accountUpdateComplete = new(false);
    private readonly ManualResetEvent _positionsComplete = new(false);
    private readonly ManualResetEvent _connectionComplete = new(false);

    public IbkrConnection(IBKRCredentials credentials)
    {
        _credentials = credentials;
        _signal = new EReaderMonitorSignal();
        _client = new EClientSocket(this, _signal);
    }

    public bool IsConnected => _isConnected;

    #region Connection Management

    public async Task<bool> ConnectAsync()
    {
        Console.WriteLine($"[IBKR] Connecting to {_credentials.Host}:{_credentials.Port} (ClientId: {_credentials.ClientId})...");

        _connectionComplete.Reset();

        // Run eConnect in a separate task to avoid blocking
        await Task.Run(() => _client.eConnect(_credentials.Host, _credentials.Port, _credentials.ClientId));

        Console.WriteLine($"[IBKR] eConnect() completed. IsConnected: {_client.IsConnected()}");

        if (!_client.IsConnected())
        {
            Console.WriteLine("[IBKR] ❌ Failed to connect (eConnect failed immediately)");
            return false;
        }

        // Start message processing thread immediately
        var reader = new EReader(_client, _signal);
        reader.Start();
        Console.WriteLine("[IBKR] EReader started, launching message processing thread...");

        _ = Task.Run(() =>
        {
            Console.WriteLine("[IBKR] Message processing thread started");
            while (_client.IsConnected())
            {
                _signal.waitForSignal();
                reader.processMsgs();
            }
            Console.WriteLine("[IBKR] Message processing thread exited");
        });

        // Wait for connection acknowledgment (with timeout)
        Console.WriteLine("[IBKR] Waiting for connection acknowledgment (10s timeout)...");
        var connected = await Task.Run(() => _connectionComplete.WaitOne(10000));

        if (!connected || !_client.IsConnected())
        {
            Console.WriteLine($"[IBKR] ❌ Failed to connect (timeout or connection refused). Connected signal: {connected}, IsConnected: {_client.IsConnected()}");
            return false;
        }

        _isConnected = true;
        Console.WriteLine("[IBKR] ✅ Connected successfully");
        return true;
    }

    public void Disconnect()
    {
        if (_client.IsConnected())
        {
            _client.eDisconnect();
            _isConnected = false;
            Console.WriteLine("[IBKR] Disconnected");
        }
    }

    #endregion

    #region Read-Only Operations

    public async Task<AccountInfo?> GetAccountInfoAsync(string accountId, int timeoutMs = 5000)
    {
        if (!_isConnected)
            throw new InvalidOperationException("Not connected to IBKR");

        _accountUpdateComplete.Reset();
        _accountData.Clear();

        Console.WriteLine($"[IBKR] Requesting account summary for {accountId}...");

        var reqId = _nextRequestId++;
        _client.reqAccountSummary(reqId, "All", "NetLiquidation,TotalCashValue,BuyingPower,MaintMarginReq");

        // Wait for response asynchronously
        await Task.Run(() => _accountUpdateComplete.WaitOne(timeoutMs));

        if (!_accountUpdateComplete.WaitOne(0))
        {
            Console.WriteLine("[IBKR] ⚠️  Account summary request timed out");
            return null;
        }

        return _accountData.Values.FirstOrDefault();
    }

    public async Task<List<PositionInfo>> GetPositionsAsync(int timeoutMs = 5000)
    {
        if (!_isConnected)
            throw new InvalidOperationException("Not connected to IBKR");

        _positionsComplete.Reset();
        _positions.Clear();

        Console.WriteLine("[IBKR] Requesting positions...");

        _client.reqPositions();

        // Wait for response asynchronously
        await Task.Run(() => _positionsComplete.WaitOne(timeoutMs));

        if (!_positionsComplete.WaitOne(0))
        {
            Console.WriteLine("[IBKR] ⚠️  Positions request timed out");
            return new List<PositionInfo>();
        }

        return _positions.Values.ToList();
    }

    #endregion

    #region EWrapper Implementation - Connection

    public void connectionClosed()
    {
        _isConnected = false;
        Console.WriteLine("[IBKR] Connection closed");
    }

    public void connectAck()
    {
        Console.WriteLine("[IBKR] Connection acknowledged");
        _connectionComplete.Set();
    }

    public void error(int id, int errorCode, string errorMsg, string advancedOrderRejectJson)
    {
        // Always log all errors during connection debugging
        Console.WriteLine($"[IBKR] Error/Info [{errorCode}]: {errorMsg}");

        // Filter out informational messages
        if (errorCode == 2104 || errorCode == 2106 || errorCode == 2158)
        {
            // Connection success indicators
            _connectionComplete.Set();
        }
        else if (errorCode >= 2000 && errorCode < 3000)
        {
            // Warnings
        }
        else if (errorCode == 502 || errorCode == 503 || errorCode == 504)
        {
            // Connection errors
            Console.WriteLine($"[IBKR] ❌ Connection error: {errorMsg}");
        }
    }

    public void error(Exception e)
    {
        Console.WriteLine($"[IBKR] Exception: {e.Message}");
    }

    public void error(string str)
    {
        Console.WriteLine($"[IBKR] Error: {str}");
    }

    public void error(int id, long orderId, int errorCode, string errorMsg, string advancedOrderRejectJson)
    {
        // Newer error signature with orderId as long
        if (errorCode == 2104 || errorCode == 2106 || errorCode == 2158)
        {
            Console.WriteLine($"[IBKR] Info: {errorMsg}");
        }
        else if (errorCode >= 2000 && errorCode < 3000)
        {
            Console.WriteLine($"[IBKR] Warning [{errorCode}]: {errorMsg}");
        }
        else
        {
            Console.WriteLine($"[IBKR] Error [{errorCode}]: {errorMsg}");
        }
    }

    #endregion

    #region EWrapper Implementation - Account Data

    public void accountSummary(int reqId, string account, string tag, string value, string currency)
    {
        if (!_accountData.ContainsKey(account))
        {
            _accountData[account] = new AccountInfo { AccountId = account };
        }

        var info = _accountData[account];
        info.AllValues[tag] = value;

        switch (tag)
        {
            case "NetLiquidation":
                info.NetLiquidation = decimal.Parse(value);
                break;
            case "TotalCashValue":
                info.Cash = decimal.Parse(value);
                break;
            case "BuyingPower":
                info.BuyingPower = decimal.Parse(value);
                break;
            case "MaintMarginReq":
                info.MaintenanceMargin = decimal.Parse(value);
                if (info.NetLiquidation > 0)
                    info.MaintenancePct = info.MaintenanceMargin / info.NetLiquidation;
                break;
        }

        Console.WriteLine($"[IBKR] Account {account}: {tag} = {value}");
    }

    public void accountSummaryEnd(int reqId)
    {
        Console.WriteLine("[IBKR] Account summary complete");
        _accountUpdateComplete.Set();
    }

    public void position(string account, Contract contract, decimal pos, double avgCost)
    {
        var key = $"{account}_{contract.Symbol}_{contract.SecType}_{contract.Strike}_{contract.LastTradeDateOrContractMonth}";

        var posInfo = new PositionInfo
        {
            Account = account,
            Symbol = contract.Symbol,
            SecType = contract.SecType,
            Right = contract.Right,
            Strike = contract.Strike,
            Expiry = contract.LastTradeDateOrContractMonth,
            Position = pos,
            AvgCost = avgCost
        };

        _positions[key] = posInfo;

        var desc = contract.SecType == "OPT"
            ? $"{contract.Symbol} {contract.Right} {contract.Strike} exp:{contract.LastTradeDateOrContractMonth}"
            : contract.Symbol;

        Console.WriteLine($"[IBKR] Position: {desc} | Qty: {pos} | Avg: ${avgCost:F2}");
    }

    public void positionEnd()
    {
        Console.WriteLine($"[IBKR] Positions complete ({_positions.Count} positions)");
        _positionsComplete.Set();
    }

    #endregion

    #region EWrapper Implementation - Required but Unused

    public void currentTime(long time) { }
    public void nextValidId(int orderId)
    {
        _nextRequestId = orderId;
        _connectionComplete.Set();
        Console.WriteLine($"[IBKR] Next valid order ID: {orderId}");
    }
    public void managedAccounts(string accountsList)
    {
        Console.WriteLine($"[IBKR] Managed accounts: {accountsList}");
    }

    // Market data (unused in read-only mode)
    public void tickPrice(int tickerId, int field, double price, TickAttrib attribs) { }
    public void tickSize(int tickerId, int field, decimal size) { }
    public void tickString(int tickerId, int tickType, string value) { }
    public void tickGeneric(int tickerId, int field, double value) { }
    public void tickOptionComputation(int tickerId, int field, int tickAttrib, double impliedVolatility, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice) { }
    public void tickSnapshotEnd(int tickerId) { }
    public void tickReqParams(int tickerId, double minTick, string bboExchange, int snapshotPermissions) { }
    public void marketDataType(int reqId, int marketDataType) { }

    // Orders (read-only - not implemented)
    public void orderStatus(int orderId, string status, decimal filled, decimal remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice) { }
    public void orderStatus(int orderId, string status, decimal filled, decimal remaining, double avgFillPrice, long permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice) { }
    public void openOrder(int orderId, Contract contract, Order order, OrderState orderState) { }
    public void openOrderEnd() { }
    public void execDetails(int reqId, Contract contract, IBApi.Execution execution) { }
    public void execDetailsEnd(int reqId) { }
    // CommissionReport removed in newer API - use commissionAndFeesReport instead
    public void commissionAndFeesReport(CommissionAndFeesReport commissionAndFeesReport) { }

    // Other required methods
    public void contractDetails(int reqId, ContractDetails contractDetails) { }
    public void contractDetailsEnd(int reqId) { }
    public void updateAccountValue(string key, string value, string currency, string accountName) { }
    public void updatePortfolio(Contract contract, decimal position, double marketPrice, double marketValue, double averageCost, double unrealisedPNL, double realisedPNL, string accountName) { }
    public void updateAccountTime(string timestamp) { }
    public void accountDownloadEnd(string account) { }
    public void bondContractDetails(int reqId, ContractDetails contract) { }
    public void updateMktDepth(int tickerId, int position, int operation, int side, double price, decimal size) { }
    public void updateMktDepthL2(int tickerId, int position, string marketMaker, int operation, int side, double price, decimal size, bool isSmartDepth) { }
    public void updateNewsBulletin(int msgId, int msgType, string message, string origExchange) { }
    public void receiveFA(int faDataType, string faXmlData) { }
    public void historicalData(int reqId, Bar bar) { }
    public void historicalDataEnd(int reqId, string start, string end) { }
    public void scannerParameters(string xml) { }
    public void scannerData(int reqId, int rank, ContractDetails contractDetails, string distance, string benchmark, string projection, string legsStr) { }
    public void scannerDataEnd(int reqId) { }
    public void realtimeBar(int reqId, long time, double open, double high, double low, double close, decimal volume, decimal WAP, int count) { }
    public void fundamentalData(int reqId, string data) { }
    public void deltaNeutralValidation(int reqId, DeltaNeutralContract deltaNeutralContract) { }
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
    public void displayGroupList(int reqId, string groups) { }
    public void displayGroupUpdated(int reqId, string contractInfo) { }
    public void verifyMessageAPI(string apiData) { }
    public void verifyCompleted(bool isSuccessful, string errorText) { }
    public void verifyAndAuthMessageAPI(string apiData, string xyzChallenge) { }
    public void verifyAndAuthCompleted(bool isSuccessful, string errorText) { }
    public void positionMulti(int reqId, string account, string modelCode, Contract contract, decimal pos, double avgCost) { }
    public void positionMultiEnd(int reqId) { }
    public void accountUpdateMulti(int reqId, string account, string modelCode, string key, string value, string currency) { }
    public void accountUpdateMultiEnd(int reqId) { }
    public void securityDefinitionOptionParameter(int reqId, string exchange, int underlyingConId, string tradingClass, string multiplier, HashSet<string> expirations, HashSet<double> strikes) { }
    public void securityDefinitionOptionParameterEnd(int reqId) { }
    public void softDollarTiers(int reqId, SoftDollarTier[] tiers) { }
    public void familyCodes(FamilyCode[] familyCodes) { }
    public void symbolSamples(int reqId, ContractDescription[] contractDescriptions) { }
    public void mktDepthExchanges(DepthMktDataDescription[] depthMktDataDescriptions) { }
    public void tickNews(int tickerId, long timeStamp, string providerCode, string articleId, string headline, string extraData) { }
    public void smartComponents(int reqId, Dictionary<int, KeyValuePair<string, char>> theMap) { }
    public void newsProviders(NewsProvider[] newsProviders) { }
    public void newsArticle(int requestId, int articleType, string articleText) { }
    public void historicalNews(int requestId, string time, string providerCode, string articleId, string headline) { }
    public void historicalNewsEnd(int requestId, bool hasMore) { }
    public void headTimestamp(int reqId, string headTimestamp) { }
    public void histogramData(int reqId, HistogramEntry[] data) { }
    public void rerouteMktDataReq(int reqId, int conId, string exchange) { }
    public void rerouteMktDepthReq(int reqId, int conId, string exchange) { }
    public void historicalDataUpdate(int reqId, Bar bar) { }
    public void currentTimeInMillis(long time) { }
    public void tickEFP(int tickerId, int tickType, double basisPoints, string formattedBasisPoints, double impliedFuture, int holdDays, string futureLastTradeDate, double dividendImpact, double dividendsToLastTradeDate) { }

    // ProtoBuf methods (new in recent API versions)
    public void orderStatusProtoBuf(IBApi.protobuf.OrderStatus orderStatusProto) { }
    public void openOrderProtoBuf(IBApi.protobuf.OpenOrder openOrderProto) { }
    public void openOrdersEndProtoBuf(IBApi.protobuf.OpenOrdersEnd openOrdersEndProto) { }
    public void errorProtoBuf(IBApi.protobuf.ErrorMessage errorMessageProto)
    {
        Console.WriteLine($"[IBKR] ProtoBuf Error [{errorMessageProto.ErrorCode}]: {errorMessageProto.ErrorMsg}");
    }
    public void execDetailsProtoBuf(IBApi.protobuf.ExecutionDetails executionDetailsProto) { }
    public void execDetailsEndProtoBuf(IBApi.protobuf.ExecutionDetailsEnd executionDetailsEndProto) { }
    public void completedOrderProtoBuf(IBApi.protobuf.CompletedOrder completedOrderProto) { }
    public void completedOrdersEndProtoBuf(IBApi.protobuf.CompletedOrdersEnd completedOrdersEndProto) { }
    public void orderBoundProtoBuf(IBApi.protobuf.OrderBound orderBoundProto) { }
    public void contractDataProtoBuf(IBApi.protobuf.ContractData contractDataProto) { }
    public void bondContractDataProtoBuf(IBApi.protobuf.ContractData bondContractDataProto) { }
    public void contractDataEndProtoBuf(IBApi.protobuf.ContractDataEnd contractDataEndProto) { }
    public void tickPriceProtoBuf(IBApi.protobuf.TickPrice tickPriceProto) { }
    public void tickSizeProtoBuf(IBApi.protobuf.TickSize tickSizeProto) { }
    public void tickOptionComputationProtoBuf(IBApi.protobuf.TickOptionComputation tickOptionComputationProto) { }
    public void tickGenericProtoBuf(IBApi.protobuf.TickGeneric tickGenericProto) { }
    public void tickStringProtoBuf(IBApi.protobuf.TickString tickStringProto) { }
    public void tickSnapshotEndProtoBuf(IBApi.protobuf.TickSnapshotEnd tickSnapshotEndProto) { }
    public void updateMarketDepthProtoBuf(IBApi.protobuf.MarketDepth marketDepthProto) { }
    public void updateMarketDepthL2ProtoBuf(IBApi.protobuf.MarketDepthL2 marketDepthL2Proto) { }
    public void marketDataTypeProtoBuf(IBApi.protobuf.MarketDataType marketDataTypeProto) { }
    public void tickReqParamsProtoBuf(IBApi.protobuf.TickReqParams tickReqParamsProto) { }
    public void updateAccountValueProtoBuf(IBApi.protobuf.AccountValue accountValueProto) { }
    public void updatePortfolioProtoBuf(IBApi.protobuf.PortfolioValue portfolioValueProto) { }
    public void updateAccountTimeProtoBuf(IBApi.protobuf.AccountUpdateTime accountUpdateTimeProto) { }
    public void accountDataEndProtoBuf(IBApi.protobuf.AccountDataEnd accountDataEndProto) { }
    public void accountSummaryProtoBuf(IBApi.protobuf.AccountSummary accountSummaryProto) { }
    public void accountSummaryEndProtoBuf(IBApi.protobuf.AccountSummaryEnd accountSummaryEndProto) { }
    public void positionProtoBuf(IBApi.protobuf.Position positionProto) { }
    public void positionEndProtoBuf(IBApi.protobuf.PositionEnd positionEndProto) { }
    public void accountUpdateMultiProtoBuf(IBApi.protobuf.AccountUpdateMulti accountUpdateMultiProto) { }
    public void accountUpdateMultiEndProtoBuf(IBApi.protobuf.AccountUpdateMultiEnd accountUpdateMultiEndProto) { }
    public void positionMultiProtoBuf(IBApi.protobuf.PositionMulti positionMultiProto) { }
    public void positionMultiEndProtoBuf(IBApi.protobuf.PositionMultiEnd positionMultiEndProto) { }
    public void historicalDataProtoBuf(IBApi.protobuf.HistoricalData historicalDataProto) { }
    public void historicalDataEndProtoBuf(IBApi.protobuf.HistoricalDataEnd historicalDataEndProto) { }
    public void historicalDataUpdateProtoBuf(IBApi.protobuf.HistoricalDataUpdate historicalDataUpdateProto) { }
    public void realTimeBarTickProtoBuf(IBApi.protobuf.RealTimeBarTick realTimeBarTickProto) { }
    public void fundamentalsDataProtoBuf(IBApi.protobuf.FundamentalsData fundamentalsDataProto) { }
    public void scannerParametersProtoBuf(IBApi.protobuf.ScannerParameters scannerParametersProto) { }
    public void scannerDataProtoBuf(IBApi.protobuf.ScannerData scannerDataProto) { }
    public void commissionAndFeesReportProtoBuf(IBApi.protobuf.CommissionAndFeesReport commissionAndFeesReportProto) { }
    public void updateNewsBulletinProtoBuf(IBApi.protobuf.NewsBulletin newsBulletinProto) { }
    public void historicalTicksProtoBuf(IBApi.protobuf.HistoricalTicks historicalTicksProto) { }
    public void historicalTicksBidAskProtoBuf(IBApi.protobuf.HistoricalTicksBidAsk historicalTicksBidAskProto) { }
    public void historicalTicksLastProtoBuf(IBApi.protobuf.HistoricalTicksLast historicalTicksLastProto) { }
    public void managedAccountsProtoBuf(IBApi.protobuf.ManagedAccounts managedAccountsProto) { }
    public void tickByTickDataProtoBuf(IBApi.protobuf.TickByTickData tickByTickDataProto) { }
    public void headTimestampProtoBuf(IBApi.protobuf.HeadTimestamp headTimestampProto) { }
    public void histogramDataProtoBuf(IBApi.protobuf.HistogramData histogramDataProto) { }
    public void newsProvidersProtoBuf(IBApi.protobuf.NewsProviders newsProvidersProto) { }
    public void tickNewsProtoBuf(IBApi.protobuf.TickNews tickNewsProto) { }
    public void newsArticleProtoBuf(IBApi.protobuf.NewsArticle newsArticleProto) { }
    public void historicalNewsProtoBuf(IBApi.protobuf.HistoricalNews historicalNewsProto) { }
    public void historicalNewsEndProtoBuf(IBApi.protobuf.HistoricalNewsEnd historicalNewsEndProto) { }
    public void pnlProtoBuf(IBApi.protobuf.PnL pnlProto) { }
    public void pnlSingleProtoBuf(IBApi.protobuf.PnLSingle pnlSingleProto) { }
    public void wshMetaDataProtoBuf(IBApi.protobuf.WshMetaData wshMetaDataProto) { }
    public void wshEventDataProtoBuf(IBApi.protobuf.WshEventData wshEventDataProto) { }
    public void receiveFAProtoBuf(IBApi.protobuf.ReceiveFA receiveFAProto) { }
    public void replaceFAEndProtoBuf(IBApi.protobuf.ReplaceFAEnd replaceFAEndProto) { }
    public void historicalScheduleProtoBuf(IBApi.protobuf.HistoricalSchedule historicalScheduleProto) { }
    public void rerouteMarketDataRequestProtoBuf(IBApi.protobuf.RerouteMarketDataRequest rerouteMarketDataRequestProto) { }
    public void rerouteMarketDepthRequestProtoBuf(IBApi.protobuf.RerouteMarketDepthRequest rerouteMarketDepthRequestProto) { }
    public void secDefOptParameterProtoBuf(IBApi.protobuf.SecDefOptParameter secDefOptParameterProto) { }
    public void secDefOptParameterEndProtoBuf(IBApi.protobuf.SecDefOptParameterEnd secDefOptParameterEndProto) { }
    public void softDollarTiersProtoBuf(IBApi.protobuf.SoftDollarTiers softDollarTiersProto) { }
    public void familyCodesProtoBuf(IBApi.protobuf.FamilyCodes familyCodesProto) { }
    public void symbolSamplesProtoBuf(IBApi.protobuf.SymbolSamples symbolSamplesProto) { }
    public void smartComponentsProtoBuf(IBApi.protobuf.SmartComponents smartComponentsProto) { }
    public void marketRuleProtoBuf(IBApi.protobuf.MarketRule marketRuleProto) { }
    public void userInfoProtoBuf(IBApi.protobuf.UserInfo userInfoProto) { }
    public void nextValidIdProtoBuf(IBApi.protobuf.NextValidId nextValidIdProto) { }
    public void currentTimeProtoBuf(IBApi.protobuf.CurrentTime currentTimeProto) { }
    public void currentTimeInMillisProtoBuf(IBApi.protobuf.CurrentTimeInMillis currentTimeInMillisProto) { }
    public void verifyMessageApiProtoBuf(IBApi.protobuf.VerifyMessageApi verifyMessageApiProto) { }
    public void verifyCompletedProtoBuf(IBApi.protobuf.VerifyCompleted verifyCompletedProto) { }
    public void displayGroupListProtoBuf(IBApi.protobuf.DisplayGroupList displayGroupListProto) { }
    public void displayGroupUpdatedProtoBuf(IBApi.protobuf.DisplayGroupUpdated displayGroupUpdatedProto) { }
    public void marketDepthExchangesProtoBuf(IBApi.protobuf.MarketDepthExchanges marketDepthExchangesProto) { }

    #endregion
}
