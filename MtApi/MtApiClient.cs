﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MTApiService;
using System.Drawing;
using System.Collections;
using System.Net;

namespace MtApi
{
    public delegate void MtApiQuoteHandler(object sender, string symbol, double bid, double ask);

    public sealed class MtApiClient
    {
        private const int DOUBLE_ARRAY_LIMIT = 64800;

        #region MetaTrader Constants

        //Special constant
        public const int NULL = 0;
        public const int EMPTY = -1;
        #endregion

        #region ctor

        public MtApiClient()
        {
            mClient.QuoteAdded +=new MtClient.MtQuoteHandler(mClient_QuoteAdded);
            mClient.QuoteRemoved += new MtClient.MtQuoteHandler(mClient_QuoteRemoved);
            mClient.QuoteUpdated += new MtClient.MtQuoteHandler(mClient_QuoteUpdated);
            mClient.ServerDisconnected += new EventHandler(mClient_ServerDisconnected);
            mClient.ServerFailed += new EventHandler(mClient_ServerFailed);
        }
        #endregion

        #region Public Methods
        ///<summary>
        ///Connect with MetaTrader API. Async method.
        ///</summary>
        ///<param name="host">Address of MetaTrader host (ex. 192.168.1.2)</param>
        ///<param name="port">Port of host connection (default 8222) </param>
        public void BeginConnect(string host, int port)
        {
            //if (string.IsNullOrEmpty(host) == false && (host.Equals("localhost") || host.Equals("127.0.0.1")))
            //{
            //    this.BeginConnect(port);
            //}
            //else
            //{
            Action<string, int> connectAction = Connect;
            connectAction.BeginInvoke(host, port, null, null);
            //}
        }

        ///<summary>
        ///Connect with MetaTrader API. Async method.
        ///</summary>
        ///<param name="port">Port of host connection (default 8222) </param>
        public void BeginConnect(int port)
        {
            Action<int> connectAction = Connect;
            connectAction.BeginInvoke(port, null, null);
        }

        ///<summary>
        ///Disconnect from MetaTrader API. Async method.
        ///</summary>
        public void BeginDisconnect()
        {
            Action disconnectAction = Disconnect;
            disconnectAction.BeginInvoke(null, null);
        }

        ///<summary>
        ///Load quotes connected into MetaTrader API.
        ///</summary>
        public IEnumerable<MtQuote> GetQuotes()
        {
            var quotes = mClient.GetQuotes();
            return quotes != null ? (from q in quotes select q.Parse()) : null;
        }
        #endregion

        #region Properties        
        ///<summary>
        ///Connection status of MetaTrader API.
        ///</summary>
        public MtConnectionState ConnectionState { get; private set; }
        #endregion


        #region Trading functions

        public int OrderSend(string symbol, TradeOperation cmd, double volume, double price, int slippage, double stoploss, double takeprofit
            , string comment, int magic, DateTime expiration, Color arrow_color)
        {
            var commandParameters = new ArrayList { symbol, (int)cmd, volume, price, slippage, stoploss, takeprofit
                , comment, magic, MtApiTimeConverter.ConvertToMtTime(expiration), MtApiColorConverter.ConvertToMtColor(arrow_color) };

            return sendCommand<int>(MtCommandType.OrderSend, commandParameters);
        }

        public int OrderSend(string symbol, TradeOperation cmd, double volume, double price, int slippage, double stoploss, double takeprofit
                    , string comment, int magic, DateTime expiration)
        {
            return OrderSend(symbol, cmd, volume, price, slippage, stoploss, takeprofit, comment, magic, expiration, Color.Empty);
        }

        public int OrderSend(string symbol, TradeOperation cmd, double volume, double price, int slippage, double stoploss, double takeprofit
                    , string comment, int magic)
        {
            return OrderSend(symbol, cmd, volume, price, slippage, stoploss, takeprofit, comment, magic, DateTime.MinValue, Color.Empty);
        }

        public int OrderSend(string symbol, TradeOperation cmd, double volume, double price, int slippage, double stoploss, double takeprofit
                    , string comment)
        {
            return OrderSend(symbol, cmd, volume, price, slippage, stoploss, takeprofit, comment, 0, DateTime.MinValue, Color.Empty);
        }

        public int OrderSend(string symbol, TradeOperation cmd, double volume, double price, int slippage, double stoploss, double takeprofit)
        {
            return OrderSend(symbol, cmd, volume, price, slippage, stoploss, takeprofit, null, 0, DateTime.MinValue, Color.Empty);
        }

        public int OrderSend(string symbol, TradeOperation cmd, double volume, string price, int slippage, double stoploss, double takeprofit)
        {
            double dPrice = 0;
            if (Double.TryParse(price, out dPrice))
                return OrderSend(symbol, cmd, volume, dPrice, slippage, stoploss, takeprofit, null, 0, DateTime.MinValue, Color.Empty);
            return 0;
        }

        public bool OrderClose(int ticket, double lots, double price, int slippage, Color color)
        {
            var commandParameters = new ArrayList { ticket, lots, price, slippage, MtApiColorConverter.ConvertToMtColor(color) };
            return sendCommand<bool>(MtCommandType.OrderClose, commandParameters);
        }

        public bool OrderClose(int ticket, double lots, double price, int slippage)
        {
            return OrderClose(ticket, lots, price, slippage, Color.Empty);
        }

