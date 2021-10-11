// https://stackoverflow.com/a/32716784

using Mono.Unix;
using System;
using System.Threading.Tasks;

namespace ExitSignal
{
    public class UnixExitSignal : IExitSignal
    {
        public event EventHandler Exit;

        UnixSignal[] signals = new UnixSignal[]{
        new UnixSignal(Mono.Unix.Native.Signum.SIGTERM),
        new UnixSignal(Mono.Unix.Native.Signum.SIGINT),
        new UnixSignal(Mono.Unix.Native.Signum.SIGUSR1)
    };

        public UnixExitSignal()
        {
            Task.Factory.StartNew(() =>
            {
            // blocking call to wait for any kill signal
            int index = UnixSignal.WaitAny(signals, -1);

                if (Exit != null)
                {
                    Exit(null, EventArgs.Empty);
                }

            });
        }

    }
}
