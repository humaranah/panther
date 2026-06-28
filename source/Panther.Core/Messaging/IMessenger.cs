namespace Panther.Core.Messaging;

public interface IMessenger
{
    void Register<TMessage, TToken>(object recipient, TToken token, Action<TMessage> action)
        where TMessage : class
        where TToken : IEquatable<TToken>;
    void Send<TMessage, TToken>(TMessage message, TToken token)
        where TMessage : class
        where TToken : IEquatable<TToken>;

    void Unregister<TMessage>(object recipient)
        where TMessage : class;
}
