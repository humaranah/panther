namespace Panther.Infrastructure.BassWrapper.Models;

public interface IBassNotifier
{
    event EventHandler<BassOperationError> OperationError;
}
