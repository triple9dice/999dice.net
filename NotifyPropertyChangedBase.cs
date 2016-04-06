using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
#if !NET_35
using System.Threading.Tasks;
#endif

namespace Dice.Client.Web
{
    public abstract class NotifyPropertyChangedBase: INotifyPropertyChanged
    {
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
        protected void RaisePropertyChanged(string propname)
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
    }
}
