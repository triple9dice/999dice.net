using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace Dice.Client.Web
{
    /// <summary>
    /// Holds information about a user's session.
    /// </summary>
    public sealed class SessionInfo : NotifyPropertyChangedBase
    {
        public sealed class CurrencyInfo : NotifyPropertyChangedBase
        {
            long _BetCount, _BetWinCount;
            decimal _BetPayIn, _BetPayOut, _Balance;
            string _DepositAddress;


            /// <summary>
            /// The number of bets made.
            /// </summary>
            public long BetCount
            {
                get
                {
                    return _BetCount;
                }
                internal set
                {
                    _BetCount = value;
                    RaisePropertyChanged("BetCount");
                }
            }
            /// <summary>
            /// The total value of all bets (a negative number).
            /// This value, plus BetPayOut, equals the total profit/loss from all bets.
            /// </summary>
            public decimal BetPayIn
            {
                get
                {
                    return _BetPayIn;
                }
                internal set
                {
                    _BetPayIn = value;
                    RaisePropertyChanged("BetPayIn");
                }
            }
            /// <summary>
            /// The total value of all bet payouts.
            /// This value, plus BetPayIn, equals the total profit/loss from all bets.
            /// </summary>
            public decimal BetPayOut
            {
                get
                {
                    return _BetPayOut;
                }
                internal set
                {
                    _BetPayOut = value;
                    RaisePropertyChanged("BetPayOut");
                }
            }
            /// <summary>
            /// The total number of winning bets.
            /// </summary>
            public long BetWinCount
            {
                get
                {
                    return _BetWinCount;
                }
                internal set
                {
                    _BetWinCount = value;
                    RaisePropertyChanged("BetWinCount");
                }
            }
            /// <summary>
            /// The current account balance.
            /// </summary>
            public decimal Balance
            {
                get
                {
                    return _Balance;
                }
                internal set
                {
                    _Balance = value;
                    RaisePropertyChanged("Balance");
                }
            }
            /// <summary>
            /// A deposit address to which funds can be added.
            /// </summary>
            public string DepositAddress
            {
                get
                {
                    return _DepositAddress;
                }
                internal set
                {
                    _DepositAddress = value;
                    RaisePropertyChanged("DepositAddress");
                }
            }
        }

        /// <summary>
        /// The client's session cookie.
        /// </summary>
        public string SessionCookie { get; private set; }
        /// <summary>
        /// The client's account ID.
        /// </summary>
        public long AccountId { get; private set; }
        /// <summary>
        /// The maximum number of bets that can be submitted in a single batch.
        /// </summary>
        public int MaxBetBatchSize { get; private set; }

        string _AccountCookie, _Email, _EmergencyAddress, _Username;

        /// <summary>
        /// The client's account cookie. This value can be saved to create new sessions for the same account at a later date.
        /// </summary>
        public string AccountCookie
        {
            get
            {
                return _AccountCookie;
            }
            internal set
            {
                _AccountCookie = value;
                RaisePropertyChanged("AccountCookie");
            }
        }
        /// <summary>
        /// The email address associated to the account.
        /// </summary>
        public string Email
        {
            get
            {
                return _Email;
            }
            internal set
            {
                _Email = value;
                RaisePropertyChanged("Email");
            }
        }
        /// <summary>
        /// The emergency withdrawal address associated to the account.
        /// </summary>
        public string EmergencyAddress
        {
            get
            {
                return _EmergencyAddress;
            }
            internal set
            {
                _EmergencyAddress = value;
                RaisePropertyChanged("EmergencyAddress");
            }
        }
        /// <summary>
        /// The username for the account.
        /// </summary>
        public string Username
        {
            get
            {
                return _Username;
            }
            internal set
            {
                _Username = value;
                RaisePropertyChanged("Username");
            }
        }

        readonly Dictionary<Currencies, CurrencyInfo> currencyInfo =
            ((Currencies[])Enum.GetValues(typeof(Currencies)))
            .Where(x => x != Currencies.None)
            .ToDictionary(x => x, x => new CurrencyInfo());

        public CurrencyInfo this[Currencies currency]
        {
            get
            {
                return currencyInfo[currency];
            }
        }

        internal SessionInfo(string sessionCookie, long accountId, int maxBetBatchSize)
        {
            SessionCookie = sessionCookie;
            AccountId = accountId;
            MaxBetBatchSize = maxBetBatchSize;
        }
    }
}
