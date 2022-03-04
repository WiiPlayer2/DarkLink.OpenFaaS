using System;

namespace DarkLink.OpenFaaS;

internal class RemoteException : Exception
{
    public HttpResponseMessage Response { get; }

    public RemoteException(HttpResponseMessage response, string message)
        : base($"{response.ReasonPhrase} ({(int)response.StatusCode}) | {message}")
    {
        Response = response;
    }
}
