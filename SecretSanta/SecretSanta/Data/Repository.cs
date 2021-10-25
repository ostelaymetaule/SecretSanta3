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
        private LiteDB.ILiteDatabase _db;
        //todo: add lookup for cached data first?

        public Repository()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions()
            {

            });
            var dbPath = Path.Combine(Environment.CurrentDirectory, "rep", "myDb.db");
            _db = new LiteDB.LiteDatabase(dbPath);
        }
        /// <summary>
        /// Save that chat group to db 
        /// </summary>
        /// <param name="chatgroup"></param>
        public void Save(ChatGroup chatgroup)
        {
            var chatGroupCollection = _db.GetCollection<ChatGroup>(nameof(chatgroup));
            var exists = chatGroupCollection.Exists(x => x.ChatId == chatgroup.ChatId);
            if (!exists)
            {
                chatgroup.Status = Status.init; //todo: move to constructor?
            } 
            chatGroupCollection.Upsert(chatgroup);
            chatGroupCollection.EnsureIndex(x => x.ChatId);
        }
        /// <summary>
        /// Gets all saved chats
        /// </summary>
        /// <returns></returns>
        public List<ChatGroup> ReadAll()
        {
            var chatGroupCollection = _db.GetCollection<ChatGroup>("chatgroup");
            return chatGroupCollection.Query().Select(x => x).ToList();
        }
        /// <summary>
        /// Returns a single chatGroup by Chatid
        /// </summary>
        /// <param name="chatId">telegram intern chatId</param>
        /// <returns>First appearence of the chatGroup object with a given chatId</returns>
        public ChatGroup GetById(long chatId)
        {
            var chatGroupCollection = _db.GetCollection<ChatGroup>("chatgroup");
            return chatGroupCollection.Query().Where(x => x.ChatId == chatId).Select(x => x).FirstOrDefault();
        }


    }
}
