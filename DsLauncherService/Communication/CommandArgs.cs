using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace DsLauncherService.Communication
{
    internal class CommandArgs
    {
        private readonly Dictionary<string, string> _args = new();

        public void Add<T>(string key, T value) where T : IParsable<T>
        {
            string? valueStr = value.ToString()?
                .Replace("\n", "")
                .Replace("\r", "");
            valueStr ??= "";

            _args[key] = valueStr;
        }

        public void Add<T>(string key, IEnumerable<T> values) where T : IParsable<T>
        {
            Add(key + "[]", values.Count());

            int index = 0;
            foreach (var value in values)
            {
                Add($"{key}[{index++}]", value);
            }
        }

        public T Get<T>(string key) where T : IParsable<T>
        {
            return T.Parse(_args[key], CultureInfo.InvariantCulture);
        }

        public bool TryGet<T>(string key, [MaybeNullWhen(false)]out T value) where T : IParsable<T>
        {
            value = default;
            return _args.TryGetValue(key, out var str) && 
                T.TryParse(str, CultureInfo.InvariantCulture, out value); 
        }

        public IEnumerable<T> EnumerateArray<T>(string arrayName) where T : IParsable<T>
        {
            int count = Get<int>(arrayName + "[]");

            for (int i = 0; i < count; i++)
            {
                yield return Get<T>($"{arrayName}[{i}]");
            }
        }

        public override string ToString()
        {
            return string.Join('\n', _args.Select(pair =>
            {
                return $"{pair.Key}: {pair.Value}";
            }));
        }
    }
}
