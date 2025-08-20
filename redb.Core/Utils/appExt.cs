using Microsoft.EntityFrameworkCore;
using System.Collections;


namespace redb.Core.Utils
{
    public static class appExt
    {
        public static void forEach<T>(this IEnumerable d, Action<T> a)
        {
            //d.AsParallel().ForAll(i => a((T)i));
            foreach (object? i in d) { a((T)i); }
        }

#pragma warning disable CS8600, CS8602, CS8603, CS8604
        //public static void forEach<K, V>(this IDictionary d, Action<K, V?> a) =>
        //    d.forEach<object>(i => { Func<string, object> getV = (name) => i.GetType().GetProperty(name).GetValue(i); a((K)getV("Key"), (V?)getV("Value")); });

        public static void forEach<K, V>(this IDictionary source, Action<K, V?> action) => 
            source.forEach<DictionaryEntry>(i=>action((K)i.Key,(V?)i.Value));
        public static IQueryable<T> Filter<T>(this IQueryable<T> query, string propertyName, object propertyValue)
        => query.Where(e => Equals(EF.Property<object>(e, propertyName), propertyValue));
#pragma warning restore CS8600, CS8602, CS8603 , CS8604
    }
}
