// Основная модель карточки слова
public class WordCard
{
    // Основная информация о слове
    public string? Headword { get; set; }                     // Само слово (например, "precipitate")    
    public Dictionary<string, WordCardVariant>? RegionalVariants { get; set; }
}

public class WordCardVariant
{
    public string? VariantName { get; set; }  // "American", "British", "Scientific", "Cultural" и т.д.
    public List<Pronunciation>? Pronunciations { get; set; }    // Фонетическая транскрипция и аудио
    public string? PartOfSpeechSummary { get; set; }          // Краткое описание всех частей речи (опционально)

    public List<PartOfSpeechEntry>? PartsOfSpeechSections { get; set; }

    public List<AdditionalSection>? AdditionalSections { get; set; }
    
    public InflectionInfo? Inflection { get; set; }
    
    public Etymology? Etymology { get; set; }
    
    public Metadata? Metadata { get; set; }
}

// Модель произношения
public class Pronunciation
{
    public string? Phonetic { get; set; }        // Транскрипция, например, /ˈpresɪpɪtət/
    public string? Ipa {get; set;}
    public string? AudioUrl { get; set; }          // URL аудиофайла
}

// Модель части речи с перечнем определений
public class PartOfSpeechEntry
{
    public string? PartOfSpeech { get; set; }      // "noun", "verb", "adjective" и т.д.
    // Иногда одна и та же часть речи может представляться в нескольких вариантах (разные значения или примеры)
    public List<Definition>? Definitions { get; set; }
    
    // Дополнительные примечания, специфичные для данной части речи (например, вариативность употребления)
    public string? Notes { get; set; }
}

// Модель определения
public class Definition
{
    public string? DefinitionText { get; set; }   // Само определение
    public List<string>? Examples { get; set; }     // Примеры употребления
    public string? UsageNote { get; set; }          // Примечание об употреблении (если имеется)
    public List<string>? Synonyms { get; set; }     // Синонимы (если присутствуют)
    public List<string>? Antonyms { get; set; }     // Антонимы (если присутствуют)
    // Возможность вложенных уточнений или дополнительных комментариев
    public List<AdditionalSection>? SubSections { get; set; }
}

// Универсальная модель для дополнительных секций
public class AdditionalSection
{
    public string? Title { get; set; }            // Название секции (например, "Idioms and Phrases", "Usage Note", "Examples")
    public string? Content { get; set; }          // Содержимое секции. Если секция структурирована, возможно определение более сложной модели.
    
    // Если содержимое секции имеет более сложную структуру, можно добавить вложенную коллекцию объектов.
    public List<AdditionalSection>? NestedSections { get; set; }
}

// Модель информации об инфлексиях
public class InflectionInfo
{
    public string? PluralForm { get; set; }       // Например, "teeth" для "tooth"
    public string? Comparative { get; set; }      // Для прилагательных: "better", "worse" и т.д.
    public string? Superlative { get; set; }
    // Можно добавить дополнительные поля для нерегулярных форм
    public string? OtherInfo { get; set; }
}


// Подмодель для регионального или тематического варианта карточки

// Модель этимологии
public class Etymology
{
    public string? OriginText { get; set; }       // Происхождение слова, может включать ссылки или цитаты
}

// Модель дополнительных метаданных
public class Metadata
{
    public DateTime LastUpdated { get; set; }    // Дата последнего обновления карточки
    public List<string>? Sources { get; set; }      // Ссылки на источники, справочные материалы и т.д.
}
