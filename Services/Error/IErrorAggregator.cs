using KdxDesigner.Models;

namespace KdxDesigner.Services.Error
{
    public interface IErrorAggregator
    {
        void AddError(OutputError error);
        void AddErrors(IEnumerable<OutputError> errors);
        IReadOnlyList<OutputError> GetAllErrors();
        void Clear();
    }
}