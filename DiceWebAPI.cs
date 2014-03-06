using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Linq;
using System.Security.Cryptography;
using System.Diagnostics;
#if !NET_35
using System.Threading.Tasks;
#endif

namespace Dice.Client.Web
{
    /// <summary>
    /// All methods that interact with the API are here.
    /// </summary>
    public static class DiceWebAPI
    {
        #region Constants, Variables, Helpers
        /// <summary>
        /// All bets are between 0 and 999,999
        /// </summary>
        const long GuessSpan = 1000000;
        /// <summary>
        /// 99.9% payout. 0.1% house edge.
        /// </summary>
        const decimal HousePayout = 0.999M;

#if !NET_35
        static readonly ThreadLocal<JavaScriptSerializer> Serializers = new ThreadLocal<JavaScriptSerializer>(() => new JavaScriptSerializer());
        static JavaScriptSerializer Serializer { get { return Serializers.Value; } }
#else
        static JavaScriptSerializer Serializer { get { return new JavaScriptSerializer(); } }
#endif

#error 'set..'
#if DEBUG
        static readonly Uri WebUri = new Uri("http://localhost:64349/api/web.aspx");
#else
        //static readonly Uri WebUri = new Uri("https://www.999dice.com/api/web.aspx");
#endif

        static SHA512 CreateSHA512()
        {
            try
            {
#if !NET_35
                if (!CryptoConfig.AllowOnlyFipsAlgorithms)
#endif
                    return new SHA512Managed();
            }
            catch
            {
            }
            try
            {
                return new SHA512Cng();
            }
            catch
            {
            }
            return new SHA512CryptoServiceProvider();
        }
        #endregion

        #region Request methods
        static T Request<T>(NameValueCollection formData) where T : DiceResponse, new()
        {
            T response = new T();
            try
            {
                using (WebClient client = new WebClient())
                    response.SetRawResponse((IDictionary<string, object>)Serializer.DeserializeObject(Encoding.UTF8.GetString(client.UploadValues(WebUri, "POST", formData))));
                response.WebStatusCode = 200;
            }
            catch (WebException e)
            {
                response.ErrorMessage = e.Message;
                HttpWebResponse r = e.Response as HttpWebResponse;
                if (r != null)
                    response.WebStatusCode = (int)r.StatusCode;
            }
            return response;
        }
#if !NET_35
        static async Task<T> RequestAsync<T>(NameValueCollection formData) where T : DiceResponse, new()
        {
            T response = new T();
            try
            {
                using (WebClient client = new WebClient())
                    response.SetRawResponse((IDictionary<string, object>)Serializer.DeserializeObject(Encoding.UTF8.GetString(await client.UploadValuesTaskAsync(WebUri, "POST", formData))));
                response.WebStatusCode = 200;
            }
            catch (WebException e)
            {
                response.ErrorMessage = e.Message;
                HttpWebResponse r = e.Response as HttpWebResponse;
                if (r != null)
                    response.WebStatusCode = (int)r.StatusCode;
            }
            return response;
        }
#endif
        #endregion

