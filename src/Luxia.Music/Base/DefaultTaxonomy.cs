namespace Luxia.Music.Base;

/// <summary>Taxonomie livrée avec l'application : les 14 familles du doc 21 §3.4 et leurs étiquettes de genre usuelles (MUS-023).</summary>
public static class DefaultTaxonomy
{
    private static MusicFamily F(string id, string name, string[] labels) => new() { Id = id, Name = name, Labels = labels };

    /// <summary>La taxonomie par défaut.</summary>
    public static Taxonomy Value { get; } = new()
    {
        Families =
        [
            F("electro", "Électro / Dance", ["electro", "electronic", "edm", "dance", "eurodance", "techno", "trance", "hardstyle", "big room", "dance pop", "electropop", "club", "hands up", "drum and bass", "dubstep"]),
            F("house", "House / Disco moderne", ["house", "deep house", "french house", "nu disco", "tech house", "progressive house", "tropical house", "future house", "disco house"]),
            F("hiphop", "Hip-hop / R'n'B", ["hip hop", "hiphop", "rap", "trap", "rnb", "r n b", "rhythm and blues", "urban", "rap francais", "drill", "grime"]),
            F("pop", "Pop", ["pop", "pop music", "teen pop", "synth pop moderne", "indie pop", "pop rock moderne"]),
            F("rock", "Rock", ["rock", "pop rock", "hard rock", "alternative", "alternative rock", "indie rock", "metal", "heavy metal", "punk", "grunge", "classic rock", "rock alternatif"]),
            F("80s", "Années 80", ["80s", "annees 80", "synthpop", "synth pop", "new wave", "italo disco", "new romantic", "eighties"]),
            F("disco", "Disco / Funk / Soul", ["disco", "funk", "soul", "motown", "70s", "annees 70", "northern soul", "boogie"]),
            F("latino", "Latino", ["latin", "latino", "salsa", "reggaeton", "bachata", "kizomba", "zouk", "merengue", "cumbia", "latin pop", "dancehall latino", "afro latino"]),
            F("variete", "Variété française", ["variete", "variete francaise", "chanson", "chanson francaise", "french pop", "chanson a texte", "french"]),
            F("reggae", "Reggae / Dancehall", ["reggae", "dancehall", "ragga", "ska", "dub", "roots reggae"]),
            F("rocknroll", "Rock'n'roll / Rétro", ["rock n roll", "rockn roll", "rock and roll", "rockabilly", "twist", "50s", "60s", "annees 50", "annees 60", "doo wop", "surf", "yeye", "retro"]),
            F("bal", "Bal / Traditionnel", ["bal", "musette", "valse", "madison", "danse en ligne", "country", "accordeon", "traditionnel", "folk", "pasodoble", "tango", "java"]),
            F("festif", "Festif / Tubes de soirée", ["festif", "tube", "tubes de soiree", "chanson a boire", "karaoke", "party", "fete", "mariage", "danse collective"]),
            F("slow", "Slow / Ballade", ["slow", "ballade", "ballad", "love song", "romantique", "easy listening", "soft rock", "adult contemporary", "acoustique"]),
        ],
    };
}
