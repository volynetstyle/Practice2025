using MongoDB.Driver;

namespace practice.db
{
    public class MongoDbService
    {
        private readonly IMongoDatabase _mongoDatabase;
        private readonly IMongoCollection<WordCard> _wordCardCollection;
        private readonly IMongoCollection<WordCard> _thesaurusCollection;

        public MongoDbService(IMongoDatabase mongoDatabase)
        {
            _mongoDatabase = mongoDatabase;
            _wordCardCollection = _mongoDatabase.GetCollection<WordCard>("Dictionary");
            _thesaurusCollection = _mongoDatabase.GetCollection<WordCard>("Thesaurus");
        }


        public async Task<List<WordCard>> GetWordCardsAsync(FilterDefinition<WordCard> filter)
        {
            return await _wordCardCollection.Find(filter).ToListAsync();
        }

        public async Task<WordCard> GetWordCardByHeadwordAsync(string headword)
        {
            return await _wordCardCollection.Find(w => w.Headword == headword).FirstOrDefaultAsync();
        }

        public async Task InsertWordCardAsync(WordCard wordCard)
        {
            await _wordCardCollection.InsertOneAsync(wordCard);
            await _thesaurusCollection.InsertOneAsync(wordCard);
        }

        public async Task UpdateWordCardAsync(string headword, UpdateDefinition<WordCard> update)
        {
            await _wordCardCollection.UpdateOneAsync(w => w.Headword == headword, update);
        }

        public async Task DeleteWordCardAsync(string headword)
        {
            await _wordCardCollection.DeleteOneAsync(w => w.Headword == headword);
            await _thesaurusCollection.DeleteOneAsync(w => w.Headword == headword);
        }
    }
}


