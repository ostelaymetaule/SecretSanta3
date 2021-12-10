using LiteDB;
using Microsoft.Extensions.Caching.Memory;
using SecretSanta.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecretSanta.Data
{
    public class Repository
    {
        private Microsoft.Extensions.Caching.Memory.IMemoryCache _memoryCache;
        //private LiteDB.ILiteDatabase _db;
        private string _dbFilePath;
        //todo: add lookup for cached data first?

        public Repository()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions()
            {

            });
            var dbPath = Path.Combine(Environment.CurrentDirectory, "rep");
            if (!Directory.Exists(dbPath))
            {
                Directory.CreateDirectory(dbPath);
            }
            _dbFilePath = Path.Combine(dbPath, "myDb.db");
        }
        /// <summary>
        /// Save that chat group to db 
        /// </summary>
        /// <param name="chatgroup"></param>
        public void Save(ChatGroup chatgroup)
        {
            using (var db = new LiteDatabase(_dbFilePath))
            {
                var chatGroupCollection = db.GetCollection<ChatGroup>(nameof(chatgroup));
                var exists = chatGroupCollection.Exists(x => x.ChatId == chatgroup.ChatId);
                if (!exists)
                {
                    chatgroup.Status = Status.init; //todo: move to constructor?
                    chatGroupCollection.Upsert(chatgroup);

                }
                else
                {
                    var updated = chatGroupCollection.Update(chatgroup);
                }
                chatGroupCollection.EnsureIndex(x => x.GroupName);
            }
        }
        /// <summary>
        /// Gets all saved chats
        /// </summary>
        /// <returns></returns>
        public List<ChatGroup> ReadAll()
        {
            using (var db = new LiteDatabase(_dbFilePath))
            {
                
                var chatGroupCollection = db.GetCollection<ChatGroup>("chatgroup");
                //chatGroupCollection.DeleteAll();
                return chatGroupCollection.Query().Select(x => x).ToList();
            }
        }
        /// <summary>
        /// Returns a single chatGroup by Chatid
        /// </summary>
        /// <param name="chatId">telegram intern chatId</param>
        /// <returns>First appearence of the chatGroup object with a given chatId</returns>
        public ChatGroup GetById(long chatId)
        {
            using (var db = new LiteDatabase(_dbFilePath))
            {
                var chatGroupCollection = db.GetCollection<ChatGroup>("chatgroup");
                return chatGroupCollection.Query().Where(x => x.ChatId == chatId).Select(x => x).FirstOrDefault();
            }
        }


    }
}
