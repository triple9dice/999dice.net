using System;
using System.Collections.Generic;
using System.Linq;

namespace Dice.Client.Web
{
    /// <summary>
    /// The base class from which all API response classes are derived.
    /// </summary>
    public abstract class DiceResponse
    {
        /// <summary>
        /// True, if the API call was successful.
        /// </summary>
        public bool Success { get; internal set; }
        /// <summary>
        /// The HTTP status code for the API call, if available.
        /// </summary>
        public int WebStatusCode { get; internal set; }
        /// <summary>
        /// An error message from the API call, if available. 
        /// Standard errors like "too fast" do not return error messages, but rather set boolean values (like DiceResponse.RateLimited).
        /// </summary>
        public string ErrorMessage { get; internal set; }
        /// <summary>
        /// The raw API response converted from JSON.
        /// </summary>
        public IDictionary<string, object> RawResponse { get; private set; }
        /// <summary>
        /// True if you have exceeded rate limits.
        /// </summary>
        public bool RateLimited { get; private set; }
        /// <summary>
        /// True if the action required a valid Totp (ie Google Authenticator) code, which was not provided.
        /// </summary>
        public bool TotpFailure { get; private set; }

        internal DiceResponse()
        {
        }
        /// <summary>
        /// Stores the response object, converted from JSON.
        /// Checks for common result codes.
        /// </summary>
        /// <param name="resp">The response object, converted from JSON</param>
        virtual internal void SetRawResponse(IDictionary<string, object> resp)
        {
            RawResponse = resp;
            if (resp.ContainsKey("error"))
                ErrorMessage = (string)resp["error"];
            if (resp.ContainsKey("TooFast"))
                RateLimited = true;
            if (resp.ContainsKey("TotpFailure"))
                TotpFailure = true;
            if (resp.ContainsKey("success"))
                Success = true;
        }
    }
    public sealed class BeginSessionResponse : DiceResponse
    {
        public SessionInfo Session { get; private set; }
        public bool InvalidApiKey { get; private set; }
        public bool LoginRequired { get; private set; }
        public bool WrongUsernameOrPassword { get; private set; }

        internal override void SetRawResponse(IDictionary<string, object> resp)
        {
            base.SetRawResponse(resp);
            if (resp.ContainsKey("InvalidApiKey"))
            {
                InvalidApiKey = true;
                return;
            }
            if (resp.ContainsKey("LoginRequired"))
            {
                LoginRequired = true;
                return;
            }
            if (resp.ContainsKey("LoginInvalid"))
            {
                WrongUsernameOrPassword = true;
                return;
            }

            if (resp.ContainsKey("SessionCookie") &&
                resp.ContainsKey("AccountId") &&
                resp.ContainsKey("MaxBetBatchSize"))
            {
                Success = true;
                Session = new SessionInfo(
                    (string)resp["SessionCookie"],
                    Convert.ToInt64(resp["AccountId"]),
                    Convert.ToInt32(resp["MaxBetBatchSize"]));
                if (resp.ContainsKey("AccountCookie"))
                    Session.AccountCookie = (string)resp["AccountCookie"];
                if (resp.ContainsKey("Email"))
                    Session.Email = (string)resp["Email"];
                if (resp.ContainsKey("EmergencyAddress"))
                    Session.EmergencyAddress = (string)resp["EmergencyAddress"];

                for (int x = 0; ; ++x)
                {
                    IDictionary<string, object> c = null;
                    SessionInfo.CurrencyInfo ci = null;
                    switch (x)
                    {
                        case 0:
                            c = resp;
                            ci = Session[Currencies.BTC];
                            break;
                        case 1:
                            c = resp.ContainsKey("Doge") ? resp["Doge"] as IDictionary<string, object> : null;
                            ci = Session[Currencies.Doge];
                            break;
                        case 2:
                            c = resp.ContainsKey("LTC") ? resp["LTC"] as IDictionary<string, object> : null;
                            ci = Session[Currencies.LTC];
                            break;
                        default:
                            break;
                    }
                    if (ci == null)
                        break;
                    if (c == null)
                        continue;
                    if (c.ContainsKey("BetCount"))
                        ci.BetCount = Convert.ToInt64(c["BetCount"]);
                    if (c.ContainsKey("BetPayIn"))
                        ci.BetPayIn = Convert.ToDecimal(c["BetPayIn"]) / 100000000M;
                    if (c.ContainsKey("BetPayOut"))
                        ci.BetPayOut = Convert.ToDecimal(c["BetPayOut"]) / 100000000M;
                    if (c.ContainsKey("BetWinCount"))
                        ci.BetWinCount = Convert.ToInt64(c["BetWinCount"]);
                    if (c.ContainsKey("Balance"))
                        ci.Balance = Convert.ToDecimal(c["Balance"]) / 100000000M;
                    if (c.ContainsKey("DepositAddress"))
                        ci.DepositAddress = (string)c["DepositAddress"];
                }
            }
        }
    }
    public sealed class CreateUserResponse : DiceResponse
    {
        /// <summary>
        /// True if the account already has a username and password.
        /// </summary>
        public bool AccountAlreadyHasUser { get; private set; }
        /// <summary>
        /// True if the username is already in use by someone else.
        /// </summary>
        public bool UsernameAlreadyTaken { get; private set; }

