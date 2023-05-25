using System;
using System.Text;
using UrlShortenerApp.Services.Abstractions;

namespace UrlShortenerApp.Services;

public class RandomLetterGenerator : ILetterGenerator
{
    private const int MinimumLetter = 'A';
    private const int MaximumLetter = 'Z';
    private readonly Random random;
    private readonly ITextEntropier textEntropier;

    public RandomLetterGenerator(Random random, ITextEntropier textEntropier)
    {
        this.random = random;
        this.textEntropier = textEntropier;
    }
    
    public string GenerateLetters(ushort length, int seed = 1)
    {
        var resultBuilder = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            var letter = (char) random.Next(minValue: MinimumLetter, maxValue: MaximumLetter);
            resultBuilder.Append(letter);
        }
        
        return textEntropier.EntropyText(resultBuilder.ToString());
    }
    
}