﻿namespace NetCord.Rest;

public partial class RestClient
{
    [GenerateAlias([typeof(CurrentApplication)], CastType = typeof(Application), Modifiers = ["override"])]
    public async Task<CurrentApplication> GetCurrentApplicationAsync(RestRequestProperties? properties = null, CancellationToken cancellationToken = default)
        => new(await (await SendRequestAsync(HttpMethod.Get, $"/applications/@me", null, null, properties, cancellationToken: cancellationToken).ConfigureAwait(false)).ToObjectAsync(Serialization.Default.JsonApplication).ConfigureAwait(false), this);

    [GenerateAlias([typeof(CurrentApplication)])]
    public async Task<CurrentApplication> ModifyCurrentApplicationAsync(Action<CurrentApplicationOptions> action, RestRequestProperties? properties = null, CancellationToken cancellationToken = default)
    {
        CurrentApplicationOptions currentApplicationOptions = new();
        action(currentApplicationOptions);
        using (HttpContent content = new JsonContent<CurrentApplicationOptions>(currentApplicationOptions, Serialization.Default.CurrentApplicationOptions))
            return new(await (await SendRequestAsync(HttpMethod.Patch, content, $"/applications/@me", null, null, properties, cancellationToken: cancellationToken).ConfigureAwait(false)).ToObjectAsync(Serialization.Default.JsonApplication).ConfigureAwait(false), this);
    }
}