        internal override void SetRawResponse(IDictionary<string, object> resp)
        {
            base.SetRawResponse(resp);

            if (resp.ContainsKey("AccountHasUser"))
                AccountAlreadyHasUser = true;
            else if (resp.ContainsKey("UsernameTaken"))
                UsernameAlreadyTaken = true;
        }
    }
    public sealed class WithdrawResponse : DiceResponse
    {
        /// <summary>
        /// The amount of the withdrawal which is now pending.
        /// </summary>
        public decimal WithdrawalPending { get; private set; }
        /// <summary>
        /// True if the amount of the withdrawal is too small to process.
        /// </summary>
        public bool WithdrawalTooSmall { get; private set; }
        /// <summary>
        /// True if there are insufficient funds to make the requested withdrawal.
        /// </summary>
        public bool InsufficientFunds { get; private set; }
        /// <summary>
        /// The currency of the withdrawal
        /// </summary>
        public Currencies Currency { get; internal set; }

        internal override void SetRawResponse(IDictionary<string, object> resp)
        {
            base.SetRawResponse(resp);

            if (resp.ContainsKey("TooSmall"))
                WithdrawalTooSmall = true;
            else if (resp.ContainsKey("InsufficientFunds"))
                InsufficientFunds = true;
            else if (resp.ContainsKey("Pending"))
            {
                WithdrawalPending = Convert.ToDecimal(resp["Pending"]) / 100000000M;
                Success = true;
            }
        }
    }
    public sealed class ChangePasswordResponse : DiceResponse
    {
        /// <summary>
        /// True if the wrong password was supplied.
        /// </summary>
        public bool WrongPassword { get; private set; }

        internal override void SetRawResponse(IDictionary<string, object> resp)
        {
            base.SetRawResponse(resp);

            if (resp.ContainsKey("WrongPassword"))
                WrongPassword = true;
        }
    }
    public sealed class GetServerSeedHashResponse : DiceResponse
    {
        /// <summary>
        /// The server seed hash (SHA-256)
        /// </summary>
        public string ServerSeedHash { get; private set; }

        internal override void SetRawResponse(IDictionary<string, object> resp)
        {
            base.SetRawResponse(resp);

            if (resp.ContainsKey("Hash"))
            {
                Success = true;
                ServerSeedHash = (string)resp["Hash"];
            }
        }
    }
    public sealed class GetBalanceResponse : DiceResponse
    {
        /// <summary>
        /// The current balance of the user.
        /// </summary>
        public decimal Balance { get; private set; }
        /// <summary>
        /// The currency of the balance
        /// </summary>
        public Currencies Currency { get; internal set; }

