using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace DocumentDbRepositories.DocumentDb
{
    public class DocumentDbStorage<T> : IDocumentDbStorage<T> where T : DocumentEntity
    {
        private readonly IDocumentClient _client;
        private Database _db;
        private readonly Uri _collection;
        private readonly string _collectionId;
        private readonly string _databaseId;
        private bool _inited;

        private readonly SemaphoreSlim _sync = new SemaphoreSlim(1);

        public DocumentDbStorage(IDocumentClient client, string collection, string dbName = "maindb")
        {
            _client = client;
            _collectionId = collection;
            _databaseId = dbName;
            _collection = UriFactory.CreateDocumentCollectionUri(_databaseId, collection);
        }

        private async Task EnsureCollectionExists()
        {
            if (_inited)
                return;
            try
            {
                await _sync.WaitAsync();
                if (_inited)
                    return;
                _db = _client.CreateDatabaseQuery().Where(o => o.Id == _databaseId).AsEnumerable().FirstOrDefault() ??
                          await _client.CreateDatabaseAsync(new Database() { Id = _databaseId });
                if (!_client.CreateDocumentCollectionQuery(_db.SelfLink).Where(o => o.Id == _collectionId).AsEnumerable().Any())
                    await _client.CreateDocumentCollectionAsync(_db.SelfLink, new DocumentCollection() { Id = _collectionId });
                _inited = true;
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task InsertAsync(T item)
        {
            await EnsureCollectionExists();
            await _client.CreateDocumentAsync(_collection, item);
        }

        public async Task DeleteAsync(string id)
        {
            await EnsureCollectionExists();
            try
            {
                await _client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, id));
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                    return;
                throw;
            }
        }

        public async Task<T> GetDataAsync(string id)
        {
            await EnsureCollectionExists();
            try
            {
                var doc = await _client.ReadDocumentAsync(UriFactory.CreateDocumentUri(_db.Id, _collectionId, id));
                return (T)(dynamic)doc.Resource;
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                    return null;
                throw;
            }
        }

        public async Task<Document> GetDocumentAsync(string id)
        {
            await EnsureCollectionExists();
            try
            {
                var doc = await _client.ReadDocumentAsync(UriFactory.CreateDocumentUri(_db.Id, _collectionId, id));
                return doc;
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                    return null;
                throw;
            }
        }

        public async Task<T> ReplaceAsync(string id, Func<T, T> replaceAction)
        {
            await EnsureCollectionExists();
            while (true)
            {
                var doc = await GetDocumentAsync(id);
                if (doc == null)
                    return null;
                var typedObj = (T)(dynamic)doc;
                var updated = replaceAction(typedObj);
                if (updated == null)
                    return null;
                var ac = new AccessCondition { Condition = doc.ETag, Type = AccessConditionType.IfMatch };
                try
                {
                    await _client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, id), updated, new RequestOptions
                    {
                        AccessCondition = ac
                    });
                    return updated;
                }
                catch (DocumentClientException ex)
                {
                    if (ex.StatusCode != HttpStatusCode.PreconditionFailed)
                        throw;
                }
            }
        }

        public async Task<IEnumerable<T>> GetDataAsync(Expression<Func<T, bool>> filter)
        {
            await EnsureCollectionExists();
            var result = new List<T>();
            using (var queryable = _client.CreateDocumentQuery<T>(_collection).Where(filter).AsDocumentQuery())
            {
                while (queryable.HasMoreResults)
                {
                    result.AddRange(await queryable.ExecuteNextAsync<T>());
                }
            }
            return result;
        }
    }
}
