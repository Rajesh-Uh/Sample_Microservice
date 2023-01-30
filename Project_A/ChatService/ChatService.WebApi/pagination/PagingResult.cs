using System.Collections.Generic;

namespace Precision.WebApi.PagingHelper
{
    public class PagingResult<T>
    {
        public PagingMetadata Metadata { get; }
        public IEnumerable<T> Result { get; }

        public PagingResult(PagingMetadata metadata, IEnumerable<T> result)
        {
            Metadata = metadata;
            Result = result;
        }
    }
}