        public bool OrderCloseBy(int ticket, int opposite, Color color)
        {
            var commandParameters = new ArrayList { ticket, opposite, MtApiColorConverter.ConvertToMtColor(color) };
            return sendCommand<bool>(MtCommandType.OrderCloseBy, commandParameters);
        }

        public bool OrderCloseBy(int ticket, int opposite)
        {
            return OrderCloseBy(ticket, opposite, Color.Empty);
        }

        public double OrderClosePrice()
        {
            return sendCommand<double>(MtCommandType.OrderClosePrice, null);
        }

        public double OrderClosePrice(int ticket)
        {
            var commandParameters = new ArrayList { ticket };
            double retVal = sendCommand<double>(MtCommandType.OrderClosePriceByTicket, commandParameters);

            return retVal;
        }

        public DateTime OrderCloseTime()
        {
            var commandResponse = sendCommand<int>(MtCommandType.OrderCloseTime, null);
            return MtApiTimeConverter.ConvertFromMtTime(commandResponse);
        }

        public string OrderComment()
        {
            return sendCommand<string>(MtCommandType.OrderComment, null);
        }

        public double OrderCommission()
        {
            return sendCommand<double>(MtCommandType.OrderCommission, null);
        }

        public bool OrderDelete(int ticket, Color color)
        {
            var commandParameters = new ArrayList { ticket, MtApiColorConverter.ConvertToMtColor(color) };
            return sendCommand<bool>(MtCommandType.OrderDelete, commandParameters);
        }

        public bool OrderDelete(int ticket)
        {
            return OrderDelete(ticket, Color.Empty);
        }

        public DateTime OrderExpiration()
        {
            var commandResponse = sendCommand<int>(MtCommandType.OrderExpiration, null); 
            return MtApiTimeConverter.ConvertFromMtTime(commandResponse);
        }

        public double OrderLots()
        {
            return sendCommand<double>(MtCommandType.OrderLots, null);
        }

        public int OrderMagicNumber()
        {
            return sendCommand<int>(MtCommandType.OrderMagicNumber, null);
        }

        public bool OrderModify(int ticket, double price, double stoploss, double takeprofit, DateTime expiration, Color arrow_color)
        {
            var commandParameters = new ArrayList { ticket, price, stoploss, takeprofit, MtApiTimeConverter.ConvertToMtTime(expiration), MtApiColorConverter.ConvertToMtColor(arrow_color) };
            return sendCommand<bool>(MtCommandType.OrderModify, commandParameters);
        }

        public bool OrderModify(int ticket, double price, double stoploss, double takeprofit, DateTime expiration)
        {
            return OrderModify(ticket, price, stoploss, takeprofit, expiration, Color.Empty);
        }

        public double OrderOpenPrice()
        {
            return sendCommand<double>(MtCommandType.OrderOpenPrice, null);
        }

        public double OrderOpenPrice(int ticket)
        {
            var commandParameters = new ArrayList { ticket };
            double retVal = sendCommand<double>(MtCommandType.OrderOpenPriceByTicket, commandParameters);

            return retVal;
        }

        public DateTime OrderOpenTime()
        {
            var commandResponse = sendCommand<int>(MtCommandType.OrderOpenTime, null);            
            return MtApiTimeConverter.ConvertFromMtTime(commandResponse);
        }

        public void OrderPrint()
        {
            sendCommand<object>(MtCommandType.OrderPrint, null);
        }

        public double OrderProfit()
        {
            return sendCommand<double>(MtCommandType.OrderProfit, null);
        }

        public bool OrderSelect(int index, OrderSelectMode select, OrderSelectSource pool)
        {
            var commandParameters = new ArrayList { index, (int)select, (int)pool };
            return sendCommand<bool>(MtCommandType.OrderSelect, commandParameters);
        }

        public bool OrderSelect(int index, OrderSelectMode select)
        {
            return OrderSelect(index, select, OrderSelectSource.MODE_TRADES);
        }

        public int OrdersHistoryTotal()
        {
            return sendCommand<int>(MtCommandType.OrdersHistoryTotal, null);
        }

        public double OrderStopLoss()
        {
            return sendCommand<double>(MtCommandType.OrderStopLoss, null);
        }

        public int OrdersTotal()
        {
            return sendCommand<int>(MtCommandType.OrdersTotal, null);
        }

        public double OrderSwap()
        {
            return  sendCommand<double>(MtCommandType.OrderSwap, null);
        }

        public string OrderSymbol()
        {
            return sendCommand<string>(MtCommandType.OrderSymbol, null);
        }

        public double OrderTakeProfit()
        {
            return  sendCommand<double>(MtCommandType.OrderTakeProfit, null);
        }

        public int OrderTicket()
        {
            return sendCommand<int>(MtCommandType.OrderTicket, null);
        }

        public TradeOperation OrderType()
        {
            int retVal = sendCommand<int>(MtCommandType.OrderType, null);

            return (TradeOperation) retVal;
        }

        public bool OrderCloseAll()
        {
            return sendCommand<bool>(MtCommandType.OrderCloseAll, null);
        }
        #endregion

        #region Check Status

        public int GetLastError()
        {
            return sendCommand<int>(MtCommandType.GetLastError, null);
        }

        public bool IsConnected()
        {
            return sendCommand<bool>(MtCommandType.IsConnected, null);
        }

        public bool IsDemo()
        {
            return sendCommand<bool>(MtCommandType.IsDemo, null);
        }

        public bool IsDllsAllowed()
        {
            return sendCommand<bool>(MtCommandType.IsDllsAllowed, null);
        }

