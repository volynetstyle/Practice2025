using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace practice.entities { 
    public class ThesaurusEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public required string Id { get; set; }

        [BsonElement("word")]
        public required string Word { get; set; }

        [BsonElement("partOfSpeech")]
        public string? PartOfSpeech { get; set; }

        [BsonElement("senses")]
        public List<Sense>? Senses { get; set; }

        [BsonElement("metadata")]
        public Metadata? Metadata { get; set; }
    }

    public class Sense
    {
        [BsonElement("definition")]
        public string? Definition { get; set; }

        [BsonElement("synonyms")]
        public List<string>? Synonyms { get; set; }

        [BsonElement("antonyms")]
        public List<string>? Antonyms { get; set; }
    }
}