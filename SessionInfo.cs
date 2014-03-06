using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
#if !NET_35
using System.Threading.Tasks;
#endif

namespace Dice.Client.Web
{
    /// <summary>
    /// Holds information about a user's session.
    /// </summary>
    public sealed class SessionInfo : INotifyPropertyChanged
    {
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

        string _AccountCookie, _Email, _EmergencyAddress, _DepositAddress, _Username;
        long _ClientSeed, _BetCount, _BetWinCount;
        decimal _BetPayIn, _BetPayOut, _Balance;

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
        /// The seed used for betting.
        /// </summary>
        public long ClientSeed
        {
            get
            {
                return _ClientSeed;
            }
            internal set
            {
                _ClientSeed = value;
                RaisePropertyChanged("ClientSeed");
            }
        }
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

        internal SessionInfo(string sessionCookie, long accountId, int maxBetBatchSize)
        {
            SessionCookie = sessionCookie;
            AccountId = accountId;
            MaxBetBatchSize = maxBetBatchSize;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        int PropertyUpdatesPaused;
        internal void PauseUpdates()
        {
            Interlocked.Exchange(ref PropertyUpdatesPaused, 1);
        }
        internal void UnpauseUpdates()
        {
            if (Interlocked.Exchange(ref PropertyUpdatesPaused, 0) != 0)
                QueueRaise();
        }
        void QueueRaise()
        {
            if (Interlocked.Exchange(ref RaisePropertyChangedQueued, 1) != 0)
                return; 
#if !NET_35
            Task.Factory.StartNew(RaisePropertiesChanged);
#else
            ThreadPool.QueueUserWorkItem(x => RaisePropertiesChanged());            
#endif
        }
        readonly HashSet<string> Changed = new HashSet<string>();
        int RaisePropertyChangedQueued;
        void RaisePropertyChanged(string propname)
        {
            if (PropertyChanged == null)
                return;
            lock (Changed)
                if (!Changed.Add(propname))
                    return;
            if (Interlocked.CompareExchange(ref PropertyUpdatesPaused, 0, 0) != 0)
                return;
            QueueRaise();
        }
        void RaisePropertiesChanged()
        {
            string[] chg;
            lock (Changed)
            {
                chg = Changed.ToArray();
                Changed.Clear();
                Interlocked.Exchange(ref RaisePropertyChangedQueued, 0);
            }
            var pc = PropertyChanged;
            if (pc != null)
                foreach (var n in chg)
                    pc(this, new PropertyChangedEventArgs(n));
        }
        #endregion
    }
}