        public bool IsExpertEnabled()
        {
            return sendCommand<bool>(MtCommandType.IsExpertEnabled, null);
        }

        public bool IsLibrariesAllowed()
        {
            return sendCommand<bool>(MtCommandType.IsLibrariesAllowed, null);
        }

        public bool IsOptimization()
        {
            return sendCommand<bool>(MtCommandType.IsOptimization, null);
        }

        public bool IsStopped()
        {
            return sendCommand<bool>(MtCommandType.IsStopped, null);
        }

        public bool IsTesting()
        {
            return sendCommand<bool>(MtCommandType.IsTesting, null);
        }

        public bool IsTradeAllowed()
        {
            return sendCommand<bool>(MtCommandType.IsTradeAllowed, null);
        }

        public bool IsTradeContextBusy()
        {
            return sendCommand<bool>(MtCommandType.IsTradeContextBusy, null);
        }

        public bool IsVisualMode()
        {
            return sendCommand<bool>(MtCommandType.IsVisualMode, null);
        }

        public int UninitializeReason()
        {
            return sendCommand<int>(MtCommandType.UninitializeReason, null);
        }

        public string ErrorDescription(int errorCode)
        {
            var commandParameters = new ArrayList { errorCode };
            return sendCommand<string>(MtCommandType.ErrorDescription, commandParameters);
        }

        #endregion

        #region Account Information
        
        public double AccountBalance()
        {
            return sendCommand<double>(MtCommandType.AccountBalance, null);
        }

        public double AccountCredit()
        {
            return sendCommand<double>(MtCommandType.AccountCredit, null);
        }

        public string AccountCompany()
        {
            return sendCommand<string>(MtCommandType.AccountCompany, null);
        }

        public string AccountCurrency()
        {
            return sendCommand<string>(MtCommandType.AccountCurrency, null);
        }

        public double AccountEquity()
        {
            return sendCommand<double>(MtCommandType.AccountEquity, null);
        }

        public double AccountFreeMargin()
        {
            return sendCommand<double>(MtCommandType.AccountFreeMargin, null);
        }

        public double AccountFreeMarginCheck(string symbol, TradeOperation cmd, double volume)
        {
            var commandParameters = new ArrayList { symbol, (int)cmd, volume };
            return sendCommand<double>(MtCommandType.AccountFreeMarginCheck, commandParameters);
        }

        public double AccountFreeMarginMode()
        {
            return sendCommand<double>(MtCommandType.AccountFreeMarginMode, null);
        }

        public int AccountLeverage()
        {
            return sendCommand<int>(MtCommandType.AccountLeverage, null);
        }

        public double AccountMargin()
        {
            return sendCommand<double>(MtCommandType.AccountMargin, null);
        }

        public string AccountName()
        {
            return sendCommand<string>(MtCommandType.AccountName, null);
        }

        public int AccountNumber()
        {
            return sendCommand<int>(MtCommandType.AccountNumber, null);
        }

        public double AccountProfit()
        {
            return sendCommand<double>(MtCommandType.AccountProfit, null);
        }

        public string AccountServer()
        {
            return sendCommand<string>(MtCommandType.AccountServer, null);
        }

        public int AccountStopoutLevel()
        {
            return sendCommand<int>(MtCommandType.AccountStopoutLevel, null);
        }

        public int AccountStopoutMode()
        {
            return sendCommand<int>(MtCommandType.AccountStopoutMode, null);
        }

        #endregion

        #region Common Function

        public void Alert(string msg)
        {
            var commandParameters = new ArrayList { msg };
            sendCommand<object>(MtCommandType.Alert, commandParameters);
        }

        public void Comment(string msg)
        {
            var commandParameters = new ArrayList { msg };
            sendCommand<object>(MtCommandType.Comment, commandParameters);
        }

        public int GetTickCount()
        {
            return sendCommand<int>(MtCommandType.GetTickCount, null);
        }

        public double MarketInfo(string symbol, MarketInfoModeType type)
        {
            var commandParameters = new ArrayList { symbol, (int)type };
            return sendCommand<double>(MtCommandType.MarketInfo, commandParameters);
        }

        public int MessageBox(string text, string caption, int flag)
        {
            var commandParameters = new ArrayList { text, caption, flag };
            return sendCommand<int>(MtCommandType.MessageBoxA, commandParameters);
        }

        public int MessageBox(string text, string caption)
        {
            return MessageBox(text, caption, EMPTY);
        }

        public int MessageBox(string text)
        {
            var commandParameters = new ArrayList { text };
            return sendCommand<int>(MtCommandType.MessageBox, commandParameters);
        }

        public void PlaySound(string filename)
        {
            var commandParameters = new ArrayList { filename };
            sendCommand<object>(MtCommandType.PlaySound, commandParameters);
        }

        public void Print(string msg)
        {
            var commandParameters = new ArrayList { msg };
            sendCommand<object>(MtCommandType.Print, commandParameters);
        }

        public bool SendFTP(string filename)
        {
            var commandParameters = new ArrayList { filename };
            return sendCommand<bool>(MtCommandType.SendFTP, commandParameters);
        }

        public bool SendFTP(string filename, string ftp_path)
        {
            var commandParameters = new ArrayList { filename, ftp_path };
            return sendCommand<bool>(MtCommandType.SendFTPA, commandParameters);
        }

        public void SendMail(string subject, string some_text)
        {
            var commandParameters = new ArrayList { subject, some_text };
            sendCommand<object>(MtCommandType.SendMail, commandParameters);
        }

