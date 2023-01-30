
namespace Precision.WebApi
{
    public static class ParseHelper
    {
        public static T[] ParseToArray<T>(string valueString)
        {
            return valueString.Split(',').Select(s => (T)Convert.ChangeType(s, typeof(T))).ToArray();
        }
        public static List<T> ParseToList<T>(string valueString)
        {
            return ParseToArray<T>(valueString).ToList();
        }
    }
}
