using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocumentDbRepositories;
using DocumentDbRepositories.DocumentDb;
using Microsoft.Azure.Documents.Client;
using NUnit.Framework;

namespace Bitcoin.Tests
{
    [TestFixture]
    public class DocumentDbTests
    {
        private static DocumentDbStorage<Order> _storage;

        static DocumentDbTests()
        {
            _storage = new DocumentDbStorage<Order>(new DocumentClient(new Uri("https://lkebitcoin.documents.azure.com:443"),
             "TCLVOmIm5ZM1010fm78EK0Fnrzg72Ccgi3wq8bBwxG9lLY7hljsAhyac5z0v1OhDbVqLAmq9SoxnQfY4idvkZg=="), "test_collection");
        }

        [Test]
        public async Task TestInsertDoc()
        {

            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Order 1"
            };
            await _storage.InsertAsync(order);
            var order2 = await _storage.GetDataAsync(order.Id);
            Assert.AreEqual(order2.Id, order.Id);
        }

        [Test]
        public async Task TestGetNotExistsDocument()
        {
            var doc = await _storage.GetDataAsync(Guid.NewGuid().ToString());
            Assert.IsNull(doc);
        }

        [Test]
        public async Task TestDeleteDocument()
        {
            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Delete order"
            };
            await _storage.InsertAsync(order);
            var added = await _storage.GetDataAsync(order.Id);
            Assert.AreEqual(added.Id, order.Id);

            await _storage.DeleteAsync(order.Id);

            var deleted = await _storage.GetDataAsync(order.Id);
            Assert.IsNull(deleted);

            await _storage.DeleteAsync(order.Id);
        }

        [Test]
        public async Task TestReplaceDocument()
        {
            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                Name = "order 1",
                Version = 1
            };
            await _storage.InsertAsync(order);

            await Task.WhenAll(Enumerable.Range(1, 9).Select(o =>
            {
                return Task.Run(async () =>
                {
                    await _storage.ReplaceAsync(order.Id, order1 =>
                    {
                        order1.Version++;
                        return order1;
                    });
                });
            }));

            var updated = await _storage.GetDataAsync(order.Id);
            Assert.AreEqual(10, updated.Version);
        }

        private class Order : DocumentEntity
        {
            public string Name { get; set; }

            public DateTime CreateDt { get; set; }

            public int Version { get; set; }

            public Order()
            {
                CreateDt = DateTime.UtcNow;
            }
        }
    }
}
