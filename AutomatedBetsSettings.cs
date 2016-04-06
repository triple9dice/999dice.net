namespace Dice.Client.Web
{
    /// <summary>
    /// This is a reuseable class containing parameters used for placing batches of automated bets.
    /// </summary>
    public sealed class AutomatedBetsSettings
    {
        decimal basePayIn, maxAllowedPayIn, increaseOnWinPercent, increaseOnLosePercent, startingPayIn;
        Currencies currency;
        
        /// <summary>
        /// The low range of the guess for the bets.
        /// </summary>
        public long GuessLow;
        /// <summary>
        /// The high range of the guess for the bets.
        /// </summary>
        public long GuessHigh;
        /// <summary>
        /// The maximum number of bets to place in a batch.
        /// </summary>
        public int MaxBets;
        /// <summary>
        /// If true, after a win, the next bet's value will be BasePayIn. IncreaseOnWinPercent should be 0.
        /// If false, after a win, increase the next bet's value by IncreaseOnWinPercent.
        /// </summary>
        public bool ResetOnWin;
        /// <summary>
        /// If true, after a loss, the next bet's value will be BasePayIn. IncreaseOnLossPercent should be 0.
        /// If false, after a loss, increase the next bet's value by IncreaseOnLossPercent.
        /// </summary>
        public bool ResetOnLose;
        /// <summary>
        /// If true, after losing a bet equal to MaxAllowedPayIn, the next bet's value will be BasePayIn.
        /// </summary>
        public bool ResetOnLoseMaxBet;
        /// <summary>
        /// If true, after losing a bet equal to MaxAllowedPayIn, the bet batch will end.
        /// </summary>
        public bool StopOnLoseMaxBet;
        /// <summary>
        /// If set, the balance will be checked after each bet, and if it is at least this value, the bet batch will end.
        /// </summary>
        public decimal StopMaxBalance;
        /// <summary>
        /// If set, the balance will be checked after each bet, and if it is less than this value, the bet batch will end.
        /// </summary>
        public decimal StopMinBalance;
        /// <summary>
        /// Set this to a random number.
        /// </summary>
        public int ClientSeed;

        /// <summary>
        /// The base (lowest) value of the bet.
        /// Options like ResetOnWin, ResetOnLose, etc, reset the bet to this value.
        /// This is always a negative number.
        /// </summary>
        public decimal BasePayIn
        {
            get
            {
                return basePayIn;
            }
            set
            {
                basePayIn = value > 0 ? -value : value;
            }
        }
        /// <summary>
        /// The largest allowable value of a bet.
        /// This is always a negative number.
        /// </summary>
        public decimal MaxAllowedPayIn
        {
            get
            {
                return maxAllowedPayIn;
            }
            set
            {
                maxAllowedPayIn = value > 0 ? -value : value;
            }
        }
        /// <summary>
        /// The value of the first bet in the batch.
        /// If not set, this will default to the value of BasePayIn.
        /// This is always a negative number.
        /// </summary>
        public decimal StartingPayIn
        {
            get
            {
                return startingPayIn == 0 ? BasePayIn : startingPayIn;
            }
            set
            {
                startingPayIn = value > 0 ? -value : value;
            }
        }
        /// <summary>
        /// After a winning bet, increase the next bet amount by this percentage.
        /// 1 = 100% increase (double).
        /// Rounded to 6 decimal places.
        /// </summary>
        public decimal IncreaseOnWinPercent
        {
            get
            {
                return increaseOnWinPercent;
            }
            set
            {
                increaseOnWinPercent = decimal.Round(value, 6);
            }
        }
        /// <summary>
        /// After a losing bet, increase the next bet amount by this percentage.
        /// 1 = 100% increase (double).
        /// Rounded to 6 decimal places.
        /// </summary>
        public decimal IncreaseOnLosePercent
        {
            get
            {
                return increaseOnLosePercent;
            }
            set
            {
                increaseOnLosePercent = decimal.Round(value, 6);
            }
        }
        /// <summary>
        /// The currency to use for these bet.
        /// </summary>
        public Currencies Currency
        {
            get
            {
                return currency;
            }
            set
            {
                currency = value;
            }
        }
    }
}
