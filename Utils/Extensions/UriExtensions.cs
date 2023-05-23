using System;

namespace UrlShortenerApp.Utils.Extensions;

public static class UriExtensions
{
    public static string GetHost(this Uri uri)
    {
        var host = uri.Host;
        var parts = host.Split('.');

        return parts.Length switch
        {
            > 2 => parts[1],
            > 1 => parts[0],
            _ => host
        };
    }
    
    public static string GetFirstThreeLettersOfHost(this string host)
    {
        const byte RequiredLength = 3;
        var hostLength = host.Length;

        //Take first three letters from host name and convert these letters to lowercase,
        //and add 'a' characters to the end of the string if it is less than 3 characters long
        var firstThreeLetters = host[..Math.Min(hostLength, RequiredLength)]
                                .ToLower() +
                            new string('a', Math.Max(RequiredLength - hostLength, 0));
        
        return firstThreeLetters;
    }
}