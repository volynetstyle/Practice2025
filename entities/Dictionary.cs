public class WordCard
{
   
    public string? Headword { get; set; }                     
    public Dictionary<string, WordCardVariant>? RegionalVariants { get; set; }
}

public class WordCardVariant
{
    public string? VariantName { get; set; }  
        public List<Pronunciation>? Pronunciations { get; set; }   
    public string? PartOfSpeechSummary { get; set; }          

    public List<PartOfSpeechEntry>? PartsOfSpeechSections { get; set; }

    public List<AdditionalSection>? AdditionalSections { get; set; }
    
    public InflectionInfo? Inflection { get; set; }
    
    public Etymology? Etymology { get; set; }
    
    public Metadata? Metadata { get; set; }
}

public class Pronunciation
{
    public string? Phonetic { get; set; }        
    public string? Ipa {get; set;}
    public string? AudioUrl { get; set; }         
}

public class PartOfSpeechEntry
{
    public string? PartOfSpeech { get; set; }     
    public List<Definition>? Definitions { get; set; }
    
    public string? Notes { get; set; }
}

// Модель определения
public class Definition
{
    public string? DefinitionText { get; set; }  
    public List<string>? Examples { get; set; }     
    public string? UsageNote { get; set; }         
    public List<string>? Synonyms { get; set; }    
        public List<string>? Antonyms { get; set; }     
    public List<AdditionalSection>? SubSections { get; set; }
}

public class AdditionalSection
{
    public string? Title { get; set; }            
        public string? Content { get; set; }        
    public List<AdditionalSection>? NestedSections { get; set; }
}

public class InflectionInfo
{
    public string? PluralForm { get; set; }       
    public string? Comparative { get; set; }     
    public string? Superlative { get; set; }
    public string? OtherInfo { get; set; }
}



public class Etymology
{
    public string? OriginText { get; set; }       
}

public class Metadata
{
    public DateTime LastUpdated { get; set; }    
    public List<string>? Sources { get; set; }     
}
