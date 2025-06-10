using KdxDesigner.Models;

using System.Collections.Generic;
using System.Linq;

namespace KdxDesigner.Services.Error
{
    public class ErrorAggregator : IErrorAggregator
    {
        private readonly List<OutputError> _errors = new();
        private readonly object _lock = new object();

        public void AddError(OutputError error)
        {
            lock (_lock) { _errors.Add(error); }
        }

        public void AddErrors(IEnumerable<OutputError> errors)
        {
            if (errors == null) return;
            lock (_lock) { _errors.AddRange(errors); }
        }

        public IReadOnlyList<OutputError> GetAllErrors()
        {
            lock (_lock) { return _errors.ToList().AsReadOnly(); }
        }

        public void Clear()
        {
            lock (_lock) { _errors.Clear(); }
        }
    }
}