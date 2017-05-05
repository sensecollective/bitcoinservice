using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;

namespace DocumentDbRepositories.DocumentDb
{
    public interface IDocumentDbStorage<T> where T : DocumentEntity
    {
        Task InsertAsync(T item);
        Task DeleteAsync(string id);
        Task<T> GetDataAsync(string id);
        Task<Document> GetDocumentAsync(string id);
        Task<T> ReplaceAsync(string id, Func<T, T> replaceAction);
    }
}
