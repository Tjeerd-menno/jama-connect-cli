using System.Globalization;
using System.Text;

namespace JamaConnect.Infrastructure.JamaConnect;

internal sealed class EndpointBuilder
{
    private readonly string _path;
    private readonly List<(string Name, string Value)> _parameters = [];

    public EndpointBuilder(string path)
    {
        _path = path;
    }

    public EndpointBuilder Add(string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            _parameters.Add((name, value));
        }

        return this;
    }

    public EndpointBuilder Add(string name, int? value)
    {
        if (value is not null)
        {
            _parameters.Add((name, value.Value.ToString(CultureInfo.InvariantCulture)));
        }

        return this;
    }

    public EndpointBuilder Add(string name, DateTimeOffset? value)
    {
        if (value is not null)
        {
            _parameters.Add((name, value.Value.ToString("O", CultureInfo.InvariantCulture)));
        }

        return this;
    }

    public EndpointBuilder AddMany(string name, IReadOnlyList<string> values)
    {
        if (values.Count > 0)
        {
            _parameters.Add((name, string.Join(",", values)));
        }

        return this;
    }

    public override string ToString()
    {
        if (_parameters.Count == 0)
        {
            return _path;
        }

        var builder = new StringBuilder(_path);
        builder.Append('?');
        for (var i = 0; i < _parameters.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('&');
            }

            builder.Append(Uri.EscapeDataString(_parameters[i].Name));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(_parameters[i].Value));
        }

        return builder.ToString();
    }
}
