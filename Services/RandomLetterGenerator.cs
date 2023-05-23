using System;
using System.Text;
using UrlShortenerApp.Services.Abstractions;

namespace UrlShortenerApp.Services;

public class RandomLetterGenerator : ILetterGenerator
{
    private const int MinimumLetter = 'A';
    private const int MaximumLetter = 'Z';
    private readonly Random random;

    public RandomLetterGenerator(Random random)
    {
        this.random = random;
    }
    
    public string GenerateLetters(ushort length)
    {
        var resultBuilder = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            var letter = (char) random.Next(minValue: MinimumLetter, maxValue: MaximumLetter);
            resultBuilder.Append(letter);
        }

        return resultBuilder.ToString();
    }
}