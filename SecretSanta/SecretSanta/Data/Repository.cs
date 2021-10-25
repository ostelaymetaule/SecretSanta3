using Microsoft.Extensions.Caching.Memory;
using SecretSanta.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecretSanta.Data
{
    internal class Repository
    {
        private Microsoft.Extensions.Caching.Memory.IMemoryCache _memoryCache;

        public Repository()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions()
            {
                
            });
        }
        public void Save(ChatGroup chatgroup)
        {

        }
        public List<ChatGroup> ReadAll()
        {
            return new List<ChatGroup>();
        }


    }
}
