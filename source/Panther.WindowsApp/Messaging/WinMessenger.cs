using CommunityToolkit.Mvvm.Messaging;
using System;
using IMessenger = Panther.Core.Messaging.IMessenger;

namespace Panther.WindowsApp.Messaging;

public class WinMessenger : IMessenger
{
    public void Register<TMessage, TToken>(object recipient, TToken token, Action<TMessage> action)
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        WeakReferenceMessenger.Default.Register<object, TMessage, TToken>(
            recipient,
            token,
            (r, m) => action(m)
        );
    }

    public void Send<TMessage, TToken>(TMessage message, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>
    {
        WeakReferenceMessenger.Default.Send<TMessage, TToken>(message, token);
    }

    public void Unregister<TMessage>(object recipient)
        where TMessage : class
    {
        WeakReferenceMessenger.Default.Unregister<TMessage>(recipient);
    }
}
