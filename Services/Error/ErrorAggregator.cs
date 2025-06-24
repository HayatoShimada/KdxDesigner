using KdxDesigner.Models;

using System.Collections.Generic;
using System.Linq;

namespace KdxDesigner.Services.Error
{
    public class ErrorAggregator : IErrorAggregator
    {
        private readonly List<OutputError> _errors = new();
        private readonly object _lock = new object();
        private readonly int? _mnemonicId;
        public ErrorAggregator(int? mnemonicId = null)
        {
            _mnemonicId = mnemonicId;
        }


        public void AddError(OutputError error)
        {
            if (_mnemonicId != null) error.MnemonicId = _mnemonicId;
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