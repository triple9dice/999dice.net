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
using System.Globalization;
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

        public static Uri WebUri = new Uri("https://www.999dice.com/api/web.aspx");

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
        static NameValueCollection GetFormDataLogin(string apiKey, string username, string password, int totp)
        {
            return new NameValueCollection
            {
                { "a", "Login" },
                { "Key", apiKey },
                { "Username", username },
                { "Password", password },
                { "Totp", totp.ToString() }
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
        static NameValueCollection GetFormDataWithdraw(string sessionCookie, decimal amount, string address, Currencies currency)
        {
            return new NameValueCollection
            {
                { "a", "Withdraw" },
                { "s", sessionCookie },
                { "Amount", ((long)(amount*100000000)).ToString() },
                { "Address", address },
                { "Currency", currency.ToString() }
            };
        }
        static NameValueCollection GetFormDataWithdraw(string sessionCookie, decimal amount, string address, int totp, Currencies currency)
        {
            return new NameValueCollection
            {
                { "a", "Withdraw" },
                { "s", sessionCookie },
                { "Amount", ((long)(amount*100000000)).ToString() },
                { "Address", address },
                { "Totp", totp.ToString() },
                { "Currency", currency.ToString() }
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
        static NameValueCollection GetFormDataGetBalance(string sessionCookie, Currencies currency)
        {
            return new NameValueCollection
            {
                { "a", "GetBalance" },
                { "s", sessionCookie },
                { "Currency", currency.ToString() }
            };
        }
        static NameValueCollection GetFormDataGetDepositAddress(string sessionCookie, Currencies currency)
        {
            return new NameValueCollection
            {
                { "a", "GetDepositAddress" },
                { "s", sessionCookie },
                { "Currency", currency.ToString() }
            };
        }
        static NameValueCollection GetFormDataPlaceBet(string sessionCookie, decimal payIn, long guessLow, long guessHigh, int clientSeed, Currencies currency)
        {
            return new NameValueCollection
            {
                { "a", "PlaceBet" },
                { "s", sessionCookie },
                { "PayIn", ((long)(payIn*100000000)).ToString() },
                { "Low", guessLow.ToString() },
                { "High", guessHigh.ToString() },
                { "ClientSeed", clientSeed.ToString() },
                { "Currency", currency.ToString() }
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
                { "StopMinBalance", ((long)(settings.StopMinBalance*100000000)).ToString() },
                { "StartingPayIn", ((long)(settings.StartingPayIn*100000000)).ToString() },
                { "Compact", "1" },
                { "ClientSeed", settings.ClientSeed.ToString() },
                { "Currency", settings.Currency.ToString() }
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
        static GetBalanceResponse Process(SessionInfo session, GetBalanceResponse res, Currencies currency)
        {
            res.Currency = currency;
            if (res.Success)
                session[currency].Balance = res.Balance;
            return res;
        }
        static WithdrawResponse Process(SessionInfo session, WithdrawResponse res, Currencies currency)
        {
            res.Currency = currency;
            if (res.Success)
                session[currency].Balance -= res.WithdrawalPending;
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
        static GetDepositAddressResponse Process(SessionInfo session, GetDepositAddressResponse res, Currencies currency)
        {
            res.Currency = currency;
            if (res.Success)
                session[currency].DepositAddress = res.DepositAddress;
            return res;
        }
        static PlaceBetResponse Process(SessionInfo session, PlaceBetResponse res, decimal payIn, long guessLow, long guessHigh, int clientSeed, Currencies currency)
        {
            res.Currency = currency;
            if (res.Success)
            {
                session.PauseUpdates();
                try
                {
                    ++session[currency].BetCount;
                    session[currency].BetPayIn += payIn;
                    session[currency].BetPayOut += res.PayOut;
                    session[currency].Balance = res.StartingBalance + res.PayOut + payIn;
                    if (res.Secret >= guessLow && res.Secret <= guessHigh)
                        ++session[currency].BetWinCount;
                }
                finally
                {
                    session.UnpauseUpdates();
                }
            }
            return res;
        }
        static PlaceAutomatedBetsResponse Process(SessionInfo session, PlaceAutomatedBetsResponse res, AutomatedBetsSettings settings)
        {
            res.Currency = settings.Currency;
            if (res.Success)
            {
                byte[] seed = new byte[res.ServerSeed.Length / 2];
                for (int x = 0; x < seed.Length; ++x)
                    seed[x] = byte.Parse(res.ServerSeed.Substring(x * 2, 2), System.Globalization.NumberStyles.HexNumber);
                byte[] client = BitConverter.GetBytes(settings.ClientSeed).Reverse().ToArray();
                decimal payin = settings.StartingPayIn;
                decimal bal = res.StartingBalance;
                for (int x = 0; x < res.BetCount; ++x)
                {
                    byte[] data = seed.Concat(client).Concat(BitConverter.GetBytes(x).Reverse()).ToArray();
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

                session[settings.Currency].PauseUpdates();
                try
                {
                    for (int x = 0; x < res.BetCount; ++x)
                        if (res.Secrets[x] >= settings.GuessLow && res.Secrets[x] <= settings.GuessHigh)
                            ++session[settings.Currency].BetWinCount;
                    session[settings.Currency].BetCount += res.BetCount;
                    session[settings.Currency].Balance = res.StartingBalance + res.TotalPayIn + res.TotalPayOut;
                    session[settings.Currency].BetPayIn += res.TotalPayIn;
                    session[settings.Currency].BetPayOut += res.TotalPayOut;
                }
                finally
                {
                    session[settings.Currency].UnpauseUpdates();
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
        static void Validate(Currencies currency)
        {
            if (currency == Currencies.None || !Enum.IsDefined(typeof(Currencies), currency))
                throw new ArgumentOutOfRangeException();
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
        static void Validate(SessionInfo session, long guessLow, long guessHigh, Currencies currency)
        {
            if (session == null)
                throw new ArgumentNullException();
            if (guessLow < 0 || guessLow > guessHigh || guessHigh >= GuessSpan)
                throw new ArgumentOutOfRangeException("0 <= GuessLow <= GuessHigh <= " + GuessSpan.ToString());
            Validate(currency);
        }
        static void Validate(SessionInfo session, AutomatedBetsSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException();
            Validate(session, settings.GuessLow, settings.GuessHigh, settings.Currency);
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
            return decimal.Floor((HousePayout / CalculateChanceToWin(guessLow, guessHigh)) * 1000000M) / 1000000M;
        }
        public static int GenerateBetResult(string serverSeed, int clientSeed, int betNumber)
        {
            if (serverSeed == null)
                throw new ArgumentNullException();
            if (betNumber < 0 || serverSeed.Length != 64)
                throw new ArgumentOutOfRangeException();

            Func<string, byte[]> strtobytes = s => Enumerable
                .Range(0, s.Length / 2)
                .Select(x => byte.Parse(s.Substring(x * 2, 2), NumberStyles.HexNumber))
                .ToArray();
            byte[] server = strtobytes(serverSeed);
            byte[] client = BitConverter.GetBytes(clientSeed).Reverse().ToArray();
            byte[] num = BitConverter.GetBytes(betNumber).Reverse().ToArray();
            byte[] data = server.Concat(client).Concat(num).ToArray();
            using (SHA512 sha512 = new SHA512Managed())
            {
                byte[] hash = sha512.ComputeHash(sha512.ComputeHash(data));
                while (true)
                {
                    for (int x = 0; x <= 61; x += 3)
                    {
                        long result = (hash[x] << 16) | (hash[x + 1] << 8) | hash[x + 2];
                        if (result < 16000000)
                            return (int)(result % 1000000);
                    }
                    hash = sha512.ComputeHash(hash);
                }
            }
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
        public static BeginSessionResponse BeginSession(string apiKey, string username, string password, int totp)
        {
            Validate(apiKey, username);
            return Process(Request<BeginSessionResponse>(GetFormDataLogin(apiKey, username, password, totp)), username);
        }
        public static CreateUserResponse CreateUser(SessionInfo session, string username, string password)
        {
            Validate(session, username, password);
            username = username.Trim();
            return Process(session, Request<CreateUserResponse>(GetFormDataCreateUser(session.SessionCookie, username, password)), username);
        }
        public static GetBalanceResponse GetBalance(SessionInfo session, Currencies currency)
        {
            Validate(session);
            Validate(currency);
            return Process(session, Request<GetBalanceResponse>(GetFormDataGetBalance(session.SessionCookie, currency)), currency);
        }
        public static WithdrawResponse WithdrawAll(SessionInfo session, string address, Currencies currency)
        {
            return Withdraw(session, 0, address, currency);
        }
        public static WithdrawResponse WithdrawAll(SessionInfo session, string address, int totp, Currencies currency)
        {
            return Withdraw(session, 0, address, totp, currency);
        }
        public static WithdrawResponse Withdraw(SessionInfo session, decimal amount, string address, Currencies currency)
        {
            Validate(session, address);
            Validate(currency);
            return Process(session, Request<WithdrawResponse>(GetFormDataWithdraw(session.SessionCookie, amount, address, currency)), currency);
        }
        public static WithdrawResponse Withdraw(SessionInfo session, decimal amount, string address, int totp, Currencies currency)
        {
            Validate(session, address);
            Validate(currency);
            return Process(session, Request<WithdrawResponse>(GetFormDataWithdraw(session.SessionCookie, amount, address, totp, currency)), currency);
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
        public static GetDepositAddressResponse GetDepositAddress(SessionInfo session, Currencies currency)
        {
            Validate(session);
            Validate(currency);
            return Process(session, Request<GetDepositAddressResponse>(GetFormDataGetDepositAddress(session.SessionCookie, currency)), currency);
        }
        public static PlaceBetResponse PlaceBet(SessionInfo session, decimal payIn, long guessLow, long guessHigh, int clientSeed, Currencies currency)
        {
            Validate(session, guessLow, guessHigh, currency);
            if (payIn > 0) payIn = -payIn;
            return Process(session, Request<PlaceBetResponse>(GetFormDataPlaceBet(session.SessionCookie, payIn, guessLow, guessHigh, clientSeed, currency)), payIn, guessLow, guessHigh, clientSeed, currency);
        }
        public static PlaceAutomatedBetsResponse PlaceAutomatedBets(SessionInfo session, AutomatedBetsSettings settings)
        {
            Validate(session, settings);
            return Process(session, Request<PlaceAutomatedBetsResponse>(
                GetFormDataPlaceAutomatedBets(session.SessionCookie, settings)), settings);
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
        public static async Task<BeginSessionResponse> BeginSessionAsync(string apiKey, string username, string password, int totp)
        {
            Validate(apiKey, username);
            return Process(await RequestAsync<BeginSessionResponse>(GetFormDataLogin(apiKey, username, password, totp)), username);
        }
        public static async Task<CreateUserResponse> CreateUserAsync(SessionInfo session, string username, string password)
        {
            Validate(session, username, password);
            username = username.Trim();
            return Process(session, await RequestAsync<CreateUserResponse>(GetFormDataCreateUser(session.SessionCookie, username, password)), username);
        }
        public static async Task<GetBalanceResponse> GetBalanceAsync(SessionInfo session, Currencies currency)
        {
            Validate(session);
            Validate(currency);
            return Process(session, await RequestAsync<GetBalanceResponse>(GetFormDataGetBalance(session.SessionCookie, currency)), currency);
        }
        public static async Task<WithdrawResponse> WithdrawAllAsync(SessionInfo session, string address, Currencies currency)
        {
            return await WithdrawAsync(session, 0, address, currency);
        }
        public static async Task<WithdrawResponse> WithdrawAllAsync(SessionInfo session, string address, int totp, Currencies currency)
        {
            return await WithdrawAsync(session, 0, address, totp, currency);
        }
        public static async Task<WithdrawResponse> WithdrawAsync(SessionInfo session, decimal amount, string address, Currencies currency)
        {
            Validate(session, address);
            return Process(session, await RequestAsync<WithdrawResponse>(GetFormDataWithdraw(session.SessionCookie, amount, address, currency)), currency);
        }
        public static async Task<WithdrawResponse> WithdrawAsync(SessionInfo session, decimal amount, string address, int totp, Currencies currency)
        {
            Validate(session, address);
            return Process(session, await RequestAsync<WithdrawResponse>(GetFormDataWithdraw(session.SessionCookie, amount, address, totp, currency)), currency);
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
        public static async Task<GetDepositAddressResponse> GetDepositAddressAsync(SessionInfo session, Currencies currency)
        {
            Validate(session);
            return Process(session, await RequestAsync<GetDepositAddressResponse>(GetFormDataGetDepositAddress(session.SessionCookie, currency)), currency);
        }
        public static async Task<PlaceBetResponse> PlaceBetAsync(SessionInfo session, decimal payIn, long guessLow, long guessHigh, int clientSeed, Currencies currency)
        {
            Validate(session, guessLow, guessHigh, currency);
            if (payIn > 0) payIn = -payIn;
            return Process(session, await RequestAsync<PlaceBetResponse>(GetFormDataPlaceBet(session.SessionCookie, payIn, guessLow, guessHigh, clientSeed, currency)), payIn, guessLow, guessHigh, clientSeed, currency);
        }
        public static async Task<PlaceAutomatedBetsResponse> PlaceAutomatedBetsAsync(SessionInfo session, AutomatedBetsSettings settings)
        {
            Validate(session, settings);
            return Process(session, await RequestAsync<PlaceAutomatedBetsResponse>(
                GetFormDataPlaceAutomatedBets(session.SessionCookie, settings)), settings);
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
        public static IAsyncResult BeginBeginSession(string apiKey, string username, string password, int totp, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => BeginSession(apiKey, username, password, totp));
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
        public static IAsyncResult BeginGetBalance(SessionInfo session, Currencies currency, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => GetBalance(session, currency));
        }
        public static GetBalanceResponse EndGetBalance(IAsyncResult i)
        {
            return GetWorkResult<GetBalanceResponse>(i);
        }
        public static IAsyncResult BeginWithdrawAll(SessionInfo session, string address, Currencies currency, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => WithdrawAll(session, address, currency));
        }
        public static IAsyncResult BeginWithdrawAll(SessionInfo session, string address, int totp, Currencies currency, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => WithdrawAll(session, address, totp, currency));
        }
        public static IAsyncResult BeginWithdraw(SessionInfo session, decimal amount, string address, Currencies currency, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => Withdraw(session, amount, address, currency));
        }
        public static IAsyncResult BeginWithdraw(SessionInfo session, decimal amount, string address, int totp, Currencies currency, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => Withdraw(session, amount, address, totp, currency));
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
        public static IAsyncResult BeginGetDepositAddress(SessionInfo session, Currencies currency, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => GetDepositAddress(session, currency));
        }
        public static GetDepositAddressResponse EndGetDepositAddress(IAsyncResult i)
        {
            return GetWorkResult<GetDepositAddressResponse>(i);
        }
        public static IAsyncResult BeginPlaceBet(SessionInfo session, decimal payIn, long guessLow, long guessHigh, int clientSeed, Currencies currency, AsyncCallback complete, object state)
        {
            return QueueWork(complete, state, () => PlaceBet(session, payIn, guessLow, guessHigh, clientSeed, currency));
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