        #region Form data creation
        static NameValueCollection GetFormDataCreateAccount(string apiKey)
        {
            return new NameValueCollection
            {
                { "a", "CreateAccount" },
                { "Key", apiKey }
            };
        }
        static NameValueCollection GetFormDataBeginSession(string apiKey, string accountCookie)
        {
            return new NameValueCollection
            {
                { "a", "BeginSession" },
                { "Key", apiKey },
                { "AccountCookie", accountCookie }
            };
        }
        static NameValueCollection GetFormDataLogin(string apiKey, string username, string password)
        {
            return new NameValueCollection
            {
                { "a", "Login" },
                { "Key", apiKey },
                { "Username", username },
                { "Password", password }
            };
        }
        static NameValueCollection GetFormDataCreateUser(string sessionCookie, string username, string password)
        {
            return new NameValueCollection
            {
                { "a", "CreateUser" },
                { "s", sessionCookie },
                { "Username", username },
                { "Password", password }
            };
        }
        static NameValueCollection GetFormDataWithdraw(string sessionCookie, decimal amount, string address)
        {
            return new NameValueCollection
            {
                { "a", "Withdraw" },
                { "s", sessionCookie },
                { "Amount", ((long)(amount*100000000)).ToString() },
                { "Address", address }
            };
        }
        static NameValueCollection GetFormDataChangePassword(string sessionCookie, string oldPassword, string newPassword)
        {
            return new NameValueCollection
            {
                { "a", "ChangePassword" },
                { "s", sessionCookie },
                { "OldPassword", oldPassword },
                { "NewPassword", newPassword }
            };
        }
        static NameValueCollection GetFormDataGetServerSeedHash(string sessionCookie)
        {
            return new NameValueCollection
            {
                { "a", "GetServerSeedHash" },
                { "s", sessionCookie }
            };
        }
        static NameValueCollection GetFormDataSetClientSeed(string sessionCookie, long seed)
        {
            return new NameValueCollection
            {
                { "a", "SetClientSeed" },
                { "s", sessionCookie },
                { "Seed", seed.ToString() }
            };
        }
        static NameValueCollection GetFormDataUpdateEmail(string sessionCookie, string email)
        {
            return new NameValueCollection
            {
                { "a", "UpdateEmail" },
                { "s", sessionCookie },
                { "Email", email }
            };
        }
        static NameValueCollection GetFormDataUpdateEmergencyAddress(string sessionCookie, string address)
        {
            return new NameValueCollection
            {
                { "a", "UpdateEmergencyAddress" },
                { "s", sessionCookie },
                { "Address", address }
            };
        }
        static NameValueCollection GetFormDataGetBalance(string sessionCookie)
        {
            return new NameValueCollection
            {
                { "a", "GetBalance" },
                { "s", sessionCookie }
            };
        }
        static NameValueCollection GetFormDataGetDepositAddress(string sessionCookie)
        {
            return new NameValueCollection
            {
                { "a", "GetDepositAddress" },
                { "s", sessionCookie }
            };
        }
        static NameValueCollection GetFormDataPlaceBet(string sessionCookie, decimal payIn, long guessLow, long guessHigh)
        {
            return new NameValueCollection
            {
                { "a", "PlaceBet" },
                { "s", sessionCookie },
                { "PayIn", ((long)(payIn*100000000)).ToString() },
                { "Low", guessLow.ToString() },
                { "High", guessHigh.ToString() }
            };
        }
        static NameValueCollection GetFormDataPlaceAutomatedBets(string sessionCookie, AutomatedBetsSettings settings)
        {
            return new NameValueCollection
            {
                { "a", "PlaceAutomatedBets" },
                { "s", sessionCookie },
                { "BasePayIn", ((long)(settings.BasePayIn*100000000)).ToString() },
                { "Low", settings.GuessLow.ToString() },
                { "High", settings.GuessHigh.ToString() },
                { "MaxBets", settings.MaxBets.ToString() },
                { "ResetOnWin", settings.ResetOnWin.ToString() },
                { "ResetOnLose", settings.ResetOnLose.ToString() },
                { "IncreaseOnWinPercent", settings.IncreaseOnWinPercent.ToString() },
                { "IncreaseOnLosePercent", settings.IncreaseOnLosePercent.ToString() },
                { "MaxPayIn", ((long)(settings.MaxAllowedPayIn*100000000)).ToString() },
                { "ResetOnLoseMaxBet", settings.ResetOnLoseMaxBet.ToString() },
                { "StopOnLoseMaxBet", settings.StopOnLoseMaxBet.ToString() },
                { "StopMaxBalance", ((long)(settings.StopMaxBalance*100000000)).ToString() },
                { "StopMinBalance", ((long)settings.StopMinBalance*100000000).ToString() },
                { "StartingPayIn", ((long)settings.StartingPayIn*100000000).ToString() },
                { "Compact", "1" }
            };
        }
        #endregion

