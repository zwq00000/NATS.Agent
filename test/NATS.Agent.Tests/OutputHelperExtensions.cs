using Newtonsoft.Json;

namespace Xunit.Abstractions {
    internal static class OutputHelperExtensions {

        private static readonly JsonSerializerSettings settings = new () {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        public static void WriteJson<T> (this ITestOutputHelper output, T data) {
            var json = JsonConvert.SerializeObject (data, Formatting.Indented, settings);
            output.WriteLine (json);
        }
    }
}