        internal override void SetRawResponse(IDictionary<string, object> resp)
        {
            base.SetRawResponse(resp);

            if (resp.ContainsKey("Balance"))
            {
                Success = true;
                Balance = Convert.ToDecimal(resp["Balance"]) / 100000000M;
            }
        }
    }
    public sealed class SetClientSeedResponse : DiceResponse
    {
    }
    public sealed class UpdateEmailResponse : DiceResponse
    {
    }
    public sealed class UpdateEmergencyAddressResponse : DiceResponse
    {
    }
    public sealed class GetDepositAddressResponse : DiceResponse
    {
        /// <summary>
        /// A bitcoin address accepting deposits for the user.
        /// </summary>
        public string DepositAddress { get; private set; }
        /// <summary>
        /// The currency of the deposit address
        /// </summary>
        public Currencies Currency { get; internal set; }

        internal override void SetRawResponse(IDictionary<string, object> resp)
        {
            base.SetRawResponse(resp);

            if (resp.ContainsKey("Address"))
            {
                Success = true;
                DepositAddress = (string)resp["Address"];
            }
        }
    }
    public sealed class PlaceBetResponse : DiceResponse
    {
        /// <summary>
        /// True if the chance to win is too high.
        /// </summary>
        public bool ChanceTooHigh { get; private set; }
        /// <summary>
        /// True if the chance to win is too low.
        /// </summary>
        public bool ChanceTooLow { get; private set; }
        /// <summary>
        /// True if there are insufficient funds to place the bet.
        /// </summary>
        public bool InsufficientFunds { get; private set; }
        /// <summary>
        /// True if there is no possible way the user could profit from a bet that has a cost.
        /// </summary>
        public bool NoPossibleProfit { get; private set; }
        /// <summary>
        /// True if the payout for this bet could be greater than what the site is willing to risk.
        /// </summary>
        public bool MaxPayoutExceeded { get; private set; }

        /// <summary>
        /// The bet's ID.
        /// </summary>
        public long BetId { get; private set; }
        /// <summary>
        /// The payout for a winning bet.
        /// </summary>
        public decimal PayOut { get; private set; }
        /// <summary>
        /// The secret value generated for the bet.
        /// </summary>
        public long Secret { get; private set; }
        /// <summary>
        /// The user's balance immediately before placing this bet.
        /// </summary>
        public decimal StartingBalance { get; private set; }
        /// <summary>
        /// The server seed used for this bet.
        /// </summary>
        public string ServerSeed { get; private set; }

        /// <summary>
        /// The currency of the bet
        /// </summary>
        public Currencies Currency { get; internal set; }

        internal override void SetRawResponse(IDictionary<string, object> resp)
        {
            base.SetRawResponse(resp);

            if (resp.ContainsKey("ChanceTooHigh"))
                ChanceTooHigh = true;
            else if (resp.ContainsKey("ChanceTooLow"))
                ChanceTooLow = true;
            else if (resp.ContainsKey("InsufficientFunds"))
                InsufficientFunds = true;
            else if (resp.ContainsKey("NoPossibleProfit"))
                NoPossibleProfit = true;
            else if (resp.ContainsKey("MaxPayoutExceeded"))
                MaxPayoutExceeded = true;
            else if (resp.ContainsKey("BetId") &&
                resp.ContainsKey("PayOut") &&
                resp.ContainsKey("Secret") &&
                resp.ContainsKey("StartingBalance") &&
                resp.ContainsKey("ServerSeed"))
            {
                Success = true;
                BetId = Convert.ToInt64(resp["BetId"]);
                PayOut = Convert.ToDecimal(resp["PayOut"]) / 100000000M;
                Secret = Convert.ToInt64(resp["Secret"]);
                StartingBalance = Convert.ToDecimal(resp["StartingBalance"]) / 100000000M;
                ServerSeed = (string)resp["ServerSeed"];
            }
        }
    }
    public sealed class PlaceAutomatedBetsResponse : DiceResponse
    {
        /// <summary>
        /// True if the chance to win is too high.
        /// </summary>
        public bool ChanceTooHigh { get; private set; }
        /// <summary>
        /// True if the chance to win is too low.
        /// </summary>
        public bool ChanceTooLow { get; private set; }
        /// <summary>
        /// True if there are insufficient funds to place the bet.
        /// </summary>
        public bool InsufficientFunds { get; private set; }
        /// <summary>
        /// True if there is no possible way the user could profit from a bet that has a cost.
        /// </summary>
        public bool NoPossibleProfit { get; private set; }
        /// <summary>
        /// True if the payout for this bet could be greater than what the site is willing to risk.
        /// </summary>
        public bool MaxPayoutExceeded { get; private set; }

