// https://stackoverflow.com/a/32716784

using System;

namespace ExitSignal
{
    public interface IExitSignal
    {
        event EventHandler Exit;
    }
}