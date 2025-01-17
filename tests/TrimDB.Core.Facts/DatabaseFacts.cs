﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrimDB.Core.InMemory.SkipList32;
using TrimDB.Core.Storage.Blocks.MemoryMappedCache;
using Xunit;

namespace TrimDB.Core.Facts
{
    public class DatabaseFacts
    {
        [Fact]
        public async Task TestSkipListOverflow()
        {
            var loadedWords = CommonData.Words;
            var folder = "D:\\Database";
            foreach (var f in System.IO.Directory.GetFiles(folder))
            {
                System.IO.File.Delete(f);
            }

            using var blocks = new MMapBlockCache();
            var dbOptions = new TrimDatabaseOptions() { DatabaseFolder = folder };
            var db = new TrimDatabase(dbOptions);

            await db.LoadAsync();

            foreach (var word in loadedWords)
            {
                var utf8 = Encoding.UTF8.GetBytes(word);
                var value = Encoding.UTF8.GetBytes($"VALUE={word}");
                await db.PutAsync(utf8, value);
            }

            var key = Encoding.UTF8.GetBytes(loadedWords[0]);
            var expectedValue = Encoding.UTF8.GetBytes($"VALUE={loadedWords[0]}");

            var result = await db.GetAsync(key);

            Assert.Equal(expectedValue.ToArray(), result.ToArray());

            key = Encoding.UTF8.GetBytes(loadedWords[loadedWords.Length / 2]);
            expectedValue = Encoding.UTF8.GetBytes($"VALUE={loadedWords[loadedWords.Length / 2]}");
            result = await db.GetAsync(key);

            Assert.Equal(expectedValue.ToArray(), result.ToArray());

            key = Encoding.UTF8.GetBytes(loadedWords[^1]);
            expectedValue = Encoding.UTF8.GetBytes($"VALUE={loadedWords[^1]}");
            result = await db.GetAsync(key);

            Assert.Equal(expectedValue.ToArray(), result.ToArray());


            //foreach (var word in loadedWords)
            //{
            //    var utf8 = Encoding.UTF8.GetBytes(word);
            //    var value = Encoding.UTF8.GetBytes($"VALUE={word}");
            //    await db.PutAsync(utf8, value);
            //}

            //await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }
}