        public void Sleep(int milliseconds)
        {
            var commandParameters = new ArrayList { milliseconds };
            sendCommand<object>(MtCommandType.Sleep, commandParameters);
        }

        #endregion

        #region Client Terminal Functions

        public string TerminalCompany()
        {
            return sendCommand<string>(MtCommandType.TerminalCompany, null);
        }

        public string TerminalName()
        {
            return sendCommand<string>(MtCommandType.TerminalName, null);
        }

        public string TerminalPath()
        {
            return sendCommand<string>(MtCommandType.TerminalPath, null);
        }

        #endregion

        #region Date and Time Functions

        public int Day()
        {
            return sendCommand<int>(MtCommandType.Day, null);
        }

        public int DayOfWeek()
        {
            return sendCommand<int>(MtCommandType.DayOfWeek, null);
        }

        public int DayOfYear()
        {
            return sendCommand<int>(MtCommandType.DayOfYear, null);
        }

        public int Hour()
        {
            return sendCommand<int>(MtCommandType.Hour, null);
        }

        public int Minute()
        {
            return sendCommand<int>(MtCommandType.Minute, null);
        }

        public int Month()
        {
            return sendCommand<int>(MtCommandType.Month, null);
        }

        public int Seconds()
        {
            return sendCommand<int>(MtCommandType.Seconds, null);
        }

        public DateTime TimeCurrent()
        {
            var commandResponse = sendCommand<int>(MtCommandType.TimeCurrent, null);
            return MtApiTimeConverter.ConvertFromMtTime(commandResponse);
        }

        public int TimeDay(DateTime date)
        {
            var commandParameters = new ArrayList { MtApiTimeConverter.ConvertToMtTime(date) };
            return sendCommand<int>(MtCommandType.TimeDay, commandParameters);
        }

        public int TimeDayOfWeek(DateTime date)
        {
            var commandParameters = new ArrayList { MtApiTimeConverter.ConvertToMtTime(date) };
            return sendCommand<int>(MtCommandType.TimeDayOfWeek, commandParameters);
        }

        public int TimeDayOfYear(DateTime date)
        {
            var commandParameters = new ArrayList { MtApiTimeConverter.ConvertToMtTime(date) };
            return sendCommand<int>(MtCommandType.TimeDayOfYear, commandParameters);
        }

        public int TimeHour(DateTime time)
        {
            var commandParameters = new ArrayList { MtApiTimeConverter.ConvertToMtTime(time) };
            return sendCommand<int>(MtCommandType.TimeHour, commandParameters);
        }

        public DateTime TimeLocal()
        {
            var commandResponse = sendCommand<int>(MtCommandType.TimeLocal, null);
            return MtApiTimeConverter.ConvertFromMtTime(commandResponse);
        }

        public int TimeMinute(DateTime time)
        {
            var commandParameters = new ArrayList { MtApiTimeConverter.ConvertToMtTime(time) };
            return sendCommand<int>(MtCommandType.TimeMinute, commandParameters);
        }

        public int TimeMonth(DateTime time)
        {
            var commandParameters = new ArrayList { MtApiTimeConverter.ConvertToMtTime(time) };
            return sendCommand<int>(MtCommandType.TimeMonth, commandParameters);
        }

        public int TimeSeconds(DateTime time)
        {
            var commandParameters = new ArrayList { MtApiTimeConverter.ConvertToMtTime(time) };
            return sendCommand<int>(MtCommandType.TimeSeconds, commandParameters);
        }

        public int TimeYear(DateTime time)
        {
            var commandParameters = new ArrayList { MtApiTimeConverter.ConvertToMtTime(time) };
            return sendCommand<int>(MtCommandType.TimeYear, commandParameters);
        }

        public int Year(DateTime time)
        {
            var commandParameters = new ArrayList { MtApiTimeConverter.ConvertToMtTime(time) };
            return sendCommand<int>(MtCommandType.Year, commandParameters);
        }

        #endregion

        #region Global Variables Functions
        public bool GlobalVariableCheck(string name)
        {
            var commandParameters = new ArrayList { name };
            return sendCommand<bool>(MtCommandType.GlobalVariableCheck, commandParameters);
        }

        public bool GlobalVariableDel(string name)
        {
            var commandParameters = new ArrayList { name };
            return sendCommand<bool>(MtCommandType.GlobalVariableDel, commandParameters);
        }

        public double GlobalVariableGet(string name)
        {
            var commandParameters = new ArrayList { name };
            return sendCommand<double>(MtCommandType.GlobalVariableGet, commandParameters);
        }

        public string GlobalVariableName(int index)
        {
            var commandParameters = new ArrayList { index };
            return sendCommand<string>(MtCommandType.GlobalVariableName, commandParameters);
        }

        public DateTime GlobalVariableSet(string name, double value)
        {
            var commandParameters = new ArrayList { name, value };
            var commandResponse = sendCommand<int>(MtCommandType.GlobalVariableSet, commandParameters);
            return MtApiTimeConverter.ConvertFromMtTime(commandResponse);
        }

        public bool GlobalVariableSetOnCondition(string name, double value, double check_value)
        {
            var commandParameters = new ArrayList { name, value, check_value };
            return sendCommand<bool>(MtCommandType.GlobalVariableSetOnCondition, commandParameters);
        }

        public int GlobalVariablesDeleteAll(string prefix_name)
        {
            var commandParameters = new ArrayList { prefix_name };
            return sendCommand<int>(MtCommandType.GlobalVariableSetOnCondition, commandParameters);
        }

