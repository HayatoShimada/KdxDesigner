using System.Collections.Generic;

namespace KdxDesigner.Models
{
    public enum FindIOResultState
    {
        NotFound,
        FoundOne,
        FoundMultiple
    }

    public class FindIOResult
    {
        public FindIOResultState State { get; init; }
        public string? SingleAddress { get; init; }
        public List<IO>? MultipleMatches { get; init; }
    }
}