        #region Processing API Results
        static BeginSessionResponse Process(BeginSessionResponse res, string username)
        {
            if (res.Success && res.Session != null)
                res.Session.Username = username;
            return res;
        }
        static CreateUserResponse Process(SessionInfo session, CreateUserResponse res, string username)
        {
            if (res.Success)
                session.Username = username;
            return res;
        }
        static GetBalanceResponse Process(SessionInfo session, GetBalanceResponse res)
        {
            if (res.Success)
                session.Balance = res.Balance;
            return res;
        }
        static WithdrawResponse Process(SessionInfo session, WithdrawResponse res)
        {
            if (res.Success)
                session.Balance -= res.WithdrawalPending;
            return res;
        }
        static SetClientSeedResponse Process(SessionInfo session, SetClientSeedResponse res, long seed)
        {
            if (res.Success)
                session.ClientSeed = seed;
            return res;
        }
        static UpdateEmailResponse Process(SessionInfo session, UpdateEmailResponse res, string email)
        {
            if (res.Success)
                session.Email = email;
            return res;
        }
        static UpdateEmergencyAddressResponse Process(SessionInfo session, UpdateEmergencyAddressResponse res, string emergencyAddress)
        {
            if (res.Success)
                session.EmergencyAddress = emergencyAddress;
            return res;
        }
        static GetDepositAddressResponse Process(SessionInfo session, GetDepositAddressResponse res)
        {
            if (res.Success)
                session.DepositAddress = res.DepositAddress;
            return res;
        }
        static PlaceBetResponse Process(SessionInfo session, PlaceBetResponse res, decimal payIn, long guessLow, long guessHigh)
        {
            if (res.Success)
            {
                session.PauseUpdates();
                try
                {
                    ++session.BetCount;
                    session.BetPayIn += payIn;
                    session.BetPayOut += res.PayOut;
                    session.Balance = res.StartingBalance + res.PayOut + payIn;
                    if (res.Secret >= guessLow && res.Secret <= guessHigh)
                        ++session.BetWinCount;
                }
                finally
                {
                    session.UnpauseUpdates();
                }
            }
            return res;
        }
        static PlaceAutomatedBetsResponse Process(SessionInfo session, PlaceAutomatedBetsResponse res, AutomatedBetsSettings settings, long clientSeed)
        {
            if (res.Success)
            {
                byte[] seed = new byte[res.ServerSeed.Length / 2];
                for (int x = 0; x < seed.Length; ++x)
                    seed[x] = byte.Parse(res.ServerSeed.Substring(x * 2, 2), System.Globalization.NumberStyles.HexNumber);
                byte[] client = BitConverter.GetBytes(clientSeed);
                decimal payin = settings.StartingPayIn;
                decimal bal = res.StartingBalance;
                for (int x = 0; x < res.BetCount; ++x)
                {
                    byte[] number = Encoding.ASCII.GetBytes(res.BetIds[x].ToString());
                    byte[] data = number.Concat(seed).Concat(client).ToArray();
                    using (SHA512 sha512 = CreateSHA512())
                    {
                        byte[] hash = sha512.ComputeHash(sha512.ComputeHash(data));
                        bool found = false;
                        while (!found)
                        {
                            for (int y = 0; y <= 61; y += 3)
                            {
                                long result = (hash[y] << 16) | (hash[y + 1] << 8) | hash[y + 2];
                                if (result < 16000000)
                                {
                                    res.Secrets[x] = result % 1000000;
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }
                    res.PayIns[x] = payin;
                    bal += payin;
                    bool win = res.Secrets[x] >= settings.GuessLow && res.Secrets[x] <= settings.GuessHigh;
                    if (win)
                    {
                        res.PayOuts[x] = CalculateWinPayout(payin, settings.GuessLow, settings.GuessHigh);
                        bal += res.PayOuts[x];
                        if (settings.ResetOnWin)
                            payin = settings.BasePayIn;
                        else
                            payin = TruncateSatoshis(payin * (1 + settings.IncreaseOnWinPercent));
                    }
                    else
                    {
                        if (settings.ResetOnLose || (settings.ResetOnLoseMaxBet && payin == settings.MaxAllowedPayIn))
                            payin = settings.BasePayIn;
                        else
                            payin = TruncateSatoshis(payin * (1 + settings.IncreaseOnLosePercent));
                    }
                    if (payin < settings.MaxAllowedPayIn && settings.MaxAllowedPayIn != 0)
                        payin = settings.MaxAllowedPayIn;
                    if (payin < -bal)
                        payin = -bal;
                }
                Debug.Assert(res.TotalPayIn == res.PayIns.Sum());
                Debug.Assert(res.TotalPayOut == res.PayOuts.Sum());

                session.PauseUpdates();
                try
                {
                    for (int x = 0; x < res.BetCount; ++x)
                        if (res.Secrets[x] >= settings.GuessLow && res.Secrets[x] <= settings.GuessHigh)
                            ++session.BetWinCount;
                    session.BetCount += res.BetCount;
                    session.Balance = res.StartingBalance + res.TotalPayIn + res.TotalPayOut;
                    session.BetPayIn += res.TotalPayIn;
                    session.BetPayOut += res.TotalPayOut;
                }
                finally
                {
                    session.UnpauseUpdates();
                }
            }
            return res;
        }
        #endregion

        #region Validation methods before making API calls
        static void Validate(string apiKey)
        {
            if (apiKey.IsNullOrWhiteSpace())
                throw new ArgumentNullException();
        }
        static void Validate(string apiKey, string other)
        {
            if (apiKey.IsNullOrWhiteSpace() || other.IsNullOrWhiteSpace())
                throw new ArgumentNullException();
        }
        static void Validate(SessionInfo session)
        {
            if (session == null)
                throw new ArgumentNullException();
        }
        static void Validate(SessionInfo session, string other)
        {
            if (session == null || other.IsNullOrWhiteSpace())
                throw new ArgumentNullException();
        }
        static void Validate(SessionInfo session, string other1, string other2)
        {
            if (session == null ||
                other1.IsNullOrWhiteSpace() ||
                other2.IsNullOrWhiteSpace())
                throw new ArgumentNullException();
        }
        static void Validate(SessionInfo session, long guessLow, long guessHigh)
        {
            if (session == null)
                throw new ArgumentNullException();
            if (guessLow < 0 || guessLow > guessHigh || guessHigh >= GuessSpan)
                throw new ArgumentOutOfRangeException("0 <= GuessLow <= GuessHigh <= " + GuessSpan.ToString());
        }
        static void Validate(SessionInfo session, AutomatedBetsSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException();
            Validate(session, settings.GuessLow, settings.GuessHigh);
            if (settings.MaxBets < 1 || settings.IncreaseOnWinPercent < 0 || settings.IncreaseOnLosePercent < 0)
                throw new ArgumentOutOfRangeException();
        }
        static void Validate(decimal maxAllowedPayIn, decimal basePayIn)
        {
            if (maxAllowedPayIn != 0 && maxAllowedPayIn > basePayIn)
                throw new ArgumentOutOfRangeException();
        }
        #endregion

        #region Public helper methods
        public static decimal TruncateSatoshis(decimal amt)
        {
            if (amt < 0)
                return decimal.Truncate(amt * -100000000M) / -100000000M;
            return decimal.Truncate(amt * 100000000M) / 100000000M;
        }

        public static decimal CalculateWinPayout(decimal payIn, long guessLow, long guessHigh)
        {
            if (payIn < 0) payIn = -payIn;
            payIn = TruncateSatoshis(payIn);
            decimal mul = CalculatePayoutMultiplier(guessLow, guessHigh);
            decimal payout = payIn * mul;
            return TruncateSatoshis(payout);
        }
        public static decimal CalculateWinProfit(decimal payIn, long guessLow, long guessHigh)
        {
            if (payIn < 0) payIn = -payIn;
            payIn = TruncateSatoshis(payIn);
            decimal payout = CalculateWinPayout(payIn, guessLow, guessHigh);
            return payout - payIn;
        }
        public static decimal CalculateChanceToWin(long guessLow, long guessHigh)
        {
            decimal odds = guessHigh - guessLow + 1;
            return odds / GuessSpan;
        }
        public static decimal CalculatePayoutMultiplier(long guessLow, long guessHigh)
        {
            return HousePayout / CalculateChanceToWin(guessLow, guessHigh);
        }
        #endregion

        #region API calls
        #region Synchronous API calls
        public static BeginSessionResponse BeginSession(string apiKey)
        {
            Validate(apiKey);
            return Request<BeginSessionResponse>(GetFormDataCreateAccount(apiKey));
        }
        public static BeginSessionResponse BeginSession(string apiKey, string accountCookie)
        {
            Validate(apiKey, accountCookie);
            return Request<BeginSessionResponse>(GetFormDataBeginSession(apiKey, accountCookie));
        }
        public static BeginSessionResponse BeginSession(string apiKey, string username, string password)
        {
            Validate(apiKey, username);
            return Process(Request<BeginSessionResponse>(GetFormDataLogin(apiKey, username, password)), username);
        }
        public static CreateUserResponse CreateUser(SessionInfo session, string username, string password)
        {
            Validate(session, username, password);
            username = username.Trim();
            return Process(session, Request<CreateUserResponse>(GetFormDataCreateUser(session.SessionCookie, username, password)), username);
        }
        public static GetBalanceResponse GetBalance(SessionInfo session)
        {
            Validate(session);
            return Process(session, Request<GetBalanceResponse>(GetFormDataGetBalance(session.SessionCookie)));
        }
        public static WithdrawResponse WithdrawAll(SessionInfo session, string address)
        {
            return Withdraw(session, 0, address);
        }
        public static WithdrawResponse Withdraw(SessionInfo session, decimal amount, string address)
        {
            Validate(session, address);
            return Process(session, Request<WithdrawResponse>(GetFormDataWithdraw(session.SessionCookie, amount, address)));
        }
        public static ChangePasswordResponse ChangePassword(SessionInfo session, string oldPassword, string newPassword)
        {
            Validate(session);
            return Request<ChangePasswordResponse>(GetFormDataChangePassword(session.SessionCookie, oldPassword, newPassword));
        }
        public static GetServerSeedHashResponse GetServerSeedHash(SessionInfo session)
        {
            Validate(session);
            return Request<GetServerSeedHashResponse>(GetFormDataGetServerSeedHash(session.SessionCookie));
        }
        public static SetClientSeedResponse SetClientSeed(SessionInfo session, long seed)
        {
            Validate(session);
            return Process(session, Request<SetClientSeedResponse>(GetFormDataSetClientSeed(session.SessionCookie, seed)), seed);
        }
        public static UpdateEmailResponse UpdateEmail(SessionInfo session, string email)
        {
            Validate(session);
            if (email != null)
                email = email.Trim();
            return Process(session, Request<UpdateEmailResponse>(GetFormDataUpdateEmail(session.SessionCookie, email)), email);
        }
        public static UpdateEmergencyAddressResponse UpdateEmergencyAddress(SessionInfo session, string emergencyAddress)
        {
            Validate(session);
            if (emergencyAddress != null)
                emergencyAddress = emergencyAddress.Trim();
            return Process(session, Request<UpdateEmergencyAddressResponse>(GetFormDataUpdateEmergencyAddress(session.SessionCookie, emergencyAddress)), emergencyAddress);
        }
        public static GetDepositAddressResponse GetDepositAddress(SessionInfo session)
        {
            Validate(session);
            return Process(session, Request<GetDepositAddressResponse>(GetFormDataGetDepositAddress(session.SessionCookie)));
        }
        public static PlaceBetResponse PlaceBet(SessionInfo session, decimal payIn, long guessLow, long guessHigh)
        {
            Validate(session, guessLow, guessHigh);
            if (payIn > 0) payIn = -payIn;
            return Process(session, Request<PlaceBetResponse>(GetFormDataPlaceBet(session.SessionCookie, payIn, guessLow, guessHigh)), payIn, guessLow, guessHigh);
        }
        public static PlaceAutomatedBetsResponse PlaceAutomatedBets(SessionInfo session, AutomatedBetsSettings settings)
        {
            Validate(session, settings);
            long clientSeed = session.ClientSeed;
            return Process(session, Request<PlaceAutomatedBetsResponse>(
                GetFormDataPlaceAutomatedBets(session.SessionCookie, settings)), settings, clientSeed);
        }
        #endregion

        #region Asynchronous API calls (await/async - .NET 4.5)
#if !NET_35
        public static async Task<BeginSessionResponse> BeginSessionAsync(string apiKey)
        {
            Validate(apiKey);
            return await RequestAsync<BeginSessionResponse>(GetFormDataCreateAccount(apiKey));
        }
        public static async Task<BeginSessionResponse> BeginSessionAsync(string apiKey, string accountCookie)
        {
            Validate(apiKey, accountCookie);
            return await RequestAsync<BeginSessionResponse>(GetFormDataBeginSession(apiKey, accountCookie));
        }
        public static async Task<BeginSessionResponse> BeginSessionAsync(string apiKey, string username, string password)
        {
            Validate(apiKey, username);
            return Process(await RequestAsync<BeginSessionResponse>(GetFormDataLogin(apiKey, username, password)), username);
        }
        public static async Task<CreateUserResponse> CreateUserAsync(SessionInfo session, string username, string password)
        {
            Validate(session, username, password);
            username = username.Trim();
            return Process(session, await RequestAsync<CreateUserResponse>(GetFormDataCreateUser(session.SessionCookie, username, password)), username);
        }
        public static async Task<GetBalanceResponse> GetBalanceAsync(SessionInfo session)
        {
            Validate(session);
            return Process(session, await RequestAsync<GetBalanceResponse>(GetFormDataGetBalance(session.SessionCookie)));
        }
        public static async Task<WithdrawResponse> WithdrawAllAsync(SessionInfo session, string address)
        {
            return await WithdrawAsync(session, 0, address);
        }
        public static async Task<WithdrawResponse> WithdrawAsync(SessionInfo session, decimal amount, string address)
        {
            Validate(session, address);
            return Process(session, await RequestAsync<WithdrawResponse>(GetFormDataWithdraw(session.SessionCookie, amount, address)));
        }
        public static async Task<ChangePasswordResponse> ChangePasswordAsync(SessionInfo session, string oldPassword, string newPassword)
        {
            Validate(session);
            return await RequestAsync<ChangePasswordResponse>(GetFormDataChangePassword(session.SessionCookie, oldPassword, newPassword));
        }
        public static async Task<GetServerSeedHashResponse> GetServerSeedHashAsync(SessionInfo session)
        {
            Validate(session);
            return await RequestAsync<GetServerSeedHashResponse>(GetFormDataGetServerSeedHash(session.SessionCookie));
        }
        public static async Task<SetClientSeedResponse> SetClientSeedAsync(SessionInfo session, long seed)
        {
            Validate(session);
            return Process(session, await RequestAsync<SetClientSeedResponse>(GetFormDataSetClientSeed(session.SessionCookie, seed)), seed);
        }
        public static async Task<UpdateEmailResponse> UpdateEmailAsync(SessionInfo session, string email)
        {
            Validate(session);
            if (email != null)
                email = email.Trim();
            return Process(session, await RequestAsync<UpdateEmailResponse>(GetFormDataUpdateEmail(session.SessionCookie, email)), email);
        }
        public static async Task<UpdateEmergencyAddressResponse> UpdateEmergencyAddressAsync(SessionInfo session, string emergencyAddress)
        {
            Validate(session);
            if (emergencyAddress != null)
                emergencyAddress = emergencyAddress.Trim();
            return Process(session, await RequestAsync<UpdateEmergencyAddressResponse>(GetFormDataUpdateEmergencyAddress(session.SessionCookie, emergencyAddress)), emergencyAddress);
        }
        public static async Task<GetDepositAddressResponse> GetDepositAddressAsync(SessionInfo session)
        {
            Validate(session);
            return Process(session, await RequestAsync<GetDepositAddressResponse>(GetFormDataGetDepositAddress(session.SessionCookie)));
        }
        public static async Task<PlaceBetResponse> PlaceBetAsync(SessionInfo session, decimal payIn, long guessLow, long guessHigh)
        {
            Validate(session, guessLow, guessHigh);
            if (payIn > 0) payIn = -payIn;
            return Process(session, await RequestAsync<PlaceBetResponse>(GetFormDataPlaceBet(session.SessionCookie, payIn, guessLow, guessHigh)), payIn, guessLow, guessHigh);
        }
        public static async Task<PlaceAutomatedBetsResponse> PlaceAutomatedBetsAsync(SessionInfo session, AutomatedBetsSettings settings)
        {
            Validate(session, settings);

            long clientSeed = session.ClientSeed;
            return Process(session, await RequestAsync<PlaceAutomatedBetsResponse>(
                GetFormDataPlaceAutomatedBets(session.SessionCookie, settings)), settings, clientSeed);
        }
#endif
        #endregion

        #region Asynchronous API calls (begin/end pattern)
        static DiceAsyncResult<T> QueueWork<T>(AsyncCallback callback, object state, Func<T> work) where T : DiceResponse
        {
            DiceAsyncResult<T> resp = new DiceAsyncResult<T>(callback, state);
            ThreadPool.QueueUserWorkItem(x =>
            {
                try
                {
                    resp.Complete(work());
                }
                catch (Exception e)
                {
                    resp.Complete(e);
                }
            });
            return resp;
        }
        static T GetWorkResult<T>(IAsyncResult i) where T : DiceResponse
        {
            using (DiceAsyncResult<T> res = (DiceAsyncResult<T>)i)
            {
                if (!res.IsCompleted)
                    res.AsyncWaitHandle.WaitOne();
                Exception e = res.Exception;
                if (e != null)
                    throw e;
                return res.Response;
            }
        }

        public static IAsyncResult BeginBeginSession(string apiKey, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => BeginSession(apiKey));
        }
        public static IAsyncResult BeginBeginSession(string apiKey, string accountCookie, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => BeginSession(apiKey, accountCookie));
        }
        public static IAsyncResult BeginBeginSession(string apiKey, string username, string password, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => BeginSession(apiKey, username, password));
        }
        public static BeginSessionResponse EndBeginSession(IAsyncResult i)
        {
            return GetWorkResult<BeginSessionResponse>(i);
        }
        public static IAsyncResult BeginCreateUser(SessionInfo session, string username, string password, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => CreateUser(session, username, password));
        }
        public static CreateUserResponse EndCreateUser(IAsyncResult i)
        {
            return GetWorkResult<CreateUserResponse>(i);
        }
        public static IAsyncResult BeginGetBalance(SessionInfo session, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => GetBalance(session));
        }
        public static GetBalanceResponse EndGetBalance(IAsyncResult i)
        {
            return GetWorkResult<GetBalanceResponse>(i);
        }
        public static IAsyncResult BeginWithdrawAll(SessionInfo session, string address, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => WithdrawAll(session, address));
        }
        public static IAsyncResult BeginWithdraw(SessionInfo session, decimal amount, string address, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => Withdraw(session, amount, address));
        }
        public static WithdrawResponse EndWithdraw(IAsyncResult i)
        {
            return GetWorkResult<WithdrawResponse>(i);
        }
        public static IAsyncResult BeginChangePassword(SessionInfo session, string oldPassword, string newPassword, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => ChangePassword(session, oldPassword, newPassword));
        }
        public static ChangePasswordResponse EndChangePassword(IAsyncResult i)
        {
            return GetWorkResult<ChangePasswordResponse>(i);
        }
        public static IAsyncResult BeginGetServerSeedHash(SessionInfo session, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => GetServerSeedHash(session));
        }
        public static GetServerSeedHashResponse EndGetServerSeedHash(IAsyncResult i)
        {
            return GetWorkResult<GetServerSeedHashResponse>(i);
        }
        public static IAsyncResult BeginSetClientSeed(SessionInfo session, long seed, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => SetClientSeed(session, seed));
        }
        public static SetClientSeedResponse EndSetClientSeed(IAsyncResult i)
        {
            return GetWorkResult<SetClientSeedResponse>(i);
        }
        public static IAsyncResult BeginUpdateEmail(SessionInfo session, string email, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => UpdateEmail(session, email));
        }
        public static UpdateEmailResponse EndUpdateEmail(IAsyncResult i)
        {
            return GetWorkResult<UpdateEmailResponse>(i);
        }
        public static IAsyncResult BeginUpdateEmergencyAddress(SessionInfo session, string emergencyAddress, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => UpdateEmergencyAddress(session, emergencyAddress));
        }
        public static UpdateEmergencyAddressResponse EndUpdateEmergencyAddress(IAsyncResult i)
        {
            return GetWorkResult<UpdateEmergencyAddressResponse>(i);
        }
        public static IAsyncResult BeginGetDepositAddress(SessionInfo session, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => GetDepositAddress(session));
        }
        public static GetDepositAddressResponse EndGetDepositAddress(IAsyncResult i)
        {
            return GetWorkResult<GetDepositAddressResponse>(i);
        }
        public static IAsyncResult BeginPlaceBet(SessionInfo session, decimal payIn, long guessLow, long guessHigh, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => PlaceBet(session, payIn, guessLow, guessHigh));
        }
        public static PlaceBetResponse EndPlaceBet(IAsyncResult i)
        {
            return GetWorkResult<PlaceBetResponse>(i);
        }
        public static IAsyncResult BeginPlaceAutomatedBets(SessionInfo session, AutomatedBetsSettings settings, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => PlaceAutomatedBets(session, settings));
        }
        public static PlaceAutomatedBetsResponse EndPlaceAutomatedBets(IAsyncResult i)
        {
            return GetWorkResult<PlaceAutomatedBetsResponse>(i);
        }
        #endregion
        #endregion
    }
}
