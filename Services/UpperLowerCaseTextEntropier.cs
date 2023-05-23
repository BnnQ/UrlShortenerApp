using System;
using System.Linq;
using System.Text;
using UrlShortenerApp.Services.Abstractions;

namespace UrlShortenerApp.Services;

public class UpperLowerCaseTextEntropier : ITextEntropier
{
    private readonly Random random;

    public UpperLowerCaseTextEntropier(Random random)
    {
        this.random = random;
    }
    
    public string EntropyText(string text)
    {
        StringBuilder entropiedTextBuilder = new();
        text = text.ToLower();

        foreach (var letter in text.Where(char.IsLetter))
        {
            entropiedTextBuilder.Append(random.Next(2) == 1 ? char.ToUpper(letter) : letter);
        }

        return entropiedTextBuilder.ToString();
    }
}