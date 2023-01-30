namespace Precision.WebApi.PagingHelper
{
    /// <summary>
    /// Links for PagingMetadata
    /// </summary>
    public class PagingLinks
    {
        public string Self { get; set; }
        public string Next { get; set; }
        public string Prev { get; set; }
        public string First { get; set; }
        public string Last { get; set; }
    }
}