        /// <summary>
        /// The ID's of the bets in the batch.
        /// </summary>
        public long[] BetIds { get; private set; }
        /// <summary>
        /// The amounts paid in for each bet in the batch.
        /// These are always negative numbers.
        /// </summary>
        public decimal[] PayIns { get; private set; }
        /// <summary>
        /// The payouts from each bet in the batch.
        /// </summary>
        public decimal[] PayOuts { get; private set; }
        /// <summary>
        /// The secret numbers generated for the bets.
        /// </summary>
        public long[] Secrets { get; private set; }
        /// <summary>
        /// The number of bets placed in this batch.
        /// </summary>
        public int BetCount { get; private set; }
        /// <summary>
        /// The total amount paid to place the bets in this batch.
        /// This value, plus TotalPayOut will equal the net profit.
        /// This is always a negative number.
        /// </summary>
        public decimal TotalPayIn { get; private set; }
        /// <summary>
        /// The total amount paid out from bets in this batch.
        /// This value, plus TotalPayIn will equal the net profit.
        /// </summary>
        public decimal TotalPayOut { get; private set; }
        /// <summary>
        /// The server seed used to generate these bets.
        /// </summary>
        public string ServerSeed { get; private set; }
        /// <summary>
        /// The user's balance immediately before placing the first bet of this batch.
        /// </summary>
        public decimal StartingBalance { get; private set; }

        /// <summary>
        /// The currency of the bets
        /// </summary>
        public Currencies Currency { get; internal set; }

        internal override void SetRawResponse(IDictionary<string, object> resp)
        {
            base.SetRawResponse(resp);

            if (resp.ContainsKey("ChanceTooHigh"))
                ChanceTooHigh = true;
            else if (resp.ContainsKey("ChanceTooLow"))
                ChanceTooLow = true;
            else if (resp.ContainsKey("InsufficientFunds"))
                InsufficientFunds = true;
            else if (resp.ContainsKey("NoPossibleProfit"))
                NoPossibleProfit = true;
            else if (resp.ContainsKey("MaxPayoutExceeded"))
                MaxPayoutExceeded = true;
            else if (resp.ContainsKey("BetId") &&
                resp.ContainsKey("PayIn") &&
                resp.ContainsKey("PayOut") &&
                resp.ContainsKey("Seed") &&
                resp.ContainsKey("BetCount") &&
                resp.ContainsKey("StartingBalance"))
            {
                Success = true;
                long BetId = Convert.ToInt64(resp["BetId"]);
                BetCount = Convert.ToInt32(resp["BetCount"]);
                ServerSeed = (string)resp["Seed"];
                TotalPayIn = Convert.ToDecimal(resp["PayIn"]) / 100000000M;
                TotalPayOut = Convert.ToDecimal(resp["PayOut"]) / 100000000M;
                StartingBalance = Convert.ToDecimal(resp["StartingBalance"]) / 100000000M;
                BetIds = Enumerable.Range(0, BetCount).Select(x => BetId).ToArray();
                Secrets = new long[BetCount];
                PayIns = new decimal[BetCount];
                PayOuts = new decimal[BetCount];
            }
        }
    }
}