        public int GlobalVariablesTotal()
        {
            return sendCommand<int>(MtCommandType.GlobalVariablesTotal, null);
        }

        #endregion

        #region Technical Indicators
        public double iAC(string symbol, ChartPeriod timeframe, int shift)
        {
            var commandParameters = new ArrayList { symbol, (int)timeframe, shift };
            return sendCommand<double>(MtCommandType.iAC, commandParameters);
        }

        public double iAD(string symbol, int timeframe, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, shift };
            return sendCommand<double>(MtCommandType.iAD, commandParameters);
        }

        public double iAlligator(string symbol, int timeframe, int jaw_period, int jaw_shift, int teeth_period, int teeth_shift, int lips_period, int lips_shift, int ma_method, int applied_price, int mode, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, jaw_period, jaw_shift, teeth_period, teeth_shift, lips_period, lips_shift, ma_method, applied_price, mode, shift };
            return sendCommand<double>(MtCommandType.iAlligator, commandParameters);
        }

        public double iADX(string symbol, int timeframe, int period, int applied_price, int mode, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, period, applied_price, mode, shift };
            return sendCommand<double>(MtCommandType.iADX, commandParameters);
        }

        public double iATR(string symbol, int timeframe, int period, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, period, shift };
            return sendCommand<double>(MtCommandType.iATR, commandParameters);
        }

        public double iAO(string symbol, int timeframe, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, shift };
            return sendCommand<double>(MtCommandType.iAO, commandParameters);
        }

        public double iBearsPower(string symbol, int timeframe, int period, int applied_price, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, period, applied_price, shift };
            return sendCommand<double>(MtCommandType.iBearsPower, commandParameters);
        }

        public double iBands(string symbol, int timeframe, int period, int deviation, int bands_shift, int applied_price, int mode, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, period, deviation, bands_shift, applied_price, mode, shift };
            return sendCommand<double>(MtCommandType.iBands, commandParameters);
        }

        public double iBandsOnArray(double[] array, int total, int period, int deviation, int bands_shift, int mode, int shift)
        {
            int arraySize = array != null ? array.Length : 0;
            var commandParameters = new ArrayList { arraySize };
            commandParameters.AddRange(array);
            commandParameters.Add(total);
            commandParameters.Add(period);
            commandParameters.Add(deviation);
            commandParameters.Add(bands_shift);
            commandParameters.Add(mode);
            commandParameters.Add(shift);

            return sendCommand<double>(MtCommandType.iBandsOnArray, commandParameters);
        }

        public double iBullsPower(string symbol, int timeframe, int period, int applied_price, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, period, applied_price, shift };
            return sendCommand<double>(MtCommandType.iBullsPower, commandParameters);
        }

        public double iCCI(string symbol, int timeframe, int period, int applied_price, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, period, applied_price, shift };
            return sendCommand<double>(MtCommandType.iCCI, commandParameters);
        }

        public double iCCIOnArray(double[] array, int total, int period, int shift)
        {
            int arraySize = array != null ? array.Length : 0;
            var commandParameters = new ArrayList { arraySize };
            commandParameters.AddRange(array);
            commandParameters.Add(total);
            commandParameters.Add(period);
            commandParameters.Add(shift);

            return sendCommand<double>(MtCommandType.iCCIOnArray, commandParameters);
        }

        public double iCustom(string symbol, int timeframe, string name, int[] parameters, int mode, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, name };
            int arraySize = parameters != null ? parameters.Length : 0;
            commandParameters.Add(arraySize);
            commandParameters.AddRange(parameters);
            commandParameters.Add(mode);
            commandParameters.Add(shift);

            return sendCommand<double>(MtCommandType.iCustom, commandParameters);
        }

        public double iCustom(string symbol, int timeframe, string name, double[] parameters, int mode, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, name };
            int arraySize = parameters != null ? parameters.Length : 0;
            commandParameters.Add(arraySize);
            commandParameters.AddRange(parameters);
            commandParameters.Add(mode);
            commandParameters.Add(shift);

            return sendCommand<double>(MtCommandType.iCustom_d, commandParameters);
        }

        public double iDeMarker(string symbol, int timeframe, int period, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, period, shift };
            return sendCommand<double>(MtCommandType.iDeMarker, commandParameters);
        }

        public double iEnvelopes(string symbol, int timeframe, int ma_period, int ma_method, int ma_shift, int applied_price, double deviation, int mode, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, ma_period, ma_method, ma_shift, applied_price, deviation, mode, shift };
            return sendCommand<double>(MtCommandType.iEnvelopes, commandParameters);
        }

        public double iEnvelopesOnArray(double[] array, int total, int ma_period, int ma_method, int ma_shift, double deviation, int mode, int shift)
        {
            int arraySize = array != null ? array.Length : 0;
            var commandParameters = new ArrayList { arraySize };
            commandParameters.AddRange(array);
            commandParameters.Add(total);
            commandParameters.Add(ma_period);
            commandParameters.Add(ma_method);
            commandParameters.Add(ma_shift);
            commandParameters.Add(deviation);
            commandParameters.Add(mode);
            commandParameters.Add(shift);

            return sendCommand<double>(MtCommandType.iEnvelopesOnArray, commandParameters);
        }

        public double iForce(string symbol, int timeframe, int period, int ma_method, int applied_price, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, period, ma_method, applied_price, shift };
            return sendCommand<double>(MtCommandType.iForce, commandParameters);
        }

        public double iFractals(string symbol, int timeframe, int mode, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, mode, shift };
            return sendCommand<double>(MtCommandType.iFractals, commandParameters);
        }

        public double iGator(string symbol, int timeframe, int jaw_period, int jaw_shift, int teeth_period, int teeth_shift, int lips_period, int lips_shift, int ma_method, int applied_price, int mode, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, jaw_period, jaw_shift, teeth_period, teeth_shift, lips_period, lips_shift, ma_method, applied_price, mode, shift };
            return sendCommand<double>(MtCommandType.iGator, commandParameters);
        }

        public double iIchimoku(string symbol, int timeframe, int tenkan_sen, int kijun_sen, int senkou_span_b, int mode, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, tenkan_sen, kijun_sen, senkou_span_b, mode, shift };
            return sendCommand<double>(MtCommandType.iIchimoku, commandParameters);
        }

        public double iBWMFI(string symbol, int timeframe, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, shift };
            return sendCommand<double>(MtCommandType.iBWMFI, commandParameters);
        }

        public double iMomentum(string symbol, int timeframe, int period, int applied_price, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, period, applied_price, shift };
            return sendCommand<double>(MtCommandType.iMomentum, commandParameters);
        }

        public double iMomentumOnArray(double[] array, int total, int period, int shift)
        {
            int arraySize = array != null ? array.Length : 0;
            var commandParameters = new ArrayList { arraySize };
            commandParameters.AddRange(array);
            commandParameters.Add(total);
            commandParameters.Add(period);
            commandParameters.Add(shift);

            return sendCommand<double>(MtCommandType.iMomentumOnArray, commandParameters);
        }

        public double iMFI(string symbol, int timeframe, int period, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, period, shift };
            return sendCommand<double>(MtCommandType.iMFI, commandParameters);
        }

        public double iMA(string symbol, int timeframe, int period, int ma_shift, int ma_method, int applied_price, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, period, ma_shift, ma_method, applied_price, shift };
            return sendCommand<double>(MtCommandType.iMA, commandParameters);
        }

        double iMAOnArray(double[] array, int total, int period, int ma_shift, int ma_method, int shift)
        {
            int arraySize = array != null ? array.Length : 0;
            var commandParameters = new ArrayList { arraySize };
            commandParameters.AddRange(array);
            commandParameters.Add(total);
            commandParameters.Add(period);
            commandParameters.Add(ma_shift);
            commandParameters.Add(ma_method);
            commandParameters.Add(shift);

            return sendCommand<double>(MtCommandType.iMAOnArray, commandParameters);
        }

        public double iOsMA(string symbol, int timeframe, int fast_ema_period, int slow_ema_period, int signal_period, int applied_price, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, fast_ema_period, slow_ema_period, signal_period, applied_price, shift };
            return sendCommand<double>(MtCommandType.iOsMA, commandParameters);
        }

        public double iMACD(string symbol, int timeframe, int fast_ema_period, int slow_ema_period, int signal_period, int applied_price, int mode, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, fast_ema_period, slow_ema_period, signal_period, applied_price, mode, shift };
            return sendCommand<double>(MtCommandType.iMACD, commandParameters);
        }

        public double iOBV(string symbol, int timeframe, int applied_price, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, applied_price, shift };
            return sendCommand<double>(MtCommandType.iOBV, commandParameters);
        }

        public double iSAR(string symbol, int timeframe, double step, double maximum, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, step, maximum, shift };
            return sendCommand<double>(MtCommandType.iSAR, commandParameters);
        }

        public double iRSI( string symbol, int timeframe, int period, int applied_price, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, period, applied_price, shift };
            return sendCommand<double>(MtCommandType.iRSI, commandParameters);
        }

        public double iRSIOnArray(double[] array, int total, int period, int shift)
        {
            int arraySize = array != null ? array.Length : 0;
            var commandParameters = new ArrayList { arraySize };
            commandParameters.AddRange(array);
            commandParameters.Add(total);
            commandParameters.Add(period);
            commandParameters.Add(shift);

            return sendCommand<double>(MtCommandType.iMomentumOnArray, commandParameters);
        }

        public double iRVI(string symbol, int timeframe, int period, int mode, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, period, mode, shift };
            return sendCommand<double>(MtCommandType.iRVI, commandParameters);
        }

        public double iStdDev(string symbol, int timeframe, int ma_period, int ma_shift, int ma_method, int applied_price, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, ma_period, ma_shift, ma_method, applied_price, shift };
            return sendCommand<double>(MtCommandType.iStdDev, commandParameters);
        }

        public double iStdDevOnArray(double[] array, int total, int ma_period, int ma_shift, int ma_method, int shift)
        {
            int arraySize = array != null ? array.Length : 0;
            var commandParameters = new ArrayList { arraySize };
            commandParameters.AddRange(array);
            commandParameters.Add(total);
            commandParameters.Add(ma_period);
            commandParameters.Add(ma_shift);
            commandParameters.Add(ma_method);
            commandParameters.Add(shift);

            return sendCommand<double>(MtCommandType.iStdDevOnArray, commandParameters);
        }

        public double iStochastic(string symbol, int timeframe, int pKperiod, int pDperiod, int slowing, int method, int price_field, int mode, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, pKperiod, pDperiod, slowing, method, price_field, mode, shift };
            return sendCommand<double>(MtCommandType.iStochastic, commandParameters);
        }

        public double iWPR(string symbol, int timeframe, int period, int shift)
        {
            var commandParameters = new ArrayList { symbol, timeframe, period, shift };
            return sendCommand<double>(MtCommandType.iWPR, commandParameters);
        }
        #endregion

        #region Timeseries access
        public int iBars(string symbol, ChartPeriod timeframe)
        {
            var commandParameters = new ArrayList { symbol, (int)timeframe };
            return sendCommand<int>(MtCommandType.iBars, commandParameters);
        }

        public int iBarShift(string symbol, ChartPeriod timeframe, DateTime time, bool exact)
        {
            var commandParameters = new ArrayList { symbol, (int)timeframe, MtApiTimeConverter.ConvertToMtTime(time), exact };
            return sendCommand<int>(MtCommandType.iBarShift, commandParameters);
        }

        public double iClose(string symbol, ChartPeriod timeframe, int shift)
        {
            var commandParameters = new ArrayList { symbol, (int)timeframe, shift };
            return sendCommand<double>(MtCommandType.iClose, commandParameters);
        }

        public double iHigh(string symbol, ChartPeriod timeframe, int shift)
        {
            var commandParameters = new ArrayList { symbol, (int)timeframe, shift };
            return sendCommand<double>(MtCommandType.iHigh, commandParameters);
        }

        public int iHighest(string symbol, ChartPeriod timeframe, SeriesIdentifier type, int count, int start)
        {
            var commandParameters = new ArrayList { symbol, (int)timeframe, (int)type, count, start };
            return sendCommand<int>(MtCommandType.iHighest, commandParameters);
        }

        public double iLow(string symbol, ChartPeriod timeframe, int shift)
        {
            var commandParameters = new ArrayList { symbol, (int)timeframe, shift };
            return sendCommand<double>(MtCommandType.iLow, commandParameters);
        }

        public int iLowest(string symbol, ChartPeriod timeframe, SeriesIdentifier type, int count, int start)
        {
            var commandParameters = new ArrayList { symbol, (int)timeframe, (int)type, count, start };
            return sendCommand<int>(MtCommandType.iLowest, commandParameters);
        }

        public double iOpen(string symbol, ChartPeriod timeframe, int shift)
        {
            var commandParameters = new ArrayList { symbol, (int)timeframe, shift };
            return sendCommand<double>(MtCommandType.iOpen, commandParameters);
        }

        public DateTime iTime(string symbol, ChartPeriod timeframe, int shift)
        {
            var commandParameters = new ArrayList { symbol, (int)timeframe, shift };
            var commandResponse = sendCommand<int>(MtCommandType.iTime, commandParameters);
            return MtApiTimeConverter.ConvertFromMtTime(commandResponse);
        }

        public double iVolume(string symbol, ChartPeriod timeframe, int shift)
        {
            var commandParameters = new ArrayList { symbol, (int)timeframe, shift };
            return sendCommand<double>(MtCommandType.iVolume, commandParameters);
        }

        //public double[] iCloseArray(string symbol, ChartPeriod timeframe, int shift, int valueCount)
        //{
        //    int doubleArraySendLimit = DOUBLE_ARRAY_LIMIT;
        //    int limitCount = valueCount / doubleArraySendLimit;
        //    int valueCountModulo = valueCount - limitCount * doubleArraySendLimit;

        //    var resultArray = new double[valueCount];
        //    for (int i = 0; i < limitCount; i++)
        //    {
        //        var commandParameters = new ArrayList { symbol, (int)timeframe, i * doubleArraySendLimit, doubleArraySendLimit };
        //        var result = sendCommand<double[]>(MtCommandType.iCloseArray, commandParameters);
        //        if (result != null)
        //            Array.Copy(result, 0, resultArray, i * doubleArraySendLimit, doubleArraySendLimit);
        //    }

        //    if (valueCountModulo > 0)
        //    {
        //        var commandParameters = new ArrayList { symbol, (int)timeframe, limitCount * doubleArraySendLimit, valueCountModulo };
        //        var result = sendCommand<double[]>(MtCommandType.iCloseArray, commandParameters);
        //        if (result != null)
        //            Array.Copy(result, 0, resultArray, limitCount * doubleArraySendLimit, valueCountModulo);
        //    }
        //    return resultArray;

        //    var commandParameters = new ArrayList { symbol, (int)timeframe, shift, valueCount };
        //    return sendCommand<double[]>(MtCommandType.iCloseArray, commandParameters);
        //}

        public double[] iCloseArray(string symbol, ChartPeriod timeframe)
        {
            var commandParameters = new ArrayList { symbol, (int)timeframe };
            return sendCommand<double[]>(MtCommandType.iCloseArray, commandParameters);
        }

        public double[] iHighArray(string symbol, ChartPeriod timeframe)
        {
            var commandParameters = new ArrayList { symbol, (int)timeframe };
            return sendCommand<double[]>(MtCommandType.iHighArray, commandParameters);
        }

        public double[] iLowArray(string symbol, ChartPeriod timeframe)
        {
            var commandParameters = new ArrayList { symbol, (int)timeframe };
            return sendCommand<double[]>(MtCommandType.iLowArray, commandParameters);
        }

        public double[] iOpenArray(string symbol, ChartPeriod timeframe)
        {
            var commandParameters = new ArrayList { symbol, (int)timeframe };
            return sendCommand<double[]>(MtCommandType.iOpenArray, commandParameters);
        }

        public double[] iVolumeArray(string symbol, ChartPeriod timeframe)
        {
            var commandParameters = new ArrayList { symbol, (int)timeframe };
            return sendCommand<double[]>(MtCommandType.iVolumeArray, commandParameters);
        }

        public DateTime[] iTimeArray(string symbol, ChartPeriod timeframe)
        {
            DateTime[] result = null;

            var commandParameters = new ArrayList { symbol, (int)timeframe };
            
            var response = sendCommand<int[]>(MtCommandType.iTimeArray, commandParameters);

            if (response != null)
            {
                result = new DateTime[response.Length];

                for(int i = 0; i < response.Length; i++)
                {
                    result[i] = MtApiTimeConverter.ConvertFromMtTime(response[i]);
                }
            }

            return result;
        }

        public bool RefreshRates()
        {
            return sendCommand<bool>(MtCommandType.RefreshRates, null);
        }

        #endregion

        #region Private Methods
        private void Connect(string host, int port)
        {
            ConnectionState = MtConnectionState.Connecting;
            ConnectionStateChanged.FireEvent(this
                , new MtConnectionEventArgs(MtConnectionState.Connecting, "Connecting to " + host + ":" + port));

            try
            {
                mClient.Open(host, port);
                mClient.Connect();
            }
            catch (Exception e)
            {
                ConnectionState = MtConnectionState.Failed;
                ConnectionStateChanged.FireEvent(this
                    , new MtConnectionEventArgs(MtConnectionState.Failed, "Failed connection to " + host + ":" + port + ". " + e.Message));
                return;
            }

            ConnectionState = MtConnectionState.Connected;
            ConnectionStateChanged.FireEvent(this
                , new MtConnectionEventArgs(MtConnectionState.Connected, "Connected  to " + host + ":" + port));
        }

        private void Connect(int port)
        {
            ConnectionState = MtConnectionState.Connecting;
            ConnectionStateChanged.FireEvent(this
                , new MtConnectionEventArgs(MtConnectionState.Connecting, "Connecting to 'localhost':" + port));

            try
            {
                mClient.Open(port);
                mClient.Connect();
            }
            catch (Exception e)
            {
                ConnectionState = MtConnectionState.Failed;
                ConnectionStateChanged.FireEvent(this
                    , new MtConnectionEventArgs(MtConnectionState.Failed, "Failed connection  to 'localhost':" + port + ". " + e.Message));

                return;
            }

            ConnectionState = MtConnectionState.Connected;
            ConnectionStateChanged.FireEvent(this
                , new MtConnectionEventArgs(MtConnectionState.Connected, "Connected to 'localhost':" + port));
        }

        private void Disconnect()
        {
            mClient.Disconnect();
            mClient.Close();

            ConnectionState = MtConnectionState.Disconnected;
            ConnectionStateChanged.FireEvent(this, new MtConnectionEventArgs(MtConnectionState.Disconnected, "Disconnected"));
        }
        private T sendCommand<T>(MtCommandType commandType, ArrayList commandParameters)
        {
            var response = mClient.SendCommand((int)commandType, commandParameters);

            T result = default(T);

            if (response is MtResponseDouble)
                result = (T)Convert.ChangeType(((MtResponseDouble)response).Value, typeof(T));

            if (response is MtResponseInt)
                result = (T)Convert.ChangeType(((MtResponseInt)response).Value, typeof(T));

            if (response is MtResponseBool)
                result = (T)Convert.ChangeType(((MtResponseBool)response).Value, typeof(T));

            if (response is MtResponseString)
                result = (T)Convert.ChangeType(((MtResponseString)response).Value, typeof(T));

            if (response is MtResponseDoubleArray)
                result = (T)Convert.ChangeType(((MtResponseDoubleArray)response).Value, typeof(T));

            if (response is MtResponseIntArray)
                result = (T)Convert.ChangeType(((MtResponseIntArray)response).Value, typeof(T));

            if (response is MtResponseArrayList)
                result = (T)Convert.ChangeType(((MtResponseArrayList)response).Value, typeof(T));

            return result;
        }

        private void mClient_QuoteUpdated(MTApiService.MtQuote quote)
        {
            if (quote != null)
            {
                if (QuoteUpdated != null)
                {
                    QuoteUpdated(this, quote.Instrument, quote.Bid, quote.Ask);
                }
            }
        }

        private void mClient_ServerDisconnected(object sender, EventArgs e)
        {
            ConnectionState = MtConnectionState.Disconnected;
            ConnectionStateChanged.FireEvent(this
                , new MtConnectionEventArgs(MtConnectionState.Disconnected, "MtApi is disconnected"));
        }

        private void mClient_ServerFailed(object sender, EventArgs e)
        {
            ConnectionState = MtConnectionState.Failed;
            ConnectionStateChanged.FireEvent(this
                , new MtConnectionEventArgs(MtConnectionState.Failed, "Failed connection with MtApi"));
        }

        private void mClient_QuoteRemoved(MTApiService.MtQuote quote)
        {
            QuoteRemoved.FireEvent(this, new MtQuoteEventArgs(quote.Parse()));
        }

        private void mClient_QuoteAdded(MTApiService.MtQuote quote)
        {
            QuoteAdded.FireEvent(this, new MtQuoteEventArgs(quote.Parse()));
        }
        #endregion

        #region Events
        public event MtApiQuoteHandler QuoteUpdated;
        public event EventHandler<MtQuoteEventArgs> QuoteAdded;
        public event EventHandler<MtQuoteEventArgs> QuoteRemoved;
        public event EventHandler<MtConnectionEventArgs> ConnectionStateChanged;
        #endregion

        #region Private Fields
        private readonly MtClient mClient = new MtClient();
        #endregion
    }
}
