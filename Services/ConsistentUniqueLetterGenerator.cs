using System;
using System.Text;
using UrlShortenerApp.Services.Abstractions;

namespace UrlShortenerApp.Services;

public class ConsistentUniqueLetterGenerator : ILetterGenerator
{
    private readonly char[] availableSymbols;

    public ConsistentUniqueLetterGenerator(char[] availableSymbols)
    {
        this.availableSymbols = availableSymbols;
    }

    public string GenerateLetters(ushort length, int seed = 1)
    {
        if (length < 1)
        {
            throw new ArgumentException(message: "Length of code should be at least 1.", paramName: nameof(length));
        }

        if (seed < 1)
        {
            throw new ArgumentException("Seed can not be less than 1");
        }

        --seed;

        const string ExceedsRangeOfValuesErrorMessage =
            "The seed exceeds the range of values for the given code length. Pass a smaller seed or increase the length of the code.";

        if (seed >= Math.Pow(availableSymbols.Length, length))
            throw new InvalidOperationException(ExceedsRangeOfValuesErrorMessage);

        if (length < 2)
        {
            if (seed >= availableSymbols.Length)
                throw new InvalidOperationException(ExceedsRangeOfValuesErrorMessage);

            return availableSymbols[seed]
                .ToString();
        }

        StringBuilder resultBuilder = new(capacity: length, maxCapacity: length);

        for (var i = 0; i < length; i++)
        {
            var remainder = seed % availableSymbols.Length;
            resultBuilder.Insert(0, availableSymbols[remainder]);
            seed /= availableSymbols.Length;
        }

        return resultBuilder.ToString();
    }
}