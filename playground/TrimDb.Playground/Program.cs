using System.Text;
using System.Threading.Tasks;
using TrimDB.Core;

namespace TrimDb.Playground
{
    class Program
    {
        static async Task Main()
        {
            var options = new TrimDatabaseOptions() { DatabaseFolder = "c:\\trimdb-testing" };
            await using var db = new TrimDatabase(options);

            await db.LoadAsync();

            var key = "test-key";
            var value = "a-value";

            var keyBytes = Encoding.UTF8.GetBytes(key);
            var valueBytes = Encoding.UTF8.GetBytes(value);

            await db.PutAsync(keyBytes, valueBytes);

            var storedValue = await db.GetAsync(keyBytes);

            var final = Encoding.UTF8.GetString(storedValue.Span);
        }
    }
